using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace HotLoad
{
    internal static class Hooks
    {
        internal static void Install()
        {
            var h = new Harmony("hotload.poc");
            h.Patch(AccessTools.Method(typeof(ModLoader), "Awake"),
                postfix: new HarmonyMethod(typeof(Hooks), nameof(AwakePost)));
            h.Patch(AccessTools.Method(typeof(ModLoader), nameof(ModLoader.SetModEnabled)),
                postfix: new HarmonyMethod(typeof(Hooks), nameof(SetModEnabledPost)));
            h.Patch(AccessTools.Method(typeof(ModLoader), "ReorderMods"),
                postfix: new HarmonyMethod(typeof(Hooks), nameof(ReorderModsPost)));
            Log.W($"hooks installed: {h.GetPatchedMethods().Count()} method(s)");
        }

        static void AwakePost(ModLoader __instance)
        {
            if (ModLoader.Instance != __instance) return;
            var go = new GameObject("HotLoadRunner");
            UnityEngine.Object.DontDestroyOnLoad(go);
            go.AddComponent<Runner>();
            Boot.UnityReady = true;
            if (Boot.Mode == Mode.Active)
                ModWorkshop.OnItemInstalled += wid => Runner.I?.EnqueueInstall(wid);
            Log.W("startup load finished; snapshot:");
            foreach (var m in ModLoader.Mods)
                Log.W($"  {m.Manifest?.ID} v{m.Manifest?.ModVersion} running={m.Instance != null} trust={m.TrustState} fp={m.Fingerprint?.Combined ?? "-"}");
        }

        static void SetModEnabledPost(string id, bool enabled)
        {
            Log.W($"SetModEnabled('{id}', {enabled})");
            if (Boot.Mode != Mode.Active || Runner.I == null) return;
            if (enabled)
            {
                Pending.Set(id, "enable");
                Runner.I.Enqueue(id);
                return;
            }
            // Disabling a mod that is running still needs a restart; cancelling an enable that never loaded does not.
            if (ModLoader.Mods.Any(m => m.Manifest?.ID == id && m.Instance != null)) Pending.Set(id, "disable");
            else Pending.Clear(id);
        }

        static void ReorderModsPost()
        {
            if (Boot.Mode == Mode.Active) Pending.Set("*order", "load order changed");
        }
    }

    // Why a restart is still needed. The native RestartRequired flag is global, so it is only cleared
    // when every change of this session has been applied without one.
    internal static class Pending
    {
        static readonly Dictionary<string, string> Reasons = new Dictionary<string, string>();
        static readonly MethodInfo SetRestart = AccessTools.PropertySetter(typeof(ModLoader), nameof(ModLoader.RestartRequired));
        static readonly MethodInfo RefreshNotes = AccessTools.Method(typeof(ModUIManager), "RefreshNotes");

        internal static void Set(string key, string why) => Reasons[key] = why;

        internal static void Clear(string key)
        {
            Reasons.Remove(key);
            Apply();
        }

        internal static void Apply()
        {
            if (Reasons.Count > 0)
            {
                Log.W("restart still required: " + string.Join(", ", Reasons.Select(r => $"{r.Key}={r.Value}")));
                return;
            }
            if (!ModLoader.RestartRequired) return;
            SetRestart.Invoke(null, new object[] { false });
            if (ModUIManager.Instance != null) RefreshNotes.Invoke(ModUIManager.Instance, null);
            Log.W("all changes applied without restart; restart note cleared");
        }
    }

    // Requests are queued from the UI callback and handled one per frame on the main thread.
    internal class Runner : MonoBehaviour
    {
        internal static Runner I;
        readonly Queue<string> _queue = new Queue<string>();
        readonly HashSet<string> _attempted = new HashSet<string>();

        static readonly MethodInfo LoadModFromFolder = AccessTools.Method(typeof(ModLoader), "LoadModFromFolder");
        static readonly MethodInfo IsModEnabled = AccessTools.Method(typeof(ModLoader), "IsModEnabled");
        static readonly MethodInfo LoadTextures = AccessTools.Method(typeof(ModLoader), "LoadTextures");
        static readonly MethodInfo LoadLocalization = AccessTools.Method(typeof(ModLoader), "LoadLocalization");
        static readonly MethodInfo LoadSounds = AccessTools.Method(typeof(ModLoader), "LoadSoundsCoroutine");

        static readonly MethodInfo ParseManifest = AccessTools.Method(typeof(ModLoader), "ParseManifest");
        readonly Queue<ulong> _installs = new Queue<ulong>();

        void Awake() => I = this;

        internal void Enqueue(string id)
        {
            if (!_queue.Contains(id)) _queue.Enqueue(id);
        }

        internal void EnqueueInstall(ulong workshopId)
        {
            Log.W($"Workshop item {workshopId} installed");
            if (!_installs.Contains(workshopId)) _installs.Enqueue(workshopId);
        }

        void Update()
        {
            if (_installs.Count > 0)
            {
                var wid = _installs.Dequeue();
                try { Register(wid); }
                catch (Exception e) { Log.W($"workshop {wid}: unexpected error, left for restart: {e}"); Pending.Set("workshop:" + wid, "error"); }
                return;
            }
            if (_queue.Count == 0) return;
            var id = _queue.Dequeue();
            try { Process(id); }
            catch (Exception e) { Log.W($"'{id}': unexpected error, left for restart: {e}"); }
        }

        // S2: a Workshop item installed while the game runs gets registered (disabled) the same way
        // LoadWorkshopMods does at startup, so it shows up in the mod list and can be enabled (S1).
        void Register(ulong wid)
        {
            if (ModLoader.Mods.Any(m => m.WorkshopId == wid || m.ShadowedWorkshopId == wid))
            { Log.W($"workshop {wid}: already registered (update or repeated notice); an update still needs a restart"); Pending.Set("workshop:" + wid, "installed again or updated"); return; }

            var folder = ModWorkshop.GetInstalledItems().Where(i => i.Item1 == wid).Select(i => i.Item2).FirstOrDefault();
            if (folder == null) { Log.W($"workshop {wid}: not in the installed list yet; not registered"); Pending.Set("workshop:" + wid, "not registered"); return; }

            var manifest = (ModManifest)ParseManifest.Invoke(ParseManifest.IsStatic ? null : ModLoader.Instance, new object[] { folder });
            if (manifest == null) { Log.W($"workshop {wid}: manifest unreadable; not registered"); Pending.Set("workshop:" + wid, "not registered"); return; }
            var id = manifest.ID;
            if (Records(id).Count > 0) { Log.W($"workshop {wid}: ID '{id}' already registered from another folder; not registered"); Pending.Set("workshop:" + wid, "not registered"); return; }
            // An ID enabled from an earlier subscription would be loaded straight away by the native loader.
            if ((bool)IsModEnabled.Invoke(null, new object[] { id })) { Log.W($"workshop {wid}: '{id}' is already marked enabled; left for restart"); Pending.Set("workshop:" + wid, "not registered"); return; }

            LoadModFromFolder.Invoke(ModLoader.Instance, new object[] { folder, ModSource.Workshop, wid });
            var recs = Records(id);
            Log.W($"workshop {wid}: registered '{id}' v{manifest.ModVersion} from {folder}; records={recs.Count}, running={recs.Any(r => r.Instance != null)}");
            ModWorkshop.RequestDetails(new[] { wid });
            RebuildList();
            Pending.Apply();
        }

        static List<LoadedMod> Records(string id) => ModLoader.Mods.Where(m => m.Manifest?.ID == id).ToList();

        void Process(string id)
        {
            var recs = Records(id);
            if (recs.Count != 1) { Refuse(id, $"{recs.Count} record(s) with this ID"); return; }
            var p = recs[0];
            if (p.Instance != null || p.Assembly != null) { Log.W($"'{id}': already loaded, nothing to do"); Pending.Clear(id); return; }
            if (_attempted.Contains(id)) { Refuse(id, "already attempted in this session"); return; }
            if (!(bool)IsModEnabled.Invoke(null, new object[] { id })) { Refuse(id, "not enabled"); return; }
            if (p.TrustState != ModTrustState.Match) { Refuse(id, $"trust state {p.TrustState}"); return; }

            var fp = p.Fingerprint?.Combined;
            var ver = p.Manifest.ModVersion;
            Boot.ReloadAllow();
            if (Boot.Deny.Contains(id)) { Refuse(id, "denied in hotload.cfg"); return; }
            if (!Boot.Allow.Any(a => a[0] == id && a[1] == ver && a[2] == fp))
            {
                if (!Boot.Auto) { Refuse(id, $"not whitelisted (version {ver}, fingerprint {fp ?? "-"})"); return; }
                var why = Eligibility.Check(p);
                if (why != null) { Refuse(id, $"auto check: {why} (version {ver}, fingerprint {fp ?? "-"})"); return; }
                Log.W($"'{id}': not on the allow list, passed the auto check");
            }
            if (p.Manifest.Prerequisites != null && p.Manifest.Prerequisites.Count > 0) { Refuse(id, "has prerequisites"); return; }
            if (System.IO.Directory.Exists(Path.Combine(p.FolderPath, "LootTables"))) { Refuse(id, "ships loot tables"); return; }
            if (System.IO.Directory.Exists(PaperBookRegistry.BooksFolder(p))) { Refuse(id, "ships books"); return; }
            var folder = Path.GetFullPath(p.FolderPath);
            if (AppDomain.CurrentDomain.GetAssemblies().Any(a => FromFolder(a, folder))) { Refuse(id, "an assembly from its folder is already loaded"); return; }

            // Take the placeholder out so the native loader does not register a second record.
            var index = ModLoader.Mods.IndexOf(p);
            ModLoader.Mods.RemoveAt(index);
            _attempted.Add(id);
            Log.W($"'{id}': loading via native LoadModFromFolder (index {index}, fp {fp})");

            try { LoadModFromFolder.Invoke(ModLoader.Instance, new object[] { p.FolderPath, p.Source, p.WorkshopId }); }
            catch (TargetInvocationException e) { Log.W($"'{id}': LoadModFromFolder threw: {e.InnerException}"); }

            var added = Records(id);
            if (added.Count == 0)
            {
                ModLoader.Mods.Insert(index, p);
                Refuse(id, "native loader registered nothing; placeholder restored");
                RebuildList();
                return;
            }
            if (added.Count > 1) Log.W($"'{id}': WARNING {added.Count} records after load");
            var n = added[added.Count - 1];
            ModLoader.Mods.Remove(n);

            if (n.Assembly == null && n.Instance == null)
            {
                ModLoader.Mods.Insert(index, p);
                Refuse(id, $"native loader did not load it (trust {n.TrustState}, note {n.TrustNote ?? "-"}, error {n.LoadError ?? "-"}); placeholder restored");
                RebuildList();
                return;
            }

            ModLoader.Mods.Insert(Math.Min(index, ModLoader.Mods.Count), n);
            if (n.Instance == null || n.LoadError != null)
            {
                var leftover = Harmony.GetAllPatchedMethods().Count(m => Harmony.GetPatchInfo(m)?.Owners.Contains("mod." + id) == true);
                Log.W($"'{id}': PARTIAL LOAD FAILURE, mod code may have run; restart required. error: {n.LoadError ?? "-"}; instance={(n.Instance != null)}, assembly={(n.Assembly != null)}, harmony patches left={leftover}");
                RebuildList();
                return;
            }

            LoadTextures.Invoke(ModLoader.Instance, new object[] { n });
            LoadLocalization.Invoke(ModLoader.Instance, new object[] { n });
            // The sound coroutine fires ModHook.OnModAssetsLoaded itself when done.
            ModLoader.Instance.StartCoroutine((System.Collections.IEnumerator)LoadSounds.Invoke(ModLoader.Instance, new object[] { n }));
            Log.W($"'{id}': HOT LOADED v{n.Manifest.ModVersion} trust={n.TrustState} fp={n.Fingerprint?.Combined ?? "-"} records={Records(id).Count}");
            RebuildList();
            Pending.Clear(id);
        }

        static bool FromFolder(Assembly a, string folder)
        {
            try { return !a.IsDynamic && a.Location.Length > 0 && Path.GetFullPath(a.Location).StartsWith(folder, StringComparison.OrdinalIgnoreCase); }
            catch { return false; }
        }

        static void Refuse(string id, string why) => Log.W($"'{id}': not hot loaded ({why}); restart still required");

        static void RebuildList()
        {
            var ui = ModUIManager.Instance;
            if (ui == null) return;
            try { ui.PopulateLayoutList(); }
            catch (Exception e) { Log.W("mod list rebuild failed: " + e.Message); }
        }
    }
}

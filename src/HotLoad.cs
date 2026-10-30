// HotLoad PoC: load a Workshop/local mod right after the native enable flow approves it,
// instead of waiting for a restart. Only main-menu hot enable (S1a) for whitelisted mods.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Doorstop
{
    public static class Entrypoint
    {
        // Doorstop 4 calls this before Unity has finished starting; touch no game or Unity types here.
        public static void Start() => HotLoad.Boot.Start();
    }
}

namespace HotLoad
{
    internal enum Mode { Log, Hooks, Active }

    internal static class Boot
    {
        internal static string Dir;
        internal static Mode Mode = Mode.Log;
        internal static readonly List<string[]> Allow = new List<string[]>(); // id, version, fingerprint
        internal static readonly HashSet<string> Deny = new HashSet<string>();
        internal static bool Auto; // auto=on: mods that pass Eligibility.Check need no allow line
        internal static volatile bool UnityReady;
        static bool _installed;

        internal static void Start()
        {
            Dir = Path.GetDirectoryName(typeof(Boot).Assembly.Location);
            ReadConfig();
            Log.W($"injected, mode={Mode}, whitelist={Allow.Count}, dir={Dir}");
            AppDomain.CurrentDomain.AssemblyLoad += (s, e) => OnAssembly(e.LoadedAssembly);
            foreach (var a in AppDomain.CurrentDomain.GetAssemblies()) OnAssembly(a);
        }

        // The whitelist is re-read on every request so a fingerprint can be added without restarting;
        // the mode is fixed at startup.
        internal static void ReloadAllow()
        {
            var keep = Mode;
            ReadConfig();
            Mode = keep;
        }

        static void ReadConfig()
        {
            Allow.Clear();
            Deny.Clear();
            Auto = false;
            var cfg = Path.Combine(Dir, "hotload.cfg");
            if (!File.Exists(cfg)) return;
            foreach (var raw in File.ReadAllLines(cfg))
            {
                var line = raw.Trim();
                if (line.Length == 0 || line.StartsWith("#")) continue;
                var kv = line.Split(new[] { '=' }, 2);
                if (kv.Length != 2) continue;
                var key = kv[0].Trim().ToLowerInvariant();
                var val = kv[1].Trim();
                if (key == "mode")
                    Mode = val == "active" ? Mode.Active : val == "hooks" ? Mode.Hooks : Mode.Log;
                else if (key == "auto")
                    Auto = val == "on";
                else if (key == "deny")
                    Deny.Add(val);
                else if (key == "allow")
                {
                    var parts = val.Split('|').Select(p => p.Trim()).ToArray();
                    if (parts.Length == 3) Allow.Add(parts);
                }
            }
        }

        static void OnAssembly(Assembly a)
        {
            if (_installed || a.GetName().Name != "Assembly-CSharp") return;
            _installed = true;
            Log.W("Assembly-CSharp loaded");
            if (Mode == Mode.Log) return;
            try { Hooks.Install(); }
            catch (Exception e) { Log.W("hook install FAILED: " + e); }
        }
    }

    internal static class Log
    {
        static readonly object Gate = new object();

        internal static void W(string msg)
        {
            var line = "[HotLoad] " + msg;
            lock (Gate)
            {
                try { File.AppendAllText(Path.Combine(Boot.Dir, "hotload.log"), $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} {line}{Environment.NewLine}"); }
                catch { }
            }
            if (Boot.UnityReady) Unity(line);
        }

        // Separate method so UnityEngine is only touched once Unity is up.
        static void Unity(string line) => UnityEngine.Debug.Log(line);
    }
}

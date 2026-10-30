using System.IO;
using System.Linq;
using DseCecil;
using DseCecil.Cil;

namespace HotLoad
{
    // Decides whether a mod that is not on the allow list can be hot loaded. A mod is refused when it
    // ships content the game only reads at startup (items, loot, books) or subscribes to startup-only hooks.
    internal static class Eligibility
    {
        static readonly string[] StartupOnlyHooks = { "add_OnModItemDirectoryInit", "add_OnLootTablesLoaded" };

        internal static string Check(LoadedMod mod)
        {
            var textures = Path.Combine(mod.FolderPath, "Textures");
            if (System.IO.Directory.Exists(Path.Combine(textures, "Items"))) return "ships item sprites (new items)";
            if (System.IO.Directory.Exists(Path.Combine(textures, "Paper"))) return "ships paper sprites";

            foreach (var name in mod.Manifest.Assemblies ?? Enumerable.Empty<string>())
            {
                var path = Path.Combine(mod.FolderPath, name);
                if (!File.Exists(path)) return $"{name} missing";
                // Read from memory so the file is not held open before the native loader opens it.
                using (var module = ModuleDefinition.ReadModule(new MemoryStream(File.ReadAllBytes(path))))
                {
                    foreach (var type in module.GetTypes())
                    foreach (var method in type.Methods.Where(m => m.HasBody))
                    foreach (var ins in method.Body.Instructions)
                    {
                        if (ins.Operand is MethodReference r && r.DeclaringType.Name == "ModHook" && StartupOnlyHooks.Contains(r.Name))
                            return $"subscribes to startup-only hook {r.Name.Substring(4)}";
                    }
                }
            }
            return null;
        }
    }
}

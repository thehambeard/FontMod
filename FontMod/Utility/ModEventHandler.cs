using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityModManagerNet;

namespace FontMod.Utility;

public interface IModEventHandler
{
    int Priority { get; }

    void HandleModEnable();

    void HandleModDisable();
}

public class ModEventHandler
{
    private List<IModEventHandler> _eventHandlers;

    public bool Enabled { get; private set; }

    public bool Patched { get; private set; }

    public void Enable(UnityModManager.ModEntry modEntry, Assembly assembly)
    {
        if (Enabled)
        {
            Main.Logger.Debug("Already enabled.");
            return;
        }

        using (ProcessLogger process = new(Main.Logger))
        {
            try
            {
                process.Log("Enabling.");
                var dict = Harmony.VersionInfo(out var myVersion);
                process.Log($"Harmony version: {myVersion}");
                foreach (var entry in dict)
                {
                    process.Log($"Mod {entry.Key} loaded with Harmony version {entry.Value}");
                }

                process.Log("Loading settings.");

                Type[] types = assembly.GetTypes();

                if (!Patched)
                {
                    Harmony harmonyInstance = new(modEntry.Info.Id);
                    foreach (Type type in types)
                    {
                        List<HarmonyMethod> harmonyMethods = HarmonyMethodExtensions.GetFromType(type);
                        if (harmonyMethods != null && harmonyMethods.Count() > 0)
                        {
                            process.Log($"Patching: {type.FullName}");
                            try
                            {
                                PatchClassProcessor patchProcessor = harmonyInstance.CreateClassProcessor(type);
                                patchProcessor.Patch();
                            }
                            catch (Exception e)
                            {
                                Main.Logger.Error(e);
                            }
                        }
                    }
                    Patched = true;
                }

                Enabled = true;

                process.Log("Registering events.");
                _eventHandlers = types.Where(type => !type.IsInterface
                        && !type.IsAbstract
                        && typeof(IModEventHandler).IsAssignableFrom(type))
                    .Select(type => Activator.CreateInstance(type, true) as IModEventHandler).ToList();
                _eventHandlers.Sort((x, y) => x.Priority - y.Priority);

                process.Log("Raising events: OnEnable()");
                for (int i = 0; i < _eventHandlers.Count; i++)
                {
                    _eventHandlers[i].HandleModEnable();
                }
            }
            catch (Exception e)
            {
                Main.Logger.Error(e);
                Disable(modEntry, true);
                throw;
            }

            process.Log("Enabled.");
        }
    }

    public void Disable(UnityModManager.ModEntry modEntry, bool unpatch = false)
    {
        using (ProcessLogger process = new(Main.Logger))
        {
            process.Log("Disabling.");

            Enabled = false;

            // use try-catch to prevent the progression being disrupt by exceptions
            if (_eventHandlers != null)
            {
                process.Log("Raising events: OnDisable()");
                for (int i = _eventHandlers.Count - 1; i >= 0; i--)
                {
                    try { _eventHandlers[i].HandleModDisable(); }
                    catch (Exception e) { Main.Logger.Error(e); }
                }
                _eventHandlers = null;
            }

            if (unpatch)
            {
                Harmony harmonyInstance = new(modEntry.Info.Id);
                foreach (MethodBase method in harmonyInstance.GetPatchedMethods().ToList())
                {
                    var patchInfo = Harmony.GetPatchInfo(method);
                    IEnumerable<Patch> patches =
                        patchInfo.Transpilers.Concat(patchInfo.Postfixes).Concat(patchInfo.Prefixes)
                        .Where(patch => patch.owner == modEntry.Info.Id);
                    if (patches.Any())
                    {
                        process.Log($"Unpatching: {patches.First().PatchMethod.DeclaringType.FullName} from {method.DeclaringType.FullName}.{method.Name}");
                        foreach (Patch patch in patches)
                        {
                            try { harmonyInstance.Unpatch(method, patch.PatchMethod); }
                            catch (Exception e) { Main.Logger.Error(e); }
                        }
                    }
                }
                Patched = false;
            }
            process.Log("Disabled.");
        }
    }
}

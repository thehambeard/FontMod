using FontMod.UI_UMM;
using FontMod.Utility;
using System.Reflection;
using UnityModManagerNet;

namespace FontMod;

#if DEBUG
[EnableReloading]
#endif

public static class Main
{
    internal static UnityModManager.ModEntry ModEntry { get; private set; }
    internal static Utility.Logger Logger { get; private set; }
    internal static ModEventHandler ModEventHandler { get; private set; }

    public static bool Load(UnityModManager.ModEntry modEntry)
    {
        ModEntry = modEntry;
        Logger = new(modEntry.Logger);
        ModEventHandler = new();
        modEntry.OnToggle = OnToggle;
#if DEBUG
        modEntry.OnUnload = OnUnload;
        modEntry.OnGUI = UMMMenu.OnGUIDebug;
#else
        modEntry.OnGUI = UMMMenu.OnGUI;
#endif
        return true;
    }

    public static void DisableMod() => ModEventHandler.Disable(ModEntry, true);

    private static bool OnToggle(UnityModManager.ModEntry modEntry, bool value)
    {
        if (value)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            ModEventHandler.Enable(modEntry, assembly);
        }
        else
        {
            ModEventHandler.Disable(modEntry);
        }

        return true;
    }

#if DEBUG
    public static bool OnUnload(UnityModManager.ModEntry modEntry)
    {
        return true;
    }
#endif
}

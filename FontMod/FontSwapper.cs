using UnityEngine;
using HarmonyLib;
using TMPro;
using Kingmaker;
using System.IO;
using System.Linq;
using Kingmaker.Code.UI.MVVM.View.MainMenu.PC;
using Kingmaker.Code.UI.MVVM.View.MainMenu.Common;
using Kingmaker.Code.UI.MVVM.View.ContextMenu.Common;
using Kingmaker.Blueprints;
using Kingmaker.ResourceLinks.BaseInterfaces;
using Kingmaker.Modding;
using Owlcat.Runtime.UI.MVVM;

namespace FontMod;

[HarmonyPatch]
public static class FontSwapperInit
{
    static TMP_FontAsset TMP_FontAsset;

    [HarmonyPatch(typeof(GameStarter), nameof(GameStarter.FixTMPAssets))]
    [HarmonyPostfix]
    static void LoadBundle()
    {
        var fontBundle = AssetBundle
            .LoadFromFile(Path.Combine(Main.ModEntry.Path, "fontasset"));

        TMP_FontAsset = fontBundle.LoadAsset<TMP_FontAsset>(fontBundle.GetAllAssetNames().First());

        if (TMP_FontAsset == null)
            Main.Log.Error("Unable to load TMP_FontAsset.");
    }


    [HarmonyPatch(typeof(OwlcatModificationsManager), nameof(OwlcatModificationsManager.OnResourceLoaded))]
    [HarmonyPostfix]
    static void StoreResourcePatch(object resource, string guid)
    {
        if (TMP_FontAsset != null && resource is GameObject gameObject)
            SetTexts(gameObject);
    }

    [HarmonyPatch(typeof(ViewBase<IViewModel>), "Bind")]
    [HarmonyPostfix]
    static void BindPatch(ViewBase<IViewModel> __instance, IViewModel viewModel) => SetTexts(__instance.gameObject);

    private static void SetTexts(GameObject gameObject)
    {
        var texts = gameObject.GetComponentsInChildren<TextMeshProUGUI>();

        for (int i = 0; i < texts.Length; i++)
        {
            texts[i].m_fontAsset = TMP_FontAsset;
            texts[i].UpdateFontAsset();
        }
    }
}
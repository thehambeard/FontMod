using HarmonyLib;
using Kingmaker;
using Kingmaker.Modding;
using Owlcat.Runtime.UI.MVVM;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;

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
         if (resource is GameObject gameObject)
            SetTexts(gameObject);
    }

    [HarmonyPatch(typeof(ViewBase<IViewModel>), nameof(ViewBase<IViewModel>.Bind))]
    [HarmonyPostfix]
    static void BindPatch(ViewBase<IViewModel> __instance) => SetTexts(__instance.gameObject);

    [HarmonyPatch(typeof(ViewBase<IViewModel>), nameof(ViewBase<IViewModel>.AddDisposable))]
    [HarmonyPostfix]
    static void AddDisposablePatch(ViewBase<IViewModel> __instance) => SetTexts(__instance.gameObject);

    static void SetTexts(GameObject gameObject)
    {
        if (TMP_FontAsset == null) return;

        var texts = gameObject.GetComponentsInChildren<TextMeshProUGUI>();

        for (int i = 0; i < texts.Length; i++)
        {
            texts[i].m_fontAsset = TMP_FontAsset;
            texts[i].UpdateFontAsset();
        }
    }
}
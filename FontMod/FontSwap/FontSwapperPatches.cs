using FontMod.Utility;
using HarmonyLib;
using Kingmaker.UI.Common;
#if KM
using Kingmaker.Localization;
#endif
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using TMPro;
using UnityEngine;
namespace FontMod.FontSwap;

[HarmonyPatch]
public static class TMPTestPach
{
    private static bool CanSwapFonts
    {
        get
        {
#if KM
            return LocalizationManager.CurrentPack != null;
#else
            return true;
#endif
        }
    }

    private static TMP_FontAsset GetMappedFontAsset(TMP_FontAsset fontAsset)
    {
        return fontAsset == null || !CanSwapFonts ? fontAsset : FontMapper.Instance.GetFontMapped(fontAsset);
    }

#if RT
    static bool _afterDelay = false;

    [HarmonyPatch(typeof(Kingmaker.GameStarter), nameof(Kingmaker.GameStarter.FixTMPAssets))]
    [HarmonyPostfix]
    static void DelaySwapping() => _afterDelay = true;
    
#endif

    [HarmonyPatch(typeof(TextMeshProUGUI), nameof(TextMeshProUGUI.LoadFontAsset))]
    [HarmonyPrefix]
    static void TextPatch(TextMeshProUGUI __instance)
    {

#if RT
        if (!_afterDelay)
            return;
#endif
        __instance.m_fontAsset = GetMappedFontAsset(__instance.m_fontAsset);
    }

    [HarmonyPatch(typeof(MaterialReferenceManager), nameof(MaterialReferenceManager.TryGetFontAsset))]
    [HarmonyPostfix]
    static void TryGetFontAsset(ref TMP_FontAsset fontAsset)
    {

#if RT
        if (!_afterDelay)
            return;
#endif

        fontAsset = GetMappedFontAsset(fontAsset);
    }

    [HarmonyPatch(typeof(TMP_Text), nameof(TMP_Text.ValidateHtmlTag))]
    [HarmonyTranspiler]
    static IEnumerable<CodeInstruction> TagPatch(IEnumerable<CodeInstruction> instructions, ILGenerator iLGen)
    {
        var method = AccessTools.Method(typeof(MaterialReferenceManager), nameof(MaterialReferenceManager.AddFontAsset));

        var assetIntructions = new CodeInstruction[]
        {
            new(OpCodes.Call, method)
        };

        var instructionIndex = instructions.FindCodes(assetIntructions);

        if (instructionIndex >= 0)
        {
            var ldlocs = instructions.ElementAt(instructionIndex - 1);

            var patchCodes = new CodeInstruction[]
            {
                new(OpCodes.Ldloc_S, ldlocs.operand),
                new(OpCodes.Call, AccessTools.Method(typeof(TMPTestPach), nameof(GetMappedFontAsset))),
                new(OpCodes.Stloc_S, ldlocs.operand)
            };

            return instructions.InsertRange(instructionIndex, patchCodes, true);
        }
        else
        {
            Main.Logger.Error("TagPatch Transpile Failed.");
            return instructions;
        }
    }

#if !RT
    // Hack to fix incorrect material.  I think the bigger problem is the material tag not being handled properly.
    // Will have to prob transpile more in the TMP library.

    [HarmonyPatch(typeof(UIUtility), nameof(UIUtility.GetSaberBookFormat))]
    [HarmonyPrefix]
    static void GetSaberBookFormatPatch(string name, Color color, int size, ref Material material)
    {
        if (material == null || !CanSwapFonts)
            return;

        FontMapper mapper = FontMapper.Instance;
        FontDataModel mapping;

        if (mapper.FontMappings.TryGetValue("Saber_Dist32", out mapping))
        {
            // An ignored mapping means that both the original font and its original material must pass through untouched.
            // => Otherwise e.g. nameplate disappears?
            if (mapping.IsIgnored) {
                return;
            }
        }
        else
        {
            mapping = mapper.DefaultFontMapping;
        }

        Material mappedMaterial = mapping?.TMP_FontAsset?.material;
        if (mappedMaterial != null) {
            material = mappedMaterial;
        }
    }
#endif
}

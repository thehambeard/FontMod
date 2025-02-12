using FontMod.Utility;
using HarmonyLib;
using Kingmaker.UI.Common;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
namespace FontMod.FontSwap;

[HarmonyPatch]
public static class TMPTestPach
{

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
        if (__instance.m_fontAsset != null)
            __instance.m_fontAsset = FontMapper.Instance.GetFontMapped(__instance.m_fontAsset);
    }

    [HarmonyPatch(typeof(MaterialReferenceManager), nameof(MaterialReferenceManager.TryGetFontAsset))]
    [HarmonyPostfix]
    static void TryGetFontAsset(ref TMP_FontAsset fontAsset)
    {

#if RT
        if (!_afterDelay)
            return;
#endif

        if (fontAsset != null)
            fontAsset = FontMapper.Instance.GetFontMapped(fontAsset);
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
                new(OpCodes.Call, AccessTools.PropertyGetter(typeof(FontMapper), nameof(FontMapper.Instance))),
                new(OpCodes.Ldloc_S, ldlocs.operand),
                new(OpCodes.Callvirt, AccessTools.Method(typeof(FontMapper), nameof(FontMapper.GetFontMapped))),
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
        if (material == null)
            return;

        material = FontMapper.Instance.FontMappings.ContainsKey("Saber_Dist32") ?
            FontMapper.Instance.FontMappings["Saber_Dist32"].TMP_FontAsset.material :
            FontMapper.Instance.DefaultFontMapping.TMP_FontAsset.material;
    }
#endif
}
using FontMod.Utility;
using HarmonyLib;
using Owlcat.Runtime.UI.MVVM;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using TMPro;
using UnityEngine;
namespace FontMod;

[HarmonyPatch]
public static class TMPTestPach
{
    [HarmonyPatch(typeof(TextMeshProUGUI), nameof(TextMeshProUGUI.LoadFontAsset))]
    [HarmonyPrefix]
    static void TextPatch(TextMeshProUGUI __instance)
    {
        if (__instance.m_fontAsset != null)
            __instance.m_fontAsset = FontMapper.Instance.GetFontMapped(__instance.m_fontAsset);
    }

    [HarmonyPatch(typeof(MaterialReferenceManager), nameof(MaterialReferenceManager.TryGetFontAsset))]
    [HarmonyPostfix]
    static void TryGetFontAsset(ref TMP_FontAsset fontAsset)
    {
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
}
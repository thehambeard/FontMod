using FontMod.Utility;
using HarmonyLib;
using Kingmaker.Code.UI.MVVM.View.LoadingScreen.PC;
using Kingmaker.UI.Legacy.LoadingScreen;
using Owlcat.Runtime.UI.MVVM;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace FontMod;

public class FontSwapperPatches : IModEventHandler
{
    public int Priority => 100;
    private Harmony _harmonyInstance = new(Main.ModEntry.Info.Id);

    public static List<Type> GetAllDerivedTypesOfViewBase()
    {
        var baseType = typeof(ViewBase<>);
        var assembly = Assembly.LoadFrom(Path.Combine(Application.dataPath, @"Managed\Code.dll"));
        var derivedTypes = new List<Type>();

        try
        {
            derivedTypes.AddRange(assembly.GetTypes()
                .Where(type => type.IsClass &&
                               !type.IsAbstract &&
                               !type.ContainsGenericParameters &&
                               InheritsFromGenericBase(type, baseType)));
        }
        catch (ReflectionTypeLoadException e)
        {
            Main.Logger.Error(e);
        }
        return derivedTypes;
    }

    private static bool InheritsFromGenericBase(Type type, Type genericBaseType)
    {
        while (type != null && type != typeof(object))
        {
            var currentType = type.IsGenericType ? type.GetGenericTypeDefinition() : type;
            if (currentType == genericBaseType)
            {
                return true;
            }
            type = type.BaseType;
        }
        return false;
    }

    public void PatchAllViewBase()
    {
        foreach (var type in GetAllDerivedTypesOfViewBase())
        {
            var bindMethod = type.GetMethod("BindViewImplementation", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (bindMethod == null || bindMethod.IsAbstract)
                continue;

            PatchHelper(bindMethod);
        }
    }

    private void PatchHelper(MethodInfo methodInfo)
    {
        if (methodInfo == null)
            return;

        try
        {
            var patch = typeof(BindPatch).GetMethod(nameof(BindPatch.Patch));
            _harmonyInstance.Patch(methodInfo, postfix: new HarmonyMethod(patch));
            Main.Logger.Debug($"Patched: {methodInfo.DeclaringType.Name}");
        }
        catch 
        {
            Main.Logger.Debug($"Skipped Patch (invalid method): {methodInfo.DeclaringType.Name}");
        }
    }

    public void HandleModDisable() { }

    public void HandleModEnable() => PatchAllViewBase();

    private static class BindPatch
    {
        public static void Patch(object __instance)
        {
            if (__instance != null && __instance is Component comp)
                FontSwapper.Instance.Swap(comp.gameObject);
        }
    }
}
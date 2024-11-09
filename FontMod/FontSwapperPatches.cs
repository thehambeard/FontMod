using FontMod.Utility;
using HarmonyLib;
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
        Harmony harmonyInstance = new(Main.ModEntry.Info.Id);

        foreach (var type in GetAllDerivedTypesOfViewBase())
        {
            var bindMethod = type.GetMethod("BindViewImplementation", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (bindMethod == null || bindMethod.IsAbstract)
                continue;

            try
            {
                var patch = typeof(BindPatch).GetMethod(nameof(BindPatch.Patch));
                harmonyInstance.Patch(bindMethod, postfix: new HarmonyMethod(patch));
            }
            catch { }
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
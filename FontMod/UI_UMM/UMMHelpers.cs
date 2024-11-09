using System;
using UnityEngine;
using GL = UnityEngine.GUILayout;


namespace FontMod.UI_UMM;
public static class UMMHelpers
{
    public static readonly GUIStyle ButtonStyleFixed = new(GUI.skin.button) { fixedWidth = 75f };
    public static readonly GUIStyle LabelStyleFixed = new(GUI.skin.label) { fixedWidth = 300f };
    public static readonly GUIStyle VScopeStyleFixed = new() { fixedWidth = 600f };
    public static readonly GUIStyle HScopeStyleFixed = new() { fixedWidth = 500f };
    public static readonly GUILayoutOption[] _falseWidth = [GL.ExpandWidth(false)];

    public static void Button(string text, Action action, float width) => Button(text, action, new GUIStyle(GUI.skin.button) { fixedWidth = width });
    public static void Button(string text, Action action, GUIStyle style = null)
    {
        if (GL.Button(text, style ?? ButtonStyleFixed, _falseWidth))
            action.Invoke();
    }
    public static void Label(string text, float width) => GL.Label(text, new GUIStyle(GUI.skin.label) { fixedWidth = width });
    public static void Label(string text, GUIStyle style = null) => GL.Label(text, style ?? LabelStyleFixed, _falseWidth);
    public static void LineBreak() => VScope(() => GL.Space(10f));
    public static void Space(float width = 10f) => GL.Space(width);
    public static void HScope(Action action, GUIStyle style = null)
    {
        using (new GL.HorizontalScope(style ?? HScopeStyleFixed))
        {
            action.Invoke();
        }
    }

    public static void VScope(Action action, GUIStyle style = null)
    {
        using (new GL.VerticalScope(style ?? VScopeStyleFixed))
        {
            action.Invoke();
        }
    }
}

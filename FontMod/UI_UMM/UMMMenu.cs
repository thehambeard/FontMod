using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityModManagerNet;
using FontMod.FontSwap;
using static FontMod.UI_UMM.UMMHelpers;

namespace FontMod.UI_UMM;
public static class UMMMenu
{
    private enum State
    {
        Main,
        Mapping
    }

    private static State _state;
    private static string _currentMappingKey;

    public static void OnGUI(UnityModManager.ModEntry modEntry)
    {
        switch (_state)
        {
            case State.Main:
                OnGUIMain();
                break;
            case State.Mapping:
                OnGUIMapping();
                break;
        }
    }

    private static void OnGUIMapping()
    {
        if (string.IsNullOrEmpty(_currentMappingKey))
            _state = State.Main;

        VScope(() =>
        {
            Label($"Select mapping for game font {_currentMappingKey}");
            Button("Cancel", () =>
            {
                _currentMappingKey = null;
                _state = State.Main;
            });
            HScope(() =>
            {
                Space(20f);
                VScope(() =>
                {
                    foreach (var font in FontMapper.Instance.InstalledFonts)
                    {
                        HScope(() =>
                        {
                            Label($"{font.Name}");
                            Button("M", () =>
                            {
                                FontMapper.Instance.SetFontMapping(_currentMappingKey, font, true);
                                _currentMappingKey = null;
                                _state = State.Main;
                            });
                        });
                    }
                }, new(GUI.skin.box));
            });
        });
    }

    private static void OnGUIMain()
    {
        Button("Refresh Scene Fonts", () =>
        {
            foreach (var gObj in SceneManager
                .GetActiveScene()
                .GetRootGameObjects())
            {
                FontMapper.Instance.Swap(gObj);
            }

        }, 200f);
        HScope(() =>
        {
            VScope(() =>
            {
                Label("Current Installed Fonts:");
                Label("(located in the FontMod/Fonts folder)");

                HScope(() =>
                {
                    Space(20f);
                    VScope(() =>
                    {
                        foreach (var fonts in FontMapper.Instance.InstalledFonts)
                            Label($"{fonts.Name}");
                    }, new(GUI.skin.box));
                });
            });

            VScope(() =>
            {
                Label("Fonts In Game:");
                Label("(New fonts encountered auto-added)");

                HScope(() =>
                {
                    Space(20f);
                    VScope(() =>
                    {
                        foreach (var fonts in FontMapper.Instance.EncounteredFontNames)
                            Label($"{fonts}");
                    }, new(GUI.skin.box));
                });

            });
        });

        LineBreak();

        VScope(() =>
        {
            Label("Current Mappings:");
            Label("(X button will deleted current mapping)");
            Label("(M button will let you select a mapping)");
            Label("(I button will set to ignore all mappings)");
            Label("(Unassigned mappings will use default font)");
            Label("(Ignore mappings will use in-game font)");

            HScope(() =>
            {
                Space(20f);
                VScope(() =>
                {
                    var removeKey = string.Empty;

                    foreach (var mapping in FontMapper.Instance.FontMappings)
                    {
                        HScope(() =>
                        {
                            Label($"{mapping.Key} => {(mapping.Value.IsIgnored ? "ignored" : mapping.Value.Name)}");

                            Button("M", () =>
                            {
                                _currentMappingKey = mapping.Key;
                                _state = State.Mapping;
                            });

                            if (!FontMapper.Instance.IsDefaultKey(mapping.Key))
                            {
                                Button("X", () =>
                                {
                                    removeKey = mapping.Key;
                                });

                                Button("I", () =>
                                {
                                    if (!FontMapper.Instance.ToggleIgnored(mapping.Key) && string.IsNullOrEmpty(mapping.Value.Name))
                                        removeKey = mapping.Key;
                                });
                            }
                        });
                    }

                    foreach (var font in FontMapper.Instance.EncounteredFontNames
                        .Where(item => !FontMapper.Instance.FontMappings.ContainsKey(item)))
                    {
                        HScope(() =>
                        {
                            Label($"{font} => null");

                            Button("M", () =>
                            {
                                _currentMappingKey = font;
                                _state = State.Mapping;
                            });

                            Button("I", () =>
                            {
                                FontMapper.Instance.ToggleIgnored(font);
                            });


                        });
                    }

                    if (!string.IsNullOrEmpty(removeKey))
                        FontMapper.Instance.RemoveFontMapping(removeKey);

                }, new(GUI.skin.box));
            });
        });
    }

#if DEBUG
    public static void OnGUIDebug(UnityModManager.ModEntry modEntry)
    {
        OnGUI(modEntry);
    }
#endif
}

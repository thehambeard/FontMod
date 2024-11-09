using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.PlayerLoop;
using UnityEngine.SceneManagement;
using UnityModManagerNet;
using static FontMod.UI_UMM.UMMHelpers;
using static Kingmaker.Sound.AudioFilePackagesSettings;

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

                    foreach (var font in FontSwapper.Instance.FontMapper.InstalledFonts)
                    {
                        HScope(() =>
                        {
                            Label($"{font.Name}");
                            Button("M", () =>
                            {
                                FontSwapper.Instance.FontMapper.SetFontMapping(_currentMappingKey, font);
                                FontSwapper.Instance.FontMapper.SaveFontMappings();
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
                FontSwapper.Instance.Swap(gObj);
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
                        foreach (var fonts in FontSwapper.Instance.FontMapper.InstalledFonts)
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
                        foreach (var fonts in FontSwapper.Instance.FontMapper.EncounteredFontNames)
                            Label($"{fonts}");
                    }, new(GUI.skin.box));
                });

            });
        });

        LineBreak();

        VScope(() =>
        {
            Label("Current Mappings:");
            Label("(default cannot be deleted)");
            Label("(null mappings will use default font)");
            HScope(() =>
            {
                Space(20f);
                VScope(() =>
                {
                    var removeKey = string.Empty;

                    foreach (var mappings in FontSwapper.Instance.FontMapper.FontMappings)
                    {
                        HScope(() =>
                        {
                            Label($"{mappings.Key} => {mappings.Value.Name}");

                            Button("M", () =>
                            {
                                _currentMappingKey = mappings.Key;
                                _state = State.Mapping;
                            });

                            if (mappings.Key != FontSwapper.Instance.FontMapper.DefaultKey)
                            {
                                Button("X", () =>
                                {
                                    removeKey = mappings.Key;
                                });
                            }
                        });
                    }

                    if (!string.IsNullOrEmpty(removeKey))
                    {
                        FontSwapper.Instance.FontMapper.RemoveFontMapping(removeKey);
                        FontSwapper.Instance.FontMapper.SaveFontMappings();
                    }

                    foreach (var font in FontSwapper.Instance.FontMapper.EncounteredFontNames
                        .Except(FontSwapper.Instance.FontMapper.FontMappings.Select(x => x.Key)))
                    {
                        HScope(() =>
                        {
                            Label($"{font} => null");

                            Button("M", () =>
                            {
                                _currentMappingKey = font;
                                _state = State.Mapping;
                            });
                        });
                    }
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

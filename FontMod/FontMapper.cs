using FontMod.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore;

namespace FontMod;
public class FontMapper
{
    private readonly string _defaultKey = "default";
    private readonly string _fontNamesFile = "encounteredFonts.json";
    private readonly string _fontMappingsFile = "fontMappings.json";
    private readonly string _fontFolder = "Fonts";
    private static FontMapper _instance;
    public FontCollection InstalledFonts { get; private set; } = [];
    public HashSet<string> EncounteredFontNames { get; private set; }
    public Dictionary<string, FontDataModel> FontMappings { get; private set; }

    public static FontMapper Instance
    {
        get
        {
            if (_instance == null)
                _instance = new FontMapper();

            return _instance;
        }
    }

    private FontMapper()
    {
        _fontNamesFile = Path.Combine(Main.ModEntry.Path, _fontNamesFile);
        _fontMappingsFile = Path.Combine(Main.ModEntry.Path, _fontMappingsFile);
        _fontFolder = Path.Combine(Main.ModEntry.Path, _fontFolder);

        LoadFonts();
        LoadEncounteredFontNames();
        LoadFontMappings();

        SceneManager.sceneLoaded += SceneManager_sceneLoaded;
        SceneManager.sceneUnloaded += SceneManager_sceneUnloaded;
    }

    private void SceneManager_sceneUnloaded(Scene arg0) => SaveEncounteredFontNames();
    private void SceneManager_sceneLoaded(Scene arg0, LoadSceneMode arg1) => SaveEncounteredFontNames();


    public void SetIgnored(TMP_FontAsset gameFont, bool ignored) =>
        SetIgnored(gameFont.name, ignored);

    public void SetIgnored(string gameFontName, bool ignore)
    {
        if (FontMappings.TryGetValue(gameFontName, out var mapping))
            mapping.IsIgnored = ignore;
    }

    public bool ToggleIgnored(TMP_FontAsset gameFont) =>
        ToggleIgnored(gameFont.name);

    public bool ToggleIgnored(string gameFontName)
    {
        if (FontMappings.TryGetValue(gameFontName, out var mapping))
            mapping.IsIgnored = !mapping.IsIgnored;
        else
        {
            mapping = SetFontMapping(gameFontName, FontDataModel.CreateEmptyIgnored());
            mapping.IsIgnored = true;
        }

        SaveFontMappings();
        return mapping.IsIgnored;
    }

    public TMP_FontAsset GetFontMapped(TMP_FontAsset gameFont) => GetFontMapped_Internal(gameFont.name) ?? gameFont;

    private TMP_FontAsset GetFontMapped_Internal(string gameFontName)
    {
        if (!EncounteredFontNames.Contains(gameFontName) && !InstalledFonts.Any(f => f.Name == gameFontName))
        {
            Main.Logger.Debug($"Encountered a new font: {gameFontName}");

            lock (EncounteredFontNames)
            {
                EncounteredFontNames.Add(gameFontName);
            }
        }

        if (FontMappings.TryGetValue(gameFontName, out var fontMapping))
        {
            if (!fontMapping.IsIgnored)
                return fontMapping.TMP_FontAsset;
            else
                return null;
        }

        if (FontMappings.TryGetValue(_defaultKey, out fontMapping))
            return fontMapping.TMP_FontAsset;

        return null;
    }

    public bool IsDefaultKey(string key) => key == _defaultKey;
    public bool IsIgnored(string key) => GetFontMapped_Internal(key) == null;
    public TMP_FontAsset GetDefaultFont() => GetFontMapped_Internal(_defaultKey);
    public FontDataModel SetDefault(TMP_FontAsset gameFont) => SetFontMapping(gameFont, InstalledFonts.GetFontByName(_defaultKey));
    public FontDataModel SetDefault(string gameFontName) => SetFontMapping(gameFontName, InstalledFonts.GetFontByName(_defaultKey));

    public FontDataModel SetFontMapping(TMP_FontAsset gameFont, string customFontName) =>
        SetFontMapping(gameFont.name, InstalledFonts.GetFontByName(customFontName));

    public FontDataModel SetFontMapping(TMP_FontAsset gameFont, FontDataModel fontDataModel) =>
        SetFontMapping(gameFont.name, fontDataModel);

    public FontDataModel SetFontMapping(string gameFontName, string customFontName) =>
        SetFontMapping(gameFontName, InstalledFonts.GetFontByName(customFontName));

    public FontDataModel SetFontMapping(string gameFontName, FontDataModel fontDataModel, bool forceSave = false)
    {
        try
        {
            if (fontDataModel == null || string.IsNullOrEmpty(gameFontName))
                throw new ArgumentNullException("Mapping Error: Font names cannot be null or empty");

            if (!string.IsNullOrEmpty(fontDataModel.Name))
            {
                FontMappings[gameFontName] = InstalledFonts.GetFontByName(fontDataModel.Name);
                Main.Logger.Log($"Added Mapping: {gameFontName}->{fontDataModel.Name}");
            }
            else
                FontMappings[gameFontName] = FontDataModel.CreateEmptyIgnored();

            if (forceSave)
                SaveFontMappings();

            return FontMappings[gameFontName];
        }
        catch (Exception e)
        {
            Main.Logger.Error(e);
        }

        return null;
    }

    public void RemoveFontMapping(TMP_FontAsset gameFont) =>
        RemoveFontMapping(gameFont.name);

    public void RemoveFontMapping(string gameFontName)
    {
        try
        {
            if (string.IsNullOrEmpty(gameFontName) || !FontMappings.ContainsKey(gameFontName))
                throw new ArgumentException($"Key to remove: {gameFontName} not found in mappings");

            if (gameFontName == _defaultKey)
                throw new ArgumentException("Cannot remove default mapping. Reassign it if it needs to be changed.");

            FontMappings.Remove(gameFontName);
            SaveFontMappings();
        }
        catch (Exception e)
        {
            Main.Logger.Error(e);
        }
    }

    private void LoadFonts()
    {
        InstalledFonts.AddFromFolderPath(_fontFolder);

        if (InstalledFonts.Count == 0)
        {
            Main.Logger.Error("Critical Error! No fonts were loaded. Disabling mod.");
            Main.DisableMod();
        }
    }

    public void LoadFontMappings()
    {
        Main.Logger.Debug($"Loading Font Mappings");

        FontMappings = [];

        var fontMappings = new Dictionary<string, FontDataModel>();

        if (File.Exists(_fontMappingsFile))
            fontMappings = JSON.LoadJSONFromFile<Dictionary<string, FontDataModel>>(_fontMappingsFile) ?? [];

        if (!fontMappings.ContainsKey(_defaultKey))
            SetFontMapping(_defaultKey, InstalledFonts.FirstOrDefault(), true);

        foreach (var kvp in fontMappings)
            SetFontMapping(kvp.Key, kvp.Value);
    }

    public void SaveFontMappings()
    {
        Main.Logger.Debug($"Saving Encountered Font Names");
        JSON.SaveJSONToFile(_fontMappingsFile, FontMappings);
    }

    public void LoadEncounteredFontNames()
    {
        Main.Logger.Debug($"Loading Encountered Font Names");

        EncounteredFontNames = [];

        if (File.Exists(_fontNamesFile))
            EncounteredFontNames = JSON.LoadJSONFromFile<HashSet<string>>(_fontNamesFile) ?? [];
    }

    public void SaveEncounteredFontNames()
    {
        Main.Logger.Debug($"Saving Encountered Font Names");
        JSON.SaveJSONToFile(_fontNamesFile, EncounteredFontNames);
    }

    public void SaveAll()
    {
        SaveEncounteredFontNames();
        SaveFontMappings();
    }

    public void Swap(GameObject gameObject)
    {
        try
        {
            if (gameObject == null)
                throw new ArgumentNullException("Font swap failed due to GameObject being null");

            var texts = gameObject.GetComponentsInChildren<TextMeshProUGUI>(includeInactive: true);

            for (int i = 0; i < texts.Length; i++)
            {
                if (texts[i].font == null)
                    continue;

                var font = GetFontMapped_Internal(texts[i].font.name);

                if (font != null)
                {
                    bool isActive = texts[i].gameObject.activeSelf;

                    if (isActive)
                        texts[i].gameObject.SetActive(false);

                    texts[i].m_fontAsset = font;
                    texts[i].UpdateFontAsset();
                    texts[i].gameObject.SetActive(isActive);
                }
            }
        }
        catch (Exception e)
        {
            Main.Logger.Error(e);
        }
    }
}

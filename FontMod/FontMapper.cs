using FontMod.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FontMod;
public class FontMapper
{
    public readonly string DefaultKey = "default";
    private readonly string _fontNamesFile = "encounteredFonts.json";
    private readonly string _fontMappingsFile = "fontMappings.json";
    private readonly string _fontFolder = "Fonts";
    private Dictionary<string, string> _fontNameMappings;

    public FontCollection InstalledFonts { get; private set; } = [];
    public HashSet<string> EncounteredFontNames { get; private set; }
    public Dictionary<string, FontDataModel> FontMappings { get; private set; }

    public FontMapper()
    {
        _fontNamesFile = Path.Combine(Main.ModEntry.Path, _fontNamesFile);
        _fontMappingsFile = Path.Combine(Main.ModEntry.Path, _fontMappingsFile);
        _fontFolder = Path.Combine(Main.ModEntry.Path, _fontFolder);

        LoadFonts();
        LoadEncounteredFontNames();
        LoadFontMappings();
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

    public FontDataModel GetFontMapped(string gameFontName)
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
            return fontMapping;

        if (FontMappings.TryGetValue(DefaultKey, out fontMapping))
            return fontMapping;

        Main.Logger.Warning($"No valid mapping found for {gameFontName}. Falling back to first font loaded.");

        return InstalledFonts.FirstOrDefault();
    }

    public void SetFontMapping(string gameFontName, FontDataModel fontDataModel)
    {
        try
        {
            if (fontDataModel == null || string.IsNullOrEmpty(gameFontName))
                throw new ArgumentNullException("Mapping Error: Font names cannot be null or empty");

            FontMappings[gameFontName] = fontDataModel;
            _fontNameMappings[gameFontName] = fontDataModel.Name;

            Main.Logger.Log($"Added Mapping: {gameFontName}->{fontDataModel.Name}");
        }
        catch (Exception e)
        {
            Main.Logger.Error(e);
        }

        FontMappings[gameFontName] = fontDataModel;
    }

    public void RemoveFontMapping(string gameFontName)
    {
        try
        {
            if (string.IsNullOrEmpty(gameFontName) || !FontMappings.ContainsKey(gameFontName))
                throw new ArgumentException($"Key to remove: {gameFontName} not found in mappings");

            if (gameFontName == DefaultKey)
                throw new ArgumentException("Cannot remove default mapping. Reassign it if it needs to be changed.");

            FontMappings.Remove(gameFontName);
            _fontNameMappings.Remove(gameFontName);
        }
        catch (Exception e)
        {
            Main.Logger.Error(e);
        }
    }

    public void SetFontMapping(string gameFontName, string customFontName) => SetFontMapping(gameFontName, InstalledFonts.GetFontByName(customFontName));

    public void LoadFontMappings()
    {
        Main.Logger.Debug($"Loading Font Mappings");

        FontMappings = [];
        _fontNameMappings = [];

        var fontNameMappings = new Dictionary<string, string>();

        if (File.Exists(_fontMappingsFile))
            fontNameMappings = JSON.LoadJSONFromFile<Dictionary<string, string>>(_fontMappingsFile) ?? [];

        if (!fontNameMappings.ContainsKey(DefaultKey))
            SetFontMapping(DefaultKey, InstalledFonts.FirstOrDefault().Name);

        foreach (var kvp in fontNameMappings)
            SetFontMapping(kvp.Key, kvp.Value);
    }

    public void SaveFontMappings()
    {
        Main.Logger.Debug($"Saving Encountered Font Names");
        JSON.SaveJSONToFile(_fontMappingsFile, _fontNameMappings);
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
}

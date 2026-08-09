#if KM
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Kingmaker.Localization;
using TMPro;
using TMPro.EditorUtilities;

namespace FontMod.FontSwap;

[Serializable]
public class FontDataModel
{
    private const int FontAtlasCacheKeyVersion = 2;
    private static string requiredCharacters;

    [JsonProperty]
    public string Name { get; set; }
    [JsonProperty]
    public bool IsIgnored { get; set; }
    [JsonIgnore]
    public string FontPath { get; set; }
    [JsonIgnore]
    public TMP_FontAsset TMP_FontAsset
    {
        get
        {
            // We're lazy because large fonts (e.g. chinese fonts) can take a lot of time
            if (!fontAssetCreationAttempted && !IsIgnored && !string.IsNullOrEmpty(FontPath)) {
                fontAssetCreationAttempted = true;
                tmpFontAsset = CreateFontAsset(FontPath);
            }

            return tmpFontAsset;
        }
    }

    private TMP_FontAsset tmpFontAsset;
    private bool fontAssetCreationAttempted;

    private FontDataModel() { }

    private FontDataModel(string fontPath)
    {
        try
        {
            if (fontPath == null)
                throw new ArgumentNullException("null font in creation of FontDataModel.");

            FontPath = fontPath;
            Name = Path.GetFileNameWithoutExtension(fontPath);
        }
        catch (Exception e)
        {
            Main.Logger.Error(e);
        }
    }

    public override bool Equals(object obj)
    {
        if (obj is FontDataModel other)
            return Equals(FontPath, other.FontPath);

        return false;
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 17;
            hash = hash * 31 + (FontPath != null ? FontPath.GetHashCode() : 0);
            return hash;
        }
    }
    public static FontDataModel CreateEmptyIgnored() => new() { IsIgnored = true };
    public static FontDataModel CreateFromPath(string fontPath) => new(fontPath);
    public static TMP_FontAsset CreateFontAsset(string fontPath)
    {
        TMP_FontAsset asset = null;

        try
        {
            if (!File.Exists(fontPath))
                throw new FileNotFoundException($"File not found: {fontPath}");

            var name = Path.GetFileNameWithoutExtension(fontPath);

            var create = new TMPro_FontAssetCreatorWindow();
            create.font_TTF_path = fontPath;
            var characters = GetRequiredCharacters();
            create.SetCharacterSet(characters);
            var renderType = create.UseBitmapFontAsset ? "bitmap" : "SDF";
            var cachePath = GetFontAtlasCachePath(fontPath, characters, create);
            bool generated = false;

            if (create.TryLoadFontAtlasCache(cachePath))
            {
                Main.Logger.Log($"Loaded cached {renderType} atlas for {name}.");
            }
            else
            {
                generated = true;
                Main.Logger.Log($"Generating {create.CharacterCount} glyphs for {name} in a {create.AtlasSize}x{create.AtlasSize} {renderType} atlas.");
                create.GenerateFontAtlas();
            }

            create.CreateFontTexture();

            asset = create.UseBitmapFontAsset ? create.Save_Normal_FontAsset() : create.Save_SDF_FontAsset();

            if (asset == null)
                throw new NullReferenceException($"Creation of TMP_FontAsset failed for font {name}");
            asset.name = name;
            asset.ReadFontDefinition();

            if (generated)
            {
                try
                {
                    create.SaveFontAtlasCache(cachePath);
                }
                catch (Exception e)
                {
                    Main.Logger.Warning($"Could not cache the generated atlas for {name}: {e.Message}");
                }
            }

            Main.Logger.Log($"Created font asset {asset.name}");

            MaterialReferenceManager.AddFontAsset(asset);
        }
        catch (Exception e)
        {
            Main.Logger.Error(e);
        }

        return asset;
    }

    private static string GetFontAtlasCachePath(string fontPath, string characters, TMPro_FontAssetCreatorWindow creator)
    {
        byte[] fontHash;
        using (var stream = File.OpenRead(fontPath)) {
            using var hash = SHA256.Create();
            fontHash = hash.ComputeHash(stream);
        }

        string settings = $"{FontAtlasCacheKeyVersion}\n{creator.AtlasSize}\n{creator.UseBitmapFontAsset}\n{characters}";
        byte[] settingsBytes = Encoding.UTF8.GetBytes(settings);
        byte[] keyData = new byte[fontHash.Length + settingsBytes.Length];
        Buffer.BlockCopy(fontHash, 0, keyData, 0, fontHash.Length);
        Buffer.BlockCopy(settingsBytes, 0, keyData, fontHash.Length, settingsBytes.Length);

        byte[] cacheHash;
        using (var hash = SHA256.Create()) {
            cacheHash = hash.ComputeHash(keyData);
        }

        string cacheKey = BitConverter.ToString(cacheHash).Replace("-", "").ToLowerInvariant();
        return Path.Combine(Main.ModEntry.Path, "Cache", cacheKey + ".fontatlas");
    }

    private static string GetRequiredCharacters()
    {
        if (requiredCharacters != null) {
            return requiredCharacters;
        }

        var characters = new HashSet<char>();
        // Printable ASCII is required for TMP rich-text tags and ordinary UI text.
        for (char character = ' '; character <= '~'; character++) {
            characters.Add(character);
        }

        // TMP support characters: non-breaking space, zero-width space, ellipsis, and missing-glyph box.
        characters.Add('\u00a0');
        characters.Add('\u200b');
        characters.Add('\u2026');
        characters.Add('\u25a1');

        AddLocalizationCharacters(characters, LocalizationManager.CurrentPack);
        AddLocalizationCharacters(characters, LocalizationManager.CurrentPackFast);

        requiredCharacters = new string(characters.OrderBy(character => character).ToArray());
        Main.Logger.Log($"Preparing {requiredCharacters.Length} distinct characters for the current Kingmaker localization.");
        return requiredCharacters;
    }

    private static void AddLocalizationCharacters(HashSet<char> characters, LocalizationPack pack)
    {
        if (pack?.Strings == null) {
            return;
        }

        foreach (string value in pack.Strings.Values)
        {
            if (string.IsNullOrEmpty(value)) {
                continue;
            }

            foreach (char character in value)
            {
                if (!char.IsControl(character)) {
                    characters.Add(character);
                }
            }
        }
    }
}


#endif

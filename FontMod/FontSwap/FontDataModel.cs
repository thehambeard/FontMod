#if !KM
using Newtonsoft.Json;
using System;
using System.IO;
using TMPro;
using UnityEngine;

namespace FontMod.FontSwap;

[Serializable]
public class FontDataModel
{
    [JsonProperty]
    public string Name { get; set; }
    [JsonProperty]
    public bool IsIgnored { get; set; }

    [JsonIgnore]
    public Font Font { get; private set; }
    [JsonIgnore]
    public TMP_FontAsset TMP_FontAsset { get; private set; }


    private FontDataModel() { }

    private FontDataModel(Font font)
    {
        try
        {
            if (font == null)
                throw new ArgumentNullException("null font in creation of FontDataModel.");

            Font = font;
            Name = font.name;
            TMP_FontAsset = CreateFontAsset(font);
        }
        catch (Exception e)
        {
            Main.Logger.Error(e);
        }
    }

    public override bool Equals(object obj)
    {
        if (obj is FontDataModel other)
            return Equals(Font, other.Font) && Equals(TMP_FontAsset, other.TMP_FontAsset);

        return false;
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 17;
            hash = hash * 31 + (Font != null ? Font.GetHashCode() : 0);
            hash = hash * 31 + (TMP_FontAsset != null ? TMP_FontAsset.GetHashCode() : 0);
            return hash;
        }
    }
    public static FontDataModel CreateEmptyIgnored() => new() { IsIgnored = true };
    public static FontDataModel CreateFromFont(Font font) => new(font);
    public static FontDataModel CreateFromFontPath(string path) => new(LoadFontFromFile(path));
    public static TMP_FontAsset CreateFontAsset(Font font)
    {
        TMP_FontAsset asset = null;

        try
        {
            asset = TMP_FontAsset.CreateFontAsset(font);
            asset.name = font.name;

            if (asset == null)
                throw new NullReferenceException($"Creation of TMP_FontAsset failed for font {font.name}");
            else
                Main.Logger.Log($"Created font asset {asset.name}");
        }
        catch (Exception e)
        {
            Main.Logger.Error(e);
        }

        MaterialReferenceManager.AddFontAsset(asset);

        return asset;
    }

    public static Font LoadFontFromFile(string fontPath)
    {
        Font font = null;
        try
        {
            if (!File.Exists(fontPath))
                throw new FileNotFoundException($"File not found: {fontPath}");

            font = new(fontPath)
            {
                name = Path.GetFileNameWithoutExtension(fontPath)
            };

            if (font == null)
                throw new InvalidOperationException($"failed to load font from path {font}");
        }
        catch (Exception e)
        {
            Main.Logger.Error(e);
        }

        return font;
    }
}
#endif
#if KM
using Newtonsoft.Json;
using System;
using System.Drawing;
using System.IO;
using TMPro;
using TMPro.EditorUtilities;
using UnityEngine;
using static RootMotion.FinalIK.GrounderQuadruped;

namespace FontMod.FontSwap;

[Serializable]
public class FontDataModel
{
    [JsonProperty]
    public string Name { get; set; }
    [JsonProperty]
    public bool IsIgnored { get; set; }
    [JsonIgnore]
    public string FontPath { get; set; }
    [JsonIgnore]
    public TMP_FontAsset TMP_FontAsset { get; private set; }

    private FontDataModel() { }

    private FontDataModel(string fontPath)
    {
        try
        {
            if (fontPath == null)
                throw new ArgumentNullException("null font in creation of FontDataModel.");

            FontPath = fontPath;
            Name = Path.GetFileNameWithoutExtension(fontPath);
            TMP_FontAsset = CreateFontAsset(fontPath);
        }
        catch (Exception e)
        {
            Main.Logger.Error(e);
        }
    }

    public override bool Equals(object obj)
    {
        if (obj is FontDataModel other)
            return Equals(FontPath, other.FontPath) && Equals(TMP_FontAsset, other.TMP_FontAsset);

        return false;
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 17;
            hash = hash * 31 + (FontPath != null ? FontPath.GetHashCode() : 0);
            hash = hash * 31 + (TMP_FontAsset != null ? TMP_FontAsset.GetHashCode() : 0);
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
            create.GenerateFontAtlas();
            create.CreateFontTexture();

            asset = create.Save_SDF_FontAsset();

            if (asset == null)
                throw new NullReferenceException($"Creation of TMP_FontAsset failed for font {name}");
            else
                Main.Logger.Log($"Created font asset {asset.name}");

            asset.name = name;

            if (asset != null)
                MaterialReferenceManager.AddFontAsset(asset);
        }
        catch (Exception e)
        {
            Main.Logger.Error(e);
        }

        return asset;
    }
}


#endif
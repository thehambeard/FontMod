using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.IO;
using System.Linq;



namespace TMPro.EditorUtilities
{

    public class TMPro_FontAssetCreatorWindow
    {
        //private GUIContent m_MenuItem1 = new GUIContent("Menu Item 1");
        ////private GUIContent m_MenuItem2 = new GUIContent("Menu Item 2");

        ////Implement IHasCustomMenu.AddItemsToMenu
        //public void AddItemsToMenu(GenericMenu menu)
        //{
        //    menu.AddItem(m_MenuItem1, false, MenuItem1Selected);
        //    //menu.AddItem(m_MenuItem2, m_Item2On, MenuItem2Selected);
        //}

        //private void MenuItem1Selected()
        //{
        //    Debug.Log("Menu Item 1 selected");
        //}

        private string[] FontSizingOptions = { "Auto Sizing", "Custom Size" };
        private int FontSizingOption_Selection = 0;
        private string[] FontResolutionLabels = { "16", "32", "64", "128", "256", "512", "1024", "2048", "4096", "8192" };
        private int[] FontAtlasResolutions = { 16, 32, 64, 128, 256, 512, 1024, 2048, 4096, 8192 };
        private string[] FontCharacterSets = { "ASCII", "Extended ASCII", "ASCII Lowercase", "ASCII Uppercase", "Numbers + Symbols", "Custom Range", "Unicode Range (Hex)", "Custom Characters", "Characters from File" };
        private enum FontPackingModes { Fast = 0, Optimum = 4 };
        private FontPackingModes m_fontPackingSelection = 0;

        private int font_CharacterSet_Selection = 0;
        private enum PreviewSelectionTypes { PreviewFont, PreviewTexture, PreviewDistanceField };
        private PreviewSelectionTypes previewSelection;

        private string characterSequence = "32 - 126, 160, 8203, 8230, 9633"; //ASCII
        private string output_feedback = "";
        private string output_name_label = "Font: ";
        private string output_size_label = "Pt. Size: ";
        private string output_count_label = "Characters packed: ";
        private int m_character_Count;
        private Vector2 output_ScrollPosition;

        //private GUISkin TMP_GUISkin;
        //private GUIStyle TextureAreaBox;
        //private GUIStyle TextAreaBox;
        //private GUIStyle Section_Label;


        //private Thread MainThread;
        private Color[] Output;
        private bool isDistanceMapReady = false;
        private bool isRepaintNeeded = false;

        private Rect progressRect;
        public static float ProgressPercentage;
        private float m_renderingProgress;
        private bool isRenderingDone = false;
        private bool isProcessing = false;
        private bool isGenerationCancelled = false;

        public string font_TTF_path;
        private TMP_FontAsset m_fontAssetSelection;
        private TextAsset characterList;
        private int font_size;

        private int font_padding = 5;
        private FaceStyles font_style = FaceStyles.Normal;
        private float font_style_mod = 2;
        private RenderModes font_renderMode = RenderModes.DistanceField16;
        private int font_atlas_width = 512;
        private int font_atlas_height = 512;

        //private int m_shaderSelectionIndex;
        //private Shader m_shaderSelection;
        //private string[] m_availableShaderNames;
        //private Shader[] m_availableShaders;


        private int font_scaledownFactor = 1;
        //private int font_spread = 4;

        private FT_FaceInfo m_font_faceInfo;
        private FT_GlyphInfo[] m_font_glyphInfo;
        private byte[] m_texture_buffer;
        private Texture2D m_font_Atlas;
        //private Texture2D m_texture_Atlas;
        //private int m_packingMethod = 0;

        private Texture2D m_destination_Atlas;
        private bool includeKerningPairs = true;
        private int[] m_kerningSet;

        // Image Down Sampling Fields
        //private Texture2D sdf_Atlas;
        //private int downscale;

        //private Object prev_Selection;

        public void ON_COMPUTE_DT_EVENT(object Sender, Compute_DT_EventArgs e)
        {
            if (e.EventType == Compute_DistanceTransform_EventTypes.Completed)
            {
                Output = e.Colors;
                isProcessing = false;
                isDistanceMapReady = true;
            }
            else if (e.EventType == Compute_DistanceTransform_EventTypes.Processing)
            {
                ProgressPercentage = e.ProgressPercentage;
                isRepaintNeeded = true;
            }
        }

        /// <summary>
        /// Method which returns the character corresponding to a decimal value.
        /// </summary>
        /// <param name="sequence"></param>
        /// <returns></returns>
        int[] ParseNumberSequence(string sequence)
        {
            List<int> unicode_list = new List<int>();
            string[] sequences = sequence.Split(',');

            foreach (string seq in sequences)
            {
                string[] s1 = seq.Split('-');

                if (s1.Length == 1)
                    try
                    {
                        unicode_list.Add(int.Parse(s1[0]));
                    }
                    catch
                    {
                        Debug.Log("No characters selected or invalid format.");
                    }
                else
                {
                    for (int j = int.Parse(s1[0]); j < int.Parse(s1[1]) + 1; j++)
                    {
                        unicode_list.Add(j);
                    }
                }
            }

            return unicode_list.ToArray();
        }


        /// <summary>
        /// Method which returns the character (decimal value) from a hex sequence.
        /// </summary>
        /// <param name="sequence"></param>
        /// <returns></returns>
        int[] ParseHexNumberSequence(string sequence)
        {
            List<int> unicode_list = new List<int>();
            string[] sequences = sequence.Split(',');

            foreach (string seq in sequences)
            {
                string[] s1 = seq.Split('-');

                if (s1.Length == 1)
                    try
                    {
                        unicode_list.Add(int.Parse(s1[0], NumberStyles.AllowHexSpecifier));
                    }
                    catch
                    {
                        Debug.Log("No characters selected or invalid format.");
                    }
                else
                {
                    for (int j = int.Parse(s1[0], NumberStyles.AllowHexSpecifier); j < int.Parse(s1[1], NumberStyles.AllowHexSpecifier) + 1; j++)
                    {
                        unicode_list.Add(j);
                    }
                }
            }

            return unicode_list.ToArray();
        }

        void UpdateRenderFeedbackWindow()
        {
            font_size = m_font_faceInfo.pointSize;

            string missingGlyphReport = string.Empty;

            string colorTag = m_font_faceInfo.characterCount == m_character_Count ? "<color=#C0ffff>" : "<color=#ffff00>";
            string colorTag2 = "<color=#C0ffff>";

            missingGlyphReport = output_name_label + "<b>" + colorTag2 + m_font_faceInfo.name + "</color></b>";

            if (missingGlyphReport.Length > 60)
                missingGlyphReport += "\n" + output_size_label + "<b>" + colorTag2 + m_font_faceInfo.pointSize + "</color></b>";
            else
                missingGlyphReport += "  " + output_size_label + "<b>" + colorTag2 + m_font_faceInfo.pointSize + "</color></b>";

            missingGlyphReport += "\n" + output_count_label + "<b>" + colorTag + m_font_faceInfo.characterCount + "/" + m_character_Count + "</color></b>";

            // Report missing requested glyph
            missingGlyphReport += "\n\n<color=#ffff00><b>Missing Characters</b></color>";
            missingGlyphReport += "\n----------------------------------------";

            output_feedback = missingGlyphReport;

            for (int i = 0; i < m_character_Count; i++)
            {
                if (m_font_glyphInfo[i].x == -1)
                {
                    missingGlyphReport += "\nID: <color=#C0ffff>" + m_font_glyphInfo[i].id + "\t</color>Hex: <color=#C0ffff>" + m_font_glyphInfo[i].id.ToString("X") + "\t</color>Char [<color=#C0ffff>" + (char)m_font_glyphInfo[i].id + "</color>]";

                    if (missingGlyphReport.Length < 16300)
                        output_feedback = missingGlyphReport;
                }
            }

            if (missingGlyphReport.Length > 16300)
                output_feedback += "\n\n<color=#ffff00>Report truncated.</color>\n<color=#c0ffff>See</color> \"TextMesh Pro\\Glyph Report.txt\"";

            // Save Missing Glyph Report file
            string projectPath = Path.GetFullPath("Assets/..");
            missingGlyphReport = System.Text.RegularExpressions.Regex.Replace(missingGlyphReport, @"<[^>]*>", string.Empty);
            System.IO.File.WriteAllText(projectPath + "/Assets/Glyph Report.txt", missingGlyphReport);

            //GUIUtility.systemCopyBuffer = missingGlyphReport;
        }


        public void CreateFontTexture()
        {
            m_font_Atlas = new Texture2D(font_atlas_width, font_atlas_height, TextureFormat.Alpha8, false, true);

            Color32[] colors = new Color32[font_atlas_width * font_atlas_height];

            for (int i = 0; i < (font_atlas_width * font_atlas_height); i++)
            {
                byte c = m_texture_buffer[i];
                colors[i] = new Color32(c, c, c, c);
            }

            if (font_renderMode == RenderModes.RasterHinted)
                m_font_Atlas.filterMode = FilterMode.Point;

            m_font_Atlas.SetPixels32(colors, 0);
            m_font_Atlas.Apply(false, true);

            // Saving File for Debug
            //var pngData = m_font_Atlas.EncodeToPNG();
            //File.WriteAllBytes("Assets/Textures/Debug Font Texture.png", pngData);	

            //previewSelection = PreviewSelectionTypes.PreviewFont;
        }

        public void GenerateFontAtlas()
        {
            int error_Code;
            m_font_Atlas = null;

            error_Code = TMPro_FontPlugin.Initialize_FontEngine(); // Initialize Font Engine
            if (error_Code != 0)
            {
                if (error_Code == 0xF0)
                {
                    //Debug.Log("Font Library was already initialized!");
                    error_Code = 0;
                }
                else
                    Debug.Log("Error Code: " + error_Code + "  occurred while Initializing the FreeType Library.");
            }

            if (error_Code == 0)
            {
                error_Code = TMPro_FontPlugin.Load_TrueType_Font(font_TTF_path); // Load the selected font.

                if (error_Code != 0)
                {
                    if (error_Code == 0xF1)
                    {
                        //Debug.Log("Font was already loaded!");
                        error_Code = 0;
                    }
                    else
                        Debug.Log("Error Code: " + error_Code + "  occurred while Loading the font.");
                }
            }

            if (error_Code == 0)
            {
                if (FontSizingOption_Selection == 0) font_size = 72; // If Auto set size to 72 pts.

                error_Code = TMPro_FontPlugin.FT_Size_Font(font_size); // Load the selected font and size it accordingly.
                if (error_Code != 0)
                    Debug.Log("Error Code: " + error_Code + "  occurred while Sizing the font.");
            }

            // Define an array containing the characters we will render.
            if (error_Code == 0)
            {
                int[] character_Set = null;
                if (font_CharacterSet_Selection == 7 || font_CharacterSet_Selection == 8)
                {
                    List<int> char_List = new List<int>();

                    for (int i = 0; i < characterSequence.Length; i++)
                    {
                        // Check to make sure we don't include duplicates
                        if (char_List.FindIndex(item => item == characterSequence[i]) == -1)
                            char_List.Add(characterSequence[i]);
                        else
                        {
                            //Debug.Log("Character [" + characterSequence[i] + "] is a duplicate.");
                        }
                    }

                    character_Set = char_List.ToArray();
                }
                else if (font_CharacterSet_Selection == 6)
                {
                    character_Set = ParseHexNumberSequence(characterSequence);
                }
                else
                {
                    character_Set = ParseNumberSequence(characterSequence);
                }

                m_character_Count = character_Set.Length;

                m_texture_buffer = new byte[font_atlas_width * font_atlas_height];

                m_font_faceInfo = new FT_FaceInfo();

                m_font_glyphInfo = new FT_GlyphInfo[m_character_Count];

                int padding = font_padding;

                bool autoSizing = FontSizingOption_Selection == 0 ? true : false;

                float strokeSize = font_style_mod;
                if (font_renderMode == RenderModes.DistanceField16) strokeSize = font_style_mod * 16;
                if (font_renderMode == RenderModes.DistanceField32) strokeSize = font_style_mod * 32;

                isProcessing = true;
                isGenerationCancelled = false;

                error_Code = TMPro_FontPlugin.Render_Characters(m_texture_buffer, font_atlas_width, font_atlas_height, padding, character_Set, m_character_Count, font_style, strokeSize, autoSizing, font_renderMode, (int)m_fontPackingSelection, ref m_font_faceInfo, m_font_glyphInfo);
                isRenderingDone = true;
                Debug.Log("Font Rendering is completed.");
            }
        }


        public TMP_FontAsset Save_Normal_FontAsset()
        {
            //Debug.Log("Creating TextMeshPro font asset!");
            var font_asset = ScriptableObject.CreateInstance<TMP_FontAsset>(); // Create new TextMeshPro Font Asset.

            string tex_FileName = Path.GetFileNameWithoutExtension(font_TTF_path);

            //Set Font Asset Type
            font_asset.fontAssetType = TMP_FontAsset.FontAssetTypes.Bitmap;

            // Reference to the source font file
            //font_asset.sourceFontFile = font_TTF as Font;

            // Add FaceInfo to Font Asset
            FaceInfo face = GetFaceInfo(m_font_faceInfo, 1);
            font_asset.AddFaceInfo(face);

            // Add GlyphInfo[] to Font Asset
            TMP_Glyph[] glyphs = GetGlyphInfo(m_font_glyphInfo, 1);
            font_asset.AddGlyphInfo(glyphs);

            // Get and Add Kerning Pairs to Font Asset
            if (includeKerningPairs)
            {
                KerningTable kerningTable = GetKerningTable(font_TTF_path, (int)face.PointSize);
                font_asset.AddKerningInfo(kerningTable);
            }


            // Add Font Atlas as Sub-Asset
            font_asset.atlas = m_font_Atlas;
            m_font_Atlas.name = tex_FileName + " Atlas";

            // Create new Material and Add it as Sub-Asset
            Shader default_Shader = Shader.Find("TextMeshPro/Bitmap"); // m_shaderSelection;
            Material tmp_material = new Material(default_Shader);
            tmp_material.name = tex_FileName + " Material";
            tmp_material.SetTexture(ShaderUtilities.ID_MainTex, m_font_Atlas);
            font_asset.material = tmp_material;

            font_asset.ReadFontDefinition();

            //// NEED TO GENERATE AN EVENT TO FORCE A REDRAW OF ANY TEXTMESHPRO INSTANCES THAT MIGHT BE USING THIS FONT ASSET
            //TMPro_EventManager.ON_FONT_PROPERTY_CHANGED(true, font_asset);

            return font_asset;
        }


        public TMP_FontAsset Save_SDF_FontAsset()
        {
            Debug.Log("Creating TextMeshPro font asset!");
            var font_asset = ScriptableObject.CreateInstance<TMP_FontAsset>(); // Create new TextMeshPro Font Asset.

            string tex_FileName = Path.GetFileNameWithoutExtension(font_TTF_path);

            // Reference to the source font file
            //font_asset.sourceFontFile = font_TTF as Font;

            //Set Font Asset Type
            font_asset.fontAssetType = TMP_FontAsset.FontAssetTypes.SDF;

            //if (m_destination_Atlas != null)
            //    m_font_Atlas = m_destination_Atlas;

            // If using the C# SDF creation mode, we need the scale down factor.
            int scaleDownFactor = font_renderMode >= RenderModes.DistanceField16 ? 1 : font_scaledownFactor;

            // Add FaceInfo to Font Asset
            FaceInfo face = GetFaceInfo(m_font_faceInfo, scaleDownFactor);
            font_asset.AddFaceInfo(face);

            // Add GlyphInfo[] to Font Asset
            TMP_Glyph[] glyphs = GetGlyphInfo(m_font_glyphInfo, scaleDownFactor);
            font_asset.AddGlyphInfo(glyphs);

            // Get and Add Kerning Pairs to Font Asset
            if (includeKerningPairs)
            {
                KerningTable kerningTable = GetKerningTable(font_TTF_path, (int)face.PointSize);
                font_asset.AddKerningInfo(kerningTable);
            }

            // Add Line Breaking Rules
            //LineBreakingTable lineBreakingTable = new LineBreakingTable();
            //

            // Add Font Atlas as Sub-Asset
            font_asset.atlas = m_font_Atlas;
            m_font_Atlas.name = tex_FileName + " Atlas";

            // Create new Material and Add it as Sub-Asset
            Shader default_Shader = Shader.Find("TextMeshPro/Distance Field"); //m_shaderSelection;
            Material tmp_material = new Material(default_Shader);

            tmp_material.name = tex_FileName + " Material";
            tmp_material.SetTexture(ShaderUtilities.ID_MainTex, m_font_Atlas);
            tmp_material.SetFloat(ShaderUtilities.ID_TextureWidth, m_font_Atlas.width);
            tmp_material.SetFloat(ShaderUtilities.ID_TextureHeight, m_font_Atlas.height);

            int spread = font_padding + 1;
            tmp_material.SetFloat(ShaderUtilities.ID_GradientScale, spread); // Spread = Padding for Brute Force SDF.

            tmp_material.SetFloat(ShaderUtilities.ID_WeightNormal, font_asset.normalStyle);
            tmp_material.SetFloat(ShaderUtilities.ID_WeightBold, font_asset.boldStyle);

            font_asset.material = tmp_material;

            // Saving File for Debug
            //var pngData = destination_Atlas.EncodeToPNG();
            //File.WriteAllBytes("Assets/Textures/Debug Distance Field.png", pngData);	
            //font_asset.fontCreationSettings = SaveFontCreationSettings();

            font_asset.ReadFontDefinition();

            return font_asset;

            // m_font_Atlas = null;

            // NEED TO GENERATE AN EVENT TO FORCE A REDRAW OF ANY TEXTMESHPRO INSTANCES THAT MIGHT BE USING THIS FONT ASSET
            //TMPro_EventManager.ON_FONT_PROPERTY_CHANGED(true, font_asset);
        }


        FontCreationSetting SaveFontCreationSettings()
        {
            FontCreationSetting settings = new FontCreationSetting();
            settings.fontSourcePath = font_TTF_path;
            settings.fontSizingMode = FontSizingOption_Selection;
            settings.fontSize = font_size;
            settings.fontPadding = font_padding;
            settings.fontPackingMode = (int)m_fontPackingSelection;
            settings.fontAtlasWidth = font_atlas_width;
            settings.fontAtlasHeight = font_atlas_height;
            settings.fontCharacterSet = font_CharacterSet_Selection;
            settings.fontStyle = (int)font_style;
            settings.fontStlyeModifier = font_style_mod;
            settings.fontRenderMode = (int)font_renderMode;
            settings.fontKerning = includeKerningPairs;

            return settings;
        }

        // Convert from FT_FaceInfo to FaceInfo
        FaceInfo GetFaceInfo(FT_FaceInfo ft_face, int scaleFactor)
        {
            FaceInfo face = new FaceInfo();

            face.Name = ft_face.name;
            face.PointSize = (float)ft_face.pointSize / scaleFactor;
            face.Padding = ft_face.padding / scaleFactor;
            face.LineHeight = ft_face.lineHeight / scaleFactor;
            face.CapHeight = 0;
            face.Baseline = 0;
            face.Ascender = ft_face.ascender / scaleFactor;
            face.Descender = ft_face.descender / scaleFactor;
            face.CenterLine = ft_face.centerLine / scaleFactor;
            face.Underline = ft_face.underline / scaleFactor;
            face.UnderlineThickness = ft_face.underlineThickness == 0 ? 5 : ft_face.underlineThickness / scaleFactor; // Set Thickness to 5 if TTF value is Zero.
            face.strikethrough = (face.Ascender + face.Descender) / 2.75f;
            face.strikethroughThickness = face.UnderlineThickness;
            face.SuperscriptOffset = face.Ascender;
            face.SubscriptOffset = face.Underline;
            face.SubSize = 0.5f;
            //face.CharacterCount = ft_face.characterCount;
            face.AtlasWidth = ft_face.atlasWidth / scaleFactor;
            face.AtlasHeight = ft_face.atlasHeight / scaleFactor;

            return face;
        }


        // Convert from FT_GlyphInfo[] to GlyphInfo[]
        TMP_Glyph[] GetGlyphInfo(FT_GlyphInfo[] ft_glyphs, int scaleFactor)
        {
            List<TMP_Glyph> glyphs = new List<TMP_Glyph>();
            List<int> kerningSet = new List<int>();

            for (int i = 0; i < ft_glyphs.Length; i++)
            {
                TMP_Glyph g = new TMP_Glyph();

                g.id = ft_glyphs[i].id;
                g.x = ft_glyphs[i].x / scaleFactor;
                g.y = ft_glyphs[i].y / scaleFactor;
                g.width = ft_glyphs[i].width / scaleFactor;
                g.height = ft_glyphs[i].height / scaleFactor;
                g.xOffset = ft_glyphs[i].xOffset / scaleFactor;
                g.yOffset = ft_glyphs[i].yOffset / scaleFactor;
                g.xAdvance = ft_glyphs[i].xAdvance / scaleFactor;

                // Filter out characters with missing glyphs.
                if (g.x == -1)
                    continue;

                glyphs.Add(g);
                kerningSet.Add(g.id);
            }

            m_kerningSet = kerningSet.ToArray();

            return glyphs.ToArray();
        }


        // Get Kerning Pairs
        public KerningTable GetKerningTable(string fontFilePath, int pointSize)
        {
            KerningTable kerningInfo = new KerningTable();
            kerningInfo.kerningPairs = new List<KerningPair>();

            // Temporary Array to hold the kerning pairs from the Native Plug-in.
            FT_KerningPair[] kerningPairs = new FT_KerningPair[7500];

            int kpCount = TMPro_FontPlugin.FT_GetKerningPairs(fontFilePath, m_kerningSet, m_kerningSet.Length, kerningPairs);

            for (int i = 0; i < kpCount; i++)
            {
                // Proceed to add each kerning pairs.
                KerningPair kp = new KerningPair((uint)kerningPairs[i].ascII_Left, (uint)kerningPairs[i].ascII_Right, kerningPairs[i].xAdvanceOffset * pointSize);

                // Filter kerning pairs to avoid duplicates
                int index = kerningInfo.kerningPairs.FindIndex(item => item.firstGlyph == kp.firstGlyph && item.secondGlyph == kp.secondGlyph);

                if (index == -1)
                    kerningInfo.kerningPairs.Add(kp);
                else
                    if (!TMP_Settings.warningsDisabled) Debug.LogWarning("Kerning Key for [" + kp.firstGlyph + "] and [" + kp.secondGlyph + "] is a duplicate.");

            }

            return kerningInfo;
        }
    }
}
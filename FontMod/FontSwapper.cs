using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FontMod;

public class FontSwapper
{
    private static FontSwapper _instance;
    public readonly FontMapper FontMapper;

    public static FontSwapper Instance
    {
        get
        {
            if (_instance == null)
                _instance = new();

            return _instance;
        }
    }

    private FontSwapper()
    {
        FontMapper = new();
        SceneManager.sceneLoaded += SceneManager_sceneLoaded;
        SceneManager.sceneUnloaded += SceneManager_sceneUnloaded;
    }

    private void SceneManager_sceneUnloaded(Scene arg0) => FontMapper.SaveAll();
    private void SceneManager_sceneLoaded(Scene arg0, LoadSceneMode arg1) => FontMapper.SaveAll();

    public void Swap(GameObject gameObject)
    {
        try
        {
            if (gameObject == null)
                throw new ArgumentNullException("Font swap failed due to GameObject being null");

            var texts = gameObject.GetComponentsInChildren<TextMeshProUGUI>();

            for (int i = 0; i < texts.Length; i++)
            {
                var font = FontMapper.GetFontMapped(texts[i].font.name);

                if (font != null)
                {
                    bool isActive = texts[i].gameObject.activeSelf;

                    if (isActive)
                        texts[i].gameObject.SetActive(false);

                    texts[i].m_fontAsset = font.TMP_FontAsset;
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

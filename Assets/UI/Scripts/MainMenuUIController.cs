using UnityEngine;
using UnityEngine.UI;

public class MainMenuUIController : MonoBehaviour
{
    [SerializeField] private GameObject howToPanel;
    [SerializeField] private GameObject optionsPanel;

    private MainMenuStartupScreen startupScreen;
    private Button[] qualityButtons;
    private Text[] qualityButtonTexts;
    private Slider brightnessSlider;
    private Slider soundSlider;
    private Toggle fullscreenToggle;
    private BrightnessOverlay brightnessOverlay;

    private const string KeyQuality = "settings_quality_level";
    private const string KeyBrightness = "settings_brightness";
    private const string KeyVolume = "settings_master_volume";
    private const string KeyFullscreen = "settings_fullscreen";
    private const string KeyOptionsVersion = "settings_options_ui_version";
    private const int OptionsVersion = 2;

    private const int DefaultQuality = 2; // "High" in the Low/Medium/High/Ultra ordering we use
    private const float DefaultBrightness = 1f;
    private const float DefaultVolume = 1f;
    private const int DefaultFullscreen = 0; // 0 = windowed, 1 = fullscreen

    private string[] qualityLabels;

    private void Awake()
    {
        startupScreen = GetComponent<MainMenuStartupScreen>();

        if (howToPanel == null)
        {
            // The panels live under MainMenuPanel (not necessarily under this controller),
            // so fall back to a scene-wide lookup by name.
            howToPanel = transform.Find("HowToPanel") != null ? transform.Find("HowToPanel").gameObject : GameObject.Find("HowToPanel");
        }

        if (optionsPanel == null)
        {
            optionsPanel = transform.Find("OptionsPanel") != null ? transform.Find("OptionsPanel").gameObject : GameObject.Find("OptionsPanel");
        }
    }

    private void Start()
    {
        EnsureOptionsControls();
        ApplySavedSettings();
        HideAll();
    }

    private void EnsurePanels()
    {
        // Important for reliability: the menu panels start inactive in the scene (m_IsActive: 0),
        // so GameObject.Find won't find them. We must include inactive objects.
        if (howToPanel == null)
        {
            Transform child = transform.Find("HowToPanel");
            if (child != null) howToPanel = child.gameObject;
            else howToPanel = FindSceneObjectByNameIncludingInactive("HowToPanel");
        }

        if (optionsPanel == null)
        {
            Transform child = transform.Find("OptionsPanel");
            if (child != null) optionsPanel = child.gameObject;
            else optionsPanel = FindSceneObjectByNameIncludingInactive("OptionsPanel");
        }
    }

    private static GameObject FindSceneObjectByNameIncludingInactive(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;

        Transform[] all = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < all.Length; i++)
        {
            Transform t = all[i];
            if (t == null) continue;
            if (t.name == name) return t.gameObject;
        }

        return null;
    }

    private void HideAll()
    {
        EnsurePanels();
        if (howToPanel != null) howToPanel.SetActive(false);
        if (optionsPanel != null) optionsPanel.SetActive(false);
    }

    public void ShowHowTo()
    {
        HideAll();
        if (howToPanel != null)
        {
            EnsureHowToContents();
            howToPanel.SetActive(true);
            Image panelImage = howToPanel.GetComponent<Image>();
            if (panelImage != null)
            {
                panelImage.color = new Color(0f, 0f, 0f, 1f);
                // Prevent the opaque panel background from blocking the Back button.
                panelImage.raycastTarget = false;
            }
            howToPanel.transform.SetAsLastSibling();
        }
    }

    public void ShowOptions()
    {
        Debug.Log("[MainMenuUIController] ShowOptions clicked");
        HideAll();
        EnsurePanels();
        EnsureOptionsControls();
        Debug.Log($"[MainMenuUIController] optionsPanel={(optionsPanel != null ? optionsPanel.name : "null")} active={(optionsPanel != null ? optionsPanel.activeSelf.ToString() : "n/a")}");
        if (optionsPanel != null)
        {
            optionsPanel.SetActive(true);
            optionsPanel.transform.SetAsLastSibling();
            RefreshUiFromCurrentSettings();
        }
    }

    public void ReturnToMainMenu()
    {
        HideAll();
        if (startupScreen != null)
        {
            startupScreen.ShowMainMenu();
        }
    }

    private void EnsureOptionsControls()
    {
        if (optionsPanel == null)
        {
            EnsurePanels();
        }

        if (optionsPanel == null) return;

        // Make options panel more readable (currently too transparent in your screenshot).
        Image panelImage = optionsPanel.GetComponent<Image>();
        if (panelImage != null)
        {
            // Opaque so it doesn't look "see-through" behind the main menu background.
            panelImage.color = new Color(0f, 0f, 0f, 1f);
            // Avoid blocking clicks on controls that sit on top of this panel.
            panelImage.raycastTarget = false;
        }

        // Hide legacy placeholder body text if present.
        Transform oldBody = optionsPanel.transform.Find("OptionsBody");
        if (oldBody != null)
        {
            oldBody.gameObject.SetActive(false);
        }

        Transform controlsRoot = optionsPanel.transform.Find("OptionsControlsRoot");
        if (controlsRoot == null)
        {
            GameObject rootGO = new GameObject("OptionsControlsRoot", typeof(RectTransform));
            rootGO.transform.SetParent(optionsPanel.transform, false);
            controlsRoot = rootGO.transform;

            RectTransform rrt = rootGO.GetComponent<RectTransform>();
            rrt.anchorMin = new Vector2(0.5f, 0.5f);
            rrt.anchorMax = new Vector2(0.5f, 0.5f);
            rrt.pivot = new Vector2(0.5f, 0.5f);
            rrt.sizeDelta = new Vector2(760f, 360f);
            rrt.anchoredPosition = new Vector2(0f, 10f);
            Debug.Log("[MainMenuUIController] Created OptionsControlsRoot");
        }
        else
        {
            // Make sure it's visible and on top.
            controlsRoot.gameObject.SetActive(true);
        }

        controlsRoot.SetAsLastSibling();

        EnsureQualityButtons(controlsRoot, "GraphicsRow", new Vector2(0f, 110f),
            new[] { "Low", "Medium", "High", "Ultra" });

        brightnessSlider = EnsureSliderRow(controlsRoot, "BrightnessRow", "Brightness", new Vector2(0f, 35f), 0.4f, 1.4f);
        if (brightnessSlider != null)
        {
            brightnessSlider.onValueChanged.RemoveListener(OnBrightnessChanged);
            brightnessSlider.onValueChanged.AddListener(OnBrightnessChanged);
        }

        soundSlider = EnsureSliderRow(controlsRoot, "SoundRow", "Sound Volume", new Vector2(0f, -40f), 0f, 1f);
        if (soundSlider != null)
        {
            soundSlider.onValueChanged.RemoveListener(OnVolumeChanged);
            soundSlider.onValueChanged.AddListener(OnVolumeChanged);
        }

        fullscreenToggle = EnsureToggleRow(controlsRoot, "FullscreenRow", "Fullscreen", new Vector2(0f, -115f));
        if (fullscreenToggle != null)
        {
            fullscreenToggle.onValueChanged.RemoveListener(OnFullscreenChanged);
            fullscreenToggle.onValueChanged.AddListener(OnFullscreenChanged);
        }

        EnsureResetDefaultsButton(controlsRoot);

        Debug.Log("[MainMenuUIController] Options controls ensured");
    }

    private void EnsureHowToContents()
    {
        if (howToPanel == null) return;

        Transform titleT = howToPanel.transform.Find("HowToTitle");
        Text title = titleT != null ? titleT.GetComponent<Text>() : null;
        if (title != null)
        {
            title.text = "How To Play";
        }

        // Nudge title up to avoid overlap with the body text.
        if (titleT != null)
        {
            RectTransform titleRt = titleT.GetComponent<RectTransform>();
            if (titleRt != null)
            {
                Vector2 ap = titleRt.anchoredPosition;
                titleRt.anchoredPosition = new Vector2(ap.x, ap.y + 10f);
            }
        }

        Transform bodyT = howToPanel.transform.Find("HowToBody");
        Text body = bodyT != null ? bodyT.GetComponent<Text>() : null;
        if (body == null)
        {
            Debug.LogWarning("[MainMenuUIController] HowToBody Text component not found.");
            return;
        }

        // Short description + the control list already shown in the scene.
        body.text =
            "Your job on the alien planet is to mine resources, survive enemies, and spend what you extract to unlock upgrades.\n\n" +
            "<size=30><b>Controls:</b></size>\n" +
            "- A/D: Move\n" +
            "- Space: Jump\n" +
            "- Left Click: Shoot\n" +
            "- E: Mine\n" +
            "- X: Extract\n" +
            "- Esc: Pause\n\n" +
            "Tip: Extract before dying to keep resources.";

        // Keep the body rect from overlapping the title area.
        // (The title sits higher, so we reduce font size and keep height modest.)
        RectTransform bodyRt = body.GetComponent<RectTransform>();
        if (bodyRt != null)
        {
            // Keep font readable, but ensure we don't clip the lower control lines.
            body.fontSize = 22;
            body.lineSpacing = 1.1f;
            body.verticalOverflow = VerticalWrapMode.Overflow;
            body.resizeTextForBestFit = false;

            bodyRt.sizeDelta = new Vector2(bodyRt.sizeDelta.x, 300f);

            Vector2 ap = bodyRt.anchoredPosition;
            // Nudge slightly downward to avoid touching the title.
            ap.y = 45f;
            bodyRt.anchoredPosition = ap;
        }
    }

    private void ApplySavedSettings()
    {
        int max = Mathf.Max(0, QualitySettings.names.Length - 1);

        int savedVersion = PlayerPrefs.GetInt(KeyOptionsVersion, 0);
        if (savedVersion != OptionsVersion)
        {
            // Reset to intended defaults once after UI/options behavior changes.
            PlayerPrefs.SetInt(KeyQuality, DefaultQuality);
            PlayerPrefs.SetFloat(KeyBrightness, DefaultBrightness);
            PlayerPrefs.SetFloat(KeyVolume, DefaultVolume);
            PlayerPrefs.SetInt(KeyFullscreen, DefaultFullscreen);
            PlayerPrefs.SetInt(KeyOptionsVersion, OptionsVersion);
            PlayerPrefs.Save();
        }

        bool hasQuality = PlayerPrefs.HasKey(KeyQuality);
        int quality = hasQuality ? PlayerPrefs.GetInt(KeyQuality) : DefaultQuality;
        quality = Mathf.Clamp(quality, 0, max);
        if (!hasQuality) PlayerPrefs.SetInt(KeyQuality, quality);

        bool hasBrightness = PlayerPrefs.HasKey(KeyBrightness);
        float brightness = hasBrightness ? PlayerPrefs.GetFloat(KeyBrightness) : DefaultBrightness;
        if (!hasBrightness) PlayerPrefs.SetFloat(KeyBrightness, brightness);

        bool hasVolume = PlayerPrefs.HasKey(KeyVolume);
        float volume = hasVolume ? PlayerPrefs.GetFloat(KeyVolume) : DefaultVolume;
        if (!hasVolume) PlayerPrefs.SetFloat(KeyVolume, volume);

        bool hasFullscreen = PlayerPrefs.HasKey(KeyFullscreen);
        bool fullscreen = hasFullscreen ? (PlayerPrefs.GetInt(KeyFullscreen) == 1) : (DefaultFullscreen == 1);
        if (!hasFullscreen) PlayerPrefs.SetInt(KeyFullscreen, DefaultFullscreen);

        ApplyGraphics(quality);
        ApplyBrightness(brightness);
        ApplyVolume(volume);
        ApplyFullscreen(fullscreen);
        PlayerPrefs.Save();
        RefreshUiFromCurrentSettings();
    }

    private void RefreshUiFromCurrentSettings()
    {
        int currentQuality = PlayerPrefs.GetInt(KeyQuality, 2);
        int max = Mathf.Max(0, QualitySettings.names.Length - 1);
        int clamped = Mathf.Clamp(currentQuality, 0, max);
        if (qualityButtons != null)
        {
            for (int i = 0; i < qualityButtons.Length; i++)
            {
                if (qualityButtons[i] == null) continue;
                Image img = qualityButtons[i].GetComponent<Image>();
                if (img == null) continue;

                bool isSelected = (i == clamped);
                img.color = isSelected
                    ? new Color(0.16f, 0.42f, 0.36f, 1f)
                    : new Color(0.08f, 0.18f, 0.16f, 0.95f);

                if (qualityButtonTexts != null && i < qualityButtonTexts.Length && qualityButtonTexts[i] != null)
                {
                    Text t = qualityButtonTexts[i];
                    t.color = isSelected ? new Color(0.20f, 1f, 0.95f, 1f) : new Color(0.92f, 0.98f, 1f, 1f);
                    t.fontStyle = FontStyle.Bold;
                    t.resizeTextForBestFit = false;

                    // Clear + re-apply selected marker so it stays deterministic.
                    string baseLabel = (qualityLabels != null && i < qualityLabels.Length) ? qualityLabels[i] : t.text;
                    t.text = isSelected ? ("✓ " + baseLabel) : baseLabel;

                    Outline outline = t.GetComponent<Outline>();
                    if (outline == null) outline = t.gameObject.AddComponent<Outline>();
                    outline.effectColor = isSelected ? new Color(0f, 0f, 0f, 0.95f) : new Color(0f, 0f, 0f, 0.9f);
                    outline.effectDistance = isSelected ? new Vector2(1.2f, -1.2f) : new Vector2(1f, -1f);
                    outline.useGraphicAlpha = true;
                }
            }
        }
        if (brightnessSlider != null) brightnessSlider.SetValueWithoutNotify(PlayerPrefs.GetFloat(KeyBrightness, 1f));
        if (soundSlider != null) soundSlider.SetValueWithoutNotify(AudioListener.volume);
        if (fullscreenToggle != null) fullscreenToggle.SetIsOnWithoutNotify(Screen.fullScreen);
    }

    private void OnQualityClicked(int index)
    {
        Debug.Log($"[MainMenuUIController] Graphics quality clicked index={index}");
        ApplyGraphics(index);
        PlayerPrefs.SetInt(KeyQuality, index);
        PlayerPrefs.Save();
        RefreshUiFromCurrentSettings();
    }

    private void OnBrightnessChanged(float value)
    {
        ApplyBrightness(value);
        PlayerPrefs.SetFloat(KeyBrightness, value);
        PlayerPrefs.Save();
    }

    private void OnVolumeChanged(float value)
    {
        ApplyVolume(value);
        PlayerPrefs.SetFloat(KeyVolume, value);
        PlayerPrefs.Save();
    }

    private void OnFullscreenChanged(bool isFullscreen)
    {
        ApplyFullscreen(isFullscreen);
        PlayerPrefs.SetInt(KeyFullscreen, isFullscreen ? 1 : 0);
        PlayerPrefs.Save();
    }

    private void ApplyGraphics(int qualityLevel)
    {
        int max = Mathf.Max(0, QualitySettings.names.Length - 1);
        int clamped = Mathf.Clamp(qualityLevel, 0, max);
        QualitySettings.SetQualityLevel(clamped, true);
    }

    private void EnsureQualityButtons(Transform parent, string rowName, Vector2 rowPos, string[] labels)
    {
        Transform rowT = parent.Find(rowName);
        GameObject row = rowT != null ? rowT.gameObject : new GameObject(rowName, typeof(RectTransform));
        row.transform.SetParent(parent, false);

        RectTransform rowRT = row.GetComponent<RectTransform>();
        rowRT.anchorMin = new Vector2(0.5f, 0.5f);
        rowRT.anchorMax = new Vector2(0.5f, 0.5f);
        rowRT.pivot = new Vector2(0.5f, 0.5f);
        rowRT.anchoredPosition = rowPos;
        rowRT.sizeDelta = new Vector2(720f, 60f);

        // Label (on the left)
        if (row.transform.Find("Label") == null)
        {
            CreateLabel(row.transform, "Label", "Graphics Quality", new Vector2(-260f, 0f), 22, TextAnchor.MiddleLeft);
        }
        Transform labelT = row.transform.Find("Label");
        if (labelT != null)
        {
            RectTransform lrt = labelT.GetComponent<RectTransform>();
            if (lrt != null)
            {
                lrt.anchoredPosition = new Vector2(-260f, 0f);
                lrt.sizeDelta = new Vector2(240f, 40f);
            }
            Text lt = labelT.GetComponent<Text>();
            if (lt != null)
            {
                lt.fontSize = 22;
                lt.color = new Color(0.92f, 0.98f, 1f, 1f);
            }
        }

        qualityButtons = new Button[4];
        qualityButtonTexts = new Text[4];
        qualityLabels = labels;

        float startX = -140f;
        float stepX = 140f;
        Vector2 btnSize = new Vector2(125f, 50f);

        for (int i = 0; i < 4; i++)
        {
            string btnName = $"QualityBtn{i}";
            Transform existingBtnT = row.transform.Find(btnName);

            GameObject btnGO = existingBtnT != null ? existingBtnT.gameObject : new GameObject(btnName, typeof(RectTransform), typeof(Image), typeof(Button));
            btnGO.transform.SetParent(row.transform, false);

            RectTransform rt = btnGO.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(startX + stepX * i, 0f);
            rt.sizeDelta = btnSize;

            Image img = btnGO.GetComponent<Image>();
            img.color = new Color(0.08f, 0.18f, 0.16f, 0.95f);
            img.raycastTarget = true;
            if (img != null)
            {
                // Make the click area the button image.
                // (Text is raycast-disabled so it won't intercept.)
                Button bt = btnGO.GetComponent<Button>();
                if (bt != null) bt.targetGraphic = img;
            }

            Button b = btnGO.GetComponent<Button>();
            b.targetGraphic = img;
            ColorBlock cb = b.colors;
            cb.normalColor = new Color(0.08f, 0.18f, 0.16f, 0.95f);
            cb.highlightedColor = new Color(0.14f, 0.28f, 0.25f, 1f);
            cb.pressedColor = new Color(0.06f, 0.13f, 0.12f, 1f);
            cb.selectedColor = cb.highlightedColor;
            b.colors = cb;

            Transform textT = btnGO.transform.Find("Label");
            Text txt;
            if (textT != null && textT.GetComponent<Text>() != null)
            {
                txt = textT.GetComponent<Text>();
            }
            else
            {
                GameObject textGO = new GameObject("Label", typeof(RectTransform), typeof(Text));
                textGO.transform.SetParent(btnGO.transform, false);
                txt = textGO.GetComponent<Text>();
                RectTransform trt = txt.GetComponent<RectTransform>();
                trt.anchorMin = Vector2.zero;
                trt.anchorMax = Vector2.one;
                trt.offsetMin = Vector2.zero;
                trt.offsetMax = Vector2.zero;
            }

            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = 18;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = new Color(0.92f, 0.98f, 1f, 1f);
            txt.text = labels != null && i < labels.Length ? labels[i] : $"Q{i}";
            txt.raycastTarget = false; // ensure the hit goes to the button/image, not the label

            // Remove old listeners and re-add, so we don't stack handlers across menu opens.
            b.onClick.RemoveAllListeners();
            int captured = i;
            // SFX on button press.
            if (MainMenuAudioManager.Instance != null)
            {
                MainMenuAudioManager.Instance.AttachClickSound(b);
            }
            b.onClick.AddListener(() => OnQualityClicked(captured));

            qualityButtons[i] = b;
            qualityButtonTexts[i] = txt;
        }

        RefreshUiFromCurrentSettings();
    }

    private void EnsureResetDefaultsButton(Transform controlsRoot)
    {
        if (controlsRoot == null) return;

        string btnName = "ResetDefaultsButton";
        Transform existing = controlsRoot.Find(btnName);
        GameObject btnGO;
        if (existing != null)
        {
            btnGO = existing.gameObject;
        }
        else
        {
            btnGO = new GameObject(btnName, typeof(RectTransform), typeof(Image), typeof(Button));
            btnGO.transform.SetParent(controlsRoot, false);

            RectTransform rt = btnGO.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0f, -170f);
            rt.sizeDelta = new Vector2(520f, 60f);

            Image img = btnGO.GetComponent<Image>();
            img.color = new Color(0.08f, 0.18f, 0.16f, 0.95f);

            Button b = btnGO.GetComponent<Button>();
            ColorBlock cb = b.colors;
            cb.normalColor = img.color;
            cb.highlightedColor = new Color(0.26f, 0.34f, 0.46f, 1f);
            cb.pressedColor = new Color(0.14f, 0.2f, 0.28f, 1f);
            b.colors = cb;
            b.targetGraphic = img;

            GameObject labelGO = new GameObject("Label", typeof(RectTransform), typeof(Text));
            labelGO.transform.SetParent(btnGO.transform, false);
            RectTransform lrt = labelGO.GetComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = Vector2.zero;
            lrt.offsetMax = Vector2.zero;

            Text txt = labelGO.GetComponent<Text>();
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = 24;
            txt.fontStyle = FontStyle.Bold;
            txt.color = new Color(0.92f, 0.98f, 1f, 1f);
            txt.alignment = TextAnchor.MiddleCenter;
            txt.resizeTextForBestFit = false;
            txt.text = "Restore Defaults";
            txt.raycastTarget = false;

            Outline outline = txt.GetComponent<Outline>();
            if (outline == null) outline = txt.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.95f);
            outline.effectDistance = new Vector2(1f, -1f);
            outline.useGraphicAlpha = true;

            Button resetBtn = btnGO.GetComponent<Button>();
            resetBtn.onClick.RemoveAllListeners();
            // SFX on button press.
            if (MainMenuAudioManager.Instance != null)
            {
                MainMenuAudioManager.Instance.AttachClickSound(resetBtn);
            }
            resetBtn.onClick.AddListener(RestoreDefaultOptions);
        }

        btnGO.SetActive(true);
    }

    private void RestoreDefaultOptions()
    {
        int max = Mathf.Max(0, QualitySettings.names.Length - 1);
        int q = Mathf.Clamp(DefaultQuality, 0, max);

        PlayerPrefs.SetInt(KeyQuality, q);
        PlayerPrefs.SetFloat(KeyBrightness, DefaultBrightness);
        PlayerPrefs.SetFloat(KeyVolume, DefaultVolume);
        PlayerPrefs.SetInt(KeyFullscreen, DefaultFullscreen);
        PlayerPrefs.Save();

        ApplyGraphics(q);
        ApplyBrightness(DefaultBrightness);
        ApplyVolume(DefaultVolume);
        ApplyFullscreen(DefaultFullscreen == 1);

        RefreshUiFromCurrentSettings();
    }

    private void ApplyBrightness(float value)
    {
        float clamped = Mathf.Clamp(value, 0.4f, 1.4f);
        if (brightnessOverlay == null)
        {
            BrightnessOverlay existing = FindAnyObjectByType<BrightnessOverlay>();
            brightnessOverlay = existing != null ? existing : BrightnessOverlay.CreatePersistent();
        }
        if (brightnessOverlay != null)
        {
            brightnessOverlay.SetBrightness(clamped);
        }
    }

    private void ApplyVolume(float value)
    {
        AudioListener.volume = Mathf.Clamp01(value);
    }

    private void ApplyFullscreen(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
    }

    private Text CreateLabel(Transform parent, string name, string text, Vector2 pos, int fontSize, TextAnchor align)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(300f, 40f);

        Text t = go.GetComponent<Text>();
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = fontSize;
        t.fontStyle = FontStyle.Bold;
        t.color = Color.white;
        t.alignment = align;
        t.text = text;

        // Outline-only style so text remains crisp over the bioluminescent background.
        Outline outline = t.GetComponent<Outline>();
        if (outline == null) outline = t.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.95f);
        outline.effectDistance = new Vector2(1f, -1f);
        outline.useGraphicAlpha = true;

        Shadow shadow = t.GetComponent<Shadow>();
        if (shadow != null) shadow.enabled = false;

        // Row labels should never block clicks (prevents overlap issues).
        t.raycastTarget = false;

        return t;
    }

    private Dropdown EnsureDropdownRow(Transform parent, string rowName, string label, Vector2 rowPos, string[] options)
    {
        Transform rowT = parent.Find(rowName);
        GameObject row = rowT != null ? rowT.gameObject : new GameObject(rowName, typeof(RectTransform));
        row.transform.SetParent(parent, false);

        RectTransform rowRT = row.GetComponent<RectTransform>();
        rowRT.anchorMin = new Vector2(0.5f, 0.5f);
        rowRT.anchorMax = new Vector2(0.5f, 0.5f);
        rowRT.pivot = new Vector2(0.5f, 0.5f);
        rowRT.anchoredPosition = rowPos;
        rowRT.sizeDelta = new Vector2(720f, 60f);

        if (row.transform.Find("Label") == null)
        {
            CreateLabel(row.transform, "Label", label, new Vector2(-220f, 0f), 24, TextAnchor.MiddleLeft);
        }

        Dropdown dd = row.GetComponentInChildren<Dropdown>(true);
        if (dd == null)
        {
            GameObject ddGO = new GameObject("Dropdown", typeof(RectTransform), typeof(Image), typeof(Dropdown));
            ddGO.transform.SetParent(row.transform, false);
            RectTransform drt = ddGO.GetComponent<RectTransform>();
            drt.anchorMin = new Vector2(0.5f, 0.5f);
            drt.anchorMax = new Vector2(0.5f, 0.5f);
            drt.pivot = new Vector2(0.5f, 0.5f);
            drt.anchoredPosition = new Vector2(160f, 0f);
            drt.sizeDelta = new Vector2(280f, 48f);

            Image bg = ddGO.GetComponent<Image>();
            bg.color = new Color(0.10f, 0.18f, 0.17f, 0.95f);

            dd = ddGO.GetComponent<Dropdown>();

            Text caption = CreateLabel(ddGO.transform, "Label", "", Vector2.zero, 22, TextAnchor.MiddleCenter);
            RectTransform crt = caption.GetComponent<RectTransform>();
            crt.anchorMin = Vector2.zero;
            crt.anchorMax = Vector2.one;
            crt.offsetMin = new Vector2(12f, 4f);
            crt.offsetMax = new Vector2(-24f, -4f);
            dd.captionText = caption;

            // Minimal template setup (kept hidden); enough for interactable dropdown selection.
            GameObject templateGO = new GameObject("Template", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
            templateGO.transform.SetParent(ddGO.transform, false);
            RectTransform trt = templateGO.GetComponent<RectTransform>();
            trt.anchorMin = new Vector2(0f, 0f);
            trt.anchorMax = new Vector2(1f, 0f);
            trt.pivot = new Vector2(0.5f, 1f);
            trt.anchoredPosition = new Vector2(0f, -50f);
            trt.sizeDelta = new Vector2(0f, 160f);
            templateGO.SetActive(false);

            Image templateImg = templateGO.GetComponent<Image>();
            templateImg.color = new Color(0.08f, 0.14f, 0.13f, 0.98f);

            GameObject viewportGO = new GameObject("Viewport", typeof(RectTransform), typeof(Mask), typeof(Image));
            viewportGO.transform.SetParent(templateGO.transform, false);
            RectTransform vrt = viewportGO.GetComponent<RectTransform>();
            vrt.anchorMin = Vector2.zero;
            vrt.anchorMax = Vector2.one;
            vrt.offsetMin = Vector2.zero;
            vrt.offsetMax = Vector2.zero;
            Image vimg = viewportGO.GetComponent<Image>();
            vimg.color = new Color(1f, 1f, 1f, 0.1f);
            viewportGO.GetComponent<Mask>().showMaskGraphic = false;

            GameObject contentGO = new GameObject("Content", typeof(RectTransform));
            contentGO.transform.SetParent(viewportGO.transform, false);
            RectTransform cct = contentGO.GetComponent<RectTransform>();
            cct.anchorMin = new Vector2(0f, 1f);
            cct.anchorMax = new Vector2(1f, 1f);
            cct.pivot = new Vector2(0.5f, 1f);
            cct.anchoredPosition = Vector2.zero;
            cct.sizeDelta = new Vector2(0f, 28f);

            GameObject itemGO = new GameObject("Item", typeof(RectTransform), typeof(Toggle));
            itemGO.transform.SetParent(contentGO.transform, false);
            RectTransform irt = itemGO.GetComponent<RectTransform>();
            irt.anchorMin = new Vector2(0f, 0.5f);
            irt.anchorMax = new Vector2(1f, 0.5f);
            irt.pivot = new Vector2(0.5f, 0.5f);
            irt.sizeDelta = new Vector2(0f, 28f);

            GameObject itemBgGO = new GameObject("Item Background", typeof(RectTransform), typeof(Image));
            itemBgGO.transform.SetParent(itemGO.transform, false);
            RectTransform ibrt = itemBgGO.GetComponent<RectTransform>();
            ibrt.anchorMin = Vector2.zero;
            ibrt.anchorMax = Vector2.one;
            ibrt.offsetMin = Vector2.zero;
            ibrt.offsetMax = Vector2.zero;
            itemBgGO.GetComponent<Image>().color = new Color(0.12f, 0.22f, 0.20f, 0.95f);

            GameObject itemCheckGO = new GameObject("Item Checkmark", typeof(RectTransform), typeof(Image));
            itemCheckGO.transform.SetParent(itemBgGO.transform, false);
            RectTransform icrt = itemCheckGO.GetComponent<RectTransform>();
            icrt.anchorMin = new Vector2(0f, 0.5f);
            icrt.anchorMax = new Vector2(0f, 0.5f);
            icrt.pivot = new Vector2(0.5f, 0.5f);
            icrt.anchoredPosition = new Vector2(12f, 0f);
            icrt.sizeDelta = new Vector2(16f, 16f);
            itemCheckGO.GetComponent<Image>().color = new Color(0.22f, 0.95f, 1f, 1f);

            Text itemLabel = CreateLabel(itemGO.transform, "Item Label", "Option", new Vector2(20f, 0f), 20, TextAnchor.MiddleLeft);
            RectTransform ilrt = itemLabel.GetComponent<RectTransform>();
            ilrt.anchorMin = Vector2.zero;
            ilrt.anchorMax = Vector2.one;
            ilrt.offsetMin = new Vector2(28f, 0f);
            ilrt.offsetMax = new Vector2(-8f, 0f);

            Toggle itemToggle = itemGO.GetComponent<Toggle>();
            itemToggle.targetGraphic = itemBgGO.GetComponent<Image>();
            itemToggle.graphic = itemCheckGO.GetComponent<Image>();

            ScrollRect sr = templateGO.GetComponent<ScrollRect>();
            sr.content = cct;
            sr.viewport = vrt;
            sr.horizontal = false;

            dd.template = trt;
            dd.itemText = itemLabel;
            dd.options = new System.Collections.Generic.List<Dropdown.OptionData>();
        }

        dd.ClearOptions();
        dd.AddOptions(new System.Collections.Generic.List<string>(options));
        return dd;
    }

    private Slider EnsureSliderRow(Transform parent, string rowName, string label, Vector2 rowPos, float min, float max)
    {
        Transform rowT = parent.Find(rowName);
        GameObject row = rowT != null ? rowT.gameObject : new GameObject(rowName, typeof(RectTransform));
        row.transform.SetParent(parent, false);

        RectTransform rowRT = row.GetComponent<RectTransform>();
        rowRT.anchorMin = new Vector2(0.5f, 0.5f);
        rowRT.anchorMax = new Vector2(0.5f, 0.5f);
        rowRT.pivot = new Vector2(0.5f, 0.5f);
        rowRT.anchoredPosition = rowPos;
        rowRT.sizeDelta = new Vector2(720f, 60f);

        if (row.transform.Find("Label") == null)
        {
            CreateLabel(row.transform, "Label", label, new Vector2(-220f, 0f), 24, TextAnchor.MiddleLeft);
        }

        Slider slider = row.GetComponentInChildren<Slider>(true);
        if (slider == null)
        {
            GameObject sliderGO = new GameObject("Slider", typeof(RectTransform), typeof(Slider));
            sliderGO.transform.SetParent(row.transform, false);
            RectTransform srt = sliderGO.GetComponent<RectTransform>();
            srt.anchorMin = new Vector2(0.5f, 0.5f);
            srt.anchorMax = new Vector2(0.5f, 0.5f);
            srt.pivot = new Vector2(0.5f, 0.5f);
            srt.anchoredPosition = new Vector2(160f, 0f);
            srt.sizeDelta = new Vector2(280f, 24f);

            GameObject bgGO = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bgGO.transform.SetParent(sliderGO.transform, false);
            RectTransform brt = bgGO.GetComponent<RectTransform>();
            brt.anchorMin = Vector2.zero;
            brt.anchorMax = Vector2.one;
            brt.offsetMin = Vector2.zero;
            brt.offsetMax = Vector2.zero;
            bgGO.GetComponent<Image>().color = new Color(0.14f, 0.2f, 0.2f, 0.95f);

            GameObject fillAreaGO = new GameObject("Fill Area", typeof(RectTransform));
            fillAreaGO.transform.SetParent(sliderGO.transform, false);
            RectTransform fart = fillAreaGO.GetComponent<RectTransform>();
            fart.anchorMin = Vector2.zero;
            fart.anchorMax = Vector2.one;
            fart.offsetMin = new Vector2(6f, 6f);
            fart.offsetMax = new Vector2(-6f, -6f);

            GameObject fillGO = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fillGO.transform.SetParent(fillAreaGO.transform, false);
            RectTransform frt = fillGO.GetComponent<RectTransform>();
            frt.anchorMin = new Vector2(0f, 0f);
            frt.anchorMax = new Vector2(1f, 1f);
            frt.offsetMin = Vector2.zero;
            frt.offsetMax = Vector2.zero;
            fillGO.GetComponent<Image>().color = new Color(0.22f, 0.95f, 1f, 0.95f);

            GameObject handleGO = new GameObject("Handle", typeof(RectTransform), typeof(Image));
            handleGO.transform.SetParent(sliderGO.transform, false);
            RectTransform hrt = handleGO.GetComponent<RectTransform>();
            hrt.sizeDelta = new Vector2(18f, 30f);
            handleGO.GetComponent<Image>().color = new Color(0.92f, 0.98f, 1f, 1f);

            slider = sliderGO.GetComponent<Slider>();
            slider.minValue = min;
            slider.maxValue = max;
            slider.targetGraphic = handleGO.GetComponent<Image>();
            slider.fillRect = frt;
            slider.handleRect = hrt;
            slider.direction = Slider.Direction.LeftToRight;
        }

        slider.minValue = min;
        slider.maxValue = max;
        return slider;
    }

    private Toggle EnsureToggleRow(Transform parent, string rowName, string label, Vector2 rowPos)
    {
        Transform rowT = parent.Find(rowName);
        GameObject row = rowT != null ? rowT.gameObject : new GameObject(rowName, typeof(RectTransform));
        row.transform.SetParent(parent, false);

        RectTransform rowRT = row.GetComponent<RectTransform>();
        rowRT.anchorMin = new Vector2(0.5f, 0.5f);
        rowRT.anchorMax = new Vector2(0.5f, 0.5f);
        rowRT.pivot = new Vector2(0.5f, 0.5f);
        rowRT.anchoredPosition = rowPos;
        rowRT.sizeDelta = new Vector2(720f, 60f);

        if (row.transform.Find("Label") == null)
        {
            CreateLabel(row.transform, "Label", label, new Vector2(-220f, 0f), 24, TextAnchor.MiddleLeft);
        }

        Toggle toggle = row.GetComponentInChildren<Toggle>(true);
        if (toggle == null)
        {
            GameObject toggleGO = new GameObject("Toggle", typeof(RectTransform), typeof(Toggle));
            toggleGO.transform.SetParent(row.transform, false);
            RectTransform trt = toggleGO.GetComponent<RectTransform>();
            trt.anchorMin = new Vector2(0.5f, 0.5f);
            trt.anchorMax = new Vector2(0.5f, 0.5f);
            trt.pivot = new Vector2(0.5f, 0.5f);
            trt.anchoredPosition = new Vector2(160f, 0f);
            trt.sizeDelta = new Vector2(40f, 40f);

            GameObject bgGO = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bgGO.transform.SetParent(toggleGO.transform, false);
            RectTransform brt = bgGO.GetComponent<RectTransform>();
            brt.anchorMin = Vector2.zero;
            brt.anchorMax = Vector2.one;
            brt.offsetMin = Vector2.zero;
            brt.offsetMax = Vector2.zero;
            Image bgImg = bgGO.GetComponent<Image>();
            bgImg.color = new Color(0.10f, 0.18f, 0.17f, 0.95f);

            GameObject checkGO = new GameObject("Checkmark", typeof(RectTransform), typeof(Image));
            checkGO.transform.SetParent(bgGO.transform, false);
            RectTransform crt = checkGO.GetComponent<RectTransform>();
            crt.anchorMin = new Vector2(0.5f, 0.5f);
            crt.anchorMax = new Vector2(0.5f, 0.5f);
            crt.pivot = new Vector2(0.5f, 0.5f);
            crt.sizeDelta = new Vector2(24f, 24f);
            Image cimg = checkGO.GetComponent<Image>();
            cimg.color = new Color(0.22f, 0.95f, 1f, 1f);

            toggle = toggleGO.GetComponent<Toggle>();
            toggle.targetGraphic = bgImg;
            toggle.graphic = cimg;
        }

        return toggle;
    }
}

public class BrightnessOverlay : MonoBehaviour
{
    private Image overlayImage;

    public static BrightnessOverlay CreatePersistent()
    {
        GameObject go = new GameObject("BrightnessOverlay", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(BrightnessOverlay));
        DontDestroyOnLoad(go);

        Canvas canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9999;

        CanvasScaler scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        BrightnessOverlay overlay = go.GetComponent<BrightnessOverlay>();
        overlay.EnsureImage();
        return overlay;
    }

    private void Awake()
    {
        EnsureImage();
    }

    private void EnsureImage()
    {
        if (overlayImage != null) return;

        Transform t = transform.Find("Overlay");
        GameObject overlayGO;
        if (t != null)
        {
            overlayGO = t.gameObject;
        }
        else
        {
            overlayGO = new GameObject("Overlay", typeof(RectTransform), typeof(Image));
            overlayGO.transform.SetParent(transform, false);
        }

        RectTransform rt = overlayGO.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        overlayImage = overlayGO.GetComponent<Image>();
        overlayImage.raycastTarget = false;
        overlayImage.color = new Color(0f, 0f, 0f, 0f);
    }

    public void SetBrightness(float brightness)
    {
        EnsureImage();
        float b = Mathf.Clamp(brightness, 0.4f, 1.4f);
        if (b < 1f)
        {
            overlayImage.color = new Color(0f, 0f, 0f, 1f - b);
        }
        else
        {
            overlayImage.color = new Color(1f, 1f, 1f, Mathf.Clamp01((b - 1f) * 0.55f));
        }
    }
}


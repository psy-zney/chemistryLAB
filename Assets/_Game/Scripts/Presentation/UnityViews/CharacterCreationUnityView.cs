using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ChemistryLab.Domain;

namespace ChemistryLab.Presentation.UnityViews
{
    /// <summary>
    /// Unity view for the Facebook-style Modular Character Customization Screen.
    /// Handles visual avatar layering (skin tone, hair, outfit, goggles) and name entry.
    /// Loads Sprite assets automatically from Resources/Avatar/.
    /// </summary>
    public sealed class CharacterCreationUnityView : MonoBehaviour
    {
        [Header("Avatar Preview Layers (Stacked in Z-order)")]
        [SerializeField] private Image skinLayerImage;
        [SerializeField] private Image hairLayerImage;
        [SerializeField] private Image outfitLayerImage;
        [SerializeField] private Image glassesLayerImage;

        [Header("UI Input Controls")]
        [SerializeField] private TMP_InputField nameInputField;
        [SerializeField] private Button cycleHairButton;
        [SerializeField] private Button cycleHairColorButton;
        [SerializeField] private Button cycleSkinButton;
        [SerializeField] private Button cycleOutfitButton;
        [SerializeField] private Button cycleGlassesButton;
        [SerializeField] private Button confirmCharacterButton;

        [Header("Display Text")]
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text currentHairLabel;
        [SerializeField] private TMP_Text currentOutfitLabel;
        [SerializeField] private TMP_Text currentGlassesLabel;

        // Customization Catalog Lists
        private readonly string[] skinTones = { "#FFDBAC", "#F1C27D", "#E0AC69", "#C68642", "#8D5524" };
        private readonly string[] hairColors = { "#1A1A1A", "#4A2E10", "#8D5524", "#E6C875", "#A52A2A", "#1E90FF" };
        private readonly string[] hairStyles = { "hair_short", "hair_curly", "hair_bun", "hair_ponytail", "hair_scientist" };
        private readonly string[] outfits = { "outfit_labcoat", "outfit_hazmat", "outfit_casual", "outfit_professor" };
        private readonly string[] glasses = { "goggles_safety", "glasses_round", "glasses_nerd", "none" };

        private int skinIndex;
        private int hairColorIndex;
        private int hairStyleIndex;
        private int outfitIndex;
        private int glassesIndex;

        public event Action<AvatarData> CharacterConfirmed;

        private void Awake()
        {
            AutoSetupLayout();
            HookEvents();
            LoadAvatarSprites();
            RefreshAvatarPreview();
        }

        private void HookEvents()
        {
            if (cycleSkinButton != null) cycleSkinButton.onClick.AddListener(OnCycleSkin);
            if (cycleHairButton != null) cycleHairButton.onClick.AddListener(OnCycleHairStyle);
            if (cycleHairColorButton != null) cycleHairColorButton.onClick.AddListener(OnCycleHairColor);
            if (cycleOutfitButton != null) cycleOutfitButton.onClick.AddListener(OnCycleOutfit);
            if (cycleGlassesButton != null) cycleGlassesButton.onClick.AddListener(OnCycleGlasses);
            if (confirmCharacterButton != null) confirmCharacterButton.onClick.AddListener(OnConfirmCharacter);
        }

        private void LoadAvatarSprites()
        {
            // Load character base body sprite
            var bodySprite = Resources.Load<Sprite>("Avatar/body_base");
            if (skinLayerImage != null && bodySprite != null)
            {
                skinLayerImage.sprite = bodySprite;
            }

            // Load lab coat outfit sprite
            var outfitSprite = Resources.Load<Sprite>("Avatar/outfit_labcoat");
            if (outfitLayerImage != null && outfitSprite != null)
            {
                outfitLayerImage.sprite = outfitSprite;
            }

            // Load safety goggles sprite
            var gogglesSprite = Resources.Load<Sprite>("Avatar/goggles_safety");
            if (glassesLayerImage != null && gogglesSprite != null)
            {
                glassesLayerImage.sprite = gogglesSprite;
            }
        }

        private void OnCycleSkin()
        {
            skinIndex = (skinIndex + 1) % skinTones.Length;
            RefreshAvatarPreview();
        }

        private void OnCycleHairStyle()
        {
            hairStyleIndex = (hairStyleIndex + 1) % hairStyles.Length;
            RefreshAvatarPreview();
        }

        private void OnCycleHairColor()
        {
            hairColorIndex = (hairColorIndex + 1) % hairColors.Length;
            RefreshAvatarPreview();
        }

        private void OnCycleOutfit()
        {
            outfitIndex = (outfitIndex + 1) % outfits.Length;
            RefreshAvatarPreview();
        }

        private void OnCycleGlasses()
        {
            glassesIndex = (glassesIndex + 1) % glasses.Length;
            RefreshAvatarPreview();
        }

        public AvatarData GetCurrentAvatarData()
        {
            var playerName = nameInputField != null && !string.IsNullOrWhiteSpace(nameInputField.text)
                ? nameInputField.text.Trim()
                : "Dr. Chemist";

            return new AvatarData(
                playerName,
                skinTones[skinIndex],
                hairStyles[hairStyleIndex],
                hairColors[hairColorIndex],
                outfits[outfitIndex],
                glasses[glassesIndex]);
        }

        private void OnConfirmCharacter()
        {
            var avatar = GetCurrentAvatarData();
            Debug.Log(string.Format("[CharacterCreationView] Character Confirmed! Name: {0}, Skin: {1}, Hair: {2}, Outfit: {3}",
                avatar.PlayerName, avatar.SkinColorHex, avatar.HairStyleId, avatar.OutfitId));

            var handler = CharacterConfirmed;
            if (handler != null)
            {
                handler(avatar);
            }

            // Close creation panel to reveal the main lab
            gameObject.SetActive(false);
        }

        public void RefreshAvatarPreview()
        {
            // Apply Skin Tint
            if (skinLayerImage != null && ColorUtility.TryParseHtmlString(skinTones[skinIndex], out Color skinCol))
            {
                skinLayerImage.color = skinCol;
            }

            // Apply Hair Color & Style Label
            if (hairLayerImage != null && ColorUtility.TryParseHtmlString(hairColors[hairColorIndex], out Color hairCol))
            {
                hairLayerImage.color = hairCol;
            }

            if (currentHairLabel != null) currentHairLabel.text = "Toc: " + hairStyles[hairStyleIndex];
            if (currentOutfitLabel != null) currentOutfitLabel.text = "Trang Phuc: " + outfits[outfitIndex];
            if (currentGlassesLabel != null) currentGlassesLabel.text = "Kinh Lab: " + glasses[glassesIndex];
        }

        [ContextMenu("Auto Setup Layout")]
        public void AutoSetupLayout()
        {
            var panelImage = GetComponent<Image>();
            if (panelImage != null)
            {
                panelImage.color = new Color(0.08f, 0.1f, 0.16f, 0.98f);
            }

            // Title
            if (titleText != null)
            {
                SetRect(titleText.rectTransform, new Vector2(0, 220), new Vector2(600, 45));
                titleText.text = "TAO NHAN VAT NHA KHOA HOC";
                titleText.fontSize = 26;
                titleText.color = Color.cyan;
                titleText.alignment = TextAlignmentOptions.Center;
            }

            // Name Input Field
            if (nameInputField != null)
            {
                SetRect(nameInputField.GetComponent<RectTransform>(), new Vector2(0, 160), new Vector2(320, 40));
            }

            // Avatar Preview Layers on the Left (X: -180)
            Vector2 avatarPos = new Vector2(-180, -20);
            Vector2 avatarSize = new Vector2(220, 280);

            SetLayerImage(skinLayerImage, avatarPos, avatarSize, new Color(0.95f, 0.8f, 0.65f));
            SetLayerImage(hairLayerImage, avatarPos, avatarSize, new Color(0.2f, 0.15f, 0.1f));
            SetLayerImage(outfitLayerImage, avatarPos, avatarSize, Color.white);
            SetLayerImage(glassesLayerImage, avatarPos, avatarSize, Color.cyan);

            // Customization Buttons Column on the Right (X: +160)
            SetButton(cycleSkinButton, new Vector2(160, 100), "1. Doi Mau Da", new Color(0.8f, 0.5f, 0.3f));
            SetButton(cycleHairButton, new Vector2(160, 45), "2. Doi Kieu Toc", new Color(0.3f, 0.6f, 0.8f));
            SetButton(cycleHairColorButton, new Vector2(160, -10), "3. Nhuom Mau Toc", new Color(0.6f, 0.3f, 0.8f));
            SetButton(cycleOutfitButton, new Vector2(160, -65), "4. Doi Trang Phuc", new Color(0.3f, 0.8f, 0.5f));
            SetButton(cycleGlassesButton, new Vector2(160, -120), "5. Doi Kinh Lab", new Color(0.8f, 0.7f, 0.2f));

            SetButton(confirmCharacterButton, new Vector2(160, -195), "XAC NHAN & VAO GAME", new Color(0.1f, 0.7f, 0.3f));
        }

        private static void SetRect(RectTransform rt, Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            if (rt == null) return;
            rt.anchoredPosition = anchoredPosition;
            rt.sizeDelta = sizeDelta;
        }

        private static void SetLayerImage(Image img, Vector2 pos, Vector2 size, Color defaultColor)
        {
            if (img == null) return;
            SetRect(img.rectTransform, pos, size);
            if (img.sprite == null)
            {
                img.color = defaultColor;
            }
        }

        private static void SetButton(Button btn, Vector2 position, string label, Color btnColor)
        {
            if (btn == null) return;
            var rt = btn.GetComponent<RectTransform>();
            SetRect(rt, position, new Vector2(200, 45));

            var img = btn.GetComponent<Image>();
            if (img != null) img.color = btnColor;

            var txt = btn.GetComponentInChildren<TMP_Text>();
            if (txt != null)
            {
                txt.text = label;
                txt.fontSize = 15;
                txt.color = Color.white;
            }
        }
    }
}

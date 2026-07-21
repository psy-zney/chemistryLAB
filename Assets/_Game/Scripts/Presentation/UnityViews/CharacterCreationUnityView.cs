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

            if (currentHairLabel != null) currentHairLabel.text = "Tóc: " + hairStyles[hairStyleIndex];
            if (currentOutfitLabel != null) currentOutfitLabel.text = "Trang Phục: " + outfits[outfitIndex];
            if (currentGlassesLabel != null) currentGlassesLabel.text = "Kính Lab: " + glasses[glassesIndex];
        }

        /// <summary>
        /// Auto-arranges UI elements neatly on Canvas for instant testing.
        /// </summary>
        public void AutoSetupLayout()
        {
            var panelImage = GetComponent<Image>();
            if (panelImage != null)
            {
                panelImage.color = new Color(0.1f, 0.12f, 0.18f, 0.95f);
            }

            if (titleText != null)
            {
                titleText.text = "TẠO NHÂN VẬT NHÀ KHÓA HỌC";
                titleText.fontSize = 28;
                titleText.color = Color.cyan;
                titleText.alignment = TextAlignmentOptions.Center;
            }
        }
    }
}

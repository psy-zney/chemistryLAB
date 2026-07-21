using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ChemistryLab.Domain;

namespace ChemistryLab.Presentation.UnityViews
{
    /// <summary>
    /// Unity view adapter for the main Lobby/Home screen, matching the user's hand-drawn sketch:
    /// - Top bar: Dollars ($), Diamonds, Settings
    /// - Left column buttons: Shop, Kho (Inventory), N.Vu (Quests), N.Vat (Character Customizer)
    /// - Center: Scientist Avatar & Lobby desk preview
    /// - Bottom right: [ LAB ] (Quest Campaign Mode) and [ SANDBOX ] (Unlimited Free Creative Mode)
    /// </summary>
    public sealed class LobbyHomeUnityView : MonoBehaviour
    {
        [Header("Top Currency Header")]
        [SerializeField] private TMP_Text dollarsText;
        [SerializeField] private TMP_Text diamondsText;
        [SerializeField] private Button settingsButton;

        [Header("Left Navigation Buttons (Matching sketch)")]
        [SerializeField] private Button shopButton;
        [SerializeField] private Button inventoryButton;
        [SerializeField] private Button questButton;
        [SerializeField] private Button characterButton;

        [Header("Main Entry Actions")]
        [SerializeField] private Button enterLabButton;
        [SerializeField] private Button sandboxModeButton;

        [Header("Child Panels")]
        [SerializeField] private GameObject characterCreationPanel;
        [SerializeField] private GameObject mainLabPanel;

        private void Awake()
        {
            AutoSetupLayout();
            HookEvents();
        }

        private void HookEvents()
        {
            if (enterLabButton != null) enterLabButton.onClick.AddListener(OnEnterLabClicked);
            if (sandboxModeButton != null) sandboxModeButton.onClick.AddListener(OnEnterSandboxClicked);
            if (characterButton != null) characterButton.onClick.AddListener(OnCharacterClicked);
            if (shopButton != null) shopButton.onClick.AddListener(OnShopClicked);
            if (inventoryButton != null) inventoryButton.onClick.AddListener(OnInventoryClicked);
            if (questButton != null) questButton.onClick.AddListener(OnQuestClicked);
        }

        private void OnEnterLabClicked()
        {
            Debug.Log("[LobbyHomeView] Entering Main Lab Campaign Mode...");
            if (mainLabPanel != null)
            {
                mainLabPanel.SetActive(true);
            }
            gameObject.SetActive(false);
        }

        private void OnEnterSandboxClicked()
        {
            Debug.Log("[LobbyHomeView] Entering Free Creative Sandbox Mode (Unlimited Chemicals)...");
            if (mainLabPanel != null)
            {
                mainLabPanel.SetActive(true);
            }
            gameObject.SetActive(false);
        }

        private void OnCharacterClicked()
        {
            Debug.Log("[LobbyHomeView] Opening Character Customizer...");
            if (characterCreationPanel != null)
            {
                characterCreationPanel.SetActive(true);
            }
        }

        private void OnShopClicked()
        {
            Debug.Log("[LobbyHomeView] Opening Shop...");
        }

        private void OnInventoryClicked()
        {
            Debug.Log("[LobbyHomeView] Opening Kho / Inventory...");
        }

        private void OnQuestClicked()
        {
            Debug.Log("[LobbyHomeView] Opening Nhiem Vu / Quests...");
        }

        [ContextMenu("Auto Setup Layout")]
        public void AutoSetupLayout()
        {
            var panelImage = GetComponent<Image>();
            if (panelImage != null)
            {
                panelImage.color = new Color(0.1f, 0.13f, 0.2f, 0.98f);
            }

            // Top Header: Dollars ($), Diamonds, Settings
            if (dollarsText != null)
            {
                SetRect(dollarsText.rectTransform, new Vector2(100, 230), new Vector2(160, 40));
                dollarsText.text = "500 $";
                dollarsText.fontSize = 18;
                dollarsText.color = Color.green;
            }

            if (diamondsText != null)
            {
                SetRect(diamondsText.rectTransform, new Vector2(270, 230), new Vector2(160, 40));
                diamondsText.text = "Diamond: 100";
                diamondsText.fontSize = 18;
                diamondsText.color = Color.cyan;
            }

            SetButton(settingsButton, new Vector2(370, 230), "SET", new Color(0.3f, 0.3f, 0.4f), new Vector2(45, 40));

            // Left Column Buttons: Shop, Kho, N.Vu, N.Vat (X: -320)
            SetButton(shopButton, new Vector2(-320, 140), "Shop", new Color(0.8f, 0.4f, 0.2f));
            SetButton(inventoryButton, new Vector2(-320, 70), "Kho", new Color(0.2f, 0.6f, 0.8f));
            SetButton(questButton, new Vector2(-320, 0), "N. Vu", new Color(0.7f, 0.6f, 0.2f));
            SetButton(characterButton, new Vector2(-320, -70), "N. Vat", new Color(0.6f, 0.3f, 0.7f));

            // Bottom Right Entry Buttons: LAB (Campaign) & SANDBOX (Creative Free Mode)
            SetButton(enterLabButton, new Vector2(180, -190), "L A B", new Color(0.15f, 0.75f, 0.35f), new Vector2(150, 60));
            SetButton(sandboxModeButton, new Vector2(340, -190), "SANDBOX", new Color(0.85f, 0.45f, 0.15f), new Vector2(150, 60));

            var labTxt = enterLabButton != null ? enterLabButton.GetComponentInChildren<TMP_Text>() : null;
            if (labTxt != null)
            {
                labTxt.fontSize = 22;
                labTxt.fontStyle = FontStyles.Bold;
            }

            var sandboxTxt = sandboxModeButton != null ? sandboxModeButton.GetComponentInChildren<TMP_Text>() : null;
            if (sandboxTxt != null)
            {
                sandboxTxt.fontSize = 20;
                sandboxTxt.fontStyle = FontStyles.Bold;
            }
        }

        private static void SetRect(RectTransform rt, Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            if (rt == null) return;
            rt.anchoredPosition = anchoredPosition;
            rt.sizeDelta = sizeDelta;
        }

        private static void SetButton(Button btn, Vector2 position, string label, Color btnColor, Vector2? size = null)
        {
            if (btn == null) return;
            var rt = btn.GetComponent<RectTransform>();
            SetRect(rt, position, size ?? new Vector2(160, 50));

            var img = btn.GetComponent<Image>();
            if (img != null) img.color = btnColor;

            var txt = btn.GetComponentInChildren<TMP_Text>();
            if (txt != null)
            {
                txt.text = label;
                txt.fontSize = 18;
                txt.color = Color.white;
            }
        }
    }
}

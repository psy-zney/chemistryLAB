using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ChemistryLab.Domain;
using ChemistryLab.Presentation;

namespace ChemistryLab.Presentation.UnityViews
{
    /// <summary>
    /// Unity MonoBehaviour adapter implementing IMainLabView.
    /// Attach this to a Canvas panel in your MainLab scene.
    /// Auto-arranges UI elements for a clean presentation.
    /// </summary>
    public sealed class MainLabUnityView : MonoBehaviour, IMainLabView
    {
        [Header("UI Text Outputs (TextMeshPro)")]
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private TMP_Text benchSummaryText;
        [SerializeField] private TMP_Text experimentOutputText;

        [Header("Interactive Buttons")]
        [SerializeField] private Button pourWaterButton;
        [SerializeField] private Button pourSaltButton;
        [SerializeField] private Button pourHclButton;
        [SerializeField] private Button pourNaohButton;
        [SerializeField] private Button selectBeakerButton;
        [SerializeField] private Button resolveReactionButton;
        [SerializeField] private Button collectProductButton;
        [SerializeField] private Button washBeakerButton;

        private MainLabPresenter presenter;

        private void Awake()
        {
            AutoArrangeLayout();
        }

        private void OnValidate()
        {
            AutoArrangeLayout();
        }

        /// <summary>
        /// Automatically positions UI elements on screen cleanly without requiring manual dragging.
        /// </summary>
        public void AutoArrangeLayout()
        {
            // Panel background styling
            var panelImage = GetComponent<Image>();
            if (panelImage != null)
            {
                panelImage.color = new Color(0.12f, 0.15f, 0.22f, 0.85f); // Sleek Dark Lab theme
            }

            // Top Status Text
            if (statusText != null)
            {
                SetRect(statusText.rectTransform, new Vector2(0, 200), new Vector2(700, 50));
                statusText.fontSize = 24;
                statusText.alignment = TextAlignmentOptions.Center;
                statusText.color = Color.cyan;
            }

            // Middle Bench Summary Text
            if (benchSummaryText != null)
            {
                SetRect(benchSummaryText.rectTransform, new Vector2(0, 80), new Vector2(700, 140));
                benchSummaryText.fontSize = 20;
                benchSummaryText.alignment = TextAlignmentOptions.TopLeft;
                benchSummaryText.color = Color.white;
            }

            // Experiment Output Text
            if (experimentOutputText != null)
            {
                SetRect(experimentOutputText.rectTransform, new Vector2(0, -30), new Vector2(700, 60));
                experimentOutputText.fontSize = 22;
                experimentOutputText.alignment = TextAlignmentOptions.Center;
                experimentOutputText.color = Color.yellow;
            }

            // Arrange Buttons in clean rows
            SetButton(selectBeakerButton, new Vector2(-220, -130), "1. Select Beaker", new Color(0.2f, 0.6f, 0.9f));
            SetButton(pourWaterButton, new Vector2(-70, -130), "2. Pour Water", new Color(0.2f, 0.7f, 0.4f));
            SetButton(pourSaltButton, new Vector2(80, -130), "3. Pour Salt", new Color(0.8f, 0.8f, 0.8f));
            SetButton(resolveReactionButton, new Vector2(230, -130), "4. Mix / React", new Color(0.9f, 0.4f, 0.2f));

            SetButton(pourHclButton, new Vector2(-150, -190), "Pour HCl", new Color(0.7f, 0.3f, 0.3f));
            SetButton(pourNaohButton, new Vector2(0, -190), "Pour NaOH", new Color(0.3f, 0.4f, 0.8f));
            SetButton(collectProductButton, new Vector2(150, -190), "Collect Product", new Color(0.9f, 0.8f, 0.2f));
        }

        private static void SetRect(RectTransform rt, Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            if (rt == null) return;
            rt.anchoredPosition = anchoredPosition;
            rt.sizeDelta = sizeDelta;
        }

        private static void SetButton(Button btn, Vector2 position, string label, Color btnColor)
        {
            if (btn == null) return;
            var rt = btn.GetComponent<RectTransform>();
            SetRect(rt, position, new Vector2(140, 45));

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

        public void BindPresenter(MainLabPresenter labPresenter)
        {
            presenter = labPresenter;
            HookButtons();
            ShowLocalisedMessage("Chao muong den Phong Thi Ngiem Hoa Hoc!");
        }

        private void HookButtons()
        {
            if (pourWaterButton != null) pourWaterButton.onClick.AddListener(OnPourWater);
            if (pourSaltButton != null) pourSaltButton.onClick.AddListener(OnPourSalt);
            if (pourHclButton != null) pourHclButton.onClick.AddListener(OnPourHcl);
            if (pourNaohButton != null) pourNaohButton.onClick.AddListener(OnPourNaoh);
            if (selectBeakerButton != null) selectBeakerButton.onClick.AddListener(OnSelectBeaker);
            if (resolveReactionButton != null) resolveReactionButton.onClick.AddListener(OnResolve);
            if (collectProductButton != null) collectProductButton.onClick.AddListener(OnCollect);
            if (washBeakerButton != null) washBeakerButton.onClick.AddListener(OnWash);
        }

        private void OnPourWater() { if (presenter != null) presenter.OnSelectChemical("water", 10m); }
        private void OnPourSalt() { if (presenter != null) presenter.OnSelectChemical("salt", 5m); }
        private void OnPourHcl() { if (presenter != null) presenter.OnSelectChemical("hcl", 10m); }
        private void OnPourNaoh() { if (presenter != null) presenter.OnSelectChemical("naoh", 10m); }
        private void OnSelectBeaker() { if (presenter != null) presenter.OnSelectTool("beaker_100ml"); }
        private void OnResolve() { if (presenter != null) presenter.OnExecuteExperiment(); }
        private void OnCollect() { if (presenter != null) presenter.OnCollectProduct(); }
        private void OnWash()
        {
            if (presenter != null)
            {
                var toolId = presenter.SelectedToolId;
                if (string.IsNullOrEmpty(toolId)) toolId = "beaker_100ml";
                presenter.OnWashTool(toolId);
            }
        }

        public void ShowBench(IReadOnlyDictionary<string, decimal> selectedItems, string selectedToolId)
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== CHI TIET BAN THI NGHIEM ===");
            sb.AppendLine("Dung cu dang chon: " + (string.IsNullOrEmpty(selectedToolId) ? "(Chua chon)" : selectedToolId));
            if (selectedItems != null && selectedItems.Count > 0)
            {
                foreach (var kvp in selectedItems)
                {
                    sb.AppendLine(string.Format(" - Hoa chat {0}: {1}g", kvp.Key, kvp.Value));
                }
            }
            else
            {
                sb.AppendLine(" (Ban thi nghiem dang trong)");
            }

            if (benchSummaryText != null) benchSummaryText.text = sb.ToString();
            Debug.Log("[MainLabUnityView] ShowBench: " + sb.ToString().Replace("\r\n", " ").Replace("\n", " "));
        }

        public void ShowLocalisedMessage(string localisationKey)
        {
            if (statusText != null) statusText.text = "Trang thai: " + localisationKey;
            Debug.Log("[MainLabUnityView] Message: " + localisationKey);
        }

        public void ShowExperimentReady(string reactionId, string outputItemId, decimal outputMassGram)
        {
            var msg = string.Format("PHAN UNG THANH CONG! [{0}] tao ra {1}g {2}.", reactionId, outputMassGram, outputItemId);
            if (experimentOutputText != null) experimentOutputText.text = msg;
            if (statusText != null) statusText.text = msg;
            Debug.Log("[MainLabUnityView] Experiment Ready: " + msg);
        }

        public void ShowProductCollected(string itemId, decimal massGram)
        {
            var msg = string.Format("Da thu hoi {0}g {1} vao kho do!", massGram, itemId);
            if (experimentOutputText != null) experimentOutputText.text = msg;
            if (statusText != null) statusText.text = msg;
            Debug.Log("[MainLabUnityView] Product Collected: " + msg);
        }

        public void RefreshTool(string toolId, ToolCleanState cleanliness)
        {
            var msg = string.Format("Dung cu {0} hien tai: {1}", toolId, cleanliness);
            if (statusText != null) statusText.text = msg;
            Debug.Log("[MainLabUnityView] RefreshTool: " + msg);
        }
    }
}

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
    /// Unity view adapter implementing IMainLabView.
    /// Matches the 4-zone lab layout specified in PlanCoreGame/GamePlay.md.
    /// </summary>
    public sealed class MainLabUnityView : MonoBehaviour, IMainLabView
    {
        [Header("Top Header - NPC & Player Stats")]
        [SerializeField] private TMP_Text playerStatsText;
        [SerializeField] private TMP_Text questTaskText;

        [Header("Center Workbench Display")]
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private TMP_Text benchSummaryText;
        [SerializeField] private TMP_Text experimentOutputText;

        [Header("Left Zone: Tu Hoa Chat (Chemical Cabinet)")]
        [SerializeField] private Button pourWaterButton;
        [SerializeField] private Button pourSaltButton;
        [SerializeField] private Button pourHclButton;
        [SerializeField] private Button pourNaohButton;

        [Header("Right Zone: Tu Dung Cu (Glassware Cabinet)")]
        [SerializeField] private Button selectBeakerButton;
        [SerializeField] private Button resolveReactionButton;

        [Header("Bottom Zone: Bon Rua & Thu Hoi (Sink & Wash Station)")]
        [SerializeField] private Button collectProductButton;
        [SerializeField] private Button washBeakerButton;

        private MainLabPresenter presenter;
        private string currentToolId;
        private readonly Dictionary<string, decimal> currentBenchItems = new Dictionary<string, decimal>(System.StringComparer.Ordinal);
        private string lastExperimentReadyMsg;

        private void Awake()
        {
            AutoArrangeLayout();
        }

        [ContextMenu("Auto Arrange Layout")]
        public void AutoArrangeLayout()
        {
            var panelImage = GetComponent<Image>();
            if (panelImage != null)
            {
                panelImage.color = new Color(0.09f, 0.11f, 0.16f, 0.98f);
            }

            // Top Header Stats (X: 0, Y: 240)
            if (playerStatsText != null)
            {
                SetRect(playerStatsText.rectTransform, new Vector2(-150, 240), new Vector2(400, 40));
                playerStatsText.text = "Dollars: 500$ | Kim Cuong: 100 | Cap: 1 (EXP: 0/100)";
                playerStatsText.fontSize = 16;
                playerStatsText.color = Color.yellow;
            }

            if (questTaskText != null)
            {
                SetRect(questTaskText.rectTransform, new Vector2(250, 240), new Vector2(400, 40));
                questTaskText.text = "Nhiem vu NPC: Hoa tan Muoi NaCl vao Nuoc cat!";
                questTaskText.fontSize = 15;
                questTaskText.color = Color.green;
            }

            // Center Workbench Displays
            if (statusText != null)
            {
                SetRect(statusText.rectTransform, new Vector2(0, 185), new Vector2(500, 45));
                statusText.fontSize = 22;
                statusText.alignment = TextAlignmentOptions.Center;
                statusText.color = Color.cyan;
            }

            if (benchSummaryText != null)
            {
                SetRect(benchSummaryText.rectTransform, new Vector2(0, 50), new Vector2(420, 200));
                benchSummaryText.fontSize = 18;
                benchSummaryText.alignment = TextAlignmentOptions.TopLeft;
                benchSummaryText.color = Color.white;
            }

            if (experimentOutputText != null)
            {
                SetRect(experimentOutputText.rectTransform, new Vector2(0, -90), new Vector2(480, 50));
                experimentOutputText.fontSize = 19;
                experimentOutputText.alignment = TextAlignmentOptions.Center;
                experimentOutputText.color = Color.yellow;
            }

            // Left Zone: Tu Hoa Chat (X: -320)
            SetButton(pourWaterButton, new Vector2(-320, 120), "1. Lay Nuoc (H2O)", new Color(0.2f, 0.5f, 0.8f));
            SetButton(pourSaltButton, new Vector2(-320, 60), "2. Lay Muoi (NaCl)", new Color(0.8f, 0.7f, 0.5f));
            SetButton(pourHclButton, new Vector2(-320, 0), "3. Lay Axit (HCl)", new Color(0.8f, 0.3f, 0.3f));
            SetButton(pourNaohButton, new Vector2(-320, -60), "4. Lay Kiem (NaOH)", new Color(0.6f, 0.3f, 0.8f));

            // Right Zone: Tu Dung Cu (X: +320)
            SetButton(selectBeakerButton, new Vector2(320, 120), "Lay Coc Beaker 100ml", new Color(0.3f, 0.6f, 0.7f));
            SetButton(resolveReactionButton, new Vector2(320, 30), "KICH HOA PHAN UNG", new Color(0.9f, 0.4f, 0.1f));

            // Bottom Zone: Bon Rua & Thu Hoi (Y: -200)
            SetButton(collectProductButton, new Vector2(-120, -210), "Thu Hoi San Pham", new Color(0.2f, 0.7f, 0.3f));
            SetButton(washBeakerButton, new Vector2(120, -210), "Rua Coc Thi Nghiem", new Color(0.2f, 0.6f, 0.9f));
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
            SetRect(rt, position, new Vector2(210, 48));

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

        public void BindPresenter(MainLabPresenter mainLabPresenter)
        {
            Bind(mainLabPresenter);
        }

        public void Bind(MainLabPresenter mainLabPresenter)
        {
            presenter = mainLabPresenter;

            if (pourWaterButton != null)
            {
                pourWaterButton.onClick.RemoveAllListeners();
                pourWaterButton.onClick.AddListener(() => presenter.OnSelectChemical("water", 100m));
            }
            if (pourSaltButton != null)
            {
                pourSaltButton.onClick.RemoveAllListeners();
                pourSaltButton.onClick.AddListener(() => presenter.OnSelectChemical("salt", 10m));
            }
            if (pourHclButton != null)
            {
                pourHclButton.onClick.RemoveAllListeners();
                pourHclButton.onClick.AddListener(() => presenter.OnSelectChemical("hcl", 10m));
            }
            if (pourNaohButton != null)
            {
                pourNaohButton.onClick.RemoveAllListeners();
                pourNaohButton.onClick.AddListener(() => presenter.OnSelectChemical("naoh", 10m));
            }
            if (selectBeakerButton != null)
            {
                selectBeakerButton.onClick.RemoveAllListeners();
                selectBeakerButton.onClick.AddListener(() => presenter.OnSelectTool("beaker_100ml"));
            }
            if (resolveReactionButton != null)
            {
                resolveReactionButton.onClick.RemoveAllListeners();
                resolveReactionButton.onClick.AddListener(() => presenter.OnExecuteExperiment());
            }
            if (collectProductButton != null)
            {
                collectProductButton.onClick.RemoveAllListeners();
                collectProductButton.onClick.AddListener(() => presenter.OnCollectProduct());
            }
            if (washBeakerButton != null)
            {
                washBeakerButton.onClick.RemoveAllListeners();
                washBeakerButton.onClick.AddListener(() => presenter.OnWashTool("beaker_100ml"));
            }
        }

        // --- IMainLabView Interface Implementation ---

        public void ShowBench(IReadOnlyDictionary<string, decimal> selectedItems, string selectedToolId)
        {
            currentToolId = selectedToolId;
            currentBenchItems.Clear();
            if (selectedItems != null)
            {
                foreach (var kvp in selectedItems)
                {
                    currentBenchItems[kvp.Key] = kvp.Value;
                }
            }

            RenderBench();
        }

        public void ShowLocalisedMessage(string localisationKey)
        {
            if (statusText != null)
            {
                statusText.text = "Trang thai: " + localisationKey;
            }
        }

        public void ShowExperimentReady(string reactionId, string outputItemId, decimal outputMassGram)
        {
            lastExperimentReadyMsg = string.Format("San sang phan ung [{0}]! Tao ra {1}g {2}.", reactionId, outputMassGram, outputItemId);
            if (experimentOutputText != null)
            {
                experimentOutputText.text = lastExperimentReadyMsg;
            }
        }

        public void ShowProductCollected(string itemId, decimal massGram)
        {
            if (experimentOutputText != null)
            {
                experimentOutputText.text = string.Format("Da thu hoi {0}g san pham [{1}] vao kho!", massGram, itemId);
            }
        }

        public void RefreshTool(string toolId, ToolCleanState cleanliness)
        {
            if (statusText != null)
            {
                statusText.text = string.Format("Dung cu [{0}] hien tai: {1}", toolId, cleanliness);
            }
        }

        private void RenderBench()
        {
            if (benchSummaryText != null)
            {
                var sb = new StringBuilder();
                sb.AppendLine("<b>BAN THI NGHIEM CHINH:</b>");
                sb.AppendLine(string.Format("• Dung cu dang dung: <b>{0}</b>", currentToolId ?? "Chua chon coc"));
                sb.AppendLine("• Hoa chat da rot:");

                if (currentBenchItems.Count == 0)
                {
                    sb.AppendLine("   <i>(Chua co hoa chat trong coc)</i>");
                }
                else
                {
                    foreach (var pair in currentBenchItems)
                    {
                        sb.AppendLine(string.Format("   - <b>{0}</b>: {1}g", pair.Key, pair.Value));
                    }
                }

                benchSummaryText.text = sb.ToString();
            }
        }
    }
}

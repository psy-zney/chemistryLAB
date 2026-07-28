using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ChemistryLab.Desktop
{
    public sealed class DesktopLabHud : MonoBehaviour
    {
        private DesktopLabGame game;
        private Font bodyFont;
        private Font displayFont;
        private Font monoFont;
        private Canvas rootCanvas;
        private Text zoneText;
        private Text temperatureText;
        private Text safetyText;
        private Text missionText;
        private Text promptText;
        private Text selectedFormulaText;
        private Text selectedNameText;
        private Text selectedDetailsText;
        private Text vesselTitleText;
        private Text vesselEquationText;
        private Text vesselDetailsText;
        private Text transientText;
        private Text accessibilityText;
        private Text audioStatusText;
        private Text audioButtonText;
        private Text debugText;
        private Text playerSafetyText;
        private Text respiratorButtonText;
        private Text gasTrapButtonText;
        private GameObject inspectorPanel;
        private CanvasGroup inspectorGroup;
        private RectTransform inspectorRect;
        private GameObject pauseOverlay;
        private GameObject debugPanel;
        private Button resumeButton;
        private GameObject selectedSection;
        private GameObject vesselSection;
        private Coroutine inspectorAnimation;
        private Coroutine transientAnimation;
        private bool inspectorVisible;

        public bool InspectorVisible
        {
            get { return inspectorVisible; }
        }

        public int PauseButtonCount
        {
            get
            {
                return pauseOverlay == null
                    ? 0
                    : pauseOverlay.GetComponentsInChildren<Button>(true).Length;
            }
        }

        public bool RuntimeUiReady
        {
            get
            {
                return rootCanvas != null
                    && pauseOverlay != null
                    && debugPanel != null
                    && resumeButton != null
                    && playerSafetyText != null
                    && PauseButtonCount == 3
                    && UnityEngine.Object.FindAnyObjectByType<EventSystem>() != null;
            }
        }

        public void Initialise(DesktopLabGame owner)
        {
            game = owner;
            bodyFont = LabTheme.CreateBodyFont(16);
            displayFont = LabTheme.CreateDisplayFont(22);
            monoFont = LabTheme.CreateMonoFont(14);
            BuildInterface();
            SetAccessibilityState(LabAccessibility.ReducedMotion);
            SetInspectorVisible(false, true);
            SetDebugVisible(false);
            SetPaused(false);
        }

        public void SetZone(string zone)
        {
            if (zoneText != null)
            {
                zoneText.text = zone;
            }
        }

        public void SetTemperature(float temperatureC)
        {
            if (temperatureText != null)
            {
                temperatureText.text = temperatureC.ToString("0.0") + " °C";
            }
        }

        public void SetSafety(bool safe, string message)
        {
            if (safetyText == null)
            {
                return;
            }

            safetyText.text = safe ? "AN TOÀN" : "ĐÃ KHÓA";
            safetyText.color = safe ? LabTheme.Safe : LabTheme.Warning;
            safetyText.gameObject.name = message;
        }

        public void SetSafetySystem(LabSafetySystem state)
        {
            if (state == null || playerSafetyText == null)
            {
                return;
            }

            var incident = state.LastIncident;
            var warning = state.Health < 50f
                || incident != null && !incident.Controlled && incident.Severity >= HazardSeverity.Dangerous;
            playerSafetyText.text =
                "SỨC KHỎE  " + state.Health.ToString("0.0") + " / 100"
                + "     TÍN DỤNG  " + state.Credits + "\n"
                + "MẶT NẠ  " + (state.RespiratorEquipped ? "ĐANG ĐEO" : state.RespiratorOwned ? "ĐÃ THÁO" : "CHƯA MUA")
                + "     BÌNH CÁCH LY  " + (state.GasTrapConnected ? "ĐÃ NỐI" : "CHƯA NỐI") + "\n"
                + (incident == null ? "Chưa ghi nhận sự cố." : incident.Title + " · " + incident.Message);
            playerSafetyText.color = warning ? LabTheme.Warning : LabTheme.GraphiteInk;

            if (respiratorButtonText != null)
            {
                respiratorButtonText.text = !state.RespiratorOwned
                    ? "F6 · MUA MẶT NẠ · " + LabSafetySystem.RespiratorPrice
                    : state.RespiratorEquipped ? "F6 · THÁO MẶT NẠ" : "F6 · ĐEO MẶT NẠ";
            }

            if (gasTrapButtonText != null)
            {
                gasTrapButtonText.text = state.GasTrapConnected
                    ? "F7 · THÁO BÌNH CÁCH LY"
                    : "F7 · NỐI BÌNH CÁCH LY";
            }

            if (safetyText != null)
            {
                safetyText.text = warning ? "CẢNH BÁO" : "AN TOÀN";
                safetyText.color = warning ? LabTheme.Warning : LabTheme.Safe;
            }
        }

        public void SetMission(string title, bool completed)
        {
            if (missionText == null)
            {
                return;
            }

            missionText.text = completed
                ? "NHIỆM VỤ HOÀN THÀNH\n" + title
                : "NHIỆM VỤ ĐANG GHIM\n" + title;
            missionText.color = completed ? LabTheme.Safe : LabTheme.GraphiteInk;
        }

        public void SetInteractionPrompt(string prompt)
        {
            if (promptText == null)
            {
                return;
            }

            promptText.text = prompt;
            promptText.transform.parent.gameObject.SetActive(!string.IsNullOrWhiteSpace(prompt));
        }

        public void SetSelectedChemical(
            ChemicalDefinition chemical,
            float amountGrams,
            SynthesizedBatch batch = null,
            int inventoryCount = 0)
        {
            if (selectedFormulaText == null)
            {
                return;
            }

            if (chemical == null)
            {
                selectedFormulaText.color = LabTheme.Ink;
                selectedFormulaText.text = "—";
                selectedNameText.text = "Chưa cầm mẫu";
                selectedDetailsText.text =
                    "Đến tủ hóa chất, đặt tâm ngắm lên một chai và nhấn E.\n\n"
                    + "KHO ĐIỀU CHẾ\n" + inventoryCount
                    + " lô · nhấn I để chọn lô đã lưu.";
                return;
            }

            selectedFormulaText.color = LabTheme.Ink;
            selectedFormulaText.text = chemical.Formula;
            selectedNameText.text = chemical.Name + " · " + chemical.PhaseLabel;
            selectedDetailsText.text =
                "ĐỊNH LƯỢNG\n" + amountGrams.ToString("0.#") + " g  ·  [ / ] để thay đổi\n\n"
                + "PHÂN LOẠI\n" + chemical.FamilyLabel + "\n\n"
                + "KHỐI LƯỢNG MOL\n" + chemical.MolarMass.ToString("0.000") + " g/mol\n\n"
                + "KHỐI LƯỢNG RIÊNG\n" + chemical.Density + "\n\n"
                + "NÓNG CHẢY\n" + chemical.MeltingPoint + "\n\n"
                + "SÔI / PHÂN HỦY\n" + chemical.BoilingPoint + "\n\n"
                + "NGOẠI QUAN\n" + chemical.Appearance + "\n\n"
                + "ĐỘ TAN\n" + chemical.Solubility + "\n\n"
                + "TÍNH PHẢN ỨNG\n" + chemical.ReactivitySummary + "\n\n"
                + "CẢNH BÁO\n" + chemical.Hazards + "\n\n"
                + "THAO TÁC\n" + chemical.Handling + "\n\n"
                + "ỨNG DỤNG\n" + chemical.Use
                + "\n\nKHO ĐIỀU CHẾ\n"
                + (batch == null
                    ? inventoryCount + " lô · nhấn I để chọn lô đã lưu."
                    : "Lô " + batch.BatchId.Substring(0, Mathf.Min(8, batch.BatchId.Length))
                      + " · còn " + batch.AvailableGrams.ToString("0.000") + " g"
                      + " · tinh khiết " + (batch.PurityFraction * 100f).ToString("0.0") + "%\n"
                      + "Nguồn: " + batch.SourceEquation);
        }

        public void SetSelectedElement(PeriodicElementDefinition element)
        {
            if (selectedFormulaText == null || element == null)
            {
                return;
            }

            selectedFormulaText.color = LabTheme.Ink;
            selectedFormulaText.text = element.AtomicNumber + "  " + element.Symbol;
            selectedNameText.text = element.Name + " · " + element.CategoryLabel;
            selectedDetailsText.text =
                "NGUYÊN TỬ KHỐI\n" + element.AtomicMass.ToString("0.###") + " u\n\n"
                + "CHU KỲ / NHÓM\n" + element.Period + " / "
                + (element.Group <= 0 ? "họ actini" : element.Group.ToString()) + "\n\n"
                + "CẤU HÌNH ELECTRON\n" + element.ElectronConfiguration + "\n\n"
                + "TRẠNG THÁI · 25 °C\n" + element.Phase + "\n\n"
                + "NGOẠI QUAN / MÀU\n" + element.Appearance + "\n\n"
                + "KHỐI LƯỢNG RIÊNG\n" + element.Density + "\n\n"
                + "NÓNG CHẢY\n" + element.MeltingPoint + "\n\n"
                + "SÔI / THĂNG HOA\n" + element.BoilingPoint + "\n\n"
                + "SỐ OXI HÓA PHỔ BIẾN\n" + element.OxidationStates + "\n\n"
                + "TÍNH CHẤT HÓA HỌC\n" + element.ChemicalProperties + "\n\n"
                + "TRONG TỰ NHIÊN\n" + element.Occurrence;
            ShowChemicalSection();
        }

        public void SetVessel(
            IReadOnlyList<VesselAddition> additions,
            ReactionOutcome outcome,
            LabStation station)
        {
            if (vesselTitleText == null || outcome == null)
            {
                return;
            }

            vesselTitleText.text = outcome.Title;
            vesselEquationText.text = outcome.Equation;

            var builder = new StringBuilder();
            builder.Append("VỊ TRÍ\n");
            builder.Append(DesktopLabGame.ZoneLabel(station));
            builder.Append("\n\nTHÀNH PHẦN\n");
            if (additions == null || additions.Count == 0)
            {
                builder.Append("Cốc sạch — chưa nạp hóa chất");
            }
            else
            {
                for (var index = 0; index < additions.Count; index++)
                {
                    var addition = additions[index];
                    var definition = RuntimeChemicalRegistry.GetChemical(addition.ChemicalId);
                    builder.Append(index + 1);
                    builder.Append(". ");
                    builder.Append(definition == null ? addition.ChemicalId : definition.Formula);
                    builder.Append("  ");
                    builder.Append(addition.Grams.ToString("0.#"));
                    builder.Append(" g");
                    if (definition != null)
                    {
                        builder.Append("  ·  ");
                        builder.Append((addition.Grams / definition.MolarMass).ToString("0.0000"));
                        builder.Append(" mol");
                    }

                    builder.Append('\n');
                }
            }

            builder.Append("\nĐIỀU KIỆN HIỆN TẠI\n");
            builder.Append(outcome.ConditionSummary);
            builder.Append("\nXÚC TÁC\n");
            builder.Append(outcome.CatalystSummary);

            if (outcome.Status == ReactionStatus.Reaction)
            {
                var limiting = RuntimeChemicalRegistry.GetChemical(outcome.LimitingChemicalId);
                builder.Append("\nNGUỒN MÔ PHỎNG\n");
                builder.Append(outcome.GeneratedByRule
                    ? "Luật suy diễn · " + outcome.RuleFamily
                    : "Phản ứng mẫu đã duyệt");
                if (outcome.IsRedox)
                {
                    builder.Append("\n\nOXI HÓA–KHỬ\n");
                    builder.Append(outcome.ElectronTransferCount);
                    builder.Append(" e⁻ trao đổi sau khi quy đồng hai bán phản ứng");
                }

                builder.Append("\n\nĐỘNG HỌC ƯỚC TÍNH\n");
                builder.Append(outcome.RateClass);
                builder.Append(" · hệ số ");
                builder.Append(outcome.RateMultiplier.ToString("0.00"));
                builder.Append("× · ");
                builder.Append(outcome.EstimatedCompletionSeconds.ToString("0.0"));
                builder.Append(" s");
                if (outcome.GeneratedByRule)
                {
                    builder.Append("\n\nĐỘ TIN CẬY SẢN PHẨM\n");
                    builder.Append(outcome.ProductConfidence);
                    builder.Append("\n\nCƠ SỞ ƯỚC TÍNH\n");
                    builder.Append(outcome.GeneratedPropertyBasis);
                    if (outcome.ProductHazards != ChemicalHazardFlags.None)
                    {
                        builder.Append("\n\nCỜ NGUY HẠI SẢN PHẨM\n");
                        builder.Append(outcome.ProductHazards);
                    }
                }

                builder.Append("\n\nCHẤT GIỚI HẠN\n");
                builder.Append(limiting == null ? "—" : limiting.Formula);
                builder.Append("\n\nSẢN LƯỢNG LÝ THUYẾT\n");
                builder.Append(outcome.TheoreticalProductGrams.ToString("0.000"));
                builder.Append(" g\n\nƯỚC TÍNH THU ĐƯỢC\n");
                builder.Append(outcome.EstimatedProductGrams.ToString("0.000"));
                builder.Append(" g\n\nĐỘ TINH KHIẾT LÔ\n");
                builder.Append((outcome.ProductPurity * 100f).ToString("0.0"));
                builder.Append("%\n\nTHU SẢN PHẨM\n");
                builder.Append(outcome.Effect == ReactionEffect.Gas
                    ? "C · cần tủ hút + bình cách ly F7"
                    : "C hoặc bỏ mẫu đang cầm rồi nhấn E tại cốc");
                builder.Append("\n\nQUAN SÁT\n");
                builder.Append(outcome.Message);
                if (outcome.Hazard != null)
                {
                    builder.Append("\n\nKHÍ / HƠI NGUY HIỂM\n");
                    builder.Append(outcome.Hazard.Formula);
                    builder.Append(" · ");
                    builder.Append(outcome.Hazard.Severity);
                    builder.Append("\n");
                    builder.Append(outcome.Hazard.Warning);
                }
            }
            else
            {
                builder.Append("\n\nTRẠNG THÁI\n");
                builder.Append(outcome.Message);
            }

            builder.Append("\n\nAN TOÀN / XỬ LÝ\n");
            builder.Append(outcome.Safety);
            vesselDetailsText.text = builder.ToString();
        }

        public void ShowChemicalSection()
        {
            if (selectedSection != null)
            {
                selectedSection.SetActive(true);
            }

            if (vesselSection != null)
            {
                vesselSection.SetActive(false);
            }
        }

        public void ShowVesselSection()
        {
            if (selectedSection != null)
            {
                selectedSection.SetActive(false);
            }

            if (vesselSection != null)
            {
                vesselSection.SetActive(true);
            }
        }

        public void SetInspectorVisible(bool visible, bool immediate = false)
        {
            inspectorVisible = visible;
            if (inspectorGroup == null || inspectorRect == null)
            {
                return;
            }

            if (inspectorAnimation != null)
            {
                StopCoroutine(inspectorAnimation);
            }

            if (immediate || LabAccessibility.ReducedMotion)
            {
                inspectorGroup.alpha = visible ? 1f : 0f;
                inspectorGroup.interactable = visible;
                inspectorGroup.blocksRaycasts = visible;
                inspectorRect.anchoredPosition = new Vector2(visible ? 0f : 36f, 0f);
                inspectorPanel.SetActive(visible);
                return;
            }

            inspectorAnimation = StartCoroutine(AnimateInspector(visible));
        }

        public void SetPaused(bool paused)
        {
            if (pauseOverlay != null)
            {
                pauseOverlay.SetActive(paused);
            }

            if (paused && resumeButton != null)
            {
                resumeButton.Select();
            }
        }

        public void SetAccessibilityState(bool reducedMotion)
        {
            if (accessibilityText != null)
            {
                accessibilityText.text = reducedMotion
                    ? "F10 · MOTION GIẢM"
                    : "F10 · MOTION ĐẦY";
            }
        }

        public void SetAudioState(bool enabled)
        {
            var state = enabled ? "BẬT" : "TẮT";
            if (audioStatusText != null)
            {
                audioStatusText.text = "F9 · ÂM " + state;
            }

            if (audioButtonText != null)
            {
                audioButtonText.text = "ÂM THANH · " + state;
            }
        }

        public void SetDebugVisible(bool visible)
        {
            if (debugPanel != null)
            {
                debugPanel.SetActive(visible);
            }
        }

        public void SetDebugText(string content)
        {
            if (debugText != null)
            {
                debugText.text = content;
            }
        }

        public void ShowTransient(string message, bool warning = false)
        {
            if (transientText == null)
            {
                return;
            }

            if (transientAnimation != null)
            {
                StopCoroutine(transientAnimation);
            }

            transientText.text = message;
            transientText.color = warning ? LabTheme.Warning : LabTheme.Ink;
            transientText.transform.parent.gameObject.SetActive(true);
            transientAnimation = StartCoroutine(HideTransientLater());
        }

        private void BuildInterface()
        {
            if (UnityEngine.Object.FindAnyObjectByType<EventSystem>() == null)
            {
                var eventSystem = new GameObject(
                    "Desktop UI Event System",
                    typeof(EventSystem),
                    typeof(StandaloneInputModule));
                eventSystem.transform.SetParent(transform, false);
            }

            var canvasObject = new GameObject("Desktop HUD", typeof(Canvas), typeof(CanvasScaler));
            canvasObject.transform.SetParent(transform, false);
            rootCanvas = canvasObject.GetComponent<Canvas>();
            rootCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            rootCanvas.sortingOrder = 100;

            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(LabTheme.ReferenceWidth, LabTheme.ReferenceHeight);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            var topBar = CreatePanel(
                "Edge HUD",
                canvasObject.transform,
                new Vector2(0f, 1f),
                Vector2.one,
                new Vector2(0f, 1f),
                new Vector2(0f, -60f),
                Vector2.zero,
                LabTheme.Graphite);

            CreateText(
                "Wordmark",
                topBar.transform,
                "CHEMISTRY LAB · NATIVE",
                displayFont,
                20,
                FontStyle.Bold,
                LabTheme.GraphiteInk,
                TextAnchor.MiddleLeft,
                new Vector2(0f, 0f),
                new Vector2(0.42f, 1f),
                new Vector2(20f, 0f),
                Vector2.zero);

            zoneText = CreateText(
                "Zone",
                topBar.transform,
                "Bàn phản ứng",
                bodyFont,
                14,
                FontStyle.Normal,
                LabTheme.GraphiteInk,
                TextAnchor.MiddleRight,
                new Vector2(0.54f, 0f),
                new Vector2(0.72f, 1f),
                Vector2.zero,
                Vector2.zero);

            temperatureText = CreateText(
                "Temperature",
                topBar.transform,
                "24.0 °C",
                bodyFont,
                14,
                FontStyle.Bold,
                LabTheme.GraphiteInk,
                TextAnchor.MiddleCenter,
                new Vector2(0.72f, 0f),
                new Vector2(0.84f, 1f),
                Vector2.zero,
                Vector2.zero);

            safetyText = CreateText(
                "Safety",
                topBar.transform,
                "AN TOÀN",
                bodyFont,
                13,
                FontStyle.Bold,
                LabTheme.Safe,
                TextAnchor.MiddleCenter,
                new Vector2(0.84f, 0f),
                Vector2.one,
                Vector2.zero,
                Vector2.zero);

            var missionPanel = CreatePanel(
                "Mission",
                canvasObject.transform,
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(20f, -156f),
                new Vector2(390f, -82f),
                LabTheme.WithAlpha(LabTheme.Graphite, 0.88f));
            missionText = CreateText(
                "Mission Text",
                missionPanel.transform,
                "NHIỆM VỤ ĐANG GHIM\nTạo kết tủa xanh Cu(OH)₂",
                bodyFont,
                16,
                FontStyle.Bold,
                LabTheme.GraphiteInk,
                TextAnchor.MiddleLeft,
                Vector2.zero,
                Vector2.one,
                new Vector2(16f, 8f),
                new Vector2(-12f, -8f));

            CreateSafetyPanel(canvasObject.transform);

            var promptPanel = CreatePanel(
                "Interaction Prompt Surface",
                canvasObject.transform,
                new Vector2(0.25f, 0f),
                new Vector2(0.75f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(0f, 78f),
                new Vector2(0f, 132f),
                LabTheme.WithAlpha(LabTheme.Graphite, 0.82f));
            promptText = CreateText(
                "Interaction Prompt",
                promptPanel.transform,
                string.Empty,
                displayFont,
                19,
                FontStyle.Bold,
                LabTheme.GraphiteInk,
                TextAnchor.MiddleCenter,
                Vector2.zero,
                Vector2.one,
                new Vector2(12f, 0f),
                new Vector2(-12f, 0f));

            var transientPanel = CreatePanel(
                "Transient Surface",
                canvasObject.transform,
                Vector2.zero,
                new Vector2(0.48f, 0f),
                Vector2.zero,
                new Vector2(22f, 62f),
                new Vector2(-16f, 112f),
                LabTheme.WithAlpha(LabTheme.PaperRaised, 0.94f));
            transientText = CreateText(
                "Transient",
                transientPanel.transform,
                string.Empty,
                bodyFont,
                16,
                FontStyle.Bold,
                LabTheme.GraphiteInk,
                TextAnchor.MiddleLeft,
                Vector2.zero,
                Vector2.one,
                new Vector2(12f, 0f),
                new Vector2(-12f, 0f));
            transientPanel.SetActive(false);

            CreateDebugPanel(canvasObject.transform);
            CreateCrosshair(canvasObject.transform);
            CreateFooter(canvasObject.transform);
            CreateInspector(canvasObject.transform);
            CreatePauseOverlay(canvasObject.transform);
        }

        private void CreateFooter(Transform parent)
        {
            var footer = CreatePanel(
                "Context Controls",
                parent,
                Vector2.zero,
                new Vector2(1f, 0f),
                Vector2.zero,
                Vector2.zero,
                new Vector2(0f, 54f),
                LabTheme.Graphite);

            CreateText(
                "Movement Controls",
                footer.transform,
                "WASD  DI CHUYỂN   E  TƯƠNG TÁC   PG↑/↓  NHIỆT   F8  PHA LOÃNG   C  THU   I  KHO   ESC  DỪNG",
                bodyFont,
                13,
                FontStyle.Bold,
                LabTheme.GraphiteInk,
                TextAnchor.MiddleCenter,
                Vector2.zero,
                new Vector2(0.69f, 1f),
                new Vector2(12f, 0f),
                Vector2.zero);

            CreateText(
                "Diagnostics Control",
                footer.transform,
                "F3 · DEBUG",
                bodyFont,
                12,
                FontStyle.Bold,
                LabTheme.GraphiteInk,
                TextAnchor.MiddleCenter,
                new Vector2(0.69f, 0f),
                new Vector2(0.79f, 1f),
                Vector2.zero,
                Vector2.zero);

            audioStatusText = CreateText(
                "Audio Control",
                footer.transform,
                "F9 · ÂM BẬT",
                bodyFont,
                12,
                FontStyle.Bold,
                LabTheme.GraphiteInk,
                TextAnchor.MiddleCenter,
                new Vector2(0.79f, 0f),
                new Vector2(0.89f, 1f),
                Vector2.zero,
                Vector2.zero);

            accessibilityText = CreateText(
                "Accessibility",
                footer.transform,
                "F10 · MOTION ĐẦY",
                bodyFont,
                12,
                FontStyle.Normal,
                LabTheme.GraphiteInk,
                TextAnchor.MiddleRight,
                new Vector2(0.89f, 0f),
                Vector2.one,
                Vector2.zero,
                new Vector2(-18f, 0f));
        }

        private void CreateDebugPanel(Transform parent)
        {
            debugPanel = CreatePanel(
                "Runtime Debug Panel",
                parent,
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(20f, -614f),
                new Vector2(390f, -350f),
                LabTheme.WithAlpha(LabTheme.Graphite, 0.92f));

            CreateText(
                "Debug Title",
                debugPanel.transform,
                "RUNTIME DIAGNOSTICS · F3",
                bodyFont,
                13,
                FontStyle.Bold,
                LabTheme.Focus,
                TextAnchor.UpperLeft,
                new Vector2(0f, 1f),
                Vector2.one,
                new Vector2(16f, -38f),
                new Vector2(-14f, -10f));

            debugText = CreateText(
                "Debug Values",
                debugPanel.transform,
                "Đang đọc trạng thái runtime…",
                monoFont,
                13,
                FontStyle.Normal,
                LabTheme.GraphiteInk,
                TextAnchor.UpperLeft,
                Vector2.zero,
                Vector2.one,
                new Vector2(16f, 12f),
                new Vector2(-14f, -48f));
        }

        private void CreateSafetyPanel(Transform parent)
        {
            var panel = CreatePanel(
                "Safety Consequence Panel",
                parent,
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(20f, -334f),
                new Vector2(410f, -170f),
                LabTheme.WithAlpha(LabTheme.Graphite, 0.92f));

            playerSafetyText = CreateText(
                "Safety State",
                panel.transform,
                "SỨC KHỎE  100 / 100     TÍN DỤNG  1200\n"
                + "MẶT NẠ  CHƯA MUA     BÌNH CÁCH LY  CHƯA NỐI\nChưa ghi nhận sự cố.",
                bodyFont,
                12,
                FontStyle.Bold,
                LabTheme.GraphiteInk,
                TextAnchor.UpperLeft,
                Vector2.zero,
                Vector2.one,
                new Vector2(14f, 58f),
                new Vector2(-14f, -12f));

            var respiratorButton = CreateButton(
                "Respirator Button",
                panel.transform,
                "F6 · MUA MẶT NẠ · 250",
                new Vector2(14f, 12f),
                new Vector2(187f, 50f),
                game.ToggleRespirator);
            respiratorButtonText = respiratorButton.GetComponentInChildren<Text>();

            var trapButton = CreateButton(
                "Gas Trap Button",
                panel.transform,
                "F7 · NỐI BÌNH CÁCH LY",
                new Vector2(199f, 12f),
                new Vector2(376f, 50f),
                game.ToggleGasTrap);
            gasTrapButtonText = trapButton.GetComponentInChildren<Text>();
        }

        private void CreateInspector(Transform parent)
        {
            inspectorPanel = CreatePanel(
                "Inspector",
                parent,
                new Vector2(1f, 0f),
                Vector2.one,
                new Vector2(1f, 0.5f),
                new Vector2(-430f, 66f),
                new Vector2(-16f, -16f),
                LabTheme.PaperRaised);
            inspectorRect = inspectorPanel.GetComponent<RectTransform>();
            inspectorGroup = inspectorPanel.AddComponent<CanvasGroup>();

            var rule = CreatePanel(
                "Inspector Header",
                inspectorPanel.transform,
                new Vector2(0f, 1f),
                Vector2.one,
                new Vector2(0f, 1f),
                Vector2.zero,
                new Vector2(0f, 72f),
                LabTheme.PaperDeep);

            CreateText(
                "Inspector Title",
                rule.transform,
                "BẢNG PHÂN TÍCH · F ĐỂ ĐÓNG",
                bodyFont,
                13,
                FontStyle.Bold,
                LabTheme.Ink,
                TextAnchor.MiddleLeft,
                Vector2.zero,
                Vector2.one,
                new Vector2(18f, 0f),
                new Vector2(-14f, 0f));

            selectedSection = new GameObject("Chemical Section", typeof(RectTransform));
            selectedSection.transform.SetParent(inspectorPanel.transform, false);
            Stretch(selectedSection.GetComponent<RectTransform>(), new Vector2(0f, 0f), Vector2.one, new Vector2(18f, 18f), new Vector2(-18f, -84f));

            selectedFormulaText = CreateText(
                "Chemical Formula",
                selectedSection.transform,
                "—",
                monoFont,
                30,
                FontStyle.Bold,
                LabTheme.Ink,
                TextAnchor.UpperLeft,
                new Vector2(0f, 1f),
                Vector2.one,
                new Vector2(0f, -50f),
                Vector2.zero);

            selectedNameText = CreateText(
                "Chemical Name",
                selectedSection.transform,
                "Chưa cầm mẫu",
                displayFont,
                18,
                FontStyle.Bold,
                LabTheme.InkSoft,
                TextAnchor.UpperLeft,
                new Vector2(0f, 1f),
                Vector2.one,
                new Vector2(0f, -88f),
                new Vector2(0f, -54f));

            selectedDetailsText = CreateText(
                "Chemical Details",
                selectedSection.transform,
                "Đến tủ hóa chất và nhấn E để lấy mẫu.",
                bodyFont,
                15,
                FontStyle.Normal,
                LabTheme.Ink,
                TextAnchor.UpperLeft,
                Vector2.zero,
                Vector2.one,
                new Vector2(0f, 0f),
                new Vector2(0f, -96f));

            vesselSection = new GameObject("Vessel Section", typeof(RectTransform));
            vesselSection.transform.SetParent(inspectorPanel.transform, false);
            Stretch(vesselSection.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, new Vector2(18f, 18f), new Vector2(-18f, -84f));

            vesselTitleText = CreateText(
                "Vessel Title",
                vesselSection.transform,
                "Cốc phản ứng sạch",
                displayFont,
                24,
                FontStyle.Bold,
                LabTheme.Ink,
                TextAnchor.UpperLeft,
                new Vector2(0f, 1f),
                Vector2.one,
                new Vector2(0f, -46f),
                Vector2.zero);

            vesselEquationText = CreateText(
                "Equation",
                vesselSection.transform,
                "—",
                monoFont,
                15,
                FontStyle.Bold,
                LabTheme.AccentDeep,
                TextAnchor.UpperLeft,
                new Vector2(0f, 1f),
                Vector2.one,
                new Vector2(0f, -86f),
                new Vector2(0f, -50f));

            vesselDetailsText = CreateText(
                "Vessel Details",
                vesselSection.transform,
                "Cốc sạch — chưa nạp hóa chất.",
                bodyFont,
                15,
                FontStyle.Normal,
                LabTheme.Ink,
                TextAnchor.UpperLeft,
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                new Vector2(0f, -96f));

            ShowChemicalSection();
        }

        private void CreatePauseOverlay(Transform parent)
        {
            pauseOverlay = CreatePanel(
                "Pause Overlay",
                parent,
                Vector2.zero,
                Vector2.one,
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                Vector2.zero,
                LabTheme.WithAlpha(LabTheme.Graphite, 0.78f));
            pauseOverlay.GetComponent<Image>().raycastTarget = true;

            var card = CreatePanel(
                "Pause Card",
                pauseOverlay.transform,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(-320f, -245f),
                new Vector2(320f, 245f),
                LabTheme.PaperRaised);

            CreateText(
                "Pause Title",
                card.transform,
                "CA TRỰC ĐANG TẠM DỪNG",
                displayFont,
                27,
                FontStyle.Bold,
                LabTheme.Ink,
                TextAnchor.UpperLeft,
                Vector2.zero,
                Vector2.one,
                new Vector2(36f, 368f),
                new Vector2(-36f, -30f));

            CreateText(
                "Pause Copy",
                card.transform,
                "Con trỏ đã được mở để dùng menu.\n\n"
                + "WASD — di chuyển    E — tương tác    F — dữ liệu\n"
                + "[ / ] — định lượng    F3 — debug    F9 — âm thanh\n"
                + "Page Up / Down — gia nhiệt / làm nguội    F8 — pha loãng\n"
                + "C — thu sản phẩm    I — chọn lô đã lưu\n"
                + "F10 — giảm chuyển động    ESC — tiếp tục",
                bodyFont,
                16,
                FontStyle.Normal,
                LabTheme.InkSoft,
                TextAnchor.UpperLeft,
                Vector2.zero,
                Vector2.one,
                new Vector2(36f, 190f),
                new Vector2(-36f, -98f));

            resumeButton = CreateButton(
                "Resume Button",
                card.transform,
                "TIẾP TỤC CA THỰC HÀNH",
                new Vector2(36f, 120f),
                new Vector2(604f, 174f),
                game.ResumeFromUi);

            var audioButton = CreateButton(
                "Audio Button",
                card.transform,
                "ÂM THANH · BẬT",
                new Vector2(36f, 50f),
                new Vector2(304f, 104f),
                game.ToggleAudio);
            audioButtonText = audioButton.GetComponentInChildren<Text>();

            CreateButton(
                "Quit Button",
                card.transform,
                "THOÁT RA DESKTOP",
                new Vector2(326f, 50f),
                new Vector2(604f, 104f),
                game.QuitToDesktop);
        }

        private void CreateCrosshair(Transform parent)
        {
            var horizontal = CreatePanel(
                "Crosshair Horizontal",
                parent,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(-8f, -1f),
                new Vector2(8f, 1f),
                LabTheme.AccentInk);
            var vertical = CreatePanel(
                "Crosshair Vertical",
                parent,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(-1f, -8f),
                new Vector2(1f, 8f),
                LabTheme.AccentInk);
            horizontal.GetComponent<Image>().raycastTarget = false;
            vertical.GetComponent<Image>().raycastTarget = false;
        }

        private IEnumerator AnimateInspector(bool visible)
        {
            if (visible)
            {
                inspectorPanel.SetActive(true);
            }

            inspectorGroup.interactable = false;
            inspectorGroup.blocksRaycasts = false;
            var fromAlpha = inspectorGroup.alpha;
            var toAlpha = visible ? 1f : 0f;
            var fromPosition = inspectorRect.anchoredPosition;
            var toPosition = new Vector2(visible ? 0f : 36f, 0f);
            var elapsed = 0f;

            while (elapsed < LabTheme.DurationShort)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(elapsed / LabTheme.DurationShort);
                var eased = 1f - Mathf.Pow(1f - t, 4f);
                inspectorGroup.alpha = Mathf.Lerp(fromAlpha, toAlpha, eased);
                inspectorRect.anchoredPosition = Vector2.LerpUnclamped(fromPosition, toPosition, eased);
                yield return null;
            }

            inspectorGroup.alpha = toAlpha;
            inspectorRect.anchoredPosition = toPosition;
            inspectorGroup.interactable = visible;
            inspectorGroup.blocksRaycasts = visible;
            if (!visible)
            {
                inspectorPanel.SetActive(false);
            }
        }

        private IEnumerator HideTransientLater()
        {
            yield return new WaitForSecondsRealtime(3.6f);
            transientText.transform.parent.gameObject.SetActive(false);
        }

        private static GameObject CreatePanel(
            string name,
            Transform parent,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 offsetMin,
            Vector2 offsetMax,
            Color colour)
        {
            var panel = new GameObject(name, typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(parent, false);
            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            var image = panel.GetComponent<Image>();
            image.color = colour;
            image.raycastTarget = false;
            return panel;
        }

        private static Text CreateText(
            string name,
            Transform parent,
            string content,
            Font font,
            int fontSize,
            FontStyle style,
            Color colour,
            TextAnchor alignment,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 offsetMin,
            Vector2 offsetMax)
        {
            var textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(parent, false);
            var rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;

            var text = textObject.GetComponent<Text>();
            text.text = content;
            text.font = font;
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.color = colour;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.supportRichText = true;
            text.raycastTarget = false;
            text.lineSpacing = 1.08f;
            return text;
        }

        private Button CreateButton(
            string name,
            Transform parent,
            string label,
            Vector2 offsetMin,
            Vector2 offsetMax,
            UnityEngine.Events.UnityAction onClick)
        {
            var buttonObject = new GameObject(
                name,
                typeof(RectTransform),
                typeof(Image),
                typeof(Outline),
                typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            var rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.zero;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;

            var image = buttonObject.GetComponent<Image>();
            image.color = LabTheme.Graphite;
            image.raycastTarget = true;

            var focusOutline = buttonObject.GetComponent<Outline>();
            focusOutline.effectColor = LabTheme.Focus;
            focusOutline.effectDistance = new Vector2(2f, -2f);
            focusOutline.useGraphicAlpha = false;
            focusOutline.enabled = false;

            var button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.ColorTint;
            button.colors = new ColorBlock
            {
                normalColor = LabTheme.Graphite,
                highlightedColor = LabTheme.GraphiteRaised,
                pressedColor = LabTheme.AccentDeep,
                selectedColor = LabTheme.GraphiteRaised,
                disabledColor = LabTheme.Rule,
                colorMultiplier = 1f,
                fadeDuration = LabTheme.DurationMicro
            };
            button.onClick.AddListener(onClick);

            CreateText(
                "Label",
                buttonObject.transform,
                label,
                bodyFont,
                14,
                FontStyle.Bold,
                LabTheme.GraphiteInk,
                TextAnchor.MiddleCenter,
                Vector2.zero,
                Vector2.one,
                new Vector2(14f, 0f),
                new Vector2(-14f, 0f));

            var feedback = buttonObject.AddComponent<DesktopLabButtonFeedback>();
            feedback.Initialise(game, focusOutline);
            return button;
        }

        private static void Stretch(
            RectTransform rect,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 offsetMin,
            Vector2 offsetMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }
    }

    public sealed class DesktopLabButtonFeedback :
        MonoBehaviour,
        IPointerEnterHandler,
        IPointerDownHandler,
        ISelectHandler,
        IDeselectHandler
    {
        private DesktopLabGame game;
        private Outline focusOutline;

        public void Initialise(DesktopLabGame owner, Outline outline)
        {
            game = owner;
            focusOutline = outline;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            PlayHover();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (game != null && game.AudioSystem != null)
            {
                game.AudioSystem.PlayUiClick();
            }
        }

        public void OnSelect(BaseEventData eventData)
        {
            if (focusOutline != null)
            {
                focusOutline.enabled = true;
            }

            PlayHover();
        }

        public void OnDeselect(BaseEventData eventData)
        {
            if (focusOutline != null)
            {
                focusOutline.enabled = false;
            }
        }

        private void PlayHover()
        {
            if (game != null && game.AudioSystem != null)
            {
                game.AudioSystem.PlayUiHover();
            }
        }
    }
}

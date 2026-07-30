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
        private Text reducedMotionButtonText;
        private Text fullscreenButtonText;
        private Text debugText;
        private Text playerSafetyText;
        private Text respiratorButtonText;
        private Text gasTrapButtonText;
        private Text reactionTitleText;
        private Text reactionEquationText;
        private Text reactionDetailsText;
        private Text languageButtonText;
        private GameObject inspectorPanel;
        private CanvasGroup inspectorGroup;
        private RectTransform inspectorRect;
        private GameObject pauseOverlay;
        private GameObject mainMenuOverlay;
        private GameObject settingsOverlay;
        private GameObject reactionOverlay;
        private GameObject debugPanel;
        private Button resumeButton;
        private Button mainMenuStartButton;
        private Button settingsBackButton;
        private GameObject selectedSection;
        private GameObject vesselSection;
        private Coroutine inspectorAnimation;
        private Coroutine transientAnimation;
        private bool inspectorVisible;
        private bool settingsReturnToMainMenu;

        public bool InspectorVisible
        {
            get { return inspectorVisible; }
        }

        public LabLanguage DisplayLanguage { get; private set; }

        public bool LanguageUiReady
        {
            get
            {
                return languageButtonText != null
                    && DisplayLanguage == LabLocalization.Current;
            }
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

        public int MenuButtonCount
        {
            get
            {
                var count = 0;
                if (mainMenuOverlay != null)
                {
                    count += mainMenuOverlay.GetComponentsInChildren<Button>(true).Length;
                }

                if (pauseOverlay != null)
                {
                    count += pauseOverlay.GetComponentsInChildren<Button>(true).Length;
                }

                if (settingsOverlay != null)
                {
                    count += settingsOverlay.GetComponentsInChildren<Button>(true).Length;
                }

                return count;
            }
        }

        public bool MainMenuVisible
        {
            get { return mainMenuOverlay != null && mainMenuOverlay.activeSelf; }
        }

        public bool SettingsVisible
        {
            get { return settingsOverlay != null && settingsOverlay.activeSelf; }
        }

        public bool PauseMenuVisible
        {
            get { return pauseOverlay != null && pauseOverlay.activeSelf; }
        }

        public bool PointerInputReady
        {
            get
            {
                var eventSystem = UnityEngine.Object.FindAnyObjectByType<EventSystem>();
                return rootCanvas != null
                    && rootCanvas.GetComponent<GraphicRaycaster>() != null
                    && eventSystem != null
                    && eventSystem.GetComponent<BaseInputModule>() != null;
            }
        }

        public bool RuntimeUiReady
        {
            get
            {
                return rootCanvas != null
                    && pauseOverlay != null
                    && mainMenuOverlay != null
                    && settingsOverlay != null
                    && debugPanel != null
                    && resumeButton != null
                    && mainMenuStartButton != null
                    && settingsBackButton != null
                    && playerSafetyText != null
                    && reactionOverlay != null
                    && LanguageUiReady
                    && PauseButtonCount == 3
                    && MenuButtonCount == 11
                    && PointerInputReady;
            }
        }

        public bool VerifyResumePointerRouting()
        {
            if (!PointerInputReady || resumeButton == null || game == null || game.Player == null)
            {
                return false;
            }

            game.Player.SetPausedFromUi(true);
            ShowPauseMenu();
            Canvas.ForceUpdateCanvases();

            var eventSystem = UnityEngine.Object.FindAnyObjectByType<EventSystem>();
            var rect = resumeButton.transform as RectTransform;
            if (eventSystem == null || rect == null)
            {
                return false;
            }

            var pointer = new PointerEventData(eventSystem)
            {
                button = PointerEventData.InputButton.Left,
                position = RectTransformUtility.WorldToScreenPoint(
                    rootCanvas.worldCamera,
                    rect.position)
            };
            var hits = new List<RaycastResult>();
            var raycaster = rootCanvas.GetComponent<GraphicRaycaster>();
            raycaster.Raycast(pointer, hits);
            var pointerDiagnostics = new StringBuilder(256);
            pointerDiagnostics.Append("DESKTOP_LAB_POINTER_TEST screen=")
                .Append(Screen.width).Append('x').Append(Screen.height)
                .Append(" pointer=")
                .Append(pointer.position.x.ToString("0.0")).Append(',')
                .Append(pointer.position.y.ToString("0.0"))
                .Append(" rect=")
                .Append(rect.position.x.ToString("0.0")).Append(',')
                .Append(rect.position.y.ToString("0.0"))
                .Append(" hits=");
            for (var hitIndex = 0; hitIndex < hits.Count; hitIndex++)
            {
                if (hitIndex > 0)
                {
                    pointerDiagnostics.Append('|');
                }

                pointerDiagnostics.Append(hits[hitIndex].gameObject.name);
            }

            Debug.Log(pointerDiagnostics.ToString());
            var resumeWasHit = false;
            for (var index = 0; index < hits.Count; index++)
            {
                if (hits[index].gameObject == resumeButton.gameObject)
                {
                    resumeWasHit = true;
                    break;
                }
            }

            if (resumeWasHit)
            {
                ExecuteEvents.Execute(
                    resumeButton.gameObject,
                    pointer,
                    ExecuteEvents.pointerClickHandler);
            }

            return resumeWasHit
                && !game.Player.IsPaused
                && !MainMenuVisible
                && !PauseMenuVisible
                && !SettingsVisible;
        }

        public void Initialise(DesktopLabGame owner)
        {
            game = owner;
            bodyFont = LabTheme.CreateBodyFont(16);
            displayFont = LabTheme.CreateDisplayFont(22);
            monoFont = LabTheme.CreateMonoFont(14);
            BuildInterface();
            RefreshLanguage();
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

            safetyText.text = safe
                ? LabLocalization.Text("AN TOÀN", "SAFE")
                : LabLocalization.Text("ĐÃ KHÓA", "LOCKED");
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
            playerSafetyText.text = LabLocalization.IsEnglish
                ? "HEALTH  " + state.Health.ToString("0.0") + " / 100"
                  + "     CREDITS  " + state.Credits + "\n"
                  + "RESPIRATOR  " + (state.RespiratorEquipped ? "WORN" : state.RespiratorOwned ? "REMOVED" : "NOT OWNED")
                  + "     GAS TRAP  " + (state.GasTrapConnected ? "CONNECTED" : "DISCONNECTED") + "\n"
                  + (incident == null ? "No incident recorded." : "Safety incident · review the warning and controls.")
                : "SỨC KHỎE  " + state.Health.ToString("0.0") + " / 100"
                  + "     TÍN DỤNG  " + state.Credits + "\n"
                  + "MẶT NẠ  " + (state.RespiratorEquipped ? "ĐANG ĐEO" : state.RespiratorOwned ? "ĐÃ THÁO" : "CHƯA MUA")
                  + "     BÌNH CÁCH LY  " + (state.GasTrapConnected ? "ĐÃ NỐI" : "CHƯA NỐI") + "\n"
                  + (incident == null ? "Chưa ghi nhận sự cố." : incident.Title + " · " + incident.Message);
            playerSafetyText.color = warning ? LabTheme.Warning : LabTheme.GraphiteInk;

            if (respiratorButtonText != null)
            {
                respiratorButtonText.text = !state.RespiratorOwned
                    ? LabLocalization.Text("PPE / F6 · MUA · ", "PPE / F6 · BUY · ") + LabSafetySystem.RespiratorPrice
                    : state.RespiratorEquipped
                        ? LabLocalization.Text("PPE / F6 · THÁO", "PPE / F6 · REMOVE")
                        : LabLocalization.Text("PPE / F6 · ĐEO", "PPE / F6 · WEAR");
            }

            if (gasTrapButtonText != null)
            {
                gasTrapButtonText.text = state.GasTrapConnected
                    ? LabLocalization.Text("HỆ RỬA KHÍ / F7 · THÁO", "GAS TRAP / F7 · DISCONNECT")
                    : LabLocalization.Text("HỆ RỬA KHÍ / F7 · NỐI", "GAS TRAP / F7 · CONNECT");
            }

            if (safetyText != null)
            {
                safetyText.text = warning
                    ? LabLocalization.Text("CẢNH BÁO", "WARNING")
                    : LabLocalization.Text("AN TOÀN", "SAFE");
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
                ? LabLocalization.Text("NHIỆM VỤ HOÀN THÀNH\n", "MISSION COMPLETE\n") + title
                : LabLocalization.Text("NHIỆM VỤ ĐANG GHIM\n", "PINNED MISSION\n") + title;
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

        public bool ReactionPresentationVisible
        {
            get { return reactionOverlay != null && reactionOverlay.activeSelf; }
        }

        public void ShowReactionPresentation(ReactionOutcome outcome, LabStation station)
        {
            if (reactionOverlay == null || outcome == null)
            {
                return;
            }

            reactionTitleText.text = LabLocalization.IsEnglish
                ? "REACTION · " + DesktopLabGame.ZoneLabel(station)
                : outcome.Title + " · " + DesktopLabGame.ZoneLabel(station);
            reactionEquationText.text = string.IsNullOrWhiteSpace(outcome.Equation)
                ? LabLocalization.Text("Chưa xác định phương trình", "Equation not identified")
                : outcome.Equation;
            reactionDetailsText.text =
                LabLocalization.Text("ĐIỀU KIỆN  ", "CONDITIONS  ") + LocalizeCondition(outcome) + "\n"
                + LabLocalization.Text("XÚC TÁC  ", "CATALYST  ") + LocalizeCatalyst(outcome.CatalystSummary) + "\n"
                + LabLocalization.Text("HIỆN TƯỢNG  ", "OBSERVATION  ") + LocalizeObservation(outcome) + "\n"
                + LabLocalization.Text("SPACE / E · BỎ QUA GÓC CẬN", "SPACE / E · SKIP CLOSE-UP");
            reactionOverlay.SetActive(true);
        }

        public void HideReactionPresentation()
        {
            if (reactionOverlay != null)
            {
                reactionOverlay.SetActive(false);
            }
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
                selectedNameText.text = LabLocalization.Text("Chưa cầm mẫu", "No sample in hand");
                selectedDetailsText.text =
                    LabLocalization.Text(
                        "Đến tủ hóa chất, đặt tâm ngắm lên một chai và nhấn E.\n\n"
                        + "KHO ĐIỀU CHẾ\n" + inventoryCount
                        + " lô · nhấn I để chọn lô đã lưu.",
                        "Go to chemical storage, aim at a bottle and press E.\n\n"
                        + "SYNTHESIZED INVENTORY\n" + inventoryCount
                        + " batch(es) · press I to select a saved batch.");
                return;
            }

            selectedFormulaText.color = LabTheme.Ink;
            selectedFormulaText.text = chemical.Formula;
            selectedNameText.text = chemical.Name + " · " + chemical.PhaseLabel;
            selectedDetailsText.text =
                LabLocalization.Text("ĐỊNH LƯỢNG\n", "AMOUNT\n")
                + amountGrams.ToString("0.#") + LabLocalization.Text(" g  ·  [ / ] để thay đổi\n\n", " g  ·  [ / ] to adjust\n\n")
                + LabLocalization.Text("PHÂN LOẠI\n", "CLASS\n") + chemical.FamilyLabel + "\n\n"
                + LabLocalization.Text("KHỐI LƯỢNG MOL\n", "MOLAR MASS\n") + chemical.MolarMass.ToString("0.000") + " g/mol\n\n"
                + LabLocalization.Text("KHỐI LƯỢNG RIÊNG\n", "DENSITY\n") + chemical.Density + "\n\n"
                + LabLocalization.Text("NÓNG CHẢY\n", "MELTING POINT\n") + chemical.MeltingPoint + "\n\n"
                + LabLocalization.Text("SÔI / PHÂN HỦY\n", "BOILING / DECOMPOSITION\n") + chemical.BoilingPoint + "\n\n"
                + LabLocalization.Text("NGOẠI QUAN\n", "APPEARANCE\n") + chemical.Appearance + "\n\n"
                + LabLocalization.Text("ĐỘ TAN\n", "SOLUBILITY\n") + chemical.Solubility + "\n\n"
                + LabLocalization.Text("TÍNH PHẢN ỨNG\n", "REACTIVITY\n") + chemical.ReactivitySummary + "\n\n"
                + LabLocalization.Text("CẢNH BÁO\n", "HAZARDS\n") + chemical.Hazards + "\n\n"
                + LabLocalization.Text("THAO TÁC\n", "HANDLING\n") + chemical.Handling + "\n\n"
                + LabLocalization.Text("ỨNG DỤNG\n", "USE\n") + chemical.Use
                + LabLocalization.Text("\n\nKHO ĐIỀU CHẾ\n", "\n\nSYNTHESIZED INVENTORY\n")
                + (batch == null
                    ? inventoryCount + LabLocalization.Text(
                        " lô · nhấn I để chọn lô đã lưu.",
                        " batch(es) · press I to select a saved batch.")
                    : LabLocalization.Text("Lô ", "Batch ")
                      + batch.BatchId.Substring(0, Mathf.Min(8, batch.BatchId.Length))
                      + LabLocalization.Text(" · còn ", " · remaining ") + batch.AvailableGrams.ToString("0.000") + " g"
                      + LabLocalization.Text(" · tinh khiết ", " · purity ") + (batch.PurityFraction * 100f).ToString("0.0") + "%\n"
                      + LabLocalization.Text("Nguồn: ", "Source: ") + batch.SourceEquation);
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
                LabLocalization.Text("NGUYÊN TỬ KHỐI\n", "ATOMIC MASS\n") + element.AtomicMass.ToString("0.###") + " u\n\n"
                + LabLocalization.Text("CHU KỲ / NHÓM\n", "PERIOD / GROUP\n") + element.Period + " / "
                + (element.Group <= 0 ? LabLocalization.Text("họ actini", "actinide") : element.Group.ToString()) + "\n\n"
                + LabLocalization.Text("CẤU HÌNH ELECTRON\n", "ELECTRON CONFIGURATION\n") + element.ElectronConfiguration + "\n\n"
                + LabLocalization.Text("TRẠNG THÁI · 25 °C\n", "PHASE · 25 °C\n") + element.Phase + "\n\n"
                + LabLocalization.Text("NGOẠI QUAN / MÀU\n", "APPEARANCE / COLOUR\n") + element.Appearance + "\n\n"
                + LabLocalization.Text("KHỐI LƯỢNG RIÊNG\n", "DENSITY\n") + element.Density + "\n\n"
                + LabLocalization.Text("NÓNG CHẢY\n", "MELTING POINT\n") + element.MeltingPoint + "\n\n"
                + LabLocalization.Text("SÔI / THĂNG HOA\n", "BOILING / SUBLIMATION\n") + element.BoilingPoint + "\n\n"
                + LabLocalization.Text("SỐ OXI HÓA PHỔ BIẾN\n", "COMMON OXIDATION STATES\n") + element.OxidationStates + "\n\n"
                + LabLocalization.Text("TÍNH CHẤT HÓA HỌC\n", "CHEMICAL PROPERTIES\n") + element.ChemicalProperties + "\n\n"
                + LabLocalization.Text("TRONG TỰ NHIÊN\n", "OCCURRENCE\n") + element.Occurrence;
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

            vesselTitleText.text = LabLocalization.IsEnglish
                ? LocalizeReactionStatus(outcome.Status)
                : outcome.Title;
            vesselEquationText.text = outcome.Equation;

            var builder = new StringBuilder();
            builder.Append(LabLocalization.Text("VỊ TRÍ\n", "LOCATION\n"));
            builder.Append(DesktopLabGame.ZoneLabel(station));
            builder.Append(LabLocalization.Text("\n\nTHÀNH PHẦN\n", "\n\nCONTENTS\n"));
            if (additions == null || additions.Count == 0)
            {
                builder.Append(LabLocalization.Text(
                    "Cốc sạch — chưa nạp hóa chất",
                    "Clean vessel — no chemical loaded"));
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

            builder.Append(LabLocalization.Text("\nĐIỀU KIỆN HIỆN TẠI\n", "\nCURRENT CONDITIONS\n"));
            builder.Append(LocalizeCondition(outcome));
            builder.Append(LabLocalization.Text("\nXÚC TÁC\n", "\nCATALYST\n"));
            builder.Append(LocalizeCatalyst(outcome.CatalystSummary));

            if (outcome.Status == ReactionStatus.Reaction)
            {
                var limiting = RuntimeChemicalRegistry.GetChemical(outcome.LimitingChemicalId);
                builder.Append(LabLocalization.Text("\nNGUỒN MÔ PHỎNG\n", "\nSIMULATION SOURCE\n"));
                builder.Append(outcome.GeneratedByRule
                    ? LabLocalization.Text("Luật suy diễn · ", "Inference rule · ") + outcome.RuleFamily
                    : LabLocalization.Text("Phản ứng mẫu đã duyệt", "Reviewed reference reaction"));
                if (outcome.IsRedox)
                {
                    builder.Append(LabLocalization.Text("\n\nOXI HÓA–KHỬ\n", "\n\nREDOX\n"));
                    builder.Append(outcome.ElectronTransferCount);
                    builder.Append(LabLocalization.Text(
                        " e⁻ trao đổi sau khi quy đồng hai bán phản ứng",
                        " e⁻ transferred after balancing both half-reactions"));
                }

                builder.Append(LabLocalization.Text("\n\nĐỘNG HỌC ƯỚC TÍNH\n", "\n\nESTIMATED KINETICS\n"));
                builder.Append(outcome.RateClass);
                builder.Append(LabLocalization.Text(" · hệ số ", " · multiplier "));
                builder.Append(outcome.RateMultiplier.ToString("0.00"));
                builder.Append("× · ");
                builder.Append(outcome.EstimatedCompletionSeconds.ToString("0.0"));
                builder.Append(" s");
                if (outcome.GeneratedByRule)
                {
                    builder.Append(LabLocalization.Text("\n\nĐỘ TIN CẬY SẢN PHẨM\n", "\n\nPRODUCT CONFIDENCE\n"));
                    builder.Append(outcome.ProductConfidence);
                    builder.Append(LabLocalization.Text("\n\nCƠ SỞ ƯỚC TÍNH\n", "\n\nESTIMATION BASIS\n"));
                    builder.Append(outcome.GeneratedPropertyBasis);
                    if (outcome.ProductHazards != ChemicalHazardFlags.None)
                    {
                        builder.Append(LabLocalization.Text("\n\nCỜ NGUY HẠI SẢN PHẨM\n", "\n\nPRODUCT HAZARD FLAGS\n"));
                        builder.Append(outcome.ProductHazards);
                    }
                }

                builder.Append(LabLocalization.Text("\n\nCHẤT GIỚI HẠN\n", "\n\nLIMITING REAGENT\n"));
                builder.Append(limiting == null ? "—" : limiting.Formula);
                builder.Append(LabLocalization.Text("\n\nSẢN LƯỢNG LÝ THUYẾT\n", "\n\nTHEORETICAL YIELD\n"));
                builder.Append(outcome.TheoreticalProductGrams.ToString("0.000"));
                builder.Append(LabLocalization.Text(" g\n\nƯỚC TÍNH THU ĐƯỢC\n", " g\n\nESTIMATED RECOVERY\n"));
                builder.Append(outcome.EstimatedProductGrams.ToString("0.000"));
                builder.Append(LabLocalization.Text(" g\n\nĐỘ TINH KHIẾT LÔ\n", " g\n\nBATCH PURITY\n"));
                builder.Append((outcome.ProductPurity * 100f).ToString("0.0"));
                builder.Append(LabLocalization.Text("%\n\nTHU SẢN PHẨM\n", "%\n\nCOLLECT PRODUCT\n"));
                builder.Append(outcome.Effect == ReactionEffect.Gas
                    ? LabLocalization.Text(
                        "C · cần tủ hút + hệ rửa khí đã nối",
                        "C · requires fume hood + connected gas trap")
                    : LabLocalization.Text(
                        "C hoặc nhấn E tại bình khi tay trống",
                        "C or press E at the vessel with empty hands"));
                builder.Append(LabLocalization.Text("\n\nQUAN SÁT\n", "\n\nOBSERVATION\n"));
                builder.Append(LocalizeObservation(outcome));
                if (outcome.Hazard != null)
                {
                    builder.Append(LabLocalization.Text("\n\nKHÍ / HƠI NGUY HIỂM\n", "\n\nHAZARDOUS GAS / VAPOUR\n"));
                    builder.Append(outcome.Hazard.Formula);
                    builder.Append(" · ");
                    builder.Append(outcome.Hazard.Severity);
                    builder.Append("\n");
                    builder.Append(outcome.Hazard.Warning);
                }
            }
            else
            {
                builder.Append(LabLocalization.Text("\n\nTRẠNG THÁI\n", "\n\nSTATUS\n"));
                builder.Append(LocalizeObservation(outcome));
            }

            builder.Append(LabLocalization.Text("\n\nAN TOÀN / XỬ LÝ\n", "\n\nSAFETY / HANDLING\n"));
            builder.Append(LabLocalization.IsEnglish
                ? "Follow the PPE, ventilation and isolation warnings shown by the safety system."
                : outcome.Safety);
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
            if (!paused)
            {
                HideMenus();
                return;
            }

            ShowPauseMenu();
        }

        public void ShowMainMenu()
        {
            SetMenuState(true, false, false);
            settingsReturnToMainMenu = true;
            if (mainMenuStartButton != null)
            {
                mainMenuStartButton.Select();
            }
        }

        public void ShowPauseMenu()
        {
            SetMenuState(false, true, false);
            settingsReturnToMainMenu = false;
            if (resumeButton != null)
            {
                resumeButton.Select();
            }
        }

        public void ShowSettingsFromMainMenu()
        {
            settingsReturnToMainMenu = true;
            ShowSettings();
        }

        public void ShowSettingsFromPauseMenu()
        {
            settingsReturnToMainMenu = false;
            ShowSettings();
        }

        public void ReturnFromSettings()
        {
            if (settingsReturnToMainMenu)
            {
                ShowMainMenu();
            }
            else
            {
                ShowPauseMenu();
            }
        }

        public void HideMenus()
        {
            SetMenuState(false, false, false);
        }

        public void RefreshLanguage()
        {
            DisplayLanguage = LabLocalization.Current;
            SetNamedText("Pause Title", "TẠM DỪNG THỰC HÀNH", "PRACTICAL PAUSED");
            SetNamedText(
                "Pause Copy",
                "MỤC TIÊU HIỆN TẠI\n"
                + "Tạo kết tủa xanh Cu(OH)₂ từ CuSO₄·5H₂O và NaOH trên bàn giữa.\n\n"
                + "QUY TRÌNH VẬT LÝ\n"
                + "Lấy chai → đặt xuống khay cạnh bình → nhấn E tại bình để nạp.\n"
                + "Phản ứng không xảy ra khi hóa chất còn trên tay.\n\n"
                + "ĐIỀU KHIỂN\n"
                + "Chuột — nhìn    WASD — đi    Shift — chạy    E — tương tác\n"
                + "[ / ] — định lượng    F — dữ liệu    C — thu sản phẩm\n"
                + "Page Up / Down — nhiệt độ    F8 — pha loãng    SPACE — bỏ qua góc cận",
                "CURRENT OBJECTIVE\n"
                + "Create blue Cu(OH)₂ precipitate from CuSO₄·5H₂O and NaOH at the central bench.\n\n"
                + "PHYSICAL WORKFLOW\n"
                + "Take bottle → place it on the tray → press E at the vessel to load it.\n"
                + "No reaction occurs while a chemical is still in your hand.\n\n"
                + "CONTROLS\n"
                + "Mouse — look    WASD — move    Shift — run    E — interact\n"
                + "[ / ] — amount    F — data    C — collect product\n"
                + "Page Up / Down — temperature    F8 — dilute    SPACE — skip close-up");
            SetNamedText("Main Menu Eyebrow", "MÔ PHỎNG HÓA HỌC · PHÒNG THÍ NGHIỆM 3D", "CHEMISTRY SIMULATION · 3D LABORATORY");
            SetNamedText(
                "Main Menu Copy",
                "Tự do khám phá hóa chất, điều kiện phản ứng và an toàn phòng thí nghiệm.\n\n"
                + "NHIỆM VỤ KHỞI ĐẦU\n"
                + "Lấy CuSO₄·5H₂O và NaOH, đặt từng mẫu lên khay cạnh bình rồi nạp để tạo Cu(OH)₂ màu xanh.",
                "Freely explore chemicals, reaction conditions and laboratory safety.\n\n"
                + "STARTER MISSION\n"
                + "Take CuSO₄·5H₂O and NaOH, stage each sample on the vessel tray, then load them to form blue Cu(OH)₂.");
            SetNamedText("Settings Title", "CÀI ĐẶT", "SETTINGS");
            SetNamedText(
                "Settings Copy",
                "Các thay đổi được lưu tự động cho lần chạy tiếp theo.",
                "Changes are saved automatically for the next session.");
            SetNamedText(
                "Movement Controls",
                "WASD  DI CHUYỂN   E  LẤY / ĐẶT / NẠP   PG↑/↓  NHIỆT   C  THU   I  KHO   ESC  DỪNG",
                "WASD  MOVE   E  TAKE / PLACE / LOAD   PG↑/↓  HEAT   C  COLLECT   I  INVENTORY   ESC  PAUSE");
            SetButtonLabel("Help Button", "HƯỚNG DẪN · ESC", "GUIDE · ESC");
            SetButtonLabel("Resume Button", "BẮT ĐẦU / TIẾP TỤC THỰC HÀNH", "START / RESUME PRACTICAL");
            SetButtonLabel("Settings Button", "CÀI ĐẶT", "SETTINGS");
            SetButtonLabel("Back To Main Menu Button", "VỀ MÀN HÌNH CHÍNH", "BACK TO MAIN MENU");
            SetButtonLabel("Start Game Button", "BẮT ĐẦU / TIẾP TỤC", "START / CONTINUE");
            SetButtonLabel("Main Menu Settings Button", "CÀI ĐẶT", "SETTINGS");
            SetButtonLabel("Main Menu Quit Button", "THOÁT RA DESKTOP", "QUIT TO DESKTOP");
            SetButtonLabel("Settings Back Button", "QUAY LẠI", "BACK");
            if (languageButtonText != null)
            {
                languageButtonText.text = LabLocalization.IsEnglish
                    ? "LANGUAGE · ENGLISH"
                    : "NGÔN NGỮ · TIẾNG VIỆT";
            }
        }

        public void SetAccessibilityState(bool reducedMotion)
        {
            if (accessibilityText != null)
            {
                accessibilityText.text = reducedMotion
                    ? LabLocalization.Text("F10 · MOTION GIẢM", "F10 · REDUCED MOTION")
                    : LabLocalization.Text("F10 · MOTION ĐẦY", "F10 · FULL MOTION");
            }

            if (reducedMotionButtonText != null)
            {
                reducedMotionButtonText.text = reducedMotion
                    ? LabLocalization.Text("GIẢM CHUYỂN ĐỘNG · BẬT", "REDUCED MOTION · ON")
                    : LabLocalization.Text("GIẢM CHUYỂN ĐỘNG · TẮT", "REDUCED MOTION · OFF");
            }
        }

        public void SetAudioState(bool enabled)
        {
            var state = enabled
                ? LabLocalization.Text("BẬT", "ON")
                : LabLocalization.Text("TẮT", "OFF");
            if (audioStatusText != null)
            {
                audioStatusText.text = LabLocalization.Text("F9 · ÂM ", "F9 · AUDIO ") + state;
            }

            if (audioButtonText != null)
            {
                audioButtonText.text = LabLocalization.Text("ÂM THANH · ", "AUDIO · ") + state;
            }
        }

        public void SetFullscreenState(bool fullscreen)
        {
            if (fullscreenButtonText != null)
            {
                fullscreenButtonText.text = fullscreen
                    ? LabLocalization.Text("MÀN HÌNH · TOÀN MÀN HÌNH", "DISPLAY · FULLSCREEN")
                    : LabLocalization.Text("MÀN HÌNH · CỬA SỔ", "DISPLAY · WINDOWED");
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
            var eventSystem = UnityEngine.Object.FindAnyObjectByType<EventSystem>();
            if (eventSystem == null)
            {
                var eventSystemObject = new GameObject(
                    "Desktop UI Event System",
                    typeof(EventSystem),
                    typeof(StandaloneInputModule));
                eventSystemObject.transform.SetParent(transform, false);
                eventSystem = eventSystemObject.GetComponent<EventSystem>();
            }
            else if (eventSystem.GetComponent<BaseInputModule>() == null)
            {
                eventSystem.gameObject.AddComponent<StandaloneInputModule>();
            }

            var canvasObject = new GameObject(
                "Desktop HUD",
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
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
                new Vector2(238f, 0f),
                Vector2.zero);

            CreateButton(
                "Help Button",
                topBar.transform,
                "HƯỚNG DẪN · ESC",
                new Vector2(20f, 9f),
                new Vector2(220f, 51f),
                game.OpenHelpFromUi);

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
            CreateReactionPresentation(canvasObject.transform);
            CreateMainMenuOverlay(canvasObject.transform);
            CreatePauseOverlay(canvasObject.transform);
            CreateSettingsOverlay(canvasObject.transform);
        }

        private void CreateReactionPresentation(Transform parent)
        {
            reactionOverlay = CreatePanel(
                "Reaction Presentation",
                parent,
                new Vector2(0.25f, 0.66f),
                new Vector2(0.78f, 0.93f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                Vector2.zero,
                LabTheme.WithAlpha(LabTheme.Graphite, 0.92f));
            reactionOverlay.GetComponent<Image>().raycastTarget = false;

            reactionTitleText = CreateText(
                "Reaction Presentation Title",
                reactionOverlay.transform,
                "PHẢN ỨNG",
                displayFont,
                20,
                FontStyle.Bold,
                LabTheme.GraphiteInk,
                TextAnchor.UpperLeft,
                new Vector2(0f, 1f),
                Vector2.one,
                new Vector2(26f, -48f),
                new Vector2(-26f, -14f));
            reactionEquationText = CreateText(
                "Reaction Presentation Equation",
                reactionOverlay.transform,
                "—",
                monoFont,
                27,
                FontStyle.Bold,
                LabTheme.Focus,
                TextAnchor.MiddleCenter,
                new Vector2(0f, 0.38f),
                new Vector2(1f, 0.78f),
                new Vector2(24f, 0f),
                new Vector2(-24f, 0f));
            reactionDetailsText = CreateText(
                "Reaction Presentation Details",
                reactionOverlay.transform,
                string.Empty,
                bodyFont,
                15,
                FontStyle.Normal,
                LabTheme.GraphiteInk,
                TextAnchor.UpperLeft,
                Vector2.zero,
                new Vector2(1f, 0.38f),
                new Vector2(26f, 10f),
                new Vector2(-26f, -8f));

            reactionTitleText.raycastTarget = false;
            reactionEquationText.raycastTarget = false;
            reactionDetailsText.raycastTarget = false;
            reactionOverlay.SetActive(false);
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
                "WASD  DI CHUYỂN   E  LẤY / ĐẶT / NẠP   PG↑/↓  NHIỆT   C  THU   I  KHO   ESC  DỪNG",
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
                "PPE / F6 · MUA · 250",
                new Vector2(14f, 12f),
                new Vector2(187f, 50f),
                game.ToggleRespirator);
            respiratorButtonText = respiratorButton.GetComponentInChildren<Text>();

            var trapButton = CreateButton(
                "Gas Trap Button",
                panel.transform,
                "HỆ RỬA KHÍ / F7 · NỐI",
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
                "TẠM DỪNG THỰC HÀNH",
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
                "MỤC TIÊU HIỆN TẠI\n"
                + "Tạo kết tủa xanh Cu(OH)₂ từ CuSO₄·5H₂O và NaOH trên bàn giữa.\n\n"
                + "QUY TRÌNH VẬT LÝ\n"
                + "Lấy chai → đặt xuống khay cạnh bình → nhấn E tại bình để nạp.\n"
                + "Phản ứng không xảy ra khi hóa chất còn trên tay.\n\n"
                + "ĐIỀU KHIỂN\n"
                + "Chuột — nhìn    WASD — đi    Shift — chạy    E — tương tác\n"
                + "[ / ] — định lượng    F — dữ liệu    C — thu sản phẩm\n"
                + "Page Up / Down — nhiệt độ    F8 — pha loãng    SPACE — bỏ qua góc cận",
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
                "BẮT ĐẦU / TIẾP TỤC THỰC HÀNH",
                new Vector2(36f, 120f),
                new Vector2(604f, 174f),
                game.ResumeFromUi);

            CreateButton(
                "Settings Button",
                card.transform,
                "CÀI ĐẶT",
                new Vector2(36f, 50f),
                new Vector2(304f, 104f),
                ShowSettingsFromPauseMenu);

            CreateButton(
                "Back To Main Menu Button",
                card.transform,
                "VỀ MÀN HÌNH CHÍNH",
                new Vector2(326f, 50f),
                new Vector2(604f, 104f),
                game.ReturnToMainMenuFromUi);
        }

        private void CreateMainMenuOverlay(Transform parent)
        {
            mainMenuOverlay = CreatePanel(
                "Main Menu Overlay",
                parent,
                Vector2.zero,
                Vector2.one,
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                Vector2.zero,
                LabTheme.WithAlpha(LabTheme.Graphite, 0.82f));
            mainMenuOverlay.GetComponent<Image>().raycastTarget = true;

            var card = CreatePanel(
                "Main Menu Card",
                mainMenuOverlay.transform,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(-320f, -245f),
                new Vector2(320f, 245f),
                LabTheme.PaperRaised);

            CreateText(
                "Main Menu Eyebrow",
                card.transform,
                "MÔ PHỎNG HÓA HỌC · PHÒNG THÍ NGHIỆM 3D",
                bodyFont,
                13,
                FontStyle.Bold,
                LabTheme.AccentDeep,
                TextAnchor.UpperLeft,
                Vector2.zero,
                Vector2.one,
                new Vector2(36f, 410f),
                new Vector2(-36f, -28f));

            CreateText(
                "Main Menu Title",
                card.transform,
                "CHEMISTRY LAB",
                displayFont,
                38,
                FontStyle.Bold,
                LabTheme.Ink,
                TextAnchor.UpperLeft,
                Vector2.zero,
                Vector2.one,
                new Vector2(36f, 334f),
                new Vector2(-36f, -68f));

            CreateText(
                "Main Menu Copy",
                card.transform,
                "Tự do khám phá hóa chất, điều kiện phản ứng và an toàn phòng thí nghiệm.\n\n"
                + "NHIỆM VỤ KHỞI ĐẦU\n"
                + "Lấy CuSO₄·5H₂O và NaOH, đặt từng mẫu lên khay cạnh bình rồi nạp để tạo Cu(OH)₂ màu xanh.",
                bodyFont,
                16,
                FontStyle.Normal,
                LabTheme.InkSoft,
                TextAnchor.UpperLeft,
                Vector2.zero,
                Vector2.one,
                new Vector2(36f, 210f),
                new Vector2(-36f, -146f));

            mainMenuStartButton = CreateButton(
                "Start Game Button",
                card.transform,
                "BẮT ĐẦU / TIẾP TỤC",
                new Vector2(36f, 120f),
                new Vector2(604f, 174f),
                game.ResumeFromUi);

            CreateButton(
                "Main Menu Settings Button",
                card.transform,
                "CÀI ĐẶT",
                new Vector2(36f, 50f),
                new Vector2(304f, 104f),
                ShowSettingsFromMainMenu);

            CreateButton(
                "Main Menu Quit Button",
                card.transform,
                "THOÁT RA DESKTOP",
                new Vector2(326f, 50f),
                new Vector2(604f, 104f),
                game.QuitToDesktop);
        }

        private void CreateSettingsOverlay(Transform parent)
        {
            settingsOverlay = CreatePanel(
                "Settings Overlay",
                parent,
                Vector2.zero,
                Vector2.one,
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                Vector2.zero,
                LabTheme.WithAlpha(LabTheme.Graphite, 0.82f));
            settingsOverlay.GetComponent<Image>().raycastTarget = true;

            var card = CreatePanel(
                "Settings Card",
                settingsOverlay.transform,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(-320f, -285f),
                new Vector2(320f, 285f),
                LabTheme.PaperRaised);

            CreateText(
                "Settings Title",
                card.transform,
                "CÀI ĐẶT",
                displayFont,
                32,
                FontStyle.Bold,
                LabTheme.Ink,
                TextAnchor.UpperLeft,
                Vector2.zero,
                Vector2.one,
                new Vector2(36f, 470f),
                new Vector2(-36f, -34f));

            CreateText(
                "Settings Copy",
                card.transform,
                "Các thay đổi được lưu tự động cho lần chạy tiếp theo.",
                bodyFont,
                15,
                FontStyle.Normal,
                LabTheme.InkSoft,
                TextAnchor.UpperLeft,
                Vector2.zero,
                Vector2.one,
                new Vector2(36f, 414f),
                new Vector2(-36f, -92f));

            var languageButton = CreateButton(
                "Language Button",
                card.transform,
                "NGÔN NGỮ · TIẾNG VIỆT",
                new Vector2(36f, 320f),
                new Vector2(604f, 374f),
                game.ToggleLanguage);
            languageButtonText = languageButton.GetComponentInChildren<Text>();

            var audioButton = CreateButton(
                "Settings Audio Button",
                card.transform,
                "ÂM THANH · BẬT",
                new Vector2(36f, 250f),
                new Vector2(604f, 304f),
                game.ToggleAudio);
            audioButtonText = audioButton.GetComponentInChildren<Text>();

            var reducedMotionButton = CreateButton(
                "Reduced Motion Button",
                card.transform,
                "GIẢM CHUYỂN ĐỘNG · TẮT",
                new Vector2(36f, 180f),
                new Vector2(604f, 234f),
                game.ToggleReducedMotion);
            reducedMotionButtonText = reducedMotionButton.GetComponentInChildren<Text>();

            var fullscreenButton = CreateButton(
                "Fullscreen Button",
                card.transform,
                "MÀN HÌNH · TOÀN MÀN HÌNH",
                new Vector2(36f, 110f),
                new Vector2(604f, 164f),
                game.ToggleFullscreen);
            fullscreenButtonText = fullscreenButton.GetComponentInChildren<Text>();

            settingsBackButton = CreateButton(
                "Settings Back Button",
                card.transform,
                "QUAY LẠI",
                new Vector2(36f, 40f),
                new Vector2(604f, 94f),
                ReturnFromSettings);
        }

        private void ShowSettings()
        {
            SetMenuState(false, false, true);
            if (settingsBackButton != null)
            {
                settingsBackButton.Select();
            }
        }

        private void SetMenuState(bool mainMenu, bool pause, bool settings)
        {
            if (mainMenuOverlay != null)
            {
                mainMenuOverlay.SetActive(mainMenu);
            }

            if (pauseOverlay != null)
            {
                pauseOverlay.SetActive(pause);
            }

            if (settingsOverlay != null)
            {
                settingsOverlay.SetActive(settings);
            }
        }

        private void SetNamedText(string objectName, string vietnamese, string english)
        {
            if (rootCanvas == null)
            {
                return;
            }

            var texts = rootCanvas.GetComponentsInChildren<Text>(true);
            for (var index = 0; index < texts.Length; index++)
            {
                if (texts[index].gameObject.name == objectName)
                {
                    texts[index].text = LabLocalization.Text(vietnamese, english);
                    return;
                }
            }
        }

        private void SetButtonLabel(string objectName, string vietnamese, string english)
        {
            if (rootCanvas == null)
            {
                return;
            }

            var buttons = rootCanvas.GetComponentsInChildren<Button>(true);
            for (var index = 0; index < buttons.Length; index++)
            {
                if (buttons[index].gameObject.name != objectName)
                {
                    continue;
                }

                var label = buttons[index].GetComponentInChildren<Text>(true);
                if (label != null)
                {
                    label.text = LabLocalization.Text(vietnamese, english);
                }

                return;
            }
        }

        private static string LocalizeCondition(ReactionOutcome outcome)
        {
            if (outcome == null || !LabLocalization.IsEnglish)
            {
                return outcome == null ? "—" : outcome.ConditionSummary;
            }

            return outcome.TemperatureC.ToString("0.#") + " °C · "
                + (outcome.VolumeLitres * 1000d).ToString("0") + " mL · pH "
                + outcome.EstimatedPH.ToString("0.00") + " · "
                + outcome.TotalConcentrationMolar.ToString("0.000") + " M · "
                + outcome.RateClass;
        }

        private static string LocalizeCatalyst(string summary)
        {
            if (!LabLocalization.IsEnglish)
            {
                return summary;
            }

            if (string.IsNullOrWhiteSpace(summary))
            {
                return "No catalyst required";
            }

            if (summary.IndexOf("Không yêu cầu", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "No catalyst required";
            }

            return "Reaction profile: " + summary;
        }

        private static string LocalizeObservation(ReactionOutcome outcome)
        {
            if (outcome == null || !LabLocalization.IsEnglish)
            {
                return outcome == null ? "—" : outcome.Message;
            }

            if (outcome.Status != ReactionStatus.Reaction)
            {
                switch (outcome.Status)
                {
                    case ReactionStatus.Blocked:
                        return "Reaction blocked. Adjust the required conditions or safety controls.";
                    case ReactionStatus.Waiting:
                        return "Waiting for another reagent or a required condition.";
                    case ReactionStatus.NoMatch:
                        return "No supported reaction is predicted for the current mixture.";
                    default:
                        return "The vessel is ready.";
                }
            }

            switch (outcome.Effect)
            {
                case ReactionEffect.Precipitate:
                    return "A solid precipitate forms. Observe the product colour and settling.";
                case ReactionEffect.Gas:
                    return "Gas is released. Keep the vessel in the fume hood and use the gas trap.";
                case ReactionEffect.Heat:
                    return "The mixture changes temperature as the reaction proceeds.";
                case ReactionEffect.Colour:
                    return "A visible colour change occurs in the mixture.";
                default:
                    return "A chemical transformation is observed in the vessel.";
            }
        }

        private static string LocalizeReactionStatus(ReactionStatus status)
        {
            switch (status)
            {
                case ReactionStatus.Reaction: return "REACTION";
                case ReactionStatus.Blocked: return "REACTION BLOCKED";
                case ReactionStatus.Waiting: return "WAITING FOR REAGENT";
                case ReactionStatus.NoMatch: return "NO PREDICTED REACTION";
                default: return "CLEAN VESSEL";
            }
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

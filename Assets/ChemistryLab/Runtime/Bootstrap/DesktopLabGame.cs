using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ChemistryLab.Desktop
{
    public sealed class DesktopLabGame : MonoBehaviour
    {
        private const float BaselineTemperatureC = 24f;
        private const string MissionReactionId = "copper-hydroxide";
        private const string FullscreenPreferenceKey = "chemistryLab.desktop.fullscreen";

        private readonly Dictionary<LabStation, List<VesselAddition>> vesselAdditions =
            new Dictionary<LabStation, List<VesselAddition>>();
        private readonly Dictionary<LabStation, ReactionEnvironment> vesselEnvironments =
            new Dictionary<LabStation, ReactionEnvironment>();
        private readonly Dictionary<LabStation, VesselVisual> vesselVisuals =
            new Dictionary<LabStation, VesselVisual>();

        private readonly Dictionary<string, Material> materials =
            new Dictionary<string, Material>(StringComparer.Ordinal);

        private Transform worldRoot;
        private Transform heldSampleRoot;
        private DesktopLabHud hud;
        private FirstPersonChemistController player;
        private DesktopLabAudio audioSystem;
        private DesktopLabDiagnostics diagnostics;
        private ChemicalDefinition selectedChemical;
        private string selectedBatchId;
        private SynthesizedInventory synthesizedInventory;
        private float selectedAmountGrams = 10f;
        private LabStation currentZone = LabStation.Workbench;
        private LabStation currentVesselStation = LabStation.Workbench;
        private ReactionOutcome currentOutcome;
        private LabSafetySystem labSafety;
        private int starterChemicalCount;
        private bool missionComplete;
        private bool inspectorOpen;

        public DesktopLabHud Hud
        {
            get { return hud; }
        }

        public ChemicalDefinition SelectedChemical
        {
            get { return selectedChemical; }
        }

        public float SelectedAmountGrams
        {
            get { return selectedAmountGrams; }
        }

        public bool InspectorOpen
        {
            get { return inspectorOpen; }
        }

        public DesktopLabAudio AudioSystem
        {
            get { return audioSystem; }
        }

        public FirstPersonChemistController Player
        {
            get { return player; }
        }

        public ReactionOutcome CurrentOutcome
        {
            get { return currentOutcome; }
        }

        public LabSafetySystem SafetySystem
        {
            get { return labSafety; }
        }

        public LabStation CurrentZone
        {
            get { return currentZone; }
        }

        public LabStation CurrentVesselStation
        {
            get { return currentVesselStation; }
        }

        public int SynthesizedBatchCount
        {
            get { return synthesizedInventory == null ? 0 : synthesizedInventory.Count; }
        }

        public int StarterChemicalCount
        {
            get { return starterChemicalCount; }
        }

        public ReactionEnvironment CurrentEnvironment
        {
            get
            {
                ReactionEnvironment environment;
                return vesselEnvironments.TryGetValue(currentVesselStation, out environment)
                    ? environment
                    : null;
            }
        }

        public int GetVesselAdditionCount(LabStation station)
        {
            List<VesselAddition> additions;
            return vesselAdditions.TryGetValue(station, out additions) ? additions.Count : 0;
        }

        private void Awake()
        {
            DesktopChemistryDatabase.ValidateOrThrow();
            HighSchoolPeriodicTable.ValidateOrThrow();
            CompoundGenerationMatrix.ValidateOrThrow();
            DynamicReactionEngine.ValidateOrThrow();
            AirborneHazardCatalog.ValidateOrThrow();
            ReactionConditionEngine.ValidateOrThrow();
            RedoxReactionEngine.ValidateOrThrow();
            SynthesizedInventory.ValidateOrThrow();
            LabSafetySystem.ValidateOrThrow();
            DesktopLabAudio.ValidateSignalGenerationOrThrow();
            Application.targetFrameRate = 120;
            QualitySettings.vSyncCount = 1;
            Screen.fullScreenMode = PlayerPrefs.GetInt(FullscreenPreferenceKey, 1) == 1
                ? FullScreenMode.FullScreenWindow
                : FullScreenMode.Windowed;

            vesselAdditions[LabStation.Workbench] = new List<VesselAddition>();
            vesselAdditions[LabStation.FumeHood] = new List<VesselAddition>();
            vesselEnvironments[LabStation.Workbench] =
                new ReactionEnvironment(BaselineTemperatureC, .100d);
            vesselEnvironments[LabStation.FumeHood] =
                new ReactionEnvironment(BaselineTemperatureC, .100d);

            labSafety = new LabSafetySystem();
            RuntimeChemicalRegistry.ClearRuntime();
            synthesizedInventory = new SynthesizedInventory();
            synthesizedInventory.Load();
            CreateHud();
            BuildWorld();
            BuildAudio();
            BuildPlayer();
            BuildDiagnostics();
            RefreshOutcome(LabStation.Workbench);

            hud.SetSelectedChemical(null, selectedAmountGrams, null, SynthesizedBatchCount);
            hud.SetMission("Tạo kết tủa xanh Cu(OH)₂", false);
            hud.SetZone(ZoneLabel(currentZone));
            hud.SetAudioState(audioSystem != null && !audioSystem.IsMuted);
            hud.SetFullscreenState(Screen.fullScreenMode != FullScreenMode.Windowed);
            hud.SetSafetySystem(labSafety);
            hud.ShowTransient("Bắt đầu tại khay hóa chất trên bàn giữa: lấy CuSO₄·5H₂O và NaOH bằng phím E.");

            if (HasCommandLineFlag("-captureTest"))
            {
                Screen.fullScreenMode = FullScreenMode.Windowed;
                Screen.SetResolution(1600, 900, false);
                StartCoroutine(RunCaptureTest());
            }
            else if (HasCommandLineFlag("-smokeTest"))
            {
                StartCoroutine(RunSmokeTest());
            }
            else
            {
                player.SetPausedFromUi(true);
                hud.ShowMainMenu();
            }
        }

        public void SelectChemical(string chemicalId)
        {
            var next = RuntimeChemicalRegistry.GetChemical(chemicalId);
            if (next == null)
            {
                hud.ShowTransient("Không tìm thấy dữ liệu hóa chất: " + chemicalId, true);
                audioSystem.PlayError();
                return;
            }

            selectedChemical = next;
            selectedBatchId = null;
            UpdateHeldSample();
            hud.SetSelectedChemical(
                selectedChemical,
                selectedAmountGrams,
                null,
                SynthesizedBatchCount);
            hud.ShowChemicalSection();
            ToggleInspector(true);
            var hazard = ChemicalHazardClassifier.Classify(next);
            if (hazard.Severity >= HazardSeverity.Dangerous)
            {
                hud.SetSafety(false, hazard.Message);
                hud.ShowTransient("CẢNH BÁO HÓA CHẤT · " + hazard.Message, true);
                audioSystem.PlayError();
            }
            else
            {
                hud.ShowTransient("Đã lấy " + next.Formula + " · " + next.Name);
                audioSystem.PlaySamplePickup();
            }
        }

        public void ClearSelectedChemical()
        {
            selectedChemical = null;
            selectedBatchId = null;
            UpdateHeldSample();
            hud.SetSelectedChemical(null, selectedAmountGrams, null, SynthesizedBatchCount);
            hud.ShowTransient("Đã cất mẫu đang cầm.");
            audioSystem.PlayUiClick();
        }

        public void AdjustSelectedAmount(float deltaGrams)
        {
            selectedAmountGrams = Mathf.Clamp(selectedAmountGrams + deltaGrams, 1f, 25f);
            hud.SetSelectedChemical(
                selectedChemical,
                selectedAmountGrams,
                GetSelectedBatch(),
                SynthesizedBatchCount);
            audioSystem.PlayUiClick();
        }

        public void AddSelectedToVessel(LabStation station)
        {
            if (selectedChemical == null)
            {
                hud.ShowTransient("Hãy lấy một chai hóa chất trước khi nạp cốc.", true);
                audioSystem.PlayError();
                return;
            }

            List<VesselAddition> additions;
            if (!vesselAdditions.TryGetValue(station, out additions))
            {
                hud.ShowTransient("Vị trí này không có cốc phản ứng.", true);
                audioSystem.PlayError();
                return;
            }

            var additionGrams = selectedAmountGrams;
            var sourceBatch = GetSelectedBatch();
            if (sourceBatch != null)
            {
                additionGrams = (float)Math.Min(additionGrams, sourceBatch.AvailableGrams);
                if (additionGrams <= .0001f)
                {
                    hud.ShowTransient("Lô sản phẩm này đã hết.", true);
                    audioSystem.PlayError();
                    return;
                }
            }

            ReactionEnvironment environment;
            if (!vesselEnvironments.TryGetValue(station, out environment))
            {
                environment = new ReactionEnvironment(BaselineTemperatureC, .100d);
                vesselEnvironments[station] = environment;
            }

            var candidate = new List<VesselAddition>(additions)
            {
                new VesselAddition(selectedChemical.Id, additionGrams)
            };
            var nextOutcome = ReactionSimulator.Evaluate(candidate, station, environment);
            currentVesselStation = station;
            currentOutcome = nextOutcome;

            additions.Add(candidate[candidate.Count - 1]);
            if (sourceBatch != null)
            {
                double consumed;
                synthesizedInventory.TryConsume(sourceBatch.BatchId, additionGrams, out consumed);
                if (synthesizedInventory.Find(sourceBatch.BatchId) == null)
                {
                    selectedBatchId = null;
                    selectedChemical = null;
                    UpdateHeldSample();
                }

                hud.SetSelectedChemical(
                    selectedChemical,
                    selectedAmountGrams,
                    GetSelectedBatch(),
                    SynthesizedBatchCount);
            }
            UpdateVesselVisual(station, additions, nextOutcome);
            audioSystem.PlayPour(GetVesselPosition(station));
            hud.SetVessel(additions, nextOutcome, station);
            hud.SetTemperature(nextOutcome.TemperatureC);
            hud.SetSafety(!nextOutcome.SafetyViolation, nextOutcome.Safety);
            hud.ShowVesselSection();
            ToggleInspector(true);

            if (nextOutcome.Status == ReactionStatus.Reaction)
            {
                var incident = labSafety.Apply(nextOutcome, station);
                hud.SetSafetySystem(labSafety);
                PlayReactionEffect(station, nextOutcome);
                if (!incident.Controlled)
                {
                    hud.ShowTransient(incident.Title + " · " + incident.Message, true);
                    audioSystem.PlayHazardAlarm();
                }
                else
                {
                    hud.ShowTransient(
                        nextOutcome.Title + " · " + nextOutcome.Message
                        + (nextOutcome.GeneratedByRule ? " · suy diễn " + nextOutcome.RuleFamily : string.Empty));
                }
                if (!missionComplete
                    && nextOutcome.Reaction != null
                    && string.Equals(nextOutcome.Reaction.Id, MissionReactionId, StringComparison.Ordinal))
                {
                    missionComplete = true;
                    hud.SetMission("Tạo kết tủa xanh Cu(OH)₂", true);
                }
            }
            else
            {
                hud.ShowTransient(
                    "Đã nạp " + additionGrams.ToString("0.#")
                    + " g " + (selectedChemical == null ? "sản phẩm từ kho" : selectedChemical.Formula) + ".",
                    nextOutcome.Status == ReactionStatus.Blocked);
            }
        }

        public bool CanCollectProduct(LabStation station)
        {
            List<VesselAddition> additions;
            ReactionEnvironment environment;
            if (!vesselAdditions.TryGetValue(station, out additions)
                || !vesselEnvironments.TryGetValue(station, out environment))
            {
                return false;
            }

            var outcome = ReactionSimulator.Evaluate(additions, station, environment);
            return outcome.Status == ReactionStatus.Reaction && outcome.CanCollectProduct;
        }

        public void CollectProduct(LabStation station)
        {
            List<VesselAddition> additions;
            ReactionEnvironment environment;
            if (!vesselAdditions.TryGetValue(station, out additions)
                || !vesselEnvironments.TryGetValue(station, out environment))
            {
                hud.ShowTransient("Không tìm thấy bình phản ứng để thu sản phẩm.", true);
                audioSystem.PlayError();
                return;
            }

            var outcome = ReactionSimulator.Evaluate(additions, station, environment);
            if (outcome.Status != ReactionStatus.Reaction || !outcome.CanCollectProduct)
            {
                hud.ShowTransient("Chưa có sản phẩm đủ điều kiện để thu hồi.", true);
                audioSystem.PlayError();
                return;
            }

            if (outcome.Effect == ReactionEffect.Gas
                && (station != LabStation.FumeHood
                    || labSafety == null
                    || !labSafety.GasTrapConnected))
            {
                hud.ShowTransient(
                    "Sản phẩm khí chỉ được thu trong tủ hút khi bình cách ly đã nối (F7).",
                    true);
                audioSystem.PlayHazardAlarm();
                return;
            }

            var batch = synthesizedInventory.AddProduct(outcome);
            if (batch == null)
            {
                hud.ShowTransient("Không thể tạo lô sản phẩm từ kết quả hiện tại.", true);
                audioSystem.PlayError();
                return;
            }

            additions.Clear();
            environment.Reset(BaselineTemperatureC, .100d);
            selectedBatchId = batch.BatchId;
            selectedChemical = RuntimeChemicalRegistry.GetChemical(batch.ChemicalId);
            currentVesselStation = station;
            UpdateHeldSample();
            RefreshVesselVisual(station);
            RefreshOutcome(station);
            hud.SetSelectedChemical(
                selectedChemical,
                selectedAmountGrams,
                batch,
                SynthesizedBatchCount);
            hud.ShowChemicalSection();
            ToggleInspector(true);
            hud.ShowTransient(
                "Đã lưu lô " + batch.Formula + " · "
                + batch.AvailableGrams.ToString("0.000") + " g · độ tinh khiết "
                + (batch.PurityFraction * 100f).ToString("0.0") + "%.");
            audioSystem.PlaySamplePickup();
        }

        public void AdjustVesselTemperature(float deltaC)
        {
            ReactionEnvironment environment;
            if (!vesselEnvironments.TryGetValue(currentVesselStation, out environment))
            {
                return;
            }

            environment.ChangeTemperature(deltaC);
            RefreshVesselVisual(currentVesselStation);
            RefreshOutcome(currentVesselStation);
            hud.ShowVesselSection();
            hud.ShowTransient(
                (deltaC >= 0f ? "Đã gia nhiệt · " : "Đã làm nguội · ")
                + environment.TemperatureC.ToString("0.#") + " °C.");
            audioSystem.PlayUiClick();
        }

        public void AdjustVesselTemperature(LabStation station, float deltaC)
        {
            currentVesselStation = station;
            AdjustVesselTemperature(deltaC);
        }

        public void DiluteCurrentVessel(double addedMillilitres = 50d)
        {
            ReactionEnvironment environment;
            if (!vesselEnvironments.TryGetValue(currentVesselStation, out environment))
            {
                return;
            }

            environment.Dilute(Math.Max(0d, addedMillilitres) / 1000d);
            RefreshVesselVisual(currentVesselStation);
            RefreshOutcome(currentVesselStation);
            hud.ShowVesselSection();
            hud.ShowTransient(
                "Đã thêm dung môi · thể tích "
                + (environment.VolumeLitres * 1000d).ToString("0") + " mL.");
            audioSystem.PlayPour(GetVesselPosition(currentVesselStation));
        }

        public void CycleSynthesizedBatch()
        {
            if (synthesizedInventory == null || synthesizedInventory.Count == 0)
            {
                hud.ShowTransient("Kho sản phẩm điều chế đang trống.", true);
                audioSystem.PlayError();
                return;
            }

            var nextIndex = 0;
            if (!string.IsNullOrWhiteSpace(selectedBatchId))
            {
                for (var index = 0; index < synthesizedInventory.Count; index++)
                {
                    if (string.Equals(
                            synthesizedInventory.Batches[index].BatchId,
                            selectedBatchId,
                            StringComparison.Ordinal))
                    {
                        nextIndex = (index + 1) % synthesizedInventory.Count;
                        break;
                    }
                }
            }

            var batch = synthesizedInventory.Batches[nextIndex];
            selectedBatchId = batch.BatchId;
            selectedChemical = RuntimeChemicalRegistry.GetChemical(batch.ChemicalId);
            UpdateHeldSample();
            hud.SetSelectedChemical(
                selectedChemical,
                selectedAmountGrams,
                batch,
                SynthesizedBatchCount);
            hud.ShowChemicalSection();
            ToggleInspector(true);
            hud.ShowTransient(
                "Kho " + (nextIndex + 1) + "/" + synthesizedInventory.Count + " · "
                + batch.Formula + " · còn " + batch.AvailableGrams.ToString("0.000") + " g.");
            audioSystem.PlaySamplePickup();
        }

        public void WashVessels()
        {
            foreach (var pair in vesselAdditions)
            {
                pair.Value.Clear();
                ReactionEnvironment environment;
                if (vesselEnvironments.TryGetValue(pair.Key, out environment))
                {
                    environment.Reset(BaselineTemperatureC, .100d);
                }
                RefreshVesselVisual(pair.Key);
            }

            currentVesselStation = LabStation.Workbench;
            RefreshOutcome(currentVesselStation);
            hud.ShowVesselSection();
            ToggleInspector(true);
            hud.ShowTransient("Cốc đã được rửa và đưa về 24 °C.");
            audioSystem.PlayWash(new Vector3(-5.75f, 1.1f, 3.9f));
        }

        public void InspectElement(int atomicNumber)
        {
            var element = HighSchoolPeriodicTable.Get(atomicNumber);
            if (element == null)
            {
                hud.ShowTransient("Không tìm thấy dữ liệu nguyên tố Z = " + atomicNumber + ".", true);
                audioSystem.PlayError();
                return;
            }

            hud.SetSelectedElement(element);
            ToggleInspector(true);
            hud.ShowTransient(
                element.Symbol + " · " + element.Name + " · " + element.Appearance);
            audioSystem.PlaySamplePickup();
        }

        public void ToggleInspector()
        {
            ToggleInspector(!inspectorOpen);
        }

        public void ToggleInspector(bool visible)
        {
            inspectorOpen = visible;
            hud.SetInspectorVisible(visible);
        }

        public void SetPaused(bool paused)
        {
            Time.timeScale = paused ? 0f : 1f;
            if (hud != null)
            {
                hud.SetPaused(paused);
            }

            if (audioSystem != null)
            {
                audioSystem.SetPaused(paused);
            }
        }

        public void ResumeFromUi()
        {
            if (player != null)
            {
                hud.HideMenus();
                player.SetPausedFromUi(false);
            }
        }

        public void OpenHelpFromUi()
        {
            if (player != null)
            {
                player.SetPausedFromUi(true);
                hud.ShowPauseMenu();
            }
        }

        public void ReturnToMainMenuFromUi()
        {
            if (player != null)
            {
                player.SetPausedFromUi(true);
            }

            hud.ShowMainMenu();
        }

        public void HandleEscape()
        {
            if (hud == null || player == null)
            {
                return;
            }

            if (hud.SettingsVisible)
            {
                hud.ReturnFromSettings();
                return;
            }

            if (hud.MainMenuVisible)
            {
                return;
            }

            player.SetPausedFromUi(!player.IsPaused);
            if (player.IsPaused)
            {
                hud.ShowPauseMenu();
            }
        }

        public void ToggleAudio()
        {
            if (audioSystem == null)
            {
                return;
            }

            audioSystem.ToggleMuted();
            hud.SetAudioState(!audioSystem.IsMuted);
        }

        public void ToggleReducedMotion()
        {
            LabAccessibility.ReducedMotion = !LabAccessibility.ReducedMotion;
            hud.SetAccessibilityState(LabAccessibility.ReducedMotion);
        }

        public void ToggleFullscreen()
        {
            var fullscreen = Screen.fullScreenMode == FullScreenMode.Windowed;
            Screen.fullScreenMode = fullscreen
                ? FullScreenMode.FullScreenWindow
                : FullScreenMode.Windowed;
            PlayerPrefs.SetInt(FullscreenPreferenceKey, fullscreen ? 1 : 0);
            PlayerPrefs.Save();
            hud.SetFullscreenState(fullscreen);
        }

        public void ToggleRespirator()
        {
            if (labSafety == null)
            {
                return;
            }

            var message = labSafety.BuyOrToggleRespirator();
            hud.SetSafetySystem(labSafety);
            hud.ShowTransient(message, !labSafety.RespiratorOwned);
            if (labSafety.RespiratorOwned)
            {
                audioSystem.PlayUiClick();
            }
            else
            {
                audioSystem.PlayError();
            }
        }

        public void ToggleGasTrap()
        {
            if (labSafety == null)
            {
                return;
            }

            var message = labSafety.ToggleGasTrap();
            hud.SetSafetySystem(labSafety);
            hud.ShowTransient(message);
            audioSystem.PlayUiClick();
        }

        public void ToggleDiagnostics()
        {
            if (diagnostics != null)
            {
                diagnostics.Toggle();
            }
        }

        public void QuitToDesktop()
        {
            if (audioSystem != null)
            {
                audioSystem.PlayUiClick();
            }

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit(0);
#endif
        }

        public void UpdatePlayerZone(Vector3 position)
        {
            var next = GetZone(position);
            if (next == currentZone)
            {
                return;
            }

            currentZone = next;
            hud.SetZone(ZoneLabel(currentZone));
        }

        public static string ZoneLabel(LabStation station)
        {
            switch (station)
            {
                case LabStation.FumeHood: return "Tủ hút khí độc";
                case LabStation.Sink: return "Bồn rửa";
                case LabStation.Storage: return "Kho hóa chất";
                case LabStation.Analysis: return "Bàn phân tích";
                default: return "Bàn phản ứng";
            }
        }

        private void CreateHud()
        {
            var hudObject = new GameObject("Native Desktop HUD");
            hudObject.transform.SetParent(transform, false);
            hud = hudObject.AddComponent<DesktopLabHud>();
            hud.Initialise(this);
        }

        private void BuildAudio()
        {
            var audioObject = new GameObject("Procedural Laboratory Audio");
            audioObject.transform.SetParent(transform, false);
            audioSystem = audioObject.AddComponent<DesktopLabAudio>();
            audioSystem.Initialise();
        }

        private void BuildDiagnostics()
        {
            var diagnosticsObject = new GameObject("Runtime Diagnostics");
            diagnosticsObject.transform.SetParent(transform, false);
            diagnostics = diagnosticsObject.AddComponent<DesktopLabDiagnostics>();
            diagnostics.Initialise(this, player, audioSystem, hud);
        }

        private void BuildWorld()
        {
            worldRoot = new GameObject("Procedural Laboratory").transform;
            worldRoot.SetParent(transform, false);

            ConfigureEnvironment();
            BuildRoomShell();
            BuildCentralWorkbench();
            BuildFumeHood();
            BuildChemicalStorage(-1);
            BuildChemicalStorage(1);
            BuildAnalysisBench();
            BuildPeriodicTableWall();
            BuildSink();
            BuildSafetyEquipment();
            BuildCeilingLights();
        }

        private void ConfigureEnvironment()
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = LabTheme.Wall;
            RenderSettings.fogStartDistance = 14f;
            RenderSettings.fogEndDistance = 32f;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = LabTheme.PaperRaised;
            RenderSettings.ambientEquatorColor = LabTheme.Wall;
            RenderSettings.ambientGroundColor = LabTheme.Floor;
            RenderSettings.ambientIntensity = 0.82f;
        }

        private void BuildRoomShell()
        {
            CreatePrimitive(
                PrimitiveType.Cube,
                "Floor",
                worldRoot,
                new Vector3(0f, -0.12f, 0f),
                new Vector3(14f, 0.24f, 12f),
                GetMaterial("Floor", LabTheme.Floor, 0.08f, 0.42f),
                true);
            CreatePrimitive(
                PrimitiveType.Cube,
                "Back Wall",
                worldRoot,
                new Vector3(0f, 3.2f, -6f),
                new Vector3(14f, 6.4f, 0.24f),
                GetMaterial("Wall", LabTheme.Wall, 0f, 0.18f),
                true);
            CreatePrimitive(
                PrimitiveType.Cube,
                "Left Wall",
                worldRoot,
                new Vector3(-7f, 3.2f, 0f),
                new Vector3(0.24f, 6.4f, 12f),
                GetMaterial("WallSecondary", LabTheme.WallSecondary, 0f, 0.16f),
                true);
            CreatePrimitive(
                PrimitiveType.Cube,
                "Right Wall",
                worldRoot,
                new Vector3(7f, 3.2f, 0f),
                new Vector3(0.24f, 6.4f, 12f),
                GetMaterial("WallSecondary", LabTheme.WallSecondary, 0f, 0.16f),
                true);
            CreatePrimitive(
                PrimitiveType.Cube,
                "Front Collision",
                worldRoot,
                new Vector3(0f, 2f, 6f),
                new Vector3(14f, 4f, 0.2f),
                GetMaterial("InvisibleBarrier", LabTheme.WithAlpha(LabTheme.Wall, 0f), 0f, 0f, true),
                true);

            var ruleMaterial = GetMaterial("FloorRule", LabTheme.FloorRule, 0.1f, 0.35f);
            for (var x = -6; x <= 6; x++)
            {
                CreatePrimitive(
                    PrimitiveType.Cube,
                    "Floor Grid X " + x,
                    worldRoot,
                    new Vector3(x, 0.012f, 0f),
                    new Vector3(0.018f, 0.008f, 12f),
                    ruleMaterial,
                    false);
            }

            for (var z = -5; z <= 5; z++)
            {
                CreatePrimitive(
                    PrimitiveType.Cube,
                    "Floor Grid Z " + z,
                    worldRoot,
                    new Vector3(0f, 0.014f, z),
                    new Vector3(14f, 0.008f, 0.018f),
                    ruleMaterial,
                    false);
            }

            var windowMaterial = GetMaterial(
                "WindowGlass",
                LabTheme.WithAlpha(LabTheme.Glass, 0.34f),
                0f,
                0.88f,
                true);
            for (var index = -1; index <= 1; index++)
            {
                CreatePrimitive(
                    PrimitiveType.Cube,
                    "Back Window " + index,
                    worldRoot,
                    new Vector3(index * 2.05f, 4.05f, -5.82f),
                    new Vector3(1.8f, 1.55f, 0.08f),
                    windowMaterial,
                    false);
            }
        }

        private void BuildCentralWorkbench()
        {
            var bench = new GameObject("Central Workbench").transform;
            bench.SetParent(worldRoot, false);
            var frameMaterial = GetMaterial("GraphiteRaised", LabTheme.GraphiteRaised, 0.48f, 0.44f);
            var topMaterial = GetMaterial("BenchTop", LabTheme.BenchTop, 0.04f, 0.5f);

            CreatePrimitive(
                PrimitiveType.Cube,
                "Bench Top",
                bench,
                new Vector3(0f, 1.02f, 0f),
                new Vector3(5.4f, 0.22f, 2.2f),
                topMaterial,
                true);
            foreach (var x in new[] { -2.25f, 2.25f })
            {
                foreach (var z in new[] { -0.78f, 0.78f })
                {
                    CreatePrimitive(
                        PrimitiveType.Cube,
                        "Bench Leg",
                        bench,
                        new Vector3(x, 0.47f, z),
                        new Vector3(0.18f, 0.94f, 0.18f),
                        frameMaterial,
                        true);
                }
            }

            BuildTestTubeRack(bench, new Vector3(-1.55f, 1.28f, 0.2f));
            BuildHotplate(bench, new Vector3(1.45f, 1.23f, 0.18f));
            BuildVessel(bench, LabStation.Workbench, new Vector3(0f, 1.26f, 0f));
            BuildStarterChemicalTray(bench);
        }

        private void BuildStarterChemicalTray(Transform bench)
        {
            starterChemicalCount = 0;
            CreatePrimitive(
                PrimitiveType.Cube,
                "Starter Chemical Tray",
                bench,
                new Vector3(0f, 1.16f, 0.78f),
                new Vector3(4.1f, 0.08f, 0.55f),
                GetMaterial("StarterTray", LabTheme.GraphiteRaised, 0.32f, 0.38f),
                false);

            var starterIds = new[]
            {
                "water",
                "copper-sulfate",
                "sodium-hydroxide",
                "hydrochloric-acid"
            };
            for (var index = 0; index < starterIds.Length; index++)
            {
                var chemical = RuntimeChemicalRegistry.GetChemical(starterIds[index]);
                if (chemical == null)
                {
                    continue;
                }

                BuildChemicalBottle(
                    bench,
                    chemical,
                    new Vector3(-1.35f + index * 0.9f, 1.22f, 0.78f),
                    0,
                    true);
                starterChemicalCount++;
            }

            CreateWorldLabel(
                "KHAY KHỞI ĐỘNG · ĐẶT TÂM NGẮM VÀ NHẤN E",
                bench,
                new Vector3(0f, 2.18f, 0.78f),
                Quaternion.Euler(0f, 180f, 0f),
                LabTheme.GraphiteInk,
                0.022f);
        }

        private void BuildFumeHood()
        {
            var hood = new GameObject("Fume Hood").transform;
            hood.SetParent(worldRoot, false);
            var frame = GetMaterial("Graphite", LabTheme.Graphite, 0.36f, 0.42f);
            var interior = GetMaterial("HoodInterior", LabTheme.GraphiteRaised, 0.12f, 0.28f);
            var glass = GetMaterial("HoodGlass", LabTheme.WithAlpha(LabTheme.Glass, 0.28f), 0f, 0.88f, true);

            CreatePrimitive(
                PrimitiveType.Cube,
                "Hood Back",
                hood,
                new Vector3(0f, 2.55f, -5.35f),
                new Vector3(4.3f, 3.1f, 0.38f),
                interior,
                true);
            CreatePrimitive(
                PrimitiveType.Cube,
                "Hood Header",
                hood,
                new Vector3(0f, 4.25f, -4.78f),
                new Vector3(4.5f, 0.55f, 1.5f),
                frame,
                true);
            CreatePrimitive(
                PrimitiveType.Cube,
                "Hood Base",
                hood,
                new Vector3(0f, 1.0f, -4.65f),
                new Vector3(4.5f, 0.24f, 1.75f),
                GetMaterial("Bench", LabTheme.Bench, 0.08f, 0.44f),
                true);
            CreatePrimitive(
                PrimitiveType.Cube,
                "Hood Sash",
                hood,
                new Vector3(0f, 2.72f, -4.15f),
                new Vector3(3.95f, 1.45f, 0.06f),
                glass,
                false);
            CreatePrimitive(
                PrimitiveType.Cube,
                "Hood Left",
                hood,
                new Vector3(-2.1f, 2.55f, -4.65f),
                new Vector3(0.22f, 3.3f, 1.75f),
                frame,
                true);
            CreatePrimitive(
                PrimitiveType.Cube,
                "Hood Right",
                hood,
                new Vector3(2.1f, 2.55f, -4.65f),
                new Vector3(0.22f, 3.3f, 1.75f),
                frame,
                true);

            BuildVessel(hood, LabStation.FumeHood, new Vector3(0f, 1.28f, -4.62f));
            CreateWorldLabel(
                "FUME HOOD · KHÍ / HƠI",
                hood,
                new Vector3(0f, 4.28f, -4.0f),
                Quaternion.Euler(0f, 180f, 0f),
                LabTheme.GraphiteInk,
                0.055f);
        }

        private void BuildChemicalStorage(int side)
        {
            var label = side < 0 ? "Storage Left" : "Storage Right";
            var storage = new GameObject(label).transform;
            storage.SetParent(worldRoot, false);
            var x = side * 6.42f;
            var frame = GetMaterial("StorageFrame", LabTheme.GraphiteRaised, 0.4f, 0.4f);
            var shelf = GetMaterial("Shelf", LabTheme.Steel, 0.68f, 0.48f);

            CreatePrimitive(
                PrimitiveType.Cube,
                "Cabinet Back",
                storage,
                new Vector3(x, 2.55f, -0.9f),
                new Vector3(0.38f, 5.1f, 9.2f),
                frame,
                true);
            for (var row = 0; row < 4; row++)
            {
                CreatePrimitive(
                    PrimitiveType.Cube,
                    "Shelf " + row,
                    storage,
                    new Vector3(x - side * 0.38f, 0.42f + row * 1.08f, -0.9f),
                    new Vector3(0.72f, 0.08f, 9.0f),
                    shelf,
                    true);
            }

            var perSide = Mathf.CeilToInt(DesktopChemistryDatabase.AllChemicals.Count / 2f);
            var firstIndex = side < 0 ? 0 : perSide;
            var lastIndex = Mathf.Min(firstIndex + perSide, DesktopChemistryDatabase.AllChemicals.Count);
            for (var chemicalIndex = firstIndex; chemicalIndex < lastIndex; chemicalIndex++)
            {
                var localIndex = chemicalIndex - firstIndex;
                var chemical = DesktopChemistryDatabase.AllChemicals[chemicalIndex];
                var row = localIndex / 5;
                var column = localIndex % 5;
                var z = -4.2f + column * 1.65f;
                var position = new Vector3(x - side * 0.88f, 0.48f + row * 1.08f, z);
                BuildChemicalBottle(storage, chemical, position, side);
            }

            CreateWorldLabel(
                side < 0 ? "KHO A · DUNG DỊCH / MUỐI" : "KHO B · KIM LOẠI / CHẤT OXI HÓA",
                storage,
                new Vector3(x - side * 0.72f, 4.72f, -0.8f),
                Quaternion.Euler(0f, side < 0 ? -90f : 90f, 0f),
                LabTheme.GraphiteInk,
                0.045f);
        }

        private void BuildPeriodicTableWall()
        {
            var table = new GameObject("Interactive High School Periodic Table").transform;
            table.SetParent(worldRoot, false);
            var elements = HighSchoolPeriodicTable.All;
            CreateWorldLabel(
                "BẢNG TUẦN HOÀN · " + elements.Count + " NGUYÊN TỐ THPT",
                table,
                new Vector3(0f, 5.62f, 5.76f),
                Quaternion.identity,
                LabTheme.GraphiteInk,
                0.042f);

            for (var index = 0; index < elements.Count; index++)
            {
                var element = elements[index];
                var displayGroup = element.Group <= 0 ? 8 : element.Group;
                var position = new Vector3(
                    -5.53f + (displayGroup - 1) * 0.65f,
                    5.12f - (element.Period - 1) * 0.57f,
                    5.82f);
                var categoryColour = HighSchoolPeriodicTable.CategoryColour(element.Category);
                var tile = CreatePrimitive(
                    PrimitiveType.Cube,
                    "Element " + element.AtomicNumber + " " + element.Symbol,
                    table,
                    position,
                    new Vector3(0.56f, 0.47f, 0.07f),
                    GetMaterial(
                        "ElementCategory_" + element.Category,
                        categoryColour,
                        0.08f,
                        0.46f),
                    true);

                var focus = CreatePrimitive(
                    PrimitiveType.Cube,
                    "Element Focus",
                    tile.transform,
                    new Vector3(0f, 0f, -0.54f),
                    new Vector3(1.10f, 1.11f, 0.13f),
                    GetMaterial("Focus", LabTheme.Focus, 0f, 0.66f),
                    false);
                CreateWorldLabel(
                    element.AtomicNumber + "\n" + element.Symbol,
                    table,
                    position + new Vector3(0f, 0f, -0.055f),
                    Quaternion.identity,
                    LabTheme.GraphiteInk,
                    0.026f);

                var interaction = tile.AddComponent<ElementTileInteractable>();
                interaction.AtomicNumber = element.AtomicNumber;
                interaction.Initialise(this, focus);
            }
        }

        private void BuildChemicalBottle(
            Transform parent,
            ChemicalDefinition chemical,
            Vector3 position,
            int wallSide)
        {
            BuildChemicalBottle(parent, chemical, position, wallSide, false);
        }

        private void BuildChemicalBottle(
            Transform parent,
            ChemicalDefinition chemical,
            Vector3 position,
            int wallSide,
            bool facePlayer)
        {
            var bottle = new GameObject("Bottle " + chemical.Formula);
            bottle.transform.SetParent(parent, false);
            bottle.transform.position = position;

            var collider = bottle.AddComponent<CapsuleCollider>();
            collider.radius = 0.28f;
            collider.height = 0.92f;
            collider.center = new Vector3(0f, 0.36f, 0f);

            var glass = GetMaterial(
                "BottleGlass",
                LabTheme.WithAlpha(LabTheme.Glass, 0.3f),
                0f,
                0.9f,
                true);
            CreatePrimitive(
                PrimitiveType.Cylinder,
                "Glass Bottle",
                bottle.transform,
                new Vector3(0f, 0.34f, 0f),
                new Vector3(0.25f, 0.36f, 0.25f),
                glass,
                false);
            CreatePrimitive(
                PrimitiveType.Cylinder,
                "Cap",
                bottle.transform,
                new Vector3(0f, 0.78f, 0f),
                new Vector3(0.18f, 0.09f, 0.18f),
                GetMaterial("BottleCap", LabTheme.Graphite, 0.12f, 0.34f),
                false);
            CreateChemicalContents(
                bottle.transform,
                chemical,
                new Vector3(0f, 0.27f, 0f),
                0.82f);

            var highlight = CreatePrimitive(
                PrimitiveType.Cylinder,
                "Focus Ring",
                bottle.transform,
                new Vector3(0f, 0.015f, 0f),
                new Vector3(0.34f, 0.008f, 0.34f),
                GetMaterial("Focus", LabTheme.Focus, 0f, 0.66f),
                false);

            var interactable = bottle.AddComponent<ChemicalBottleInteractable>();
            interactable.ChemicalId = chemical.Id;
            interactable.Initialise(this, highlight);

            var labelOffset = facePlayer
                ? new Vector3(0f, 0.36f, 0.29f)
                : new Vector3(-wallSide * 0.29f, 0.36f, 0f);
            var labelRotation = facePlayer
                ? Quaternion.Euler(0f, 180f, 0f)
                : Quaternion.Euler(0f, wallSide < 0 ? -90f : 90f, 0f);
            CreateWorldLabel(
                chemical.Formula,
                bottle.transform,
                labelOffset,
                labelRotation,
                LabTheme.GraphiteInk,
                facePlayer ? 0.024f : 0.037f);
        }

        private void BuildAnalysisBench()
        {
            var analysis = new GameObject("Analysis Bench").transform;
            analysis.SetParent(worldRoot, false);
            var top = GetMaterial("BenchTop", LabTheme.BenchTop, 0.04f, 0.5f);
            var dark = GetMaterial("Graphite", LabTheme.Graphite, 0.36f, 0.42f);

            CreatePrimitive(
                PrimitiveType.Cube,
                "Analysis Counter",
                analysis,
                new Vector3(5.35f, 0.92f, -2.5f),
                new Vector3(2.6f, 0.22f, 2.5f),
                top,
                true);
            CreatePrimitive(
                PrimitiveType.Cylinder,
                "Microscope Base",
                analysis,
                new Vector3(5.1f, 1.12f, -2.65f),
                new Vector3(0.45f, 0.09f, 0.45f),
                dark,
                false);
            var arm = CreatePrimitive(
                PrimitiveType.Cube,
                "Microscope Arm",
                analysis,
                new Vector3(5.05f, 1.62f, -2.7f),
                new Vector3(0.15f, 0.9f, 0.16f),
                dark,
                false);
            arm.transform.rotation = Quaternion.Euler(0f, 0f, -18f);
            var eyepiece = CreatePrimitive(
                PrimitiveType.Cylinder,
                "Microscope Eyepiece",
                analysis,
                new Vector3(4.9f, 2.05f, -2.7f),
                new Vector3(0.13f, 0.3f, 0.13f),
                dark,
                false);
            eyepiece.transform.rotation = Quaternion.Euler(0f, 0f, 72f);

            var screen = CreatePrimitive(
                PrimitiveType.Cube,
                "Analysis Screen",
                analysis,
                new Vector3(5.8f, 1.72f, -2.65f),
                new Vector3(0.09f, 1.02f, 1.4f),
                dark,
                false);
            CreatePrimitive(
                PrimitiveType.Cube,
                "Analysis Screen Signal",
                screen.transform,
                new Vector3(-0.052f, 0f, 0f),
                new Vector3(0.01f, 0.78f, 1.15f),
                GetMaterial("ScreenSignal", LabTheme.Accent, 0f, 0.55f),
                false);

            var interaction = new GameObject("Analysis Interaction");
            interaction.transform.SetParent(analysis, false);
            interaction.transform.position = new Vector3(5.15f, 1.2f, -1.65f);
            var collider = interaction.AddComponent<BoxCollider>();
            collider.size = new Vector3(2.7f, 1.2f, 0.5f);
            var highlight = CreatePrimitive(
                PrimitiveType.Cube,
                "Analysis Focus",
                interaction.transform,
                new Vector3(0f, -0.5f, 0f),
                new Vector3(2.5f, 0.025f, 0.08f),
                GetMaterial("Focus", LabTheme.Focus, 0f, 0.66f),
                false);
            var interactable = interaction.AddComponent<AnalysisInteractable>();
            interactable.Initialise(this, highlight);
        }

        private void BuildSink()
        {
            var sink = new GameObject("Sink Station").transform;
            sink.SetParent(worldRoot, false);
            var counter = GetMaterial("Bench", LabTheme.Bench, 0.08f, 0.44f);
            var metal = GetMaterial("Steel", LabTheme.Steel, 0.82f, 0.7f);
            CreatePrimitive(
                PrimitiveType.Cube,
                "Sink Counter",
                sink,
                new Vector3(5.25f, 0.92f, 3.35f),
                new Vector3(2.9f, 0.24f, 2.2f),
                counter,
                true);
            CreatePrimitive(
                PrimitiveType.Cube,
                "Sink Basin",
                sink,
                new Vector3(5.25f, 1.05f, 3.35f),
                new Vector3(1.5f, 0.08f, 1.05f),
                GetMaterial("SinkWater", LabTheme.WithAlpha(LabTheme.Glass, 0.72f), 0f, 0.88f, true),
                false);
            CreatePrimitive(
                PrimitiveType.Cylinder,
                "Sink Tap",
                sink,
                new Vector3(5.25f, 1.48f, 4.05f),
                new Vector3(0.09f, 0.45f, 0.09f),
                metal,
                false);

            var interaction = new GameObject("Sink Interaction");
            interaction.transform.SetParent(sink, false);
            interaction.transform.position = new Vector3(5.25f, 1.2f, 2.45f);
            var collider = interaction.AddComponent<BoxCollider>();
            collider.size = new Vector3(2.8f, 1.2f, 0.45f);
            var highlight = CreatePrimitive(
                PrimitiveType.Cube,
                "Sink Focus",
                interaction.transform,
                new Vector3(0f, -0.5f, 0f),
                new Vector3(2.4f, 0.025f, 0.08f),
                GetMaterial("Focus", LabTheme.Focus, 0f, 0.66f),
                false);
            var interactable = interaction.AddComponent<SinkInteractable>();
            interactable.Initialise(this, highlight);
        }

        private void BuildSafetyEquipment()
        {
            var safety = new GameObject("Safety Equipment").transform;
            safety.SetParent(worldRoot, false);
            var safeMaterial = GetMaterial("Safe", LabTheme.Safe, 0.18f, 0.5f);
            var steel = GetMaterial("Steel", LabTheme.Steel, 0.82f, 0.7f);
            CreatePrimitive(
                PrimitiveType.Cylinder,
                "Emergency Shower Pipe",
                safety,
                new Vector3(-5.8f, 2.9f, 4.65f),
                new Vector3(0.08f, 2.6f, 0.08f),
                steel,
                false);
            CreatePrimitive(
                PrimitiveType.Cylinder,
                "Emergency Shower Head",
                safety,
                new Vector3(-5.8f, 5.25f, 4.65f),
                new Vector3(0.45f, 0.12f, 0.45f),
                safeMaterial,
                false);
            CreatePrimitive(
                PrimitiveType.Cube,
                "Safety Sign",
                safety,
                new Vector3(-6.82f, 3.6f, 4.2f),
                new Vector3(0.05f, 0.72f, 0.72f),
                safeMaterial,
                false);
        }

        private void BuildCeilingLights()
        {
            var lightMaterial = GetMaterial("CeilingLight", LabTheme.PaperRaised, 0f, 0.82f);
            for (var xIndex = -1; xIndex <= 1; xIndex++)
            {
                for (var zIndex = -1; zIndex <= 1; zIndex++)
                {
                    var position = new Vector3(xIndex * 4f, 5.85f, zIndex * 3.6f);
                    CreatePrimitive(
                        PrimitiveType.Cube,
                        "Ceiling Panel",
                        worldRoot,
                        position,
                        new Vector3(2.1f, 0.08f, 0.62f),
                        lightMaterial,
                        false);
                    var pointObject = new GameObject("Ceiling Point Light");
                    pointObject.transform.SetParent(worldRoot, false);
                    pointObject.transform.position = position + Vector3.down * 0.2f;
                    var point = pointObject.AddComponent<Light>();
                    point.type = LightType.Point;
                    point.color = LabTheme.PaperRaised;
                    point.intensity = 1.1f;
                    point.range = 8f;
                    point.shadows = LightShadows.Soft;
                    point.shadowStrength = 0.3f;
                }
            }

            var sunObject = new GameObject("Laboratory Sun");
            sunObject.transform.SetParent(worldRoot, false);
            sunObject.transform.rotation = Quaternion.Euler(42f, -32f, 0f);
            var sun = sunObject.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = LabTheme.PaperRaised;
            sun.intensity = 0.62f;
            sun.shadows = LightShadows.Soft;
        }

        private void BuildTestTubeRack(Transform parent, Vector3 position)
        {
            var rack = new GameObject("Test Tube Rack").transform;
            rack.SetParent(parent, false);
            rack.position = position;
            var frame = GetMaterial("Graphite", LabTheme.Graphite, 0.36f, 0.42f);
            CreatePrimitive(
                PrimitiveType.Cube,
                "Rack Base",
                rack,
                Vector3.zero,
                new Vector3(1.4f, 0.08f, 0.42f),
                frame,
                false);
            for (var index = 0; index < 4; index++)
            {
                var x = -0.48f + index * 0.32f;
                CreatePrimitive(
                    PrimitiveType.Cylinder,
                    "Tube " + index,
                    rack,
                    new Vector3(x, 0.32f, 0f),
                    new Vector3(0.08f, 0.34f, 0.08f),
                    GetMaterial("BottleGlass", LabTheme.WithAlpha(LabTheme.Glass, 0.3f), 0f, 0.9f, true),
                    false);
            }
        }

        private void BuildHotplate(Transform parent, Vector3 position)
        {
            var plate = new GameObject("Hotplate").transform;
            plate.SetParent(parent, false);
            plate.position = position;
            var collider = plate.gameObject.AddComponent<BoxCollider>();
            collider.size = new Vector3(1.1f, .36f, .82f);
            collider.center = new Vector3(0f, .08f, 0f);
            CreatePrimitive(
                PrimitiveType.Cube,
                "Hotplate Body",
                plate,
                Vector3.zero,
                new Vector3(1.1f, 0.18f, 0.82f),
                GetMaterial("Graphite", LabTheme.Graphite, 0.36f, 0.42f),
                false);
            CreatePrimitive(
                PrimitiveType.Cylinder,
                "Hotplate Surface",
                plate,
                new Vector3(0f, 0.12f, 0f),
                new Vector3(0.36f, 0.025f, 0.36f),
                GetMaterial("SteelDark", LabTheme.SteelDark, 0.84f, 0.62f),
                false);
            var interactable = plate.gameObject.AddComponent<ThermalControlInteractable>();
            interactable.Station = LabStation.Workbench;
            interactable.Initialise(this);
        }

        private void BuildVessel(Transform parent, LabStation station, Vector3 worldPosition)
        {
            var vessel = new GameObject(station == LabStation.FumeHood ? "Fume Hood Vessel" : "Workbench Vessel");
            vessel.transform.SetParent(parent, false);
            vessel.transform.position = worldPosition;
            var collider = vessel.AddComponent<BoxCollider>();
            collider.size = new Vector3(0.95f, 1.2f, 0.95f);
            collider.center = new Vector3(0f, 0.35f, 0f);

            CreatePrimitive(
                PrimitiveType.Cylinder,
                "Beaker Glass",
                vessel.transform,
                new Vector3(0f, 0.34f, 0f),
                new Vector3(0.48f, 0.48f, 0.48f),
                GetMaterial("BeakerGlass", LabTheme.WithAlpha(LabTheme.Glass, 0.26f), 0f, 0.92f, true),
                false);
            var liquid = CreatePrimitive(
                PrimitiveType.Cylinder,
                "Vessel Contents",
                vessel.transform,
                new Vector3(0f, 0.18f, 0f),
                new Vector3(0.39f, 0.19f, 0.39f),
                GetMaterial("EmptyLiquid", LabTheme.WithAlpha(LabTheme.Glass, 0.16f), 0f, 0.74f, true),
                false);
            var highlight = CreatePrimitive(
                PrimitiveType.Cylinder,
                "Vessel Focus",
                vessel.transform,
                new Vector3(0f, -0.02f, 0f),
                new Vector3(0.58f, 0.01f, 0.58f),
                GetMaterial("Focus", LabTheme.Focus, 0f, 0.66f),
                false);
            var particles = CreateReactionParticles(vessel.transform);

            var interactable = vessel.AddComponent<VesselInteractable>();
            interactable.Station = station;
            interactable.Initialise(this, highlight);

            vesselVisuals[station] = new VesselVisual
            {
                Root = vessel.transform,
                LiquidRenderer = liquid.GetComponent<Renderer>(),
                Particles = particles
            };
        }

        private ParticleSystem CreateReactionParticles(Transform parent)
        {
            var particleObject = new GameObject("Reaction Particles");
            particleObject.transform.SetParent(parent, false);
            particleObject.transform.localPosition = new Vector3(0f, 0.3f, 0f);
            var particles = particleObject.AddComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = particles.main;
            main.loop = false;
            main.duration = 2.4f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.8f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.12f, 0.42f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.035f, 0.09f);
            main.maxParticles = 80;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;

            var emission = particles.emission;
            emission.rateOverTime = 22f;

            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.34f;

            var particleRenderer = particleObject.GetComponent<ParticleSystemRenderer>();
            particleRenderer.material = GetMaterial("Particle", LabTheme.Glass, 0f, 0.52f, true);
            return particles;
        }

        private void BuildPlayer()
        {
            var playerObject = new GameObject("First Person Chemist");
            playerObject.transform.SetParent(transform, false);
            playerObject.transform.position = new Vector3(0f, 0.02f, 4.7f);
            playerObject.transform.rotation = Quaternion.Euler(0f, 180f, 0f);

            var character = playerObject.AddComponent<CharacterController>();
            character.height = 1.82f;
            character.radius = 0.32f;
            character.center = new Vector3(0f, 0.91f, 0f);
            character.stepOffset = 0.28f;
            character.slopeLimit = 50f;

            var cameraObject = new GameObject("Chemist Camera");
            cameraObject.transform.SetParent(playerObject.transform, false);
            cameraObject.transform.localPosition = new Vector3(0f, 1.63f, 0f);
            var camera = cameraObject.AddComponent<Camera>();
            camera.fieldOfView = 66f;
            camera.nearClipPlane = 0.03f;
            camera.farClipPlane = 70f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = LabTheme.Wall;
            camera.allowHDR = true;
            camera.allowMSAA = true;
            cameraObject.AddComponent<AudioListener>();

            var hands = BuildChemistHands(cameraObject.transform);
            player = playerObject.AddComponent<FirstPersonChemistController>();
            player.Initialise(this, camera, hands);
        }

        private Transform BuildChemistHands(Transform cameraTransform)
        {
            var root = new GameObject("POV Chemist Arms").transform;
            root.SetParent(cameraTransform, false);
            root.localPosition = new Vector3(0f, -0.43f, 0.72f);

            var coat = GetMaterial("Coat", LabTheme.Coat, 0f, 0.32f);
            var glove = GetMaterial("Glove", LabTheme.Glove, 0f, 0.44f);

            BuildArm(root, "Left Arm", new Vector3(-0.34f, -0.02f, 0f), -13f, coat, glove);
            var right = BuildArm(root, "Right Arm", new Vector3(0.34f, -0.02f, 0f), 13f, coat, glove);

            heldSampleRoot = new GameObject("Held Sample").transform;
            heldSampleRoot.SetParent(right, false);
            heldSampleRoot.localPosition = new Vector3(-0.09f, 0.34f, 0.02f);
            heldSampleRoot.localRotation = Quaternion.Euler(6f, 0f, -8f);
            heldSampleRoot.gameObject.SetActive(false);
            return root;
        }

        private Transform BuildArm(
            Transform parent,
            string name,
            Vector3 localPosition,
            float roll,
            Material coat,
            Material glove)
        {
            var arm = new GameObject(name).transform;
            arm.SetParent(parent, false);
            arm.localPosition = localPosition;
            arm.localRotation = Quaternion.Euler(8f, 0f, roll);

            var sleeve = CreatePrimitive(
                PrimitiveType.Capsule,
                "Lab Coat Sleeve",
                arm,
                new Vector3(0f, -0.08f, 0.03f),
                new Vector3(0.12f, 0.28f, 0.12f),
                coat,
                false);
            sleeve.transform.localRotation = Quaternion.Euler(0f, 0f, -roll * 0.28f);
            CreatePrimitive(
                PrimitiveType.Sphere,
                "Nitrile Glove",
                arm,
                new Vector3(0f, 0.2f, 0.02f),
                new Vector3(0.13f, 0.085f, 0.16f),
                glove,
                false);
            CreatePrimitive(
                PrimitiveType.Sphere,
                "Glove Thumb",
                arm,
                new Vector3(roll < 0f ? 0.08f : -0.08f, 0.18f, 0.035f),
                new Vector3(0.055f, 0.045f, 0.09f),
                glove,
                false);
            return arm;
        }

        private SynthesizedBatch GetSelectedBatch()
        {
            return synthesizedInventory == null || string.IsNullOrWhiteSpace(selectedBatchId)
                ? null
                : synthesizedInventory.Find(selectedBatchId);
        }

        private void UpdateHeldSample()
        {
            if (heldSampleRoot == null)
            {
                return;
            }

            for (var index = heldSampleRoot.childCount - 1; index >= 0; index--)
            {
                Destroy(heldSampleRoot.GetChild(index).gameObject);
            }

            if (selectedChemical == null)
            {
                heldSampleRoot.gameObject.SetActive(false);
                return;
            }

            heldSampleRoot.gameObject.SetActive(true);
            CreatePrimitive(
                PrimitiveType.Cylinder,
                "Held Glass",
                heldSampleRoot,
                Vector3.zero,
                new Vector3(0.09f, 0.19f, 0.09f),
                GetMaterial("HeldGlass", LabTheme.WithAlpha(LabTheme.Glass, 0.28f), 0f, 0.92f, true),
                false);
            CreatePrimitive(
                PrimitiveType.Cylinder,
                "Held Cap",
                heldSampleRoot,
                new Vector3(0f, 0.23f, 0f),
                new Vector3(0.075f, 0.045f, 0.075f),
                GetMaterial("BottleCap", LabTheme.Graphite, 0.12f, 0.34f),
                false);
            CreateChemicalContents(heldSampleRoot, selectedChemical, new Vector3(0f, -0.04f, 0f), 0.32f);
        }

        private void CreateChemicalContents(
            Transform parent,
            ChemicalDefinition chemical,
            Vector3 centre,
            float scale)
        {
            var material = GetChemicalMaterial(chemical);
            if (chemical.ModelKind == ChemicalModelKind.Liquid)
            {
                CreatePrimitive(
                    PrimitiveType.Cylinder,
                    chemical.Formula + " Liquid",
                    parent,
                    centre,
                    new Vector3(scale * 0.68f, scale * 0.42f, scale * 0.68f),
                    material,
                    false);
                return;
            }

            if (chemical.ModelKind == ChemicalModelKind.Metal)
            {
                for (var index = 0; index < 5; index++)
                {
                    var pellet = CreatePrimitive(
                        PrimitiveType.Cylinder,
                        chemical.Formula + " Metal " + index,
                        parent,
                        centre + new Vector3(
                            ((index % 2) - 0.5f) * scale * 0.28f,
                            (index / 2) * scale * 0.12f - scale * 0.16f,
                            ((index % 3) - 1f) * scale * 0.1f),
                        Vector3.one * scale * 0.14f,
                        material,
                        false);
                    pellet.transform.localRotation = Quaternion.Euler(90f, index * 31f, 0f);
                }

                return;
            }

            if (chemical.ModelKind == ChemicalModelKind.Powder)
            {
                for (var index = 0; index < 8; index++)
                {
                    CreatePrimitive(
                        PrimitiveType.Sphere,
                        chemical.Formula + " Powder " + index,
                        parent,
                        centre + new Vector3(
                            Mathf.Sin(index * 2.7f) * scale * 0.24f,
                            -scale * 0.2f + (index % 3) * scale * 0.07f,
                            Mathf.Cos(index * 1.9f) * scale * 0.18f),
                        Vector3.one * scale * 0.09f,
                        material,
                        false);
                }

                return;
            }

            for (var index = 0; index < 7; index++)
            {
                var crystal = CreatePrimitive(
                    PrimitiveType.Cube,
                    chemical.Formula + " Crystal " + index,
                    parent,
                    centre + new Vector3(
                        Mathf.Sin(index * 2.2f) * scale * 0.22f,
                        -scale * 0.17f + (index % 3) * scale * 0.12f,
                        Mathf.Cos(index * 1.7f) * scale * 0.16f),
                    new Vector3(scale * 0.12f, scale * 0.18f, scale * 0.11f),
                    material,
                    false);
                crystal.transform.localRotation = Quaternion.Euler(index * 17f, index * 29f, index * 11f);
            }
        }

        private void RefreshOutcome(LabStation station)
        {
            List<VesselAddition> additions;
            ReactionEnvironment environment;
            if (!vesselAdditions.TryGetValue(station, out additions)
                || !vesselEnvironments.TryGetValue(station, out environment))
            {
                return;
            }

            currentOutcome = ReactionSimulator.Evaluate(additions, station, environment);
            currentVesselStation = station;
            hud.SetVessel(additions, currentOutcome, station);
            hud.SetTemperature(currentOutcome.TemperatureC);
            hud.SetSafety(!currentOutcome.SafetyViolation, currentOutcome.Safety);
            hud.SetSafetySystem(labSafety);
        }

        private void RefreshVesselVisual(LabStation station)
        {
            List<VesselAddition> additions;
            ReactionEnvironment environment;
            if (!vesselAdditions.TryGetValue(station, out additions)
                || !vesselEnvironments.TryGetValue(station, out environment))
            {
                return;
            }

            var outcome = ReactionSimulator.Evaluate(additions, station, environment);
            UpdateVesselVisual(station, additions, outcome);
        }

        private void UpdateVesselVisual(
            LabStation station,
            IReadOnlyList<VesselAddition> additions,
            ReactionOutcome outcome)
        {
            VesselVisual visual;
            if (!vesselVisuals.TryGetValue(station, out visual) || visual.LiquidRenderer == null)
            {
                return;
            }

            Color colour;
            if (outcome != null && outcome.Status == ReactionStatus.Reaction)
            {
                colour = outcome.DisplayColour;
            }
            else if (additions != null && additions.Count > 0)
            {
                var last = RuntimeChemicalRegistry.GetChemical(additions[additions.Count - 1].ChemicalId);
                colour = last == null ? LabTheme.Glass : last.ModelColour;
            }
            else
            {
                colour = LabTheme.WithAlpha(LabTheme.Glass, 0.16f);
            }

            colour.a = outcome != null && outcome.Effect == ReactionEffect.Precipitate ? 0.92f : 0.74f;
            visual.LiquidRenderer.material = GetMaterial(
                "Vessel_" + station + "_" + colour,
                colour,
                0f,
                outcome != null && outcome.Effect == ReactionEffect.Precipitate ? 0.28f : 0.76f,
                colour.a < 0.99f);

            if (visual.Particles != null)
            {
                visual.Particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }

        private void PlayReactionEffect(LabStation station, ReactionOutcome outcome)
        {
            VesselVisual visual;
            if (!vesselVisuals.TryGetValue(station, out visual))
            {
                return;
            }

            if (audioSystem != null)
            {
                audioSystem.PlayReaction(
                    outcome.Effect,
                    visual.Root == null ? Vector3.zero : visual.Root.position,
                    outcome.TemperatureC - BaselineTemperatureC);
            }

            if (visual.Particles == null)
            {
                return;
            }

            var particles = visual.Particles;
            var main = particles.main;
            main.startColor = outcome.DisplayColour;
            main.gravityModifier = outcome.Effect == ReactionEffect.Precipitate ? 0.38f : -0.04f;
            main.startSpeed = outcome.Effect == ReactionEffect.Precipitate
                ? new ParticleSystem.MinMaxCurve(0.03f, 0.12f)
                : new ParticleSystem.MinMaxCurve(0.16f, 0.52f);
            main.startLifetime = outcome.Effect == ReactionEffect.Precipitate
                ? new ParticleSystem.MinMaxCurve(1.4f, 2.2f)
                : new ParticleSystem.MinMaxCurve(0.8f, 1.7f);
            var emission = particles.emission;
            emission.rateOverTime = LabAccessibility.ReducedMotion ? 9f : 24f;
            particles.Play(true);
        }

        private Vector3 GetVesselPosition(LabStation station)
        {
            VesselVisual visual;
            return vesselVisuals.TryGetValue(station, out visual) && visual.Root != null
                ? visual.Root.position
                : Vector3.zero;
        }

        private LabStation GetZone(Vector3 position)
        {
            if (position.z < -3.35f)
            {
                return LabStation.FumeHood;
            }

            if (position.x < -4.75f || (position.x > 4.75f && position.z < 0.7f))
            {
                return LabStation.Storage;
            }

            if (position.x > 4.15f && position.z > 1.7f)
            {
                return LabStation.Sink;
            }

            if (position.x > 4.15f)
            {
                return LabStation.Analysis;
            }

            return LabStation.Workbench;
        }

        private GameObject CreatePrimitive(
            PrimitiveType type,
            string name,
            Transform parent,
            Vector3 position,
            Vector3 scale,
            Material material,
            bool keepCollider)
        {
            var instance = GameObject.CreatePrimitive(type);
            instance.name = name;
            instance.transform.SetParent(parent, false);
            instance.transform.localPosition = position;
            instance.transform.localScale = scale;
            var renderer = instance.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = material.renderQueue >= 3000
                    ? UnityEngine.Rendering.ShadowCastingMode.Off
                    : UnityEngine.Rendering.ShadowCastingMode.On;
                renderer.receiveShadows = material.renderQueue < 3000;
            }

            if (!keepCollider)
            {
                var collider = instance.GetComponent<Collider>();
                if (collider != null)
                {
                    Destroy(collider);
                }
            }

            return instance;
        }

        private void CreateWorldLabel(
            string content,
            Transform parent,
            Vector3 position,
            Quaternion rotation,
            Color colour,
            float characterSize)
        {
            var label = new GameObject("Label " + content);
            label.transform.SetParent(parent, false);
            label.transform.localPosition = position;
            label.transform.localRotation = rotation;
            var text = label.AddComponent<TextMesh>();
            text.text = content;
            text.font = LabTheme.CreateMonoFont(24);
            text.fontSize = 48;
            text.characterSize = characterSize;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.color = colour;
            var renderer = label.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = text.font.material;
        }

        private Material GetChemicalMaterial(ChemicalDefinition chemical)
        {
            var key = "Chemical_" + chemical.Id;
            Material material;
            if (materials.TryGetValue(key, out material))
            {
                return material;
            }

            var colour = chemical.ModelColour;
            colour.a = chemical.Transparent ? 0.68f : 1f;
            material = CreateMaterial(
                key,
                colour,
                chemical.Metallic,
                chemical.Smoothness,
                chemical.Transparent);
            materials[key] = material;
            return material;
        }

        private Material GetMaterial(
            string key,
            Color colour,
            float metallic,
            float smoothness,
            bool transparent = false)
        {
            Material material;
            if (materials.TryGetValue(key, out material))
            {
                return material;
            }

            material = CreateMaterial(key, colour, metallic, smoothness, transparent);
            materials[key] = material;
            return material;
        }

        private static Material CreateMaterial(
            string name,
            Color colour,
            float metallic,
            float smoothness,
            bool transparent)
        {
            var template = Resources.Load<Material>("DesktopLabStandard");
            Material material;
            if (template != null)
            {
                material = new Material(template);
            }
            else
            {
                var shader = Shader.Find("Standard");
                if (shader == null)
                {
                    shader = Shader.Find("Universal Render Pipeline/Lit");
                }

                if (shader == null)
                {
                    shader = Shader.Find("UI/Default");
                }

                if (shader == null)
                {
                    throw new InvalidOperationException(
                        "Không tìm thấy shader vật liệu DesktopLabStandard trong Resources.");
                }

                material = new Material(shader);
            }

            material.name = name;
            material.color = colour;
            if (material.HasProperty("_Metallic"))
            {
                material.SetFloat("_Metallic", metallic);
            }

            if (material.HasProperty("_Glossiness"))
            {
                material.SetFloat("_Glossiness", smoothness);
            }

            if (transparent)
            {
                material.SetFloat("_Mode", 3f);
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                material.SetInt("_ZWrite", 0);
                material.DisableKeyword("_ALPHATEST_ON");
                material.EnableKeyword("_ALPHABLEND_ON");
                material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                material.renderQueue = 3000;
            }

            return material;
        }

        private static bool HasCommandLineFlag(string flag)
        {
            var arguments = Environment.GetCommandLineArgs();
            for (var index = 0; index < arguments.Length; index++)
            {
                if (string.Equals(arguments[index], flag, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static string GetCommandLineValue(string key)
        {
            var arguments = Environment.GetCommandLineArgs();
            for (var index = 0; index < arguments.Length - 1; index++)
            {
                if (string.Equals(arguments[index], key, StringComparison.OrdinalIgnoreCase))
                {
                    return arguments[index + 1];
                }
            }

            return null;
        }

        private IEnumerator RunCaptureTest()
        {
            var captureView = GetCommandLineValue("-captureView");
            if (string.Equals(captureView, "periodic", StringComparison.OrdinalIgnoreCase))
            {
                player.SetPausedFromUi(false);
                player.transform.position = new Vector3(0f, 0.02f, -1.7f);
                player.transform.rotation = Quaternion.identity;
            }
            else if (string.Equals(captureView, "pause", StringComparison.OrdinalIgnoreCase))
            {
                player.SetPausedFromUi(true);
                hud.ShowPauseMenu();
            }
            else if (string.Equals(captureView, "main", StringComparison.OrdinalIgnoreCase))
            {
                player.SetPausedFromUi(true);
                hud.ShowMainMenu();
            }
            else if (string.Equals(captureView, "settings", StringComparison.OrdinalIgnoreCase))
            {
                player.SetPausedFromUi(true);
                hud.ShowMainMenu();
                hud.ShowSettingsFromMainMenu();
            }
            else if (string.Equals(captureView, "debug", StringComparison.OrdinalIgnoreCase))
            {
                player.SetPausedFromUi(false);
                diagnostics.SetVisible(true);
            }
            else
            {
                player.SetPausedFromUi(false);
            }

            yield return null;
            yield return new WaitForEndOfFrame();
            var capturePath = GetCommandLineValue("-capturePath");
            if (string.IsNullOrWhiteSpace(capturePath))
            {
                capturePath = Path.Combine(
                    Application.persistentDataPath,
                    "chemistry-lab-visual-test.png");
            }

            ScreenCapture.CaptureScreenshot(capturePath);
            var timeout = Time.realtimeSinceStartup + 8f;
            while (!File.Exists(capturePath) && Time.realtimeSinceStartup < timeout)
            {
                yield return new WaitForSecondsRealtime(0.2f);
            }

            if (!File.Exists(capturePath))
            {
                Debug.LogError("DESKTOP_LAB_CAPTURE_FAIL path=" + capturePath);
                Application.Quit(3);
                yield break;
            }

            Debug.Log("DESKTOP_LAB_CAPTURE_PASS path=" + capturePath);
            Application.Quit(0);
        }

        private IEnumerator RunSmokeTest()
        {
            yield return null;
            player.SetPausedFromUi(true);
            hud.ShowMainMenu();
            var mainMenuReady = hud.MainMenuVisible && !hud.SettingsVisible && !hud.PauseMenuVisible;
            hud.ShowSettingsFromMainMenu();
            var mainSettingsReady = hud.SettingsVisible;
            HandleEscape();
            var returnedToMain = hud.MainMenuVisible && !hud.SettingsVisible;
            hud.ShowPauseMenu();
            hud.ShowSettingsFromPauseMenu();
            var pauseSettingsReady = hud.SettingsVisible;
            HandleEscape();
            var returnedToPause = hud.PauseMenuVisible && !hud.SettingsVisible;
            var menuFlowReady = mainMenuReady
                && mainSettingsReady
                && returnedToMain
                && pauseSettingsReady
                && returnedToPause;

            var additions = new List<VesselAddition>
            {
                new VesselAddition("copper-sulfate", 10d),
                new VesselAddition("sodium-hydroxide", 10d)
            };
            var outcome = ReactionSimulator.Evaluate(additions, LabStation.Workbench, BaselineTemperatureC);
            if (outcome.Status != ReactionStatus.Reaction
                || outcome.Reaction == null
                || !string.Equals(outcome.Reaction.Id, MissionReactionId, StringComparison.Ordinal)
                || outcome.TheoreticalProductGrams <= 0d
                || outcome.EstimatedProductGrams <= 0d
                || audioSystem == null
                || !audioSystem.Ready
                || audioSystem.ClipCount != 15
                || hud == null
                || !hud.RuntimeUiReady
                || labSafety == null
                || labSafety.Health < 99.9f
                || player == null
                || player.ViewCamera == null
                || player.ViewCamera.GetComponent<AudioListener>() == null
                || starterChemicalCount != 4
                || !menuFlowReady
                || diagnostics == null)
            {
                WriteSmokeReport(
                    "failed",
                    "One or more runtime assertions failed.",
                    outcome,
                    menuFlowReady);
                Debug.LogError("DESKTOP_LAB_SMOKE_FAIL");
                Application.Quit(2);
                yield break;
            }

            WriteSmokeReport("succeeded", null, outcome, menuFlowReady);
            Debug.Log(
                "DESKTOP_LAB_SMOKE_PASS chemicals="
                + DesktopChemistryDatabase.AllChemicals.Count
                + " reactions="
                + DesktopChemistryDatabase.AllReactions.Count
                + " elements="
                + HighSchoolPeriodicTable.All.Count
                + " generatedCompounds="
                + CompoundGenerationMatrix.AcceptedCompoundCount
                + " uniqueFormulas="
                + CompoundGenerationMatrix.UniqueFormulaCount
                + " product="
                + outcome.EstimatedProductGrams.ToString("0.000")
                + "g audioClips="
                + audioSystem.ClipCount
                + " pauseButtons="
                + hud.PauseButtonCount
                + " menuButtons="
                + hud.MenuButtonCount
                + " starterChemicals="
                + starterChemicalCount
                + " cameraFov="
                + player.ViewCamera.fieldOfView.ToString("0.0"));
            yield return new WaitForSecondsRealtime(0.4f);
            Application.Quit(0);
        }

        private void WriteSmokeReport(
            string result,
            string failure,
            ReactionOutcome outcome,
            bool menuFlowVerified)
        {
            var reportPath = GetCommandLineValue("-reportPath");
            if (string.IsNullOrWhiteSpace(reportPath))
            {
                return;
            }

            var parentDirectory = Path.GetDirectoryName(reportPath);
            if (!string.IsNullOrWhiteSpace(parentDirectory))
            {
                Directory.CreateDirectory(parentDirectory);
            }

            var report = new StructuredSmokeReport
            {
                schemaVersion = "1.0",
                generatedAtUtc = DateTime.UtcNow.ToString("O"),
                unityVersion = Application.unityVersion,
                result = result,
                failure = failure,
                chemicals = DesktopChemistryDatabase.AllChemicals.Count,
                reactions = DesktopChemistryDatabase.AllReactions.Count,
                elements = HighSchoolPeriodicTable.All.Count,
                generatedCompounds = CompoundGenerationMatrix.AcceptedCompoundCount,
                uniqueGeneratedFormulas = CompoundGenerationMatrix.UniqueFormulaCount,
                reviewedGeneratedCompounds = CompoundGenerationMatrix.ReviewedCompoundCount,
                estimatedProductGrams = outcome == null ? 0d : outcome.EstimatedProductGrams,
                runtimeAudioClips = audioSystem == null ? 0 : audioSystem.ClipCount,
                pauseButtons = hud == null ? 0 : hud.PauseButtonCount,
                menuButtons = hud == null ? 0 : hud.MenuButtonCount,
                menuFlowVerified = menuFlowVerified,
                starterChemicals = starterChemicalCount,
                cameraFovDegrees = player == null || player.ViewCamera == null
                    ? 0f
                    : player.ViewCamera.fieldOfView,
                graphicsDevice = SystemInfo.graphicsDeviceName
            };
            File.WriteAllText(
                reportPath,
                JsonUtility.ToJson(report, true) + Environment.NewLine);
            Debug.Log("DESKTOP_LAB_JSON_SMOKE_REPORT path=" + reportPath);
        }

        private sealed class VesselVisual
        {
            public Transform Root;
            public Renderer LiquidRenderer;
            public ParticleSystem Particles;
        }

        [Serializable]
        private sealed class StructuredSmokeReport
        {
            public string schemaVersion;
            public string generatedAtUtc;
            public string unityVersion;
            public string result;
            public string failure;
            public int chemicals;
            public int reactions;
            public int elements;
            public int generatedCompounds;
            public int uniqueGeneratedFormulas;
            public int reviewedGeneratedCompounds;
            public double estimatedProductGrams;
            public int runtimeAudioClips;
            public int pauseButtons;
            public int menuButtons;
            public bool menuFlowVerified;
            public int starterChemicals;
            public float cameraFovDegrees;
            public string graphicsDevice;
        }
    }
}

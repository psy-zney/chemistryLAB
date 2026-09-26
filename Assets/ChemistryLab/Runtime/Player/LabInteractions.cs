using UnityEngine;

namespace ChemistryLab.Desktop
{
    public abstract class LabInteractable : MonoBehaviour
    {
        protected DesktopLabGame Game { get; private set; }
        private GameObject highlight;
        private GameObject focusOutline;
        private static Material outlineMaterial;

        public abstract string Prompt { get; }

        public void Initialise(DesktopLabGame game, GameObject highlightObject = null)
        {
            Game = game;
            highlight = highlightObject;
            SetFocused(false);
        }

        public virtual void SetFocused(bool focused)
        {
            if (highlight != null)
            {
                highlight.SetActive(focused);
            }
            if (focused && focusOutline == null)
            {
                CreateFocusOutline();
            }
            if (focusOutline != null)
            {
                focusOutline.SetActive(focused);
            }
        }

        private void CreateFocusOutline()
        {
            var renderers = GetComponentsInChildren<Renderer>(false);
            var found = false;
            var bounds = new Bounds();
            for (var i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] is ParticleSystemRenderer || (highlight != null
                    && renderers[i].transform.IsChildOf(highlight.transform))) continue;
                if (!found) { bounds = renderers[i].bounds; found = true; }
                else bounds.Encapsulate(renderers[i].bounds);
            }
            if (!found) return;

            if (outlineMaterial == null)
            {
                var shader = Shader.Find("Standard");
                if (shader == null) return;
                outlineMaterial = new Material(shader) { name = "Lab Interaction Outline" };
                outlineMaterial.color = LabTheme.UiEquation;
                outlineMaterial.EnableKeyword("_EMISSION");
                outlineMaterial.SetColor("_EmissionColor", LabTheme.UiEquation);
            }
            focusOutline = new GameObject("Hover Outline");
            focusOutline.transform.SetParent(transform, false);
            bounds.Expand(0.08f);
            var min = bounds.min;
            var max = bounds.max;
            var corners = new[] {
                new Vector3(min.x, min.y, min.z), new Vector3(max.x, min.y, min.z),
                new Vector3(max.x, min.y, max.z), new Vector3(min.x, min.y, max.z),
                new Vector3(min.x, max.y, min.z), new Vector3(max.x, max.y, min.z),
                new Vector3(max.x, max.y, max.z), new Vector3(min.x, max.y, max.z)
            };
            var edges = new[] { 0,1, 1,2, 2,3, 3,0, 4,5, 5,6, 6,7, 7,4, 0,4, 1,5, 2,6, 3,7 };
            for (var i = 0; i < edges.Length; i += 2)
            {
                var start = corners[edges[i]];
                var end = corners[edges[i + 1]];
                var segment = GameObject.CreatePrimitive(PrimitiveType.Cube);
                segment.name = "Outline Edge";
                segment.transform.SetParent(focusOutline.transform, true);
                segment.transform.position = (start + end) * 0.5f;
                segment.transform.rotation = Quaternion.FromToRotation(Vector3.up, end - start);
                segment.transform.localScale = new Vector3(0.018f, (end - start).magnitude, 0.018f);
                var collider = segment.GetComponent<Collider>();
                collider.enabled = false;
                Destroy(collider);
                segment.GetComponent<Renderer>().sharedMaterial = outlineMaterial;
            }
        }

        public abstract void Interact();
    }

    public sealed class ChemicalBottleInteractable : LabInteractable
    {
        public string ChemicalId { get; set; }

        public override string Prompt
        {
            get
            {
                var chemical = DesktopChemistryDatabase.GetChemical(ChemicalId);
                return chemical == null
                    ? LabLocalization.Text("E · Kiểm tra chai", "E · Inspect bottle")
                    : LabLocalization.Text("E · Lấy ", "E · Pick up ")
                      + chemical.Formula + " — " + chemical.Name;
            }
        }

        public override void Interact()
        {
            if (Game != null)
            {
                Game.SelectChemical(ChemicalId);
            }
        }
    }

    public sealed class VesselInteractable : LabInteractable
    {
        public LabStation Station { get; set; }

        public override string Prompt
        {
            get
            {
                if (Game == null)
                {
                    return LabLocalization.Text(
                        "E · Kiểm tra bình phản ứng",
                        "E · Inspect reaction vessel");
                }

                if (Game.SelectedChemical != null)
                {
                    if (Game.HasStagedSample(Station))
                    {
                        return LabLocalization.Text(
                            "Backspace · Cất mẫu đang cầm trước khi nạp ",
                            "Backspace · Put away held sample before loading ")
                            + Game.GetStagedSampleLabel(Station);
                    }

                    return LabLocalization.Text("Cần đặt ", "Place ")
                        + Game.SelectedChemical.Formula
                        + LabLocalization.Text(
                            " xuống khay trước khi nạp bình",
                            " on the tray before loading the vessel");
                }

                if (Game.HasStagedSample(Station))
                {
                    return LabLocalization.Text("F / E · Nạp ", "F / E · Load ")
                        + Game.GetStagedSampleLabel(Station)
                        + LabLocalization.Text(" từ khay đặt mẫu", " from preparation tray");
                }

                if (Game.SelectedChemical == null)
                {
                    if (Game.CanCollectProduct(Station))
                    {
                        return LabLocalization.Text(
                            "E · Thu và lưu sản phẩm vào kho",
                            "E · Collect and store product");
                    }

                    return LabLocalization.Text(
                        "E · Bình phản ứng — cần mẫu trên khay",
                        "E · Reaction vessel — sample tray is empty");
                }

                return LabLocalization.Text(
                    "E · Kiểm tra bình phản ứng",
                    "E · Inspect reaction vessel");
            }
        }

        public override void Interact()
        {
            if (Game != null)
            {
                if (!Game.HasStagedSample(Station)
                    && Game.SelectedChemical == null
                    && Game.CanCollectProduct(Station))
                {
                    Game.CollectProduct(Station);
                }
                else
                {
                    Game.AddSelectedToVessel(Station);
                }
            }
        }
    }

    public sealed class SamplePreparationInteractable : LabInteractable
    {
        public LabStation Station { get; set; }

        public override string Prompt
        {
            get
            {
                if (Game == null)
                {
                    return LabLocalization.Text(
                        "E · Kiểm tra khay đặt mẫu",
                        "E · Inspect preparation tray");
                }

                if (Game.SelectedChemical != null)
                {
                    if (Game.HasStagedSample(Station))
                    {
                        return LabLocalization.Text("Khay đã có ", "Tray contains ")
                            + Game.GetStagedSampleLabel(Station)
                            + LabLocalization.Text(
                                " · Backspace để cất mẫu đang cầm",
                                " · Backspace to put away held sample");
                    }

                    return LabLocalization.Text("E · Đặt ", "E · Place ")
                        + Game.SelectedChemical.Formula
                        + LabLocalization.Text(" xuống khay · ", " on tray · ")
                        + Game.SelectedAmountGrams.ToString("0.#") + " g";
                }

                return Game.HasStagedSample(Station)
                    ? LabLocalization.Text("E · Cầm lại ", "E · Pick up ")
                      + Game.GetStagedSampleLabel(Station)
                      + LabLocalization.Text(" từ khay", " from tray")
                    : LabLocalization.Text(
                        "Khay đặt mẫu đang trống",
                        "Preparation tray is empty");
            }
        }

        public override void Interact()
        {
            if (Game != null)
            {
                Game.ToggleSampleOnPreparationSurface(Station);
            }
        }
    }

    public sealed class ThermalControlInteractable : LabInteractable
    {
        public LabStation Station { get; set; }

        public override string Prompt
        {
            get
            {
                return LabLocalization.Text(
                    "R / E · Gia nhiệt bình thêm 25 °C",
                    "R / E · Heat vessel by 25 °C");
            }
        }

        public override void Interact()
        {
            if (Game != null)
            {
                Game.AdjustVesselTemperature(Station, 25f);
            }
        }
    }

    public sealed class SinkInteractable : LabInteractable
    {
        public override string Prompt
        {
            get
            {
                return LabLocalization.Text(
                    "E · Rửa sạch toàn bộ cốc phản ứng",
                    "E · Wash all reaction vessels");
            }
        }

        public override void Interact()
        {
            if (Game != null)
            {
                Game.WashVessels();
            }
        }
    }

    public sealed class AnalysisInteractable : LabInteractable
    {
        public override string Prompt
        {
            get
            {
                return LabLocalization.Text(
                    "E · Mở bảng dữ liệu vật lý và hóa học",
                    "E · Open physical and chemical data");
            }
        }

        public override void Interact()
        {
            if (Game != null)
            {
                Game.ToggleInspector(true);
            }
        }
    }

    public sealed class ElementTileInteractable : LabInteractable
    {
        public int AtomicNumber { get; set; }

        public override string Prompt
        {
            get
            {
                var element = HighSchoolPeriodicTable.Get(AtomicNumber);
                return element == null
                    ? LabLocalization.Text(
                        "E · Kiểm tra ô nguyên tố",
                        "E · Inspect element tile")
                    : LabLocalization.Text("E · Phân tích ", "E · Analyse ")
                      + element.Symbol + " — " + element.Name;
            }
        }

        public override void Interact()
        {
            if (Game != null)
            {
                Game.InspectElement(AtomicNumber);
            }
        }
    }

    public sealed class RespiratorStationInteractable : LabInteractable
    {
        public override string Prompt
        {
            get
            {
                var safety = Game == null ? null : Game.SafetySystem;
                if (safety == null)
                {
                    return LabLocalization.Text(
                        "E · Kiểm tra tủ PPE",
                        "E · Inspect PPE cabinet");
                }

                if (!safety.RespiratorOwned)
                {
                    return LabLocalization.Text(
                        "E · Mua và đeo mặt nạ lọc độc · ",
                        "E · Buy and wear respirator · ")
                        + LabSafetySystem.RespiratorPrice
                        + LabLocalization.Text(" tín dụng", " credits");
                }

                return safety.RespiratorEquipped
                    ? LabLocalization.Text(
                        "E · Tháo mặt nạ lọc độc",
                        "E · Remove respirator")
                    : LabLocalization.Text(
                        "E · Đeo mặt nạ lọc độc",
                        "E · Wear respirator");
            }
        }

        public override void Interact()
        {
            if (Game != null)
            {
                Game.ToggleRespirator();
            }
        }
    }

    public sealed class GasTrapInteractable : LabInteractable
    {
        public override string Prompt
        {
            get
            {
                var safety = Game == null ? null : Game.SafetySystem;
                return safety != null && safety.GasTrapConnected
                    ? LabLocalization.Text(
                        "F / E · Tháo bình cách ly khỏi hệ rửa khí",
                        "F / E · Disconnect isolation trap")
                    : LabLocalization.Text(
                        "F / E · Nối bình cách ly vào hệ rửa khí",
                        "F / E · Connect isolation trap");
            }
        }

        public override void Interact()
        {
            if (Game != null)
            {
                Game.ToggleGasTrap();
            }
        }
    }

    public sealed class HoodVentilationInteractable : LabInteractable
    {
        public override string Prompt
        {
            get
            {
                var safety = Game == null ? null : Game.SafetySystem;
                return safety != null && safety.FumeHoodFanOn
                    ? LabLocalization.Text("F / E · Tắt quạt tủ hút", "F / E · Turn off hood fan")
                    : LabLocalization.Text("F / E · Bật quạt tủ hút", "F / E · Turn on hood fan");
            }
        }

        public override void Interact()
        {
            if (Game != null) Game.ToggleFumeHoodFan();
        }
    }

    public sealed class TestTubeRackInteractable : LabInteractable
    {
        public override string Prompt
        {
            get { return LabLocalization.Text("E · Kiểm tra giá ống nghiệm", "E · Inspect test tube rack"); }
        }

        public override void Interact()
        {
            if (Game != null && Game.Hud != null)
                Game.Hud.ShowTransient(LabLocalization.Text(
                    "Ống nghiệm dùng để quan sát. Đặt mẫu trên khay trước khi nạp bình.",
                    "Test tubes are for observation. Stage samples on the tray before loading a vessel."));
        }
    }

    [RequireComponent(typeof(CharacterController))]
    public sealed class FirstPersonChemistController : MonoBehaviour
    {
        private const float WalkSpeed = 3.6f;
        private const float RunSpeed = 6.2f;
        private const float Gravity = -22f;
        private const float LookSensitivity = 2.1f;
        private const float InteractionDistance = 3.4f;
        private const float NormalFieldOfView = 66f;
        private const float MinimumFieldOfView = 34f;

        private CharacterController controller;
        private Camera viewCamera;
        private DesktopLabGame game;
        private Transform handsRoot;
        private Vector3 handsBasePosition;
        private Vector3 cameraBasePosition;
        private LabInteractable focusedInteractable;
        private LabTouchZone moveTouchZone;
        private LabTouchZone lookTouchZone;
        private float verticalVelocity;
        private float pitch;
        private float bobPhase;
        private float stepTimer;
        private bool paused;
        private bool cinematic;
        private bool moving;
        private bool running;
        private bool sprintRequested;
        private float zoomFieldOfView = NormalFieldOfView;

        public bool IsPaused
        {
            get { return paused; }
        }

        public Camera ViewCamera
        {
            get { return viewCamera; }
        }

        public string FocusedPrompt
        {
            get { return focusedInteractable == null ? "—" : focusedInteractable.Prompt; }
        }

        public bool IsMoving
        {
            get { return moving; }
        }

        public bool IsRunning
        {
            get { return running; }
        }

        public void SetTouchZones(LabTouchZone moveZone, LabTouchZone lookZone)
        {
            moveTouchZone = moveZone;
            lookTouchZone = lookZone;
        }

        public void SetSprintRequested(bool requested)
        {
            sprintRequested = requested;
        }

        public bool CanUseGameplayActions
        {
            get { return game != null && !paused && !cinematic && !game.ReactionCameraActive
                && (game.Hud == null || (!game.Hud.MainMenuVisible && !game.Hud.PauseMenuVisible && !game.Hud.SettingsVisible)); }
        }

        public void DispatchInteract()
        {
            if (!CanUseGameplayActions) return;
            RaycastHit hit;
            if (viewCamera != null && Physics.Raycast(viewCamera.transform.position,
                viewCamera.transform.forward, out hit, InteractionDistance,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                var target = hit.collider.GetComponentInParent<LabInteractable>();
                if (target != null) target.Interact();
            }
        }

        public void DispatchInspect()
        {
            if (!CanUseGameplayActions) return;
            if (game != null)
            {
                game.ToggleInspector();
            }
        }

        public void DispatchPutAway()
        {
            if (!CanUseGameplayActions) return;
            if (game != null)
            {
                game.ClearSelectedChemical();
            }
        }

        public void DispatchAmount(float delta)
        {
            if (!CanUseGameplayActions) return;
            if (game != null)
            {
                game.AdjustSelectedAmount(delta);
            }
        }

        public void DispatchTemperature(float deltaC)
        {
            if (!CanUseGameplayActions) return;
            if (game != null)
            {
                game.AdjustVesselTemperature(deltaC);
            }
        }

        public void DispatchDilute()
        {
            if (!CanUseGameplayActions) return;
            if (game != null)
            {
                game.DiluteCurrentVessel();
            }
        }

        public void DispatchCollect()
        {
            if (!CanUseGameplayActions) return;
            if (game != null)
            {
                game.CollectProduct(game.CurrentVesselStation);
            }
        }

        public void DispatchInventory()
        {
            if (!CanUseGameplayActions) return;
            if (game != null)
            {
                game.CycleSynthesizedBatch();
            }
        }

        public void DispatchRespirator()
        {
            if (!CanUseGameplayActions) return;
            if (game != null)
            {
                game.ToggleRespirator();
            }
        }

        public void DispatchGasTrap()
        {
            if (!CanUseGameplayActions) return;
            if (game != null)
            {
                game.ToggleGasTrap();
            }
        }

        private void DispatchContextAction()
        {
            if (!CanUseGameplayActions || viewCamera == null) return;
            RaycastHit hit;
            if (!Physics.Raycast(viewCamera.transform.position, viewCamera.transform.forward,
                out hit, InteractionDistance, Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore)) return;
            var target = hit.collider.GetComponentInParent<LabInteractable>();
            if (target is VesselInteractable || target is GasTrapInteractable
                || target is HoodVentilationInteractable) target.Interact();
        }

        private void DispatchHeatAction()
        {
            if (!CanUseGameplayActions || viewCamera == null) return;
            RaycastHit hit;
            if (!Physics.Raycast(viewCamera.transform.position, viewCamera.transform.forward,
                out hit, InteractionDistance, Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore)) return;
            var target = hit.collider.GetComponentInParent<LabInteractable>();
            var vessel = target as VesselInteractable;
            if (vessel != null)
            {
                game.AdjustVesselTemperature(vessel.Station, 25f);
                if (game != null && game.AudioSystem != null)
                {
                    game.AudioSystem.PlayUiClick();
                }
            }
            var heater = target as ThermalControlInteractable;
            if (heater != null) heater.Interact();
        }

        public void DispatchPause()
        {
            if (game != null)
            {
                game.HandleEscape();
            }
        }

        public void SetCursorLocked(bool locked)
        {
            if (paused)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                return;
            }

            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }

        public void ToggleCursorLock()
        {
            if (paused)
            {
                return;
            }

            SetCursorLocked(Cursor.lockState != CursorLockMode.Locked);
        }

        public void SetCinematicMode(bool value)
        {
            cinematic = value;
            moving = false;
            running = false;
            if (moveTouchZone != null) moveTouchZone.ResetPointer();
            if (lookTouchZone != null) lookTouchZone.ResetPointer();

            if (handsRoot != null)
            {
                handsRoot.gameObject.SetActive(!value);
            }

            if (value)
            {
                if (focusedInteractable != null)
                {
                    focusedInteractable.SetFocused(false);
                    focusedInteractable = null;
                }

                if (game != null && game.Hud != null)
                {
                    game.Hud.SetInteractionPrompt(string.Empty);
                }
            }
        }

        public void Initialise(DesktopLabGame owner, Camera camera, Transform handRig)
        {
            game = owner;
            viewCamera = camera;
            handsRoot = handRig;
            handsBasePosition = handRig == null ? Vector3.zero : handRig.localPosition;
            cameraBasePosition = camera.transform.localPosition;
            controller = GetComponent<CharacterController>();
            SetPaused(false);
        }

        private void Update()
        {
            if (game == null || viewCamera == null)
            {
                return;
            }

            if (moveTouchZone == null && game.Hud != null)
            {
                moveTouchZone = game.Hud.MoveTouchZone;
                lookTouchZone = game.Hud.LookTouchZone;
            }

            if (cinematic || game.ReactionCameraActive)
            {
                if (moveTouchZone != null) moveTouchZone.ResetPointer();
                if (lookTouchZone != null) lookTouchZone.ResetPointer();

                if (Input.GetKeyDown(KeyCode.Space)
                    || Input.GetKeyDown(KeyCode.E)
                    || Input.GetKeyDown(KeyCode.Escape))
                {
                    game.SkipReactionCamera();
                }

                AnimateHands(0f, 0f);
                return;
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (moveTouchZone != null) moveTouchZone.ResetPointer();
                if (lookTouchZone != null) lookTouchZone.ResetPointer();
                game.HandleEscape();
                return;
            }

            // Gated strictly when paused or when menus are active!
            if (paused || (game.Hud != null && (game.Hud.MainMenuVisible || game.Hud.SettingsVisible || game.Hud.PauseMenuVisible)))
            {
                if (moveTouchZone != null) moveTouchZone.ResetPointer();
                if (lookTouchZone != null) lookTouchZone.ResetPointer();
                AnimateHands(0f, 0f);
                UpdateCameraMotion();
                return;
            }

            // Contextual desktop mouse cursor release without stranding pointer lock
            if (Input.GetKeyDown(KeyCode.LeftAlt) || Input.GetKeyDown(KeyCode.RightAlt))
            {
                ToggleCursorLock();
            }

            if (Cursor.lockState != CursorLockMode.Locked && !paused && !game.Hud.TouchControlsEnabled)
            {
                if (Input.GetMouseButtonDown(0))
                {
                    var eventSystem = UnityEngine.EventSystems.EventSystem.current;
                    var isPointerOverUi = eventSystem != null && eventSystem.IsPointerOverGameObject();
                    if (!isPointerOverUi)
                    {
                        SetCursorLocked(true);
                    }
                }
            }

            // Active gameplay hotkeys - using shared dispatch
            if (Input.GetKeyDown(KeyCode.F))
            {
                DispatchContextAction();
            }

            if (Input.GetKeyDown(KeyCode.V)) DispatchInspect();
            if (Input.GetKeyDown(KeyCode.R)) DispatchHeatAction();
            if (Input.GetKeyDown(KeyCode.Tab) || Input.GetKeyDown(KeyCode.Q))
                game.Hud.ToggleMissionBoard();
            if (Input.GetKeyDown(KeyCode.Backspace)) DispatchPutAway();
            for (var slot = 0; slot < 9; slot++)
            {
                if (Input.GetKeyDown((KeyCode)((int)KeyCode.Alpha1 + slot)))
                {
                    game.SelectQuickChemical(slot);
                    break;
                }
            }

            if (Input.GetKeyDown(KeyCode.F10))
            {
                game.ToggleReducedMotion();
                game.AudioSystem.PlayUiClick();
            }

            if (Input.GetKeyDown(KeyCode.F9))
            {
                game.ToggleAudio();
            }

            if (Input.GetKeyDown(KeyCode.F3))
            {
                game.ToggleDiagnostics();
            }

            if (Input.GetKeyDown(KeyCode.F6))
            {
                DispatchRespirator();
            }

            if (Input.GetKeyDown(KeyCode.F7))
            {
                DispatchGasTrap();
            }

            if (Input.GetKeyDown(KeyCode.LeftBracket))
            {
                DispatchAmount(-1f);
            }

            if (Input.GetKeyDown(KeyCode.RightBracket))
            {
                DispatchAmount(1f);
            }

            if (Input.GetKeyDown(KeyCode.PageUp))
            {
                DispatchTemperature(25f);
            }

            if (Input.GetKeyDown(KeyCode.PageDown))
            {
                DispatchTemperature(-25f);
            }

            if (Input.GetKeyDown(KeyCode.F8))
            {
                DispatchDilute();
            }

            if (Input.GetKeyDown(KeyCode.C))
            {
                DispatchCollect();
            }

            if (Input.GetKeyDown(KeyCode.I))
            {
                DispatchInventory();
            }

            UpdateLook();
            UpdateMovement();
            UpdateInteraction();
            UpdateZoom();
            UpdateCameraMotion();
            game.UpdatePlayerZone(transform.position);
        }

        private void UpdateLook()
        {
            var mouseX = 0f;
            var mouseY = 0f;
            if (Cursor.lockState == CursorLockMode.Locked && !game.Hud.TouchControlsEnabled)
            {
                mouseX = Input.GetAxisRaw("Mouse X") * LookSensitivity;
                mouseY = Input.GetAxisRaw("Mouse Y") * LookSensitivity;
            }

            var touchLook = lookTouchZone != null ? lookTouchZone.ConsumeLookDelta() : Vector2.zero;
            var totalLookX = mouseX + touchLook.x;
            var totalLookY = mouseY + touchLook.y;

            transform.Rotate(Vector3.up, totalLookX, Space.Self);
            pitch = Mathf.Clamp(pitch - totalLookY, -78f, 78f);
            viewCamera.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
            AnimateHands(totalLookX, totalLookY);
        }

        private void UpdateMovement()
        {
            var horizontal = Input.GetAxisRaw("Horizontal");
            var vertical = Input.GetAxisRaw("Vertical");
            var touchMove = moveTouchZone != null ? moveTouchZone.InputVector : Vector2.zero;
            var combined = new Vector2(horizontal + touchMove.x, vertical + touchMove.y);
            var input = Vector2.ClampMagnitude(combined, 1f);
            running = (Input.GetKey(KeyCode.LeftShift) || sprintRequested) && input.sqrMagnitude > 0.01f;
            var speed = running ? RunSpeed : WalkSpeed;
            var planar = (transform.right * input.x + transform.forward * input.y) * speed;

            if (controller.isGrounded && verticalVelocity < 0f)
            {
                verticalVelocity = -2f;
            }

            verticalVelocity += Gravity * Time.deltaTime;
            controller.Move((planar + Vector3.up * verticalVelocity) * Time.deltaTime);
            moving = input.sqrMagnitude > 0.01f;

            if (moving && controller.isGrounded)
            {
                stepTimer -= Time.deltaTime;
                if (stepTimer <= 0f)
                {
                    game.AudioSystem.PlayFootstep(running);
                    stepTimer = running ? 0.31f : 0.48f;
                }
            }
            else
            {
                stepTimer = Mathf.Min(stepTimer, 0.08f);
            }
        }

        private void UpdateInteraction()
        {
            RaycastHit hit;
            LabInteractable next = null;
            if (Physics.Raycast(
                viewCamera.transform.position,
                viewCamera.transform.forward,
                out hit,
                InteractionDistance,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore))
            {
                next = hit.collider.GetComponentInParent<LabInteractable>();
            }

            if (next != focusedInteractable)
            {
                if (focusedInteractable != null)
                {
                    focusedInteractable.SetFocused(false);
                }

                focusedInteractable = next;
                if (focusedInteractable != null)
                {
                    focusedInteractable.SetFocused(true);
                }
            }

            game.Hud.SetInteractionPrompt(
                focusedInteractable == null ? string.Empty : focusedInteractable.Prompt);

            if (focusedInteractable != null && Input.GetKeyDown(KeyCode.E))
            {
                DispatchInteract();
            }
        }

        private void UpdateZoom()
        {
            if (!game.Hud.TouchControlsEnabled && Cursor.lockState == CursorLockMode.Locked)
            {
                var scroll = Input.GetAxis("Mouse ScrollWheel");
                if (Mathf.Abs(scroll) > 0.001f)
                    zoomFieldOfView = Mathf.Clamp(zoomFieldOfView - Mathf.Sign(scroll) * 4f,
                        MinimumFieldOfView, NormalFieldOfView);
            }
        }

        private void AnimateHands(float mouseX, float mouseY)
        {
            if (handsRoot == null)
            {
                return;
            }

            if (LabAccessibility.ReducedMotion)
            {
                handsRoot.localPosition = handsBasePosition;
                handsRoot.localRotation = Quaternion.identity;
                return;
            }

            var walkBob = moving ? Mathf.Sin(Time.time * 9f) * 0.012f : Mathf.Sin(Time.time * 1.4f) * 0.002f;
            var targetPosition = handsBasePosition
                + Vector3.up * walkBob
                + Vector3.right * Mathf.Clamp(-mouseX * 0.0015f, -0.012f, 0.012f);
            var targetRotation = Quaternion.Euler(
                Mathf.Clamp(mouseY * 0.18f, -2f, 2f),
                Mathf.Clamp(-mouseX * 0.22f, -3f, 3f),
                0f);
            handsRoot.localPosition = Vector3.Lerp(handsRoot.localPosition, targetPosition, 12f * Time.deltaTime);
            handsRoot.localRotation = Quaternion.Slerp(handsRoot.localRotation, targetRotation, 10f * Time.deltaTime);
        }

        private void UpdateCameraMotion()
        {
            if (viewCamera == null)
            {
                return;
            }

            if (cinematic)
            {
                return;
            }

            var holdingZoom = !paused && !game.Hud.TouchControlsEnabled
                && ((Cursor.lockState == CursorLockMode.Locked && Input.GetMouseButton(1))
                    || Input.GetKey(KeyCode.Z));
            var targetFov = holdingZoom ? Mathf.Min(zoomFieldOfView, 40f) : zoomFieldOfView;
            if (paused || LabAccessibility.ReducedMotion)
            {
                viewCamera.transform.localPosition = cameraBasePosition;
                viewCamera.fieldOfView = paused ? NormalFieldOfView : targetFov;
                return;
            }

            if (moving && controller.isGrounded)
            {
                bobPhase += Time.deltaTime * (running ? 12.2f : 8.8f);
            }
            else
            {
                bobPhase = Mathf.Lerp(bobPhase, 0f, 5f * Time.deltaTime);
            }

            var bobStrength = moving ? (running ? 1.25f : 1f) : 0f;
            var bob = new Vector3(
                Mathf.Cos(bobPhase * 0.5f) * 0.008f,
                Mathf.Sin(bobPhase) * 0.017f,
                0f) * bobStrength;
            viewCamera.transform.localPosition = Vector3.Lerp(
                viewCamera.transform.localPosition,
                cameraBasePosition + bob,
                14f * Time.deltaTime);
            viewCamera.fieldOfView = Mathf.Lerp(
                viewCamera.fieldOfView,
                holdingZoom || zoomFieldOfView < NormalFieldOfView ? targetFov : running ? 70f : NormalFieldOfView,
                1f - Mathf.Exp(-10f * Time.deltaTime));
        }

        public void SetPausedFromUi(bool value)
        {
            SetPaused(value);
        }

        private void SetPaused(bool value)
        {
            paused = value;
            if (value && focusedInteractable != null)
            {
                focusedInteractable.SetFocused(false);
                focusedInteractable = null;
                if (game != null && game.Hud != null)
                    game.Hud.SetInteractionPrompt(string.Empty);
            }
            if (moveTouchZone != null) moveTouchZone.ResetPointer();
            if (lookTouchZone != null) lookTouchZone.ResetPointer();
            var touch = game != null && game.Hud != null && game.Hud.TouchControlsEnabled;
            Cursor.lockState = value || touch ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = value || touch;
            if (game != null)
            {
                game.SetPaused(value);
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus && !paused)
            {
                SetPaused(true);
            }
        }
    }
}

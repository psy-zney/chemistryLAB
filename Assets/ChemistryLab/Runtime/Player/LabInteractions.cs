using UnityEngine;

namespace ChemistryLab.Desktop
{
    public abstract class LabInteractable : MonoBehaviour
    {
        protected DesktopLabGame Game { get; private set; }
        private GameObject highlight;

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
                            "Q · Cất mẫu đang cầm trước khi nạp ",
                            "Q · Put away held sample before loading ")
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
                    return LabLocalization.Text("E · Nạp ", "E · Load ")
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
                                " · Q để cất mẫu đang cầm",
                                " · Q to put away held sample");
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
                    "E · Gia nhiệt bình thêm 25 °C",
                    "E · Heat vessel by 25 °C");
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
                        "E · Tháo bình cách ly khỏi hệ rửa khí",
                        "E · Disconnect isolation trap")
                    : LabLocalization.Text(
                        "E · Nối bình cách ly vào hệ rửa khí",
                        "E · Connect isolation trap");
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

    [RequireComponent(typeof(CharacterController))]
    public sealed class FirstPersonChemistController : MonoBehaviour
    {
        private const float WalkSpeed = 3.6f;
        private const float RunSpeed = 6.2f;
        private const float Gravity = -22f;
        private const float LookSensitivity = 2.1f;
        private const float InteractionDistance = 3.4f;

        private CharacterController controller;
        private Camera viewCamera;
        private DesktopLabGame game;
        private Transform handsRoot;
        private Vector3 handsBasePosition;
        private Vector3 cameraBasePosition;
        private LabInteractable focusedInteractable;
        private float verticalVelocity;
        private float pitch;
        private float bobPhase;
        private float stepTimer;
        private bool paused;
        private bool cinematic;
        private bool moving;
        private bool running;

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

        public void SetCinematicMode(bool value)
        {
            cinematic = value;
            moving = false;
            running = false;
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

            if (cinematic || game.ReactionCameraActive)
            {
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
                game.HandleEscape();
                return;
            }

            if (Input.GetKeyDown(KeyCode.F))
            {
                game.ToggleInspector();
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
                game.ToggleRespirator();
            }

            if (Input.GetKeyDown(KeyCode.F7))
            {
                game.ToggleGasTrap();
            }

            if (Input.GetKeyDown(KeyCode.LeftBracket))
            {
                game.AdjustSelectedAmount(-1f);
            }

            if (Input.GetKeyDown(KeyCode.RightBracket))
            {
                game.AdjustSelectedAmount(1f);
            }

            if (Input.GetKeyDown(KeyCode.Q))
            {
                game.ClearSelectedChemical();
            }

            if (paused)
            {
                AnimateHands(0f, 0f);
                UpdateCameraMotion();
                return;
            }

            if (Input.GetKeyDown(KeyCode.PageUp))
            {
                game.AdjustVesselTemperature(25f);
            }

            if (Input.GetKeyDown(KeyCode.PageDown))
            {
                game.AdjustVesselTemperature(-25f);
            }

            if (Input.GetKeyDown(KeyCode.F8))
            {
                game.DiluteCurrentVessel();
            }

            if (Input.GetKeyDown(KeyCode.C))
            {
                game.CollectProduct(game.CurrentVesselStation);
            }

            if (Input.GetKeyDown(KeyCode.I))
            {
                game.CycleSynthesizedBatch();
            }

            UpdateLook();
            UpdateMovement();
            UpdateInteraction();
            UpdateCameraMotion();
            game.UpdatePlayerZone(transform.position);
        }

        private void UpdateLook()
        {
            var mouseX = Input.GetAxisRaw("Mouse X") * LookSensitivity;
            var mouseY = Input.GetAxisRaw("Mouse Y") * LookSensitivity;

            transform.Rotate(Vector3.up, mouseX, Space.Self);
            pitch = Mathf.Clamp(pitch - mouseY, -78f, 78f);
            viewCamera.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
            AnimateHands(mouseX, mouseY);
        }

        private void UpdateMovement()
        {
            var horizontal = Input.GetAxisRaw("Horizontal");
            var vertical = Input.GetAxisRaw("Vertical");
            var input = Vector2.ClampMagnitude(new Vector2(horizontal, vertical), 1f);
            running = Input.GetKey(KeyCode.LeftShift) && input.sqrMagnitude > 0.01f;
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
                focusedInteractable.Interact();
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

            if (paused || LabAccessibility.ReducedMotion)
            {
                viewCamera.transform.localPosition = Vector3.Lerp(
                    viewCamera.transform.localPosition,
                    cameraBasePosition,
                    14f * Time.unscaledDeltaTime);
                viewCamera.fieldOfView = Mathf.Lerp(
                    viewCamera.fieldOfView,
                    66f,
                    10f * Time.unscaledDeltaTime);
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
                running ? 70f : 66f,
                7f * Time.deltaTime);
        }

        public void SetPausedFromUi(bool value)
        {
            SetPaused(value);
        }

        private void SetPaused(bool value)
        {
            paused = value;
            Cursor.lockState = value ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = value;
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

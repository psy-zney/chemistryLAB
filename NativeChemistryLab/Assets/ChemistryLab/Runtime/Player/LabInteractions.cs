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
                    ? "E · Kiểm tra chai"
                    : "E · Lấy " + chemical.Formula + " — " + chemical.Name;
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
                if (Game == null || Game.SelectedChemical == null)
                {
                    return "E · Cốc phản ứng — cần chọn hóa chất";
                }

                return "E · Nạp " + Game.SelectedAmountGrams.ToString("0.#")
                    + " g " + Game.SelectedChemical.Formula;
            }
        }

        public override void Interact()
        {
            if (Game != null)
            {
                Game.AddSelectedToVessel(Station);
            }
        }
    }

    public sealed class SinkInteractable : LabInteractable
    {
        public override string Prompt
        {
            get { return "E · Rửa sạch toàn bộ cốc phản ứng"; }
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
            get { return "E · Mở bảng dữ liệu vật lý và hóa học"; }
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
                    ? "E · Kiểm tra ô nguyên tố"
                    : "E · Phân tích " + element.Symbol + " — " + element.Name;
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

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                SetPaused(!paused);
            }

            if (Input.GetKeyDown(KeyCode.F))
            {
                game.ToggleInspector();
            }

            if (Input.GetKeyDown(KeyCode.F10))
            {
                LabAccessibility.ReducedMotion = !LabAccessibility.ReducedMotion;
                game.Hud.SetAccessibilityState(LabAccessibility.ReducedMotion);
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

            UpdateLook();
            UpdateMovement();
            UpdateInteraction();
            UpdateCameraMotion();
            game.UpdatePlayerZone(transform.position);
        }

        private void UpdateLook()
        {
            var mouseX = Input.GetAxis("Mouse X") * LookSensitivity;
            var mouseY = Input.GetAxis("Mouse Y") * LookSensitivity;

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
            if (!hasFocus)
            {
                SetPaused(true);
            }
        }
    }
}

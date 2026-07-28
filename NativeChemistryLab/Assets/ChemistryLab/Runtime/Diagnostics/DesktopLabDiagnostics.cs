using System.Text;
using UnityEngine;
using UnityEngine.Profiling;

namespace ChemistryLab.Desktop
{
    /// <summary>
    /// A compact in-game diagnostics surface for build verification without the Unity Editor.
    /// </summary>
    public sealed class DesktopLabDiagnostics : MonoBehaviour
    {
        private readonly StringBuilder builder = new StringBuilder(512);

        private DesktopLabGame game;
        private FirstPersonChemistController player;
        private DesktopLabAudio audioSystem;
        private DesktopLabHud hud;
        private float smoothedFrameSeconds = 1f / 60f;
        private float nextRefreshAt;

        public bool Visible { get; private set; }

        public void Initialise(
            DesktopLabGame owner,
            FirstPersonChemistController controller,
            DesktopLabAudio audio,
            DesktopLabHud interfaceHud)
        {
            game = owner;
            player = controller;
            audioSystem = audio;
            hud = interfaceHud;
            SetVisible(false);
        }

        private void Update()
        {
            var frameSeconds = Mathf.Max(0.0001f, Time.unscaledDeltaTime);
            smoothedFrameSeconds = Mathf.Lerp(smoothedFrameSeconds, frameSeconds, 0.08f);

            if (!Visible || Time.unscaledTime < nextRefreshAt)
            {
                return;
            }

            nextRefreshAt = Time.unscaledTime + 0.2f;
            RefreshText();
        }

        public void Toggle()
        {
            SetVisible(!Visible);
            if (audioSystem != null)
            {
                audioSystem.PlayUiClick();
            }
        }

        public void SetVisible(bool value)
        {
            Visible = value;
            if (hud != null)
            {
                hud.SetDebugVisible(value);
            }

            if (value)
            {
                RefreshText();
            }
        }

        private void RefreshText()
        {
            if (hud == null || game == null || player == null)
            {
                return;
            }

            var camera = player.ViewCamera;
            var position = player.transform.position;
            var selected = game.SelectedChemical;
            var outcome = game.CurrentOutcome;
            var memoryMb = Profiler.GetTotalAllocatedMemoryLong() / (1024f * 1024f);
            var device = SystemInfo.graphicsDeviceName;
            if (device.Length > 31)
            {
                device = device.Substring(0, 31) + "…";
            }

            builder.Length = 0;
            builder.Append("FPS      ").Append((1f / smoothedFrameSeconds).ToString("0"))
                .Append("  ·  ").Append((smoothedFrameSeconds * 1000f).ToString("0.0")).Append(" ms\n");
            builder.Append("PLAYER   ")
                .Append(position.x.ToString("0.00")).Append("  ")
                .Append(position.y.ToString("0.00")).Append("  ")
                .Append(position.z.ToString("0.00")).Append('\n');
            builder.Append("CAMERA   ")
                .Append(camera == null ? "—" : camera.fieldOfView.ToString("0.0") + "°")
                .Append(player.IsRunning ? "  RUN" : player.IsMoving ? "  WALK" : "  IDLE")
                .Append('\n');
            builder.Append("ZONE     ").Append(DesktopLabGame.ZoneLabel(game.CurrentZone)).Append('\n');
            builder.Append("FOCUS    ").Append(Compact(player.FocusedPrompt, 34)).Append('\n');
            builder.Append("SAMPLE   ")
                .Append(selected == null ? "—" : selected.Formula + " · " + game.SelectedAmountGrams.ToString("0.#") + " g")
                .Append('\n');
            builder.Append("VESSEL   ").Append(DesktopLabGame.ZoneLabel(game.CurrentVesselStation))
                .Append(" · ").Append(game.GetVesselAdditionCount(game.CurrentVesselStation)).Append(" mẫu\n");
            builder.Append("STATE    ")
                .Append(outcome == null ? "—" : outcome.Status + " · " + Compact(outcome.Title, 22))
                .Append('\n');
            builder.Append("DATA     ")
                .Append(HighSchoolPeriodicTable.All.Count).Append(" nguyên tố · ")
                .Append(DesktopChemistryDatabase.AllChemicals.Count).Append(" chất · ")
                .Append(DesktopChemistryDatabase.AllReactions.Count).Append(" p/ư\n");
            builder.Append("AUDIO    ")
                .Append(audioSystem == null ? "—" : audioSystem.StatusLabel + " · " + audioSystem.ClipCount + " clip")
                .Append('\n');
            builder.Append("MEMORY   ").Append(memoryMb.ToString("0.0")).Append(" MB\n");
            builder.Append("GPU      ").Append(device);
            hud.SetDebugText(builder.ToString());
        }

        private static string Compact(string value, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "—";
            }

            value = value.Replace("\n", " ").Trim();
            return value.Length <= maxLength ? value : value.Substring(0, maxLength - 1) + "…";
        }
    }
}

using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ChemistryLab.Presentation.UnityViews
{
    /// <summary>
    /// Lobby view for the mobile game. It repairs incomplete scene wiring at runtime
    /// by creating the diorama UI only when the serialized references are absent.
    /// This keeps hand-authored scenes and their existing gameplay flow intact.
    /// </summary>
    public sealed class LobbyHomeUnityView : MonoBehaviour
    {
        [Header("Top Currency Header")]
        [SerializeField] private TMP_Text dollarsText;
        [SerializeField] private TMP_Text diamondsText;
        [SerializeField] private Button settingsButton;

        [Header("Left Navigation")]
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

        private bool createdRuntimeLobby;

        private void Awake()
        {
            ResolveScenePanels();
            if (dollarsText == null || enterLabButton == null) BuildDioramaLobby();
            AutoSetupLayout();
            HookEvents();
        }

        private void ResolveScenePanels()
        {
            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null) return;

            // MainLab.unity was saved with its root Canvas at (0,0,0), which
            // makes every child UI invisible and leaves only the camera colour.
            canvas.transform.localScale = Vector3.one;

            var scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler != null)
            {
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1080f, 1920f);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = .5f;
            }

            if (characterCreationPanel == null)
            {
                var panel = canvas.transform.Find("CharacterCreationPanel");
                if (panel != null) characterCreationPanel = panel.gameObject;
            }
            if (mainLabPanel == null)
            {
                var panel = canvas.transform.Find("LabPanel");
                if (panel != null) mainLabPanel = panel.gameObject;
            }

            // The scene was saved with all three panels active. The empty lobby was
            // last in canvas order and covered the lab; make its intended state explicit.
            if (characterCreationPanel != null) characterCreationPanel.SetActive(false);
            if (mainLabPanel != null) mainLabPanel.SetActive(false);
        }

        private void HookEvents()
        {
            AddClick(enterLabButton, OnEnterLabClicked);
            AddClick(sandboxModeButton, OnEnterSandboxClicked);
            AddClick(characterButton, OnCharacterClicked);
            AddClick(shopButton, OnShopClicked);
            AddClick(inventoryButton, OnInventoryClicked);
            AddClick(questButton, OnQuestClicked);
            AddClick(settingsButton, OnSettingsClicked);
        }

        private static void AddClick(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button == null) return;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(action);
        }

        private void OnEnterLabClicked()
        {
            Debug.Log("[LobbyHomeView] Entering Main Lab Campaign Mode...");
            if (mainLabPanel != null) mainLabPanel.SetActive(true);
            gameObject.SetActive(false);
        }

        private void OnEnterSandboxClicked()
        {
            Debug.Log("[LobbyHomeView] Entering Free Creative Sandbox Mode...");
            if (mainLabPanel != null) mainLabPanel.SetActive(true);
            gameObject.SetActive(false);
        }

        private void OnCharacterClicked()
        {
            Debug.Log("[LobbyHomeView] Opening Character Customizer...");
            if (characterCreationPanel != null) characterCreationPanel.SetActive(true);
        }

        private static void OnShopClicked() { Debug.Log("[LobbyHomeView] Opening Shop..."); }
        private static void OnInventoryClicked() { Debug.Log("[LobbyHomeView] Opening Kho / Inventory..."); }
        private static void OnQuestClicked() { Debug.Log("[LobbyHomeView] Opening Nhiem Vu / Quests..."); }
        private static void OnSettingsClicked() { Debug.Log("[LobbyHomeView] Opening Settings..."); }

        [ContextMenu("Auto Setup Layout")]
        public void AutoSetupLayout()
        {
            if (!createdRuntimeLobby) return;
            if (dollarsText != null) dollarsText.text = "500 $";
            if (diamondsText != null) diamondsText.text = "10  ◆";
        }

        private void BuildDioramaLobby()
        {
            createdRuntimeLobby = true;
            var root = transform as RectTransform;
            if (root == null) return;

            var existingImage = GetComponent<Image>();
            if (existingImage != null) existingImage.color = new Color(0.94f, 0.98f, 1f, 1f);

            // Back wall, floor and perspective cues.
            MakePanel("Wall", root, new Vector2(0f, .34f), new Vector2(1f, 1f), Color.white);
            MakePanel("Floor", root, new Vector2(0f, 0f), new Vector2(1f, .40f), new Color(.77f, .87f, .95f, 1f));
            for (var i = 0; i < 5; i++)
            {
                var y = .06f + i * .07f;
                MakePanel("FloorLine", root, new Vector2(.07f, y), new Vector2(.93f, y + .004f), new Color(.31f, .43f, .56f, .42f));
            }

            BuildCabinet(root, "Left cabinet", new Vector2(.035f, .35f), new Vector2(.245f, .86f));
            BuildCabinet(root, "Right cabinet", new Vector2(.755f, .35f), new Vector2(.965f, .86f));
            BuildSink(root);
            BuildScientist(root);
            BuildBench(root);

            var header = MakePanel("Header", root, new Vector2(.025f, .89f), new Vector2(.975f, .975f), new Color(.08f, .15f, .23f, .92f));
            dollarsText = MakeLabel("Dollars", header, "500 $", new Vector2(.04f, .18f), new Vector2(.26f, .82f), 34, new Color(.19f, .88f, .46f));
            diamondsText = MakeLabel("Diamonds", header, "10  ◆", new Vector2(.28f, .18f), new Vector2(.48f, .82f), 34, new Color(.62f, .42f, .95f));
            MakeLabel("Title", header, "CHEMISTRY LAB", new Vector2(.49f, .18f), new Vector2(.84f, .82f), 38, Color.white, TextAlignmentOptions.Center);
            settingsButton = MakeButton("Settings", header, "⚙", new Vector2(.89f, .18f), new Vector2(.97f, .82f), new Color(.12f, .66f, .86f));

            var nav = MakePanel("Navigation", root, new Vector2(.025f, .42f), new Vector2(.20f, .84f), new Color(.08f, .15f, .23f, .88f));
            shopButton = MakeButton("Shop", nav, "SHOP", new Vector2(.08f, .71f), new Vector2(.92f, .91f), new Color(.98f, .59f, .15f));
            inventoryButton = MakeButton("Inventory", nav, "KHO", new Vector2(.08f, .48f), new Vector2(.92f, .68f), new Color(.16f, .62f, .88f));
            questButton = MakeButton("Quest", nav, "NHIỆM VỤ", new Vector2(.08f, .25f), new Vector2(.92f, .45f), new Color(.60f, .40f, .91f));
            characterButton = MakeButton("Character", nav, "NHÂN VẬT", new Vector2(.08f, .02f), new Vector2(.92f, .22f), new Color(.19f, .77f, .53f));

            enterLabButton = MakeButton("Enter Lab", root, "VÀO LAB", new Vector2(.69f, .05f), new Vector2(.94f, .15f), new Color(.00f, .66f, .91f));
            sandboxModeButton = MakeButton("Sandbox", root, "SANDBOX", new Vector2(.43f, .05f), new Vector2(.66f, .15f), new Color(.57f, .35f, .86f));
            MakeLabel("Hint", root, "Chạm VÀO LAB để bắt đầu thí nghiệm", new Vector2(.23f, .16f), new Vector2(.77f, .21f), 24, new Color(.10f, .20f, .30f), TextAlignmentOptions.Center);
        }

        private static void BuildCabinet(RectTransform root, string name, Vector2 min, Vector2 max)
        {
            var artwork = MakeArtwork(name + " artwork", root, min, max, "Art/cabinet");
            if (artwork != null)
            {
                if (name.StartsWith("Right")) artwork.localScale = new Vector3(-1f, 1f, 1f);
                artwork.gameObject.AddComponent<LobbyFloat>();
                return;
            }

            var cabinet = MakePanel(name, root, min, max, new Color(.08f, .17f, .25f, .97f));
            MakePanel("Glass", cabinet, new Vector2(.07f, .07f), new Vector2(.93f, .93f), new Color(.42f, .84f, .96f, .34f));
            for (var i = 0; i < 3; i++)
            {
                var y = .21f + i * .25f;
                MakePanel("Shelf", cabinet, new Vector2(.10f, y), new Vector2(.90f, y + .025f), new Color(.12f, .24f, .34f, .8f));
                var bottle = MakePanel("Bottle", cabinet, new Vector2(.20f + (i % 2) * .30f, y + .04f), new Vector2(.40f + (i % 2) * .30f, y + .18f), i == 0 ? new Color(.66f, .28f, .97f, .92f) : new Color(.12f, .91f, .76f, .92f));
                bottle.gameObject.AddComponent<LobbyFloat>();
            }
        }

        private static void BuildSink(RectTransform root)
        {
            MakePanel("Sink shadow", root, new Vector2(.36f, .61f), new Vector2(.64f, .73f), new Color(.22f, .29f, .36f, .25f));
            var sink = MakePanel("Sink", root, new Vector2(.37f, .63f), new Vector2(.63f, .75f), new Color(.61f, .68f, .74f, 1f));
            MakePanel("Basin", sink, new Vector2(.10f, .22f), new Vector2(.90f, .70f), new Color(.12f, .18f, .23f, 1f));
            MakePanel("Faucet", root, new Vector2(.48f, .745f), new Vector2(.52f, .81f), new Color(.18f, .28f, .35f, 1f));
        }

        private static void BuildScientist(RectTransform root)
        {
            var artwork = MakeArtwork("Scientist artwork", root, new Vector2(.365f, .32f), new Vector2(.635f, .70f), "Art/scientist-chibi");
            if (artwork != null)
            {
                artwork.gameObject.AddComponent<LobbyFloat>();
                return;
            }

            var body = MakePanel("Scientist coat", root, new Vector2(.43f, .35f), new Vector2(.57f, .53f), Color.white);
            MakePanel("Head", root, new Vector2(.445f, .50f), new Vector2(.555f, .64f), new Color(1f, .75f, .52f));
            MakePanel("Hair", root, new Vector2(.44f, .60f), new Vector2(.56f, .66f), new Color(.95f, .60f, .08f));
            MakePanel("Goggles", root, new Vector2(.455f, .54f), new Vector2(.545f, .585f), new Color(.00f, .78f, .96f, .95f));
            body.gameObject.AddComponent<LobbyFloat>();
        }

        private static void BuildBench(RectTransform root)
        {
            MakePanel("Bench shadow", root, new Vector2(.20f, .16f), new Vector2(.80f, .22f), new Color(.08f, .12f, .18f, .28f));
            var bench = MakePanel("Workbench", root, new Vector2(.20f, .20f), new Vector2(.80f, .39f), new Color(.95f, .96f, .94f, 1f));
            MakePanel("Bench frame", bench, new Vector2(.02f, .00f), new Vector2(.98f, .12f), new Color(.10f, .18f, .27f, 1f));
            var purpleFlask = MakeArtwork("Purple flask", bench, new Vector2(.09f, .20f), new Vector2(.27f, .83f), "Art/flask-purple");
            var tubeRack = MakeArtwork("Test tube rack", bench, new Vector2(.35f, .26f), new Vector2(.64f, .76f), "Art/tube-rack");
            var cyanFlask = MakeArtwork("Cyan flask", bench, new Vector2(.73f, .19f), new Vector2(.91f, .83f), "Art/flask-cyan");
            if (purpleFlask != null) purpleFlask.gameObject.AddComponent<LobbyFloat>();
            if (tubeRack != null) tubeRack.gameObject.AddComponent<LobbyFloat>();
            if (cyanFlask != null) cyanFlask.gameObject.AddComponent<LobbyFloat>();

            // Keep a legible fallback if Resources has not been imported yet.
            if (purpleFlask == null) MakePanel("Purple flask fallback", bench, new Vector2(.12f, .22f), new Vector2(.25f, .72f), new Color(.68f, .20f, .94f, .94f));
            if (tubeRack == null) MakePanel("Rack fallback", bench, new Vector2(.42f, .25f), new Vector2(.58f, .72f), new Color(.96f, .70f, .12f, 1f));
            if (cyanFlask == null) MakePanel("Cyan flask fallback", bench, new Vector2(.73f, .20f), new Vector2(.87f, .72f), new Color(.00f, .76f, .93f, .9f));
        }

        private static RectTransform MakePanel(string name, RectTransform parent, Vector2 min, Vector2 max, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            go.GetComponent<Image>().color = color;
            return rect;
        }

        private static RectTransform MakeArtwork(string name, RectTransform parent, Vector2 min, Vector2 max, string resourcePath)
        {
            var texture = Resources.Load<Texture2D>(resourcePath);
            if (texture == null) return null;

            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var image = go.GetComponent<RawImage>();
            image.texture = texture;
            image.raycastTarget = false;
            return rect;
        }

        private static TMP_Text MakeLabel(string name, RectTransform parent, string text, Vector2 min, Vector2 max, float fontSize, Color color, TextAlignmentOptions alignment = TextAlignmentOptions.Left)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var label = go.GetComponent<TextMeshProUGUI>();
            label.text = text;
            label.font = TMP_Settings.defaultFontAsset;
            label.fontSize = fontSize;
            label.color = color;
            label.alignment = alignment;
            label.fontStyle = FontStyles.Bold;
            return label;
        }

        private static Button MakeButton(string name, RectTransform parent, string label, Vector2 min, Vector2 max, Color color)
        {
            var rect = MakePanel(name, parent, min, max, color);
            var button = rect.gameObject.AddComponent<Button>();
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(.84f, .96f, 1f, 1f);
            colors.pressedColor = new Color(.72f, .86f, .94f, 1f);
            colors.selectedColor = colors.highlightedColor;
            button.colors = colors;
            MakeLabel("Label", rect, label, new Vector2(.04f, .08f), new Vector2(.96f, .92f), 27, Color.white, TextAlignmentOptions.Center);
            return button;
        }
    }

    /// <summary>Small transform-only idle animation for independently drawn laboratory props.</summary>
    public sealed class LobbyFloat : MonoBehaviour
    {
        private Vector3 baseScale;
        private float phase;

        private void Awake()
        {
            baseScale = transform.localScale;
            phase = Random.value * 6.283185f;
        }

        private void Update()
        {
            var pulse = 1f + Mathf.Sin(Time.unscaledTime * 1.1f + phase) * .025f;
            transform.localScale = baseScale * pulse;
        }
    }
}

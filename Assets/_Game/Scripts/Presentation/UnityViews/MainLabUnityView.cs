// Hallmark · pre-emit critique: P5 H4 E4 S5 R4 V4 · playful laboratory diorama.
using System;
using System.Collections.Generic;
using System.Text;
using ChemistryLab.Domain;
using ChemistryLab.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ChemistryLab.Presentation.UnityViews
{
    /// <summary>
    /// Runtime-built four-zone laboratory. It intentionally keeps the old scene children intact
    /// but inactive, so a partial scene can be upgraded without losing serialized references.
    /// </summary>
    public sealed class MainLabUnityView : MonoBehaviour, IMainLabView
    {
        private static readonly Color Ink = new Color(.055f, .11f, .16f, 1f);
        private static readonly Color Wall = new Color(.88f, .94f, .96f, 1f);
        private static readonly Color Paper = new Color(.97f, .99f, 1f, 1f);
        private static readonly Color Teal = new Color(.04f, .55f, .62f, 1f);
        private static readonly Color Navy = new Color(.08f, .20f, .29f, 1f);
        private static readonly Color Gold = new Color(.96f, .62f, .14f, 1f);
        private static readonly Color Danger = new Color(.78f, .25f, .22f, 1f);

        private MainLabPresenter presenter;
        private TMP_Text statusText;
        private TMP_Text benchSummaryText;
        private TMP_Text outputText;
        private TMP_Text toolStateText;
        private TMP_Text selectedChemicalText;
        private TMP_Text quantityHintText;
        private TMP_InputField quantityInput;
        private Image beakerLiquid;
        private Button pourButton;
        private RectTransform inventoryList;
        private RectTransform recipeList;
        private readonly List<ChemicalInventoryEntry> inventory = new List<ChemicalInventoryEntry>();
        private readonly List<ReactionEntry> reactions = new List<ReactionEntry>();
        private readonly Dictionary<string, decimal> benchItems = new Dictionary<string, decimal>(StringComparer.Ordinal);
        private string selectedChemicalId;
        private string selectedToolId;
        private string selectedReactionId;
        private bool built;

        private void Awake()
        {
            BuildLabIfNeeded();
        }

        public void BindPresenter(MainLabPresenter mainLabPresenter) { Bind(mainLabPresenter); }

        public void Bind(MainLabPresenter mainLabPresenter)
        {
            presenter = mainLabPresenter;
        }

        public void ShowBench(IReadOnlyDictionary<string, decimal> selectedItems, string toolId)
        {
            selectedToolId = toolId;
            benchItems.Clear();
            if (selectedItems != null)
                foreach (var pair in selectedItems) benchItems[pair.Key] = pair.Value;

            var total = 0m;
            foreach (var pair in benchItems) total += pair.Value;
            if (benchSummaryText != null)
            {
                var text = new StringBuilder();
                text.AppendLine(benchItems.Count == 0 ? "Cốc đang trống" : "Hỗn hợp trong cốc");
                foreach (var pair in benchItems) text.AppendLine("• " + pair.Key + "  " + pair.Value + " g");
                text.Append("Tổng: " + total + " / 100 g");
                benchSummaryText.text = text.ToString();
            }
            if (toolStateText != null) toolStateText.text = string.IsNullOrEmpty(toolId) ? "Chưa lấy dụng cụ" : "Beaker 100 ml đã chọn";
            if (beakerLiquid != null)
            {
                beakerLiquid.rectTransform.anchorMax = new Vector2(1f, Mathf.Clamp01((float)total / 100f));
                beakerLiquid.gameObject.SetActive(total > 0m);
            }
            UpdateQuantityControls();
        }

        public void ShowLocalisedMessage(string localisationKey)
        {
            if (statusText != null) statusText.text = "Thông báo: " + Humanize(localisationKey);
        }

        public void ShowExperimentReady(string reactionId, string outputItemId, decimal outputMassGram)
        {
            if (outputText != null) outputText.text = "Phản ứng hoàn tất: " + outputMassGram + " g " + outputItemId + ". Hãy thu hồi sản phẩm.";
            if (statusText != null) statusText.text = "Sản phẩm đang ở trong cốc.";
        }

        public void ShowProductCollected(string itemId, decimal massGram)
        {
            if (outputText != null) outputText.text = "Đã đưa " + massGram + " g " + itemId + " vào kho.";
        }

        public void RefreshTool(string toolId, ToolCleanState cleanliness)
        {
            if (toolStateText != null) toolStateText.text = "Beaker 100 ml: " + (cleanliness == ToolCleanState.Clean ? "sạch" : "cần rửa");
        }

        public void ShowChemicalInventory(IReadOnlyList<ChemicalInventoryEntry> entries)
        {
            inventory.Clear();
            if (entries != null) for (var index = 0; index < entries.Count; index++) inventory.Add(entries[index]);
            if (FindChemical(selectedChemicalId) == null) selectedChemicalId = null;
            RenderInventory();
            UpdateQuantityControls();
        }

        public void ShowReactionOptions(IReadOnlyList<ReactionEntry> entries, string selectedId)
        {
            reactions.Clear();
            if (entries != null) for (var index = 0; index < entries.Count; index++) reactions.Add(entries[index]);
            selectedReactionId = selectedId;
            RenderRecipes();
        }

        private void BuildLabIfNeeded()
        {
            if (built) return;
            built = true;
            var root = transform as RectTransform;
            if (root == null) return;

            // Preserve rather than delete the incomplete authoring-time controls.
            for (var index = 0; index < root.childCount; index++) root.GetChild(index).gameObject.SetActive(false);
            var image = GetComponent<Image>();
            if (image != null) image.color = Wall;

            var runtime = MakePanel("RuntimeLab", root, Vector2.zero, Vector2.one, Wall);
            MakePanel("Floor", runtime, new Vector2(0f, 0f), new Vector2(1f, .28f), new Color(.70f, .81f, .85f, 1f));
            for (var index = 0; index < 4; index++)
            {
                var y = .06f + index * .052f;
                MakePanel("Floor tile", runtime, new Vector2(.02f, y), new Vector2(.98f, y + .004f), new Color(.30f, .47f, .54f, .25f));
            }

            var header = MakePanel("Header", runtime, new Vector2(.02f, .89f), new Vector2(.98f, .975f), Ink);
            MakeLabel("Title", header, "PHÒNG THÍ NGHIỆM", new Vector2(.03f, .14f), new Vector2(.48f, .88f), 31, Color.white);
            statusText = MakeLabel("Status", header, "Chọn cốc, công thức rồi lấy hoá chất từ tủ.", new Vector2(.49f, .18f), new Vector2(.97f, .82f), 17, new Color(.76f, .91f, .93f), TextAlignmentOptions.Right);

            BuildChemicalCabinet(runtime);
            BuildWorkbench(runtime);
            BuildRecipeShelf(runtime);
            BuildWashStation(runtime);
        }

        private void BuildChemicalCabinet(RectTransform root)
        {
            var cabinet = MakePanel("Tủ hoá chất", root, new Vector2(.025f, .22f), new Vector2(.265f, .865f), Navy);
            MakeLabel("Cabinet title", cabinet, "TỦ HOÁ CHẤT", new Vector2(.06f, .91f), new Vector2(.94f, .985f), 20, Color.white, TextAlignmentOptions.Center);
            MakeLabel("Cabinet hint", cabinet, "Chọn chất trong kho", new Vector2(.08f, .84f), new Vector2(.92f, .90f), 13, new Color(.67f, .83f, .87f), TextAlignmentOptions.Center);
            inventoryList = MakePanel("Inventory cards", cabinet, new Vector2(.06f, .36f), new Vector2(.94f, .82f), new Color(.05f, .14f, .20f, .52f));
            selectedChemicalText = MakeLabel("Selected chemical", cabinet, "Chưa chọn hoá chất", new Vector2(.08f, .29f), new Vector2(.92f, .35f), 15, Color.white, TextAlignmentOptions.Center);
            MakeLabel("Quantity label", cabinet, "LƯỢNG RÓT (g)", new Vector2(.08f, .23f), new Vector2(.92f, .28f), 13, new Color(.67f, .83f, .87f));
            var minus = MakeButton("Decrease", cabinet, "−", new Vector2(.08f, .135f), new Vector2(.25f, .215f), Teal, 24);
            minus.onClick.AddListener(delegate { ChangeQuantity(-1); });
            quantityInput = MakeNumberInput(cabinet, new Vector2(.28f, .135f), new Vector2(.70f, .215f));
            quantityInput.onEndEdit.AddListener(delegate { UpdateQuantityControls(); });
            var plus = MakeButton("Increase", cabinet, "+", new Vector2(.73f, .135f), new Vector2(.90f, .215f), Teal, 24);
            plus.onClick.AddListener(delegate { ChangeQuantity(1); });
            quantityHintText = MakeLabel("Quantity hint", cabinet, "Chọn một chất để nhập lượng.", new Vector2(.08f, .09f), new Vector2(.92f, .13f), 12, new Color(.75f, .86f, .89f), TextAlignmentOptions.Center);
            pourButton = MakeButton("Pour", cabinet, "RÓT VÀO CỐC", new Vector2(.08f, .015f), new Vector2(.92f, .078f), Gold, 17);
            pourButton.onClick.AddListener(PourSelectedChemical);
        }

        private void BuildWorkbench(RectTransform root)
        {
            var bench = MakePanel("Bàn thí nghiệm", root, new Vector2(.285f, .245f), new Vector2(.705f, .865f), Paper);
            MakePanel("Bench rim", bench, new Vector2(.02f, .02f), new Vector2(.98f, .10f), new Color(.20f, .37f, .42f, 1f));
            MakeLabel("Bench title", bench, "BÀN THÍ NGHIỆM", new Vector2(.05f, .91f), new Vector2(.95f, .98f), 21, Ink, TextAlignmentOptions.Center);
            toolStateText = MakeLabel("Tool state", bench, "Chưa lấy dụng cụ", new Vector2(.08f, .83f), new Vector2(.92f, .89f), 15, Teal, TextAlignmentOptions.Center);
            var beaker = MakePanel("Beaker", bench, new Vector2(.30f, .35f), new Vector2(.70f, .76f), new Color(.63f, .88f, .94f, .42f));
            MakePanel("Beaker lip", bench, new Vector2(.27f, .75f), new Vector2(.73f, .79f), new Color(.18f, .44f, .52f, 1f));
            beakerLiquid = MakePanel("Liquid", beaker, new Vector2(.05f, 0f), new Vector2(.95f, 0f), new Color(.10f, .72f, .79f, .72f)).GetComponent<Image>();
            beakerLiquid.gameObject.SetActive(false);
            benchSummaryText = MakeLabel("Bench summary", bench, "Cốc đang trống\nTổng: 0 / 100 g", new Vector2(.08f, .14f), new Vector2(.92f, .31f), 16, Ink, TextAlignmentOptions.Center);
            outputText = MakeLabel("Output", bench, "Chọn một công thức để bắt đầu.", new Vector2(.06f, .04f), new Vector2(.94f, .12f), 14, new Color(.24f, .41f, .47f), TextAlignmentOptions.Center);
        }

        private void BuildRecipeShelf(RectTransform root)
        {
            var shelf = MakePanel("Tủ dụng cụ và công thức", root, new Vector2(.725f, .42f), new Vector2(.975f, .865f), Navy);
            MakeLabel("Shelf title", shelf, "DỤNG CỤ & CÔNG THỨC", new Vector2(.04f, .90f), new Vector2(.96f, .985f), 17, Color.white, TextAlignmentOptions.Center);
            var beaker = MakeButton("Select beaker", shelf, "LẤY BEAKER 100 ml", new Vector2(.08f, .80f), new Vector2(.92f, .875f), Teal, 15);
            beaker.onClick.AddListener(delegate { if (presenter != null) presenter.OnSelectTool("beaker_100ml"); });
            MakeLabel("Recipe label", shelf, "CÔNG THỨC", new Vector2(.08f, .72f), new Vector2(.92f, .78f), 14, new Color(.67f, .83f, .87f), TextAlignmentOptions.Center);
            recipeList = MakePanel("Recipe cards", shelf, new Vector2(.07f, .08f), new Vector2(.93f, .70f), new Color(.05f, .14f, .20f, .52f));
        }

        private void BuildWashStation(RectTransform root)
        {
            var station = MakePanel("Bồn rửa", root, new Vector2(.285f, .035f), new Vector2(.975f, .215f), new Color(.20f, .38f, .45f, 1f));
            MakeLabel("Sink title", station, "BỒN RỬA & THU HỒI", new Vector2(.04f, .61f), new Vector2(.34f, .91f), 18, Color.white);
            MakeLabel("Sink subtitle", station, "Thu sản phẩm xong thì rửa cốc để làm phản ứng mới.", new Vector2(.04f, .20f), new Vector2(.40f, .57f), 13, new Color(.77f, .90f, .92f));
            var run = MakeButton("Run reaction", station, "THỰC HIỆN PHẢN ỨNG", new Vector2(.43f, .22f), new Vector2(.64f, .80f), Gold, 16);
            run.onClick.AddListener(delegate { if (presenter != null) presenter.OnExecuteExperiment(); });
            var collect = MakeButton("Collect product", station, "THU HỒI", new Vector2(.67f, .22f), new Vector2(.81f, .80f), new Color(.16f, .64f, .38f), 16);
            collect.onClick.AddListener(delegate { if (presenter != null) presenter.OnCollectProduct(); });
            var wash = MakeButton("Wash beaker", station, "RỬA CỐC", new Vector2(.83f, .22f), new Vector2(.97f, .80f), new Color(.19f, .50f, .78f), 16);
            wash.onClick.AddListener(delegate { if (presenter != null) presenter.OnWashTool("beaker_100ml"); });
        }

        private void RenderInventory()
        {
            if (inventoryList == null) return;
            ClearChildren(inventoryList);
            var count = Math.Max(inventory.Count, 1);
            for (var index = 0; index < inventory.Count; index++)
            {
                var entry = inventory[index];
                var max = 1f - index / (float)count;
                var min = 1f - (index + 1) / (float)count;
                var colour = ParseColour(entry.Colour, new Color(.19f, .47f, .59f, 1f));
                var card = MakeButton("Chemical " + entry.Id, inventoryList, entry.Formula + "  •  " + entry.AvailableGram + " g", new Vector2(.04f, min + .015f), new Vector2(.96f, max - .015f), colour, 15);
                var captured = entry.Id;
                card.onClick.AddListener(delegate { SelectChemical(captured); });
            }
            if (inventory.Count == 0) MakeLabel("Empty inventory", inventoryList, "Kho đang trống", new Vector2(.08f, .40f), new Vector2(.92f, .60f), 15, Color.white, TextAlignmentOptions.Center);
        }

        private void RenderRecipes()
        {
            if (recipeList == null) return;
            ClearChildren(recipeList);
            var count = Math.Max(reactions.Count, 1);
            for (var index = 0; index < reactions.Count; index++)
            {
                var entry = reactions[index];
                var max = 1f - index / (float)count;
                var min = 1f - (index + 1) / (float)count;
                var active = string.Equals(entry.Id, selectedReactionId, StringComparison.Ordinal);
                var card = MakeButton("Recipe " + entry.Id, recipeList, (active ? "✓ " : "") + entry.Equation, new Vector2(.04f, min + .01f), new Vector2(.96f, max - .01f), active ? Teal : new Color(.16f, .31f, .38f, 1f), 12);
                var captured = entry.Id;
                card.onClick.AddListener(delegate { if (presenter != null) presenter.OnSelectReaction(captured); });
            }
        }

        private void SelectChemical(string id)
        {
            selectedChemicalId = id;
            var chemical = FindChemical(id);
            if (selectedChemicalText != null && chemical != null) selectedChemicalText.text = chemical.Formula + " • " + chemical.AvailableGram + " g trong kho";
            if (quantityInput != null) quantityInput.text = "1";
            UpdateQuantityControls();
        }

        private void ChangeQuantity(int delta)
        {
            var current = ReadQuantity();
            if (quantityInput != null) quantityInput.text = Math.Max(1, current + delta).ToString();
            UpdateQuantityControls();
        }

        private void UpdateQuantityControls()
        {
            var chemical = FindChemical(selectedChemicalId);
            var limit = GetQuantityLimit(chemical);
            var valid = chemical != null && limit > 0;
            if (quantityInput != null)
            {
                quantityInput.interactable = valid;
                if (valid)
                {
                    var quantity = Mathf.Clamp(ReadQuantity(), 1, limit);
                    quantityInput.text = quantity.ToString();
                }
            }
            if (pourButton != null) pourButton.interactable = valid;
            if (quantityHintText != null) quantityHintText.text = chemical == null ? "Chọn một chất để nhập lượng." : (limit > 0 ? "Tối đa " + limit + " g (kho và cốc)." : "Cốc đã đầy hoặc chất đã hết.");
        }

        private void PourSelectedChemical()
        {
            var chemical = FindChemical(selectedChemicalId);
            if (chemical == null || presenter == null) return;
            if (string.IsNullOrEmpty(selectedToolId))
            {
                ShowLocalisedMessage("Hãy lấy Beaker 100 ml trước khi rót.");
                return;
            }
            var amount = Math.Min(ReadQuantity(), GetQuantityLimit(chemical));
            if (amount <= 0) { ShowLocalisedMessage("Lượng rót không hợp lệ."); return; }
            presenter.OnSelectChemical(chemical.Id, amount);
        }

        private int GetQuantityLimit(ChemicalInventoryEntry chemical)
        {
            if (chemical == null) return 0;
            decimal total = 0m;
            foreach (var pair in benchItems)
                if (!string.Equals(pair.Key, chemical.Id, StringComparison.Ordinal)) total += pair.Value;
            var room = Math.Max(0m, 100m - total);
            return (int)Math.Min(chemical.AvailableGram, room);
        }

        private int ReadQuantity()
        {
            int value;
            return quantityInput != null && int.TryParse(quantityInput.text, out value) ? Math.Max(1, value) : 1;
        }

        private ChemicalInventoryEntry FindChemical(string id)
        {
            for (var index = 0; index < inventory.Count; index++) if (string.Equals(inventory[index].Id, id, StringComparison.Ordinal)) return inventory[index];
            return null;
        }

        private static string Humanize(string key) { return string.IsNullOrEmpty(key) ? "Không xác định" : key.Replace('.', ' '); }

        private static void ClearChildren(RectTransform parent)
        {
            for (var index = parent.childCount - 1; index >= 0; index--) Destroy(parent.GetChild(index).gameObject);
        }

        private static RectTransform MakePanel(string name, RectTransform parent, Vector2 min, Vector2 max, Color colour)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>(); rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
            go.GetComponent<Image>().color = colour;
            return rect;
        }

        private static TMP_Text MakeLabel(string name, RectTransform parent, string text, Vector2 min, Vector2 max, float size, Color colour, TextAlignmentOptions alignment = TextAlignmentOptions.Left)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>(); rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
            var label = go.GetComponent<TextMeshProUGUI>(); label.font = TMP_Settings.defaultFontAsset; label.text = text; label.fontSize = size; label.color = colour; label.alignment = alignment; label.textWrappingMode = TextWrappingModes.Normal;
            return label;
        }

        private static Button MakeButton(string name, RectTransform parent, string label, Vector2 min, Vector2 max, Color colour, float size)
        {
            var rect = MakePanel(name, parent, min, max, colour);
            var button = rect.gameObject.AddComponent<Button>();
            var colors = button.colors; colors.normalColor = Color.white; colors.highlightedColor = new Color(.88f, .98f, 1f, 1f); colors.pressedColor = new Color(.72f, .86f, .90f, 1f); colors.disabledColor = new Color(1f, 1f, 1f, .42f); button.colors = colors;
            MakeLabel("Label", rect, label, new Vector2(.04f, .08f), new Vector2(.96f, .92f), size, colour == Gold ? Ink : Color.white, TextAlignmentOptions.Center);
            return button;
        }

        private static TMP_InputField MakeNumberInput(RectTransform parent, Vector2 min, Vector2 max)
        {
            var rect = MakePanel("Quantity input", parent, min, max, Color.white);
            var input = rect.gameObject.AddComponent<TMP_InputField>(); input.contentType = TMP_InputField.ContentType.IntegerNumber;
            var text = MakeLabel("Text", rect, "1", new Vector2(.10f, .05f), new Vector2(.90f, .95f), 20, Ink, TextAlignmentOptions.Center);
            text.fontStyle = FontStyles.Normal;
            input.textComponent = text as TextMeshProUGUI;
            input.text = "1";
            return input;
        }

        private static Color ParseColour(string html, Color fallback)
        {
            Color parsed;
            return !string.IsNullOrEmpty(html) && ColorUtility.TryParseHtmlString(html, out parsed) ? parsed : fallback;
        }
    }
}

using System.Collections.Generic;
using UnityEngine;

public class ChemistryLabGame : MonoBehaviour
{
    enum GameScreen { Lobby, Lab }
    enum Panel { None, Shop, Inventory, Quests, Character }

    GameScreen screen = GameScreen.Lobby;
    Panel panel = Panel.None;
    readonly List<string> beaker = new List<string>();
    string selected = "NaCl";
    string message = "Chọn hóa chất để bắt đầu thí nghiệm.";
    int dollars = 1000, diamonds = 50, exp = 120;
    float toastUntil;
    GUIStyle title, text, button, cyanButton, greenButton, panelStyle, small;

    readonly string[] chemicals = { "NaCl", "H₂O", "HCl", "NaOH", "CuSO₄", "H₂SO₄", "KMnO₄", "BaCl₂" };

    void Awake()
    {
        Application.targetFrameRate = 60;
        Screen.orientation = ScreenOrientation.Portrait;
        BuildStyles();
    }

    void BuildStyles()
    {
        title = Style(40, new Color(0.12f, 0.16f, 0.23f), FontStyle.Bold, TextAnchor.MiddleCenter);
        text = Style(24, new Color(0.12f, 0.16f, 0.23f), FontStyle.Normal, TextAnchor.MiddleCenter);
        small = Style(18, new Color(0.12f, 0.16f, 0.23f), FontStyle.Bold, TextAnchor.MiddleCenter);
        button = Style(23, Color.white, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0.12f, 0.16f, 0.23f));
        cyanButton = Style(25, Color.white, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0f, 0.66f, 0.91f));
        greenButton = Style(21, Color.white, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0.18f, 0.8f, 0.44f));
        panelStyle = Style(22, new Color(0.12f, 0.16f, 0.23f), FontStyle.Normal, TextAnchor.UpperCenter, new Color(0.97f, 0.99f, 1f));
    }

    GUIStyle Style(int size, Color color, FontStyle font, TextAnchor align, Color? background = null)
    {
        var result = new GUIStyle(GUI.skin.label) { fontSize = size, fontStyle = font, alignment = align, normal = { textColor = color } };
        if (background.HasValue)
        {
            var tex = new Texture2D(1, 1); tex.SetPixel(0, 0, background.Value); tex.Apply();
            result.normal.background = tex; result.hover.background = tex; result.active.background = tex;
            result.border = new RectOffset(16, 16, 16, 16); result.padding = new RectOffset(10, 10, 7, 7);
        }
        return result;
    }

    void OnGUI()
    {
        float scale = Mathf.Min(Screen.width / 1080f, Screen.height / 1920f);
        GUI.matrix = Matrix4x4.TRS(new Vector3((Screen.width - 1080 * scale) * .5f, (Screen.height - 1920 * scale) * .5f, 0), Quaternion.identity, new Vector3(scale, scale, 1));
        if (screen == GameScreen.Lobby) Lobby(); else Lab();
        if (panel != Panel.None) Modal();
        if (Time.time < toastUntil) GUI.Label(new Rect(180, 1620, 720, 72), message, panelStyle);
    }

    void Rect(float x, float y, float w, float h, Color color) { var old = GUI.color; GUI.color = color; GUI.DrawTexture(new Rect(x, y, w, h), Texture2D.whiteTexture); GUI.color = old; }
    bool Button(Rect r, string label, GUIStyle style) => GUI.Button(r, label, style);

    void Lobby()
    {
        Rect(0, 0, 1080, 1920, new Color(.92f, .97f, 1));
        Rect(0, 670, 1080, 1250, new Color(.82f, .9f, .96f));
        // one point perspective floor
        for (int i = 0; i < 13; i++) { float x = i * 90; GUI.DrawTexture(new Rect(x, 670, 3, 1250), Texture2D.whiteTexture); }
        for (int y = 760; y < 1920; y += 145) Rect(0, y, 1080, 3, new Color(.45f, .56f, .67f));
        Rect(40, 35, 410, 92, Color.white); GUI.Label(new Rect(45, 40, 400, 80), "CHEMISTRY LAB", title);
        GUI.Label(new Rect(610, 45, 430, 54), "$ " + dollars + "     ♦ " + diamonds + "     ✦ " + exp, small);
        // sink
        Rect(330, 250, 420, 150, new Color(.62f, .72f, .82f)); Rect(375, 290, 330, 60, new Color(.12f, .16f, .23f));
        if (Button(new Rect(398, 405, 285, 55), "RỬA THIẾT BỊ", cyanButton)) Toast("Thiết bị đã được làm sạch.");
        Cabinet(25, 610, "TỦ HÓA CHẤT", new Color(.65f, .88f, 1)); Cabinet(825, 610, "TỦ DỤNG CỤ", new Color(.68f, .92f, 1));
        // scientist and bench
        GUI.Label(new Rect(380, 640, 320, 360), "👨‍🔬", Style(230, Color.white, FontStyle.Normal, TextAnchor.MiddleCenter));
        Rect(105, 1110, 870, 390, new Color(.97f, .98f, 1)); Rect(145, 1470, 80, 220, new Color(.12f, .16f, .23f)); Rect(855, 1470, 80, 220, new Color(.12f, .16f, .23f));
        Flask(250, 930, new Color(.65f, .25f, .95f), "BÌNH TÍM"); TestRack(465, 960); Flask(720, 930, new Color(0f, .82f, .92f), "BÌNH CYAN");
        string[] nav = { "SHOP", "KHO", "NHIỆM VỤ", "NHÂN VẬT" };
        for (int i = 0; i < nav.Length; i++) if (Button(new Rect(32 + i * 258, 1740, 232, 76), nav[i], button)) panel = (Panel)(i + 1);
        if (Button(new Rect(720, 1560, 310, 100), "VÀO LAB", cyanButton)) screen = GameScreen.Lab;
    }

    void Cabinet(float x, float y, string label, Color glass)
    {
        Rect(x, y, 230, 650, new Color(.12f, .16f, .23f)); Rect(x + 13, y + 45, 204, 570, glass); GUI.Label(new Rect(x, y + 4, 230, 42), label, small);
        for (int row = 0; row < 3; row++) { Rect(x + 16, y + 215 + row * 140, 198, 4, new Color(.12f, .16f, .23f)); for (int b = 0; b < 2; b++) { Color[] c = { new Color(.7f,.3f,.95f), new Color(.2f,.85f,.5f), new Color(1,.85f,.3f), new Color(.2f,.72f,1) }; Rect(x + 42 + b * 88, y + 95 + row * 140, 48, 82, c[(row * 2 + b) % c.Length]); } }
    }

    void Flask(float x, float y, Color liquid, string label) { Rect(x + 48, y, 44, 90, new Color(.85f,.96f,1)); Rect(x, y + 82, 140, 130, liquid); GUI.Label(new Rect(x - 20, y + 215, 180, 42), label, small); }
    void TestRack(float x, float y) { Rect(x, y + 145, 180, 48, new Color(1f,.72f,.05f)); Color[] c = { Color.red, Color.green, Color.yellow }; for (int i = 0; i < 3; i++) { Rect(x + 22 + 53 * i, y, 30, 150, new Color(.85f,.96f,1)); Rect(x + 24 + 53 * i, y + 60 + i * 13, 26, 80 - i * 13, c[i]); } }

    void Lab()
    {
        Rect(0, 0, 1080, 1920, new Color(.91f,.97f,1));
        if (Button(new Rect(35, 35, 220, 76), "← LOBBY", button)) screen = GameScreen.Lobby;
        GUI.Label(new Rect(300, 35, 480, 76), "PHÒNG THÍ NGHIỆM", title);
        if (Button(new Rect(820, 35, 220, 76), "RỬA", cyanButton)) { beaker.Clear(); Toast("Cốc và trạng thái phản ứng đã được đặt lại."); }
        Rect(35, 145, 300, 1500, Color.white); GUI.Label(new Rect(40, 160, 290, 48), "TỦ HÓA CHẤT", title);
        for (int i = 0; i < chemicals.Length; i++) if (Button(new Rect(55, 235 + i * 105, 260, 78), chemicals[i], selected == chemicals[i] ? cyanButton : button)) selected = chemicals[i];
        if (Button(new Rect(55, 1110, 260, 90), "ĐỔ VÀO CỐC", greenButton)) AddChemical();
        Rect(370, 145, 675, 1500, new Color(.98f,.99f,1)); GUI.Label(new Rect(380, 165, 650, 55), "BÀN THÍ NGHIỆM", title);
        Color liquid = beaker.Count == 0 ? new Color(.85f,.95f,1) : beaker.Contains("HCl") && beaker.Contains("NaOH") ? new Color(.4f,.8f,1) : beaker.Contains("CuSO₄") ? new Color(.2f,.6f,1) : new Color(.65f,.3f,.95f);
        Flask(615, 570, liquid, beaker.Count == 0 ? "Cốc rỗng" : string.Join(" + ", beaker.ToArray()));
        GUI.Label(new Rect(400, 1020, 620, 180), message, text);
        GUI.Label(new Rect(400, 1240, 620, 70), "Nhiệt độ: " + (beaker.Contains("HCl") && beaker.Contains("NaOH") ? "78°C" : "25°C"), title);
    }

    void AddChemical()
    {
        if (beaker.Contains(selected)) { Toast("Hóa chất này đã có trong cốc."); return; }
        beaker.Add(selected);
        if (beaker.Contains("HCl") && beaker.Contains("NaOH")) { dollars += 60; exp += 10; Toast("Trung hòa tỏa nhiệt thành công! +60$ +10 EXP"); }
        else if (beaker.Contains("BaCl₂") && beaker.Contains("H₂SO₄")) Toast("Kết tủa trắng BaSO₄ xuất hiện.");
        else if (beaker.Contains("HCl") && beaker.Contains("KMnO₄")) { dollars = Mathf.Max(0, dollars - 150); Toast("Sự cố khí clo! -150$"); }
        else Toast("Đã thêm " + selected + ".");
    }

    void Modal()
    {
        Rect(70, 330, 940, 900, new Color(.96f,.99f,1));
        string heading = panel == Panel.Shop ? "SHOP" : panel == Panel.Inventory ? "KHO HÓA CHẤT" : panel == Panel.Quests ? "NHIỆM VỤ" : "NHÂN VẬT";
        GUI.Label(new Rect(100, 360, 880, 80), heading, title);
        string body = panel == Panel.Shop ? "Mua hóa chất và máy móc bằng tiền hoặc kim cương." : panel == Panel.Inventory ? "NaCl · H₂O · HCl · NaOH · CuSO₄ · H₂SO₄ · KMnO₄ · BaCl₂" : panel == Panel.Quests ? "Hoàn thành phản ứng an toàn để nhận EXP và phần thưởng." : "Nhà khoa học chibi · áo blouse · kính cyan · găng tay xanh.";
        GUI.Label(new Rect(145, 520, 790, 300), body, text);
        if (Button(new Rect(340, 1060, 400, 86), "ĐÓNG", cyanButton)) panel = Panel.None;
    }

    void Toast(string value) { message = value; toastUntil = Time.time + 2.5f; }
}

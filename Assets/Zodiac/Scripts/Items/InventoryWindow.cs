using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Cửa sổ hành trang (phím I hoặc Tab, nút Select tay cầm) — mở thì game tạm dừng:
///   • Trái: chân dung, 5 ô trang bị (Vũ khí · Giáp · Bùa · Ngọc · Ngọc) và bảng chỉ số hiện tại.
///   • Giữa: túi đồ 6 × 5 ô (viền màu theo độ hiếm, số lượng ở góc, số 1/2 nếu đã gán phím dùng nhanh).
///   • Phải: thông tin món đang chọn + nút Trang bị / Tháo · Dùng · Gán phím 1 · Gán phím 2 · Vứt; nút Xếp gọn túi.
/// Bấm chuột vào ô để chọn. Trang <b>Tu luyện</b>: cấp, XP, cộng điểm 4 thuộc tính, luyện Huyết / Khí Đan
/// (InventoryWindow.Progression.cs). Luôn hiện thanh dùng nhanh 2 ô, cấp + thanh XP và tiền ở góc dưới trái.
/// Toàn bộ giao diện dựng bằng code khi vào màn (không phụ thuộc scene).
/// </summary>
public partial class InventoryWindow : MonoBehaviour
{
    const float SlotSize = 96f, SlotGap = 10f;
    static readonly Color PanelColor = new Color(0.11f, 0.08f, 0.06f, 0.96f);
    static readonly Color SlotColor = new Color(0.22f, 0.17f, 0.13f, 0.95f);
    static readonly Color Gold = new Color(0.95f, 0.78f, 0.35f);
    static readonly Color Ink = new Color(0.95f, 0.9f, 0.8f);

    class Slot
    {
        public Image frame, icon;
        public Text count, key;
    }

    public static bool IsOpen { get; private set; }
    static InventoryWindow instance;

    /// <summary>Mở hành trang ở trang Tu luyện (Bàn thờ tổ trong làng).</summary>
    public static void OpenTraining()
    {
        if (instance == null || IsOpen) return;
        instance.Open();
        if (IsOpen) instance.ShowPage(true);
    }
    /// <summary>Frame vừa đóng cửa sổ (Esc đóng hành trang thì không mở luôn menu tạm dừng cùng frame).</summary>
    public static int ClosedFrame { get; private set; } = -1;

    Font font;
    GameObject window, bagPage, trainPage;
    readonly List<Slot> bagSlots = new List<Slot>();
    readonly List<Slot> equipSlots = new List<Slot>();
    readonly List<Slot> quickSlots = new List<Slot>();
    Image portrait;
    Text nameText, statsText, detailText, coinsText;
    Button equipButton, useButton, quick1Button, quick2Button, dropButton;
    Text equipLabel;
    bool selectedEquip;
    int selected = -1;
    bool disabledPlayer;

    void Awake()
    {
        instance = this;
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        BuildUi();
        window.SetActive(false);
    }

    void OnEnable() => PlayerProfile.Changed += Refresh;

    void OnDisable()
    {
        PlayerProfile.Changed -= Refresh;
        if (IsOpen) Close();
    }

    void Start() => Refresh();

    void Update()
    {
        var kb = Keyboard.current;
        bool toggle = kb != null && (kb.iKey.wasPressedThisFrame || kb.tabKey.wasPressedThisFrame);
        if (Gamepad.current != null && Gamepad.current.selectButton.wasPressedThisFrame) toggle = true;
        bool escape = kb != null && kb.escapeKey.wasPressedThisFrame;
        if (IsOpen && (toggle || escape)) Close();
        else if (!IsOpen && toggle && !PauseMenu.IsOpen && !VillageWindow.IsOpen) Open();
    }

    void Open()
    {
        var gm = ZodiacGameManager.Instance;
        if (gm != null && gm.Current != ZodiacGameManager.State.Playing) return;
        IsOpen = true;
        window.SetActive(true);
        Time.timeScale = 0f;
        var player = ZodiacPlayerController.Current;
        disabledPlayer = player != null && player.ControlEnabled;
        if (disabledPlayer) player.SetControlEnabled(false);
        selected = -1;
        Refresh();
    }

    void Close()
    {
        IsOpen = false;
        ClosedFrame = Time.frameCount;
        window.SetActive(false);
        Time.timeScale = 1f;
        var player = ZodiacPlayerController.Current;
        if (disabledPlayer && player != null) player.SetControlEnabled(true);
        disabledPlayer = false;
    }

    // ───────── Hiển thị ─────────

    void Refresh()
    {
        var profile = PlayerProfile.Current;
        for (int i = 0; i < bagSlots.Count; i++) Fill(bagSlots[i], profile.bag[i], !selectedEquip && selected == i, QuickKey(profile, profile.bag[i]));
        for (int i = 0; i < equipSlots.Count; i++) Fill(equipSlots[i], profile.equipped[i], selectedEquip && selected == i, "");
        for (int i = 0; i < quickSlots.Count; i++)
        {
            string id = profile.quick[i];
            var def = ItemDatabase.Get(id);
            var s = quickSlots[i];
            s.icon.enabled = def != null && def.icon != null;
            if (s.icon.enabled) s.icon.sprite = def.icon;
            int n = def != null ? profile.CountOf(id) : 0;
            s.count.text = def != null ? n.ToString() : "";
            s.icon.color = n > 0 ? Color.white : new Color(1f, 1f, 1f, 0.3f);
        }
        if (coinsText != null) coinsText.text = $"Đồng: {profile.coins}";
        RefreshLevelHud(profile);
        if (!IsOpen) return;
        RefreshTraining(profile);

        var player = ZodiacPlayerController.Current;
        if (player != null && player.Character != null)
        {
            portrait.sprite = player.Character.sprite;
            nameText.text = player.Character.Title;
        }
        var stats = player != null ? player.GetComponent<CharacterStats>() : null;
        if (stats != null)
            statsText.text =
                $"Máu tối đa   <b>{stats.MaxHealth}</b>\n" +
                $"Khí tối đa   <b>{stats.MaxEnergy}</b>   (hồi {stats.Get(StatType.EnergyRegen):0.#}/giây)\n" +
                $"Công         <b>{stats.Attack}</b>\n" +
                $"Giáp         <b>{stats.Defense}</b>\n" +
                $"Chí mạng     <b>{stats.Get(StatType.CritChance) * 100f:0}%</b>   (×{stats.Get(StatType.CritDamage):0.##})";

        var stack = SelectedStack(profile);
        var item = stack?.Def;
        detailText.text = item != null ? item.Tooltip(stack) : "<i>Chọn một món đồ để xem.</i>";
        bool equipment = item != null && item.type == ItemType.Equipment;
        bool consumable = item != null && item.type == ItemType.Consumable;
        equipButton.gameObject.SetActive(equipment);
        equipLabel.text = selectedEquip ? "Tháo ra" : "Trang bị";
        useButton.gameObject.SetActive(consumable && !selectedEquip);
        quick1Button.gameObject.SetActive(consumable && !selectedEquip);
        quick2Button.gameObject.SetActive(consumable && !selectedEquip);
        dropButton.gameObject.SetActive(item != null && !selectedEquip);
    }

    static string QuickKey(PlayerProfile p, ItemStack s)
    {
        if (s.IsEmpty) return "";
        for (int i = 0; i < p.quick.Length; i++) if (p.quick[i] == s.id) return (i + 1).ToString();
        return "";
    }

    void Fill(Slot slot, ItemStack stack, bool isSelected, string key)
    {
        var def = stack.Def;
        slot.icon.enabled = def != null;
        if (def != null) slot.icon.sprite = def.icon;
        slot.count.text = def != null && stack.count > 1 ? stack.count.ToString() : def != null && stack.level > 0 ? $"+{stack.level}" : "";
        slot.key.text = key;
        Color border = def != null ? ItemDefinition.RarityColor(def.rarity) : new Color(0.4f, 0.32f, 0.25f);
        slot.frame.color = isSelected ? Color.white : border;
    }

    ItemStack SelectedStack(PlayerProfile p)
    {
        if (selected < 0) return null;
        return selectedEquip ? p.equipped[selected] : p.bag[selected];
    }

    // ───────── Hành động ─────────

    void Select(bool equip, int index)
    {
        selectedEquip = equip;
        selected = index;
        var res = ZodiacResources.Get;
        if (res != null && res.coin != null) ZodiacAudio.Play(res.swing, 0.3f, 0.1f);
        Refresh();
    }

    void EquipOrUnequip()
    {
        var p = PlayerProfile.Current;
        if (selectedEquip)
        {
            if (!p.Unequip(selected)) PickupToast.Show("Túi đầy — không tháo được", new Color(1f, 0.5f, 0.4f));
            selected = -1;
        }
        else if (p.Equip(selected))
        {
            var res = ZodiacResources.Get;
            if (res != null) ZodiacAudio.Play(res.block, 0.5f, 0.05f);
            if (p.bag[selected].IsEmpty) selected = -1;
        }
        Refresh();
    }

    void UseSelected()
    {
        var player = ZodiacPlayerController.Current;
        if (player == null) return;
        if (!PlayerProfile.Current.UseAt(selected, player.GetComponent<Health>(), player.GetComponent<PlayerEnergy>()))
            PickupToast.Show("Chưa cần dùng", Ink);
        if (PlayerProfile.Current.bag[selected].IsEmpty) selected = -1;
        Refresh();
    }

    void AssignQuick(int index)
    {
        var s = SelectedStack(PlayerProfile.Current);
        if (s == null || s.IsEmpty) return;
        PlayerProfile.Current.AssignQuick(index, s.id);
    }

    void DropSelected()
    {
        var p = PlayerProfile.Current;
        var s = p.bag[selected];
        if (s.IsEmpty) return;
        var player = ZodiacPlayerController.Current;
        if (player != null) LootPickup.Spawn(s.Clone(), 0, player.transform.position + Vector3.right * player.FacingDirection * 1.5f);
        p.RemoveAt(selected, s.count);
        selected = -1;
        Refresh();
    }

    // ───────── Dựng giao diện ─────────

    void BuildUi()
    {
        var canvasGo = new GameObject("InventoryCanvas", typeof(RectTransform));
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();
        var root = canvasGo.transform;

        // Thanh dùng nhanh (luôn hiện) + tiền
        for (int i = 0; i < PlayerProfile.QuickSlots; i++)
        {
            var s = MakeSlot(root, new Vector2(0f, 0f), new Vector2(70f + i * 92f, 190f), 80f);
            s.key.text = (i + 1).ToString();
            s.frame.color = Gold;
            quickSlots.Add(s);
        }
        coinsText = MakeText(root, "Coins", "", 30, Gold, new Vector2(0f, 0f), new Vector2(30f, 130f), new Vector2(300f, 40f), TextAnchor.MiddleLeft);
        MakeText(root, "BagHint", "I / Tab: hành trang · Esc: tạm dừng", 20, Ink, new Vector2(0f, 0f), new Vector2(30f, 100f), new Vector2(400f, 30f), TextAnchor.MiddleLeft);
        BuildLevelHud(root);

        window = new GameObject("Window", typeof(RectTransform));
        window.transform.SetParent(root, false);
        Stretch((RectTransform)window.transform);
        var dim = window.AddComponent<Image>();
        dim.color = new Color(0f, 0f, 0f, 0.55f);

        var panel = MakeImage(window.transform, "Panel", PanelColor);
        Place(panel.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1560f, 860f));
        panel.gameObject.AddComponent<Outline>().effectColor = Gold;
        var panelRoot = panel.transform;
        MakeButton(panelRoot, "Hành trang", new Vector2(0.5f, 1f), new Vector2(-160f, -45f), new Vector2(280f, 60f), () => ShowPage(false));
        MakeButton(panelRoot, "Tu luyện", new Vector2(0.5f, 1f), new Vector2(160f, -45f), new Vector2(280f, 60f), () => ShowPage(true));
        bagPage = new GameObject("BagPage", typeof(RectTransform));
        bagPage.transform.SetParent(panelRoot, false);
        Stretch((RectTransform)bagPage.transform);
        trainPage = new GameObject("TrainPage", typeof(RectTransform));
        trainPage.transform.SetParent(panelRoot, false);
        Stretch((RectTransform)trainPage.transform);
        BuildTraining(trainPage.transform);
        trainPage.SetActive(false);
        var p = bagPage.transform;

        // Trái: chân dung, trang bị, chỉ số
        portrait = MakeImage(p, "Portrait", Color.white);
        portrait.preserveAspect = true;
        // Cột trái (x 40 → 500, lưới túi bắt đầu từ 540): chân dung → tên → 2 hàng ô trang bị → chỉ số
        Place(portrait.rectTransform, new Vector2(0f, 1f), new Vector2(270f, -195f), new Vector2(190f, 190f));
        nameText = MakeText(p, "Name", "", 26, Ink, new Vector2(0f, 1f), new Vector2(270f, -312f), new Vector2(440f, 36f), TextAnchor.MiddleCenter);
        for (int i = 0; i < PlayerProfile.EquipSlots.Length; i++)
        {
            int index = i;
            Vector2 pos = i < 3 ? new Vector2(150f + i * 120f, -400f) : new Vector2(210f + (i - 3) * 120f, -530f);
            var s = MakeSlot(p, new Vector2(0f, 1f), pos, SlotSize, () => Select(true, index));
            MakeText(s.frame.transform, "Label", ItemDefinition.SlotName(PlayerProfile.EquipSlots[i]), 18, Ink,
                     new Vector2(0.5f, 0f), new Vector2(0f, -16f), new Vector2(120f, 24f), TextAnchor.MiddleCenter);
            equipSlots.Add(s);
        }
        statsText = MakeText(p, "Stats", "", 24, Ink, new Vector2(0f, 1f), new Vector2(270f, -705f), new Vector2(440f, 170f), TextAnchor.UpperLeft);

        // Giữa: túi 6 × 5
        float gridLeft = 540f, gridTop = -120f;
        for (int i = 0; i < PlayerProfile.BagSize; i++)
        {
            int index = i;
            Vector2 pos = new Vector2(gridLeft + SlotSize / 2f + (i % 6) * (SlotSize + SlotGap), gridTop - SlotSize / 2f - (i / 6) * (SlotSize + SlotGap));
            bagSlots.Add(MakeSlot(p, new Vector2(0f, 1f), pos, SlotSize, () => Select(false, index)));
        }
        var sort = MakeButton(p, "Xếp gọn", new Vector2(0f, 1f), new Vector2(gridLeft + 90f, -705f), new Vector2(180f, 54f), () => PlayerProfile.Current.Sort());
        sort.name = "SortButton";

        // Phải: chi tiết + nút
        float right = 1230f;
        detailText = MakeText(p, "Detail", "", 26, Ink, new Vector2(0f, 1f), new Vector2(right + 150f, -260f), new Vector2(300f, 300f), TextAnchor.UpperLeft);
        detailText.horizontalOverflow = HorizontalWrapMode.Wrap;
        detailText.verticalOverflow = VerticalWrapMode.Overflow;
        equipButton = MakeButton(p, "Trang bị", new Vector2(0f, 1f), new Vector2(right + 150f, -450f), new Vector2(280f, 54f), EquipOrUnequip);
        equipLabel = equipButton.GetComponentInChildren<Text>();
        useButton = MakeButton(p, "Dùng", new Vector2(0f, 1f), new Vector2(right + 150f, -515f), new Vector2(280f, 54f), UseSelected);
        quick1Button = MakeButton(p, "Gán phím 1", new Vector2(0f, 1f), new Vector2(right + 80f, -580f), new Vector2(135f, 54f), () => AssignQuick(0));
        quick2Button = MakeButton(p, "Gán phím 2", new Vector2(0f, 1f), new Vector2(right + 220f, -580f), new Vector2(135f, 54f), () => AssignQuick(1));
        dropButton = MakeButton(p, "Vứt", new Vector2(0f, 1f), new Vector2(right + 150f, -645f), new Vector2(280f, 54f), DropSelected);

        MakeText(p, "Hint", "Bấm chuột vào ô để chọn · I / Tab / Esc: đóng", 22, new Color(0.8f, 0.72f, 0.6f),
                 new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(1200f, 34f), TextAnchor.MiddleCenter);
    }

    Slot MakeSlot(Transform parent, Vector2 anchor, Vector2 pos, float size, UnityEngine.Events.UnityAction onClick = null)
    {
        var frame = MakeImage(parent, "Slot", new Color(0.4f, 0.32f, 0.25f));
        Place(frame.rectTransform, anchor, pos, new Vector2(size, size));
        var back = MakeImage(frame.transform, "Back", SlotColor);
        Stretch(back.rectTransform);
        back.rectTransform.offsetMin = new Vector2(4f, 4f);
        back.rectTransform.offsetMax = new Vector2(-4f, -4f);
        back.raycastTarget = false;
        var icon = MakeImage(frame.transform, "Icon", Color.white);
        icon.preserveAspect = true;
        icon.raycastTarget = false;
        Stretch(icon.rectTransform);
        icon.rectTransform.offsetMin = new Vector2(10f, 10f);
        icon.rectTransform.offsetMax = new Vector2(-10f, -10f);
        icon.enabled = false;
        var count = MakeText(frame.transform, "Count", "", 22, Color.white, new Vector2(1f, 0f), new Vector2(-8f, 6f), new Vector2(60f, 28f), TextAnchor.LowerRight);
        count.rectTransform.pivot = new Vector2(1f, 0f);
        var key = MakeText(frame.transform, "Key", "", 20, Gold, new Vector2(0f, 1f), new Vector2(8f, -6f), new Vector2(30f, 26f), TextAnchor.UpperLeft);
        key.rectTransform.pivot = new Vector2(0f, 1f);
        if (onClick != null)
        {
            var b = frame.gameObject.AddComponent<Button>();
            b.targetGraphic = frame;
            b.onClick.AddListener(onClick);
        }
        else frame.raycastTarget = false;
        return new Slot { frame = frame, icon = icon, count = count, key = key };
    }

    Button MakeButton(Transform parent, string label, Vector2 anchor, Vector2 pos, Vector2 size, UnityEngine.Events.UnityAction onClick)
    {
        var img = MakeImage(parent, "Button " + label, new Color(0.7f, 0.15f, 0.12f));
        Place(img.rectTransform, anchor, pos, size);
        var b = img.gameObject.AddComponent<Button>();
        b.targetGraphic = img;
        var colors = b.colors;
        colors.highlightedColor = new Color(1f, 0.85f, 0.6f);
        b.colors = colors;
        b.onClick.AddListener(onClick);
        var t = MakeText(img.transform, "Label", label, 24, Color.white, new Vector2(0.5f, 0.5f), Vector2.zero, size, TextAnchor.MiddleCenter);
        Stretch(t.rectTransform);
        return b;
    }

    Image MakeImage(Transform parent, string name, Color color)
    {
        var img = new GameObject(name, typeof(RectTransform)).AddComponent<Image>();
        img.transform.SetParent(parent, false);
        img.color = color;
        return img;
    }

    Text MakeText(Transform parent, string name, string content, int size, Color color, Vector2 anchor, Vector2 pos, Vector2 box, TextAnchor align)
    {
        var t = new GameObject(name, typeof(RectTransform)).AddComponent<Text>();
        t.transform.SetParent(parent, false);
        t.font = font;
        t.text = content;
        t.fontSize = size;
        t.color = color;
        t.alignment = align;
        t.supportRichText = true;
        t.raycastTarget = false;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.gameObject.AddComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.6f);
        Place(t.rectTransform, anchor, pos, box);
        return t;
    }

    static void Place(RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = rt.anchorMax = anchor;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = Vector2.zero;
    }
}

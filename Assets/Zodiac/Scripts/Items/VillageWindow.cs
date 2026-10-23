using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Cửa sổ các điểm trong Làng Trung Tâm (mở bằng <see cref="VillageStation"/>), game tạm dừng khi mở:
///   • <b>Lò rèn</b>: chọn món trang bị (đang mặc hoặc trong túi) → xem tỉ lệ, giá → Đập (+1), tuỳ chọn Bùa bảo hộ /
///     Ngọc may mắn; Phân giải đồ thừa ra nguyên liệu (<see cref="Forge"/>).
///   • <b>Tiệm thuốc</b>: Mua · Bán · Luyện đan (<see cref="Shop"/>).
///   • <b>Cổng làng</b>: chọn màn đã mở để lên đường.
/// Nội dung dựng lại mỗi lần thao tác (ít phần tử, chỉ chạy khi bấm) — giữ nguyên ô đang chọn cho tay cầm.
/// Esc / phím Tương tác (F) / B tay cầm: đóng.
/// </summary>
public class VillageWindow : MonoBehaviour
{
    public enum Mode { Forge, Shop, Gate }
    enum ShopTab { Buy, Accessory, Sell, Craft }

    public static bool IsOpen { get; private set; }
    public static int ClosedFrame { get; private set; } = -1;
    static VillageWindow instance;

    [Tooltip("Scene các màn theo thứ tự (builder điền) — cho Cổng làng.")]
    public string[] levelScenes = System.Array.Empty<string>();
    public string[] levelTitles = System.Array.Empty<string>();

    Transform canvas;
    GameObject window;
    Transform panel, content;
    Text titleText, coinsText;
    Mode mode;
    ShopTab shopTab;
    bool selectedEquipped;
    int selected = -1;
    bool useCharm, useLuck;
    string resultText = "";
    int openedFrame;
    bool hammering;

    void Awake()
    {
        instance = this;
        canvas = ZodiacUi.Canvas(transform, "VillageCanvas", 520);
        (window, panel) = ZodiacUi.Window(canvas, "", new Vector2(1560f, 860f));
        titleText = panel.GetComponentInChildren<Text>();
        coinsText = ZodiacUi.MakeText(panel, "", 30, ZodiacUi.Gold, new Vector2(1f, 1f), new Vector2(-170f, -45f), new Vector2(300f, 50f), TextAnchor.MiddleRight);
        ZodiacUi.MakeButton(panel, "Đóng", new Vector2(0f, 1f), new Vector2(110f, -45f), new Vector2(160f, 56f), Close);
        var c = new GameObject("Content", typeof(RectTransform));
        c.transform.SetParent(panel, false);
        ZodiacUi.Stretch((RectTransform)c.transform);
        content = c.transform;
        window.SetActive(false);
    }

    void OnDestroy()
    {
        if (instance == this) instance = null;
        if (IsOpen) Time.timeScale = 1f;
        IsOpen = false;
    }

    public static void Open(Mode m)
    {
        if (instance == null || IsOpen || InventoryWindow.IsOpen || PauseMenu.IsOpen) return;
        var w = instance;
        w.mode = m;
        w.shopTab = ShopTab.Buy;
        w.selected = -1;
        w.resultText = "";
        w.useCharm = w.useLuck = false;
        IsOpen = true;
        w.openedFrame = Time.frameCount;
        w.window.SetActive(true);
        Time.timeScale = 0f;
        ZodiacPlayerController.Current?.SetControlEnabled(false);
        w.Refresh();
    }

    void Close()
    {
        if (!IsOpen) return;
        IsOpen = false;
        ClosedFrame = Time.frameCount;
        window.SetActive(false);
        Time.timeScale = 1f;
        ZodiacPlayerController.Current?.SetControlEnabled(true);
    }

    void Update()
    {
        if (!IsOpen || Time.frameCount == openedFrame) return;
        var kb = Keyboard.current;
        var interact = ZodiacInput.Find("Interact");
        if ((kb != null && kb.escapeKey.wasPressedThisFrame) || (interact != null && interact.WasPressedThisFrame()) ||
            (Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame)) Close();
    }

    void Refresh()
    {
        int keep = Time.frameCount == openedFrame ? -1 : ZodiacUi.SelectedIndex(content);
        foreach (Transform child in content)
        {
            child.gameObject.SetActive(false);
            Destroy(child.gameObject);
        }
        Build();
        ZodiacUi.Reselect(content, keep);
    }

    void Build()
    {
        var p = PlayerProfile.Current;
        coinsText.text = $"Đồng: {p.coins}";
        switch (mode)
        {
            case Mode.Forge: titleText.text = "LÒ RÈN"; BuildForge(p); break;
            case Mode.Shop: titleText.text = "TIỆM THUỐC BẮC"; BuildShop(p); break;
            default: titleText.text = "CỔNG LÀNG — LÊN ĐƯỜNG"; BuildGate(p); break;
        }
    }

    void Sound(AudioClip[] clip, float volume = 0.7f)
    {
        if (clip != null) ZodiacAudio.Play(clip, volume, 0.05f);
    }

    /// <summary>Phát clip mới; chưa dựng lại tài nguyên (mảng trống) thì dùng clip cũ thay thế.</summary>
    void Sound(AudioClip[] clip, AudioClip[] fallback, float volume = 0.7f) =>
        Sound(clip != null && clip.Length > 0 ? clip : fallback, volume);

    // ───────── Lò rèn ─────────

    void BuildForge(PlayerProfile p)
    {
        ZodiacUi.MakeText(content, "Đang mặc", 24, ZodiacUi.Muted, new Vector2(0f, 1f), new Vector2(330f, -110f), new Vector2(560f, 30f));
        for (int i = 0; i < p.equipped.Length; i++)
        {
            int index = i;
            var slot = ZodiacUi.MakeSlot(content, new Vector2(0f, 1f), new Vector2(90f + i * 110f, -175f), 96f, () => Select(true, index));
            ZodiacUi.Fill(slot, p.equipped[i], selectedEquipped && selected == i);
        }
        ZodiacUi.MakeText(content, "Trang bị trong túi", 24, ZodiacUi.Muted, new Vector2(0f, 1f), new Vector2(330f, -255f), new Vector2(560f, 30f));
        int shown = 0;
        for (int i = 0; i < p.bag.Length && shown < 24; i++)
        {
            var def = p.bag[i].Def;
            if (def == null || def.type != ItemType.Equipment) continue;
            int index = i;
            var slot = ZodiacUi.MakeSlot(content, new Vector2(0f, 1f), new Vector2(90f + (shown % 6) * 110f, -320f - (shown / 6) * 110f), 96f,
                                         () => Select(false, index));
            ZodiacUi.Fill(slot, p.bag[i], !selectedEquipped && selected == i);
            shown++;
        }
        if (shown == 0)
            ZodiacUi.MakeText(content, "<i>Túi chưa có đồ trang bị nào.</i>", 22, ZodiacUi.Muted, new Vector2(0f, 1f), new Vector2(330f, -330f), new Vector2(560f, 40f));

        var s = selected < 0 ? null : selectedEquipped ? p.equipped[selected] : p.bag[selected];
        if (s == null || s.IsEmpty || s.Def.type != ItemType.Equipment)
        {
            ZodiacUi.MakeText(content, "Chọn một món trang bị để đập.\n\n" +
                              "+1…+3 chắc chắn thành công. Từ +6, đập hỏng tụt 1 cấp — dùng Bùa bảo hộ để giữ cấp.\n" +
                              "Mỗi lần hỏng liên tiếp, lần sau dễ thành hơn.",
                              24, ZodiacUi.Ink, new Vector2(0f, 1f), new Vector2(1130f, -300f), new Vector2(680f, 300f), TextAnchor.UpperLeft);
            Result();
            return;
        }

        var def2 = s.Def;
        ZodiacUi.MakeText(content, def2.Tooltip(s), 24, ZodiacUi.Ink, new Vector2(0f, 1f), new Vector2(980f, -250f), new Vector2(380f, 300f), TextAnchor.UpperLeft);
        if (s.level >= Forge.MaxLevel)
        {
            ZodiacUi.MakeText(content, "<b>Đã đạt +10 — tối đa.</b>", 30, ZodiacUi.Gold, new Vector2(0f, 1f), new Vector2(1340f, -250f), new Vector2(340f, 60f));
        }
        else
        {
            var cost = Forge.CostOf(s);
            float chance = Forge.Chance(s) + (useLuck ? 0.1f : 0f);
            string fail = Forge.DropsOnFail(s) ? "<color=#ff8a7a>hỏng tụt 1 cấp</color>" : "hỏng giữ nguyên cấp";
            ZodiacUi.MakeText(content,
                $"Lên <b>+{s.level + 1}</b>: tỉ lệ <b>{Mathf.Clamp01(chance) * 100f:0}%</b>" + (s.failStreak > 0 ? $" <size=20>(+{s.failStreak * 5}% do hỏng trước)</size>" : "") +
                $"\n{fail}\n\nTốn: <color={(p.coins >= cost.coins ? "#9fe39a" : "#ff8a7a")}>{cost.coins} đồng</color>\n" + Shop.Describe(cost.materials, p),
                24, ZodiacUi.Ink, new Vector2(0f, 1f), new Vector2(1340f, -260f), new Vector2(360f, 260f), TextAnchor.UpperLeft);

            int charms = p.CountOf(Forge.CharmId), lucks = p.CountOf(Forge.LuckId);
            if (Forge.DropsOnFail(s))
            {
                var bc = ZodiacUi.MakeButton(content, $"Bùa bảo hộ: {(useCharm ? "DÙNG" : "không")} (còn {charms})", new Vector2(0f, 1f),
                                             new Vector2(1160f, -560f), new Vector2(500f, 50f), () => { useCharm = !useCharm; Refresh(); });
                bc.interactable = charms > 0;
            }
            var bl = ZodiacUi.MakeButton(content, $"Ngọc may mắn: {(useLuck ? "DÙNG (+10%)" : "không")} (còn {lucks})", new Vector2(0f, 1f),
                                         new Vector2(1160f, -620f), new Vector2(500f, 50f), () => { useLuck = !useLuck; Refresh(); });
            bl.interactable = lucks > 0;
            var hammer = ZodiacUi.MakeButton(content, "ĐẬP!", new Vector2(0f, 1f), new Vector2(1060f, -700f), new Vector2(300f, 70f), DoUpgrade);
            hammer.interactable = Forge.CanAfford(p, cost) && !hammering;
        }
        if (!selectedEquipped)
        {
            var (id, count, coins) = Forge.SalvageOf(s);
            var salvage = ZodiacUi.MakeButton(content, $"Phân giải (+{count} {ItemDatabase.Get(id)?.displayName}, +{coins} đồng)", new Vector2(0f, 1f),
                                              new Vector2(1160f, -780f), new Vector2(560f, 50f), DoSalvage);
            salvage.GetComponent<Image>().color = new Color(0.35f, 0.3f, 0.25f);
        }
        Result();
    }

    void Result()
    {
        if (string.IsNullOrEmpty(resultText)) return;
        ZodiacUi.MakeText(content, resultText, 40, Color.white, new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(1400f, 60f));
    }

    void Select(bool equipped, int index)
    {
        if (hammering) return;   // đang gõ búa thì không đổi món
        selectedEquipped = equipped;
        selected = index;
        resultText = "";
        Refresh();
    }

    void DoUpgrade()
    {
        if (!hammering) StartCoroutine(Hammer());
    }

    /// <summary>Ba nhát búa gõ đe (game đang tạm dừng nên đợi theo giờ thật) rồi mới ra kết quả — cho hồi hộp.</summary>
    IEnumerator Hammer()
    {
        hammering = true;
        resultText = "<color=#ffcf8a>Keng…</color>";
        Refresh();
        var res = ZodiacResources.Get;
        for (int i = 0; i < 3; i++)
        {
            if (res != null) Sound(res.anvil, res.block, 0.55f + 0.15f * i);
            GameFeel.Shake(0.05f + 0.03f * i);
            yield return new WaitForSecondsRealtime(0.32f);
            if (!IsOpen)
            {
                hammering = false;
                yield break;
            }
            if (i < 2)
            {
                resultText = i == 0 ? "<color=#ffcf8a>Keng… keng…</color>" : "<color=#ffcf8a>Keng… keng… KENG!</color>";
                Refresh();
            }
        }
        hammering = false;
        ApplyUpgrade();
    }

    void ApplyUpgrade()
    {
        var p = PlayerProfile.Current;
        var res = ZodiacResources.Get;
        var result = Forge.Upgrade(p, selectedEquipped, selected, useCharm, useLuck);
        var s = selectedEquipped ? p.equipped[selected] : p.bag[selected];
        useCharm = useLuck = false;
        switch (result)
        {
            case Forge.Result.Success:
                resultText = $"<color=#ffd84d>THÀNH CÔNG!  {s.DisplayName}</color>";
                if (res != null) { Sound(res.forgeSuccess, res.win, 0.9f); Sound(res.block, 0.5f); }
                GameFeel.Shake(0.25f);
                break;
            case Forge.Result.FailKeep:
                resultText = "<color=#c8b8a8>Hỏng rồi… giữ nguyên cấp. Lần sau dễ thành hơn.</color>";
                if (res != null) Sound(res.hurt);
                break;
            case Forge.Result.Protected:
                resultText = "<color=#9fd3ff>Hỏng — Bùa bảo hộ giữ được cấp!</color>";
                if (res != null) { Sound(res.hurt); Sound(res.lantern, 0.6f); }
                break;
            case Forge.Result.FailDrop:
                resultText = $"<color=#ff8a7a>Hỏng! Tụt xuống {s.DisplayName}</color>";
                if (res != null) Sound(res.forgeBreak, res.lose, 0.8f);
                GameFeel.Shake(0.2f);
                break;
            case Forge.Result.NotEnough:
                resultText = "<color=#ff8a7a>Không đủ đồng hoặc nguyên liệu.</color>";
                break;
        }
        Refresh();
    }

    void DoSalvage()
    {
        if (selectedEquipped || selected < 0) return;
        if (Forge.Salvage(PlayerProfile.Current, selected))
        {
            resultText = "<color=#c8b8a8>Đã phân giải.</color>";
            var res = ZodiacResources.Get;
            if (res != null) Sound(res.forgeBreak, res.enemyDeath, 0.5f);
        }
        selected = -1;
        Refresh();
    }

    // ───────── Tiệm thuốc ─────────

    void BuildShop(PlayerProfile p)
    {
        string[] tabs = { "Thuốc", "Phụ kiện", "Bán", "Luyện đan" };
        for (int i = 0; i < tabs.Length; i++)
        {
            var tab = (ShopTab)i;
            var b = ZodiacUi.MakeButton(content, tabs[i], new Vector2(0.5f, 1f), new Vector2(-390f + i * 260f, -115f), new Vector2(240f, 54f),
                                        () => { shopTab = tab; selected = -1; resultText = ""; Refresh(); });
            if (tab == shopTab) b.GetComponent<Image>().color = new Color(0.9f, 0.6f, 0.15f);
        }
        switch (shopTab)
        {
            case ShopTab.Buy: BuildBuy(p); break;
            case ShopTab.Accessory: BuildAccessories(p); break;
            case ShopTab.Sell: BuildSell(p); break;
            default: BuildCraft(p); break;
        }
        Result();
    }

    void BuildBuy(PlayerProfile p)
    {
        for (int i = 0; i < Shop.Stock.Length; i++)
        {
            var (id, price) = Shop.Stock[i];
            var def = ItemDatabase.Get(id);
            if (def == null) continue;
            float y = -220f - i * 100f;
            var slot = ZodiacUi.MakeSlot(content, new Vector2(0f, 1f), new Vector2(260f, y), 84f, null);
            ZodiacUi.Fill(slot, new ItemStack { id = id, count = p.CountOf(id) }, false);
            ZodiacUi.MakeText(content, $"<b>{def.displayName}</b>\n<size=20>{def.description}</size>", 26, ZodiacUi.Ink,
                              new Vector2(0f, 1f), new Vector2(660f, y), new Vector2(680f, 84f), TextAnchor.MiddleLeft);
            var buy = ZodiacUi.MakeButton(content, $"Mua — {price} đồng", new Vector2(0f, 1f), new Vector2(1200f, y), new Vector2(300f, 60f), () =>
            {
                bool ok = Shop.Buy(PlayerProfile.Current, id, price);
                resultText = ok ? $"<color=#9fe39a>Đã mua {def.displayName}.</color>" : "<color=#ff8a7a>Không đủ đồng hoặc túi đầy.</color>";
                if (ok && ZodiacResources.Get != null) Sound(ZodiacResources.Get.coinsHandle, ZodiacResources.Get.coin);
                Refresh();
            });
            buy.interactable = p.coins >= price;
        }
    }

    /// <summary>Quầy phụ kiện: hai cột, mỗi món có chỉ số, số đang có và nút mua. Mua xong vào túi, mặc ở hành trang.</summary>
    void BuildAccessories(PlayerProfile p)
    {
        for (int i = 0; i < Shop.Accessories.Length; i++)
        {
            var (id, price) = Shop.Accessories[i];
            var def = ItemDatabase.Get(id);
            if (def == null) continue;
            float x = i % 2 == 0 ? 90f : 830f, y = -215f - (i / 2) * 118f;
            var slot = ZodiacUi.MakeSlot(content, new Vector2(0f, 1f), new Vector2(x + 30f, y), 84f, null);
            ZodiacUi.Fill(slot, new ItemStack { id = id, count = 1 }, false);
            var stats = new System.Text.StringBuilder();
            foreach (var m in def.modifiers) stats.Append(stats.Length > 0 ? " · " : "").Append(ItemDefinition.ModifierText(m));
            int owned = OwnedCount(p, id);
            string color = ColorUtility.ToHtmlStringRGB(ItemDefinition.RarityColor(def.rarity));
            ZodiacUi.MakeText(content, $"<b><color=#{color}>{def.displayName}</color></b>  <size=18>({ItemDefinition.SlotName(def.slot)}{(owned > 0 ? $" · đang có {owned}" : "")})</size>\n" +
                                       $"<size=20><color=#9fe39a>{stats}</color></size>",
                              24, ZodiacUi.Ink, new Vector2(0f, 1f), new Vector2(x + 300f, y), new Vector2(420f, 90f), TextAnchor.MiddleLeft);
            var buy = ZodiacUi.MakeButton(content, $"{price} đồng", new Vector2(0f, 1f), new Vector2(x + 610f, y), new Vector2(170f, 56f), () =>
            {
                bool ok = Shop.Buy(PlayerProfile.Current, id, price);
                resultText = ok ? $"<color=#9fe39a>Đã mua {def.displayName} — mở hành trang (I) để mặc.</color>" : "<color=#ff8a7a>Không đủ đồng hoặc túi đầy.</color>";
                if (ok && ZodiacResources.Get != null) Sound(ZodiacResources.Get.coinsHandle, ZodiacResources.Get.coin);
                Refresh();
            });
            buy.interactable = p.coins >= price;
        }
        ZodiacUi.MakeText(content, "Bùa mặc ở ô Bùa, ngọc mặc được hai viên. Bùa bảo hộ / Ngọc may mắn là nguyên liệu lò rèn, không mặc được.",
                          20, ZodiacUi.Muted, new Vector2(0.5f, 0f), new Vector2(0f, 95f), new Vector2(1400f, 30f));
        Result();
    }

    static int OwnedCount(PlayerProfile p, string id)
    {
        int n = p.CountOf(id);
        foreach (var e in p.equipped) if (!e.IsEmpty && e.id == id) n++;
        return n;
    }

    void BuildSell(PlayerProfile p)
    {
        for (int i = 0; i < p.bag.Length; i++)
        {
            int index = i;
            var slot = ZodiacUi.MakeSlot(content, new Vector2(0f, 1f), new Vector2(90f + (i % 6) * 110f, -220f - (i / 6) * 110f), 96f,
                                         () => { selected = index; resultText = ""; Refresh(); });
            ZodiacUi.Fill(slot, p.bag[i], selected == i);
        }
        var s = selected >= 0 ? p.bag[selected] : null;
        if (s == null || s.IsEmpty)
        {
            ZodiacUi.MakeText(content, "Chọn món muốn bán (nửa giá; đồ đập cao bán đắt hơn).", 24, ZodiacUi.Muted,
                              new Vector2(0f, 1f), new Vector2(1130f, -260f), new Vector2(680f, 80f), TextAnchor.UpperLeft);
            return;
        }
        ZodiacUi.MakeText(content, s.Def.Tooltip(s), 24, ZodiacUi.Ink, new Vector2(0f, 1f), new Vector2(1130f, -330f), new Vector2(680f, 300f), TextAnchor.UpperLeft);
        ZodiacUi.MakeButton(content, $"Bán 1 — {Forge.SellPrice(s)} đồng", new Vector2(0f, 1f), new Vector2(1130f, -560f), new Vector2(360f, 64f), () =>
        {
            var p2 = PlayerProfile.Current;
            int price = Forge.SellPrice(p2.bag[selected]);
            if (Shop.Sell(p2, selected))
            {
                resultText = $"<color=#9fe39a>+{price} đồng</color>";
                if (ZodiacResources.Get != null) Sound(ZodiacResources.Get.coinsHandle, ZodiacResources.Get.coin);
            }
            if (p2.bag[selected].IsEmpty) selected = -1;
            Refresh();
        });
    }

    void BuildCraft(PlayerProfile p)
    {
        for (int i = 0; i < Shop.Recipes.Length; i++)
        {
            var r = Shop.Recipes[i];
            var def = ItemDatabase.Get(r.result);
            if (def == null) continue;
            float y = -220f - i * 105f;
            var slot = ZodiacUi.MakeSlot(content, new Vector2(0f, 1f), new Vector2(260f, y), 84f, null);
            ZodiacUi.Fill(slot, new ItemStack { id = r.result, count = r.count }, false);
            ZodiacUi.MakeText(content, $"<b>{def.displayName}{(r.count > 1 ? $" ×{r.count}" : "")}</b>\n<size=22>{Shop.Describe(r.inputs, p)}</size>", 26,
                              ZodiacUi.Ink, new Vector2(0f, 1f), new Vector2(660f, y), new Vector2(680f, 90f), TextAnchor.MiddleLeft);
            var craft = ZodiacUi.MakeButton(content, "Luyện", new Vector2(0f, 1f), new Vector2(1200f, y), new Vector2(220f, 60f), () =>
            {
                bool ok = Shop.Craft(PlayerProfile.Current, r);
                resultText = ok ? $"<color=#9fe39a>Luyện thành {def.displayName}!</color>" : "<color=#ff8a7a>Thiếu nguyên liệu hoặc túi đầy.</color>";
                if (ok && ZodiacResources.Get != null) Sound(ZodiacResources.Get.lantern, 0.8f);
                Refresh();
            });
            craft.interactable = Shop.CanCraft(p, r);
        }
    }

    // ───────── Cổng làng ─────────

    void BuildGate(PlayerProfile p)
    {
        for (int i = 0; i < levelScenes.Length; i++)
        {
            int index = i;
            bool open = i < p.unlockedLevels;
            string title = i < levelTitles.Length ? levelTitles[i] : levelScenes[i];
            // Hai cột cho đủ chỗ 8 màn
            var b = ZodiacUi.MakeButton(content, open ? title : $"{title}  (chưa mở)", new Vector2(0.5f, 1f),
                                        new Vector2(i % 2 == 0 ? -360f : 360f, -180f - (i / 2) * 94f),
                                        new Vector2(680f, 80f), () =>
                                        {
                                            Close();
                                            ScreenFader.LoadScene(levelScenes[index]);
                                        });
            b.interactable = open;
        }

        // Độ khó
        float y = -200f - (levelScenes.Length + 1) / 2 * 94f;
        ZodiacUi.MakeText(content, "Độ khó", 32, ZodiacUi.Gold, new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(600f, 44f)).fontStyle = FontStyle.Bold;
        for (int d = 0; d < ZodiacDifficulty.Names.Length; d++)
        {
            int level = d;
            bool unlocked = ZodiacDifficulty.IsUnlocked(d, p);
            bool chosen = p.difficulty == d;
            string label = ZodiacDifficulty.Names[d] + (unlocked ? "" : " (khoá)");
            var b = ZodiacUi.MakeButton(content, label, new Vector2(0.5f, 1f), new Vector2((d - 1) * 330f, y - 70f), new Vector2(310f, 64f), () =>
            {
                if (PlayerProfile.Current.SetDifficulty(level) && ZodiacResources.Get != null) Sound(ZodiacResources.Get.coin, 0.6f);
                Refresh();
            });
            b.interactable = unlocked;
            if (chosen) b.targetGraphic.color = new Color(0.85f, 0.6f, 0.1f);
        }
        int current = Mathf.Clamp(p.difficulty, 0, ZodiacDifficulty.Help.Length - 1);
        ZodiacUi.MakeText(content, ZodiacDifficulty.Help[current], 24, ZodiacDifficulty.Colors[current],
                          new Vector2(0.5f, 1f), new Vector2(0f, y - 135f), new Vector2(1300f, 36f));

        ZodiacUi.MakeText(content, "Thắng một màn để mở màn kế. Đánh lại màn cũ để kiếm thêm đồ và nguyên liệu.", 24, ZodiacUi.Muted,
                          new Vector2(0.5f, 0f), new Vector2(0f, 60f), new Vector2(1300f, 40f));
    }
}

using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Hai trang mới của cửa sổ làng:
///   • <b>Rương nhà</b>: bên trái là túi (bấm một ô để cất cả chồng vào rương), bên phải là rương 60 ô (bấm để lấy về túi).
///     Nút Cất hết nguyên liệu / Xếp rương. Đồ trong rương không mang theo khi lên đường, dùng chung cho mọi con giáp.
///   • <b>Chợ phiên</b> (<see cref="Market"/>): 6 món ngẫu nhiên, mỗi lần về làng một phiên mới, mỗi món mua một lần.
/// </summary>
public partial class VillageWindow
{
    void BuildChest(PlayerProfile p)
    {
        var tl = new Vector2(0f, 1f);
        ZodiacUi.MakeText(content, "Túi — bấm để cất vào rương", 24, ZodiacUi.Muted, tl, new Vector2(330f, -110f), new Vector2(600f, 30f));
        for (int i = 0; i < p.bag.Length; i++)
        {
            int index = i;
            var slot = ZodiacUi.MakeSlot(content, tl, new Vector2(80f + (i % 6) * 100f, -170f - (i / 6) * 100f), 88f, () =>
            {
                var pr = PlayerProfile.Current;
                if (pr.bag[index].IsEmpty) return;
                string name = pr.bag[index].DisplayName;
                bool ok = pr.StoreToChest(index);
                resultText = ok ? $"<color=#9fe39a>Đã cất {name} vào rương.</color>" : "<color=#ff8a7a>Rương đầy.</color>";
                if (ok && ZodiacResources.Get != null) Sound(ZodiacResources.Get.page, ZodiacResources.Get.coin, 0.5f);
                Refresh();
            });
            ZodiacUi.Fill(slot, p.bag[i], false);
        }

        int used = 0;
        foreach (var s in p.chest) if (!s.IsEmpty) used++;
        ZodiacUi.MakeText(content, $"Rương nhà ({used}/{p.chest.Length}) — bấm để lấy về túi", 24, ZodiacUi.Gold, tl,
                          new Vector2(1100f, -110f), new Vector2(760f, 30f));
        for (int i = 0; i < p.chest.Length; i++)
        {
            int index = i;
            var slot = ZodiacUi.MakeSlot(content, tl, new Vector2(730f + (i % 10) * 78f, -165f - (i / 10) * 78f), 70f, () =>
            {
                var pr = PlayerProfile.Current;
                if (pr.chest[index].IsEmpty) return;
                string name = pr.chest[index].DisplayName;
                bool ok = pr.TakeFromChest(index);
                resultText = ok ? $"<color=#9fe39a>Đã lấy {name}.</color>" : "<color=#ff8a7a>Túi đầy.</color>";
                if (ok && ZodiacResources.Get != null) Sound(ZodiacResources.Get.page, ZodiacResources.Get.coin, 0.5f);
                Refresh();
            });
            ZodiacUi.Fill(slot, p.chest[i], false);
        }

        ZodiacUi.MakeButton(content, "Cất hết nguyên liệu", tl, new Vector2(900f, -680f), new Vector2(320f, 56f), () =>
        {
            int n = PlayerProfile.Current.StoreAllMaterials();
            resultText = n > 0 ? $"<color=#9fe39a>Đã cất {n} chồng nguyên liệu.</color>" : "<color=#c8b8a8>Túi không còn nguyên liệu (hoặc rương đầy).</color>";
            Refresh();
        });
        ZodiacUi.MakeButton(content, "Xếp rương", tl, new Vector2(1250f, -680f), new Vector2(260f, 56f), () =>
        {
            PlayerProfile.Current.SortChest();
            resultText = "";
            Refresh();
        });
        ZodiacUi.MakeText(content, "Rương dùng chung cho cả mười hai con giáp. Đồ trong rương không mang theo khi lên đường.", 20, ZodiacUi.Muted,
                          new Vector2(0.5f, 0f), new Vector2(0f, 105f), new Vector2(1400f, 30f));
        Result();
    }

    void BuildMarket(PlayerProfile p)
    {
        if (!Market.IsUnlocked(p))
        {
            ZodiacUi.MakeText(content, "Thương nhân chưa tới làng.\n\nQua màn 3 (Rừng Trúc) — đường thông thì chợ phiên mới họp.",
                              30, ZodiacUi.Ink, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1200f, 200f));
            return;
        }
        var tl = new Vector2(0f, 1f);
        ZodiacUi.MakeText(content, "Mỗi lần về làng một phiên hàng mới — món nào cũng chỉ có một, mua thì mua ngay!", 24, ZodiacUi.Muted,
                          new Vector2(0.5f, 1f), new Vector2(0f, -110f), new Vector2(1400f, 30f));
        var offers = Market.Offers(p);
        for (int i = 0; i < offers.Length; i++)
        {
            int index = i;
            var o = offers[i];
            var def = o.item.Def;
            if (def == null) continue;
            bool sold = Market.IsSold(p, i);
            float x = i % 2 == 0 ? 60f : 800f, y = -200f - (i / 2) * 175f;
            var slot = ZodiacUi.MakeSlot(content, tl, new Vector2(x + 50f, y - 40f), 90f, null);
            ZodiacUi.Fill(slot, o.item, false);
            string color = ColorUtility.ToHtmlStringRGB(ItemDefinition.RarityColor(def.rarity));
            var stats = new System.Text.StringBuilder();
            foreach (var m in o.item.EffectiveModifiers()) stats.Append(stats.Length > 0 ? " · " : "").Append(ItemDefinition.ModifierText(m));
            string detail = def.type == ItemType.Equipment ? $"<color=#9fe39a>{stats}</color>" : def.description;
            ZodiacUi.MakeText(content, $"<b><color=#{color}>{o.item.DisplayName}</color></b>{(o.item.count > 1 ? $" ×{o.item.count}" : "")}\n<size=19>{detail}</size>",
                              23, ZodiacUi.Ink, tl, new Vector2(x + 330f, y - 40f), new Vector2(430f, 130f), TextAnchor.MiddleLeft);
            var buy = ZodiacUi.MakeButton(content, sold ? "Đã bán" : $"{o.price} đồng", tl, new Vector2(x + 640f, y - 40f), new Vector2(170f, 56f), () =>
            {
                bool ok = Market.Buy(PlayerProfile.Current, index);
                resultText = ok ? $"<color=#9fe39a>Đã mua {o.item.DisplayName}.</color>" : "<color=#ff8a7a>Không đủ đồng hoặc túi đầy.</color>";
                if (ok && ZodiacResources.Get != null) Sound(ZodiacResources.Get.coinsHandle, ZodiacResources.Get.coin);
                Refresh();
            });
            buy.interactable = !sold && p.coins >= o.price;
        }
        Result();
    }
}

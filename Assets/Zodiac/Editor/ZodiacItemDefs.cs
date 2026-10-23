using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Bảng vật phẩm: tạo asset ItemDefinition trong Assets/Zodiac/Items và Assets/Zodiac/Resources/ItemDatabase.asset.
/// Biểu tượng vẽ bằng code (bình thuốc, kiếm, giáp, bùa, ngọc, nguyên liệu) lưu ở Assets/Zodiac/Art/Items;
/// giáo và khiên dùng chính ảnh đồ của lính (Art/Enemies/Giao, Khien). Dựng lại thì ghi đè số liệu theo bảng dưới.
/// Thêm đồ mới: thêm một dòng vào <see cref="Defs"/> rồi chạy lại Tools/Zodiac/Dựng prototype.
/// </summary>
public static class ZodiacItemDefs
{
    const string ItemFolder = "Assets/Zodiac/Items";
    const string IconFolder = "Assets/Zodiac/Art/Items";
    const string DatabasePath = "Assets/Zodiac/Resources/ItemDatabase.asset";

    enum Icon { Potion, Sword, Armor, Charm, Gem, Shard, Art }

    class Def
    {
        public string id, name, desc, art;
        public ItemType type;
        public ItemRarity rarity;
        public EquipSlot slot;
        public StatModifier[] mods = System.Array.Empty<StatModifier>();
        public int heal, energy, stack = 1, price = 10;
        public Icon icon;
        public Color color = Color.white;
    }

    static StatModifier Flat(StatType s, float v) => new StatModifier(s, ModifierKind.Flat, v);
    static StatModifier Pct(StatType s, float v) => new StatModifier(s, ModifierKind.Percent, v);

    static readonly Def[] Defs =
    {
        // ── Đồ dùng
        new Def { id = "binh_mau_nho", name = "Bình máu nhỏ", desc = "Nước sâm đỏ — hồi một ít máu.", type = ItemType.Consumable,
                  heal = 40, stack = 10, price = 15, icon = Icon.Potion, color = new Color(0.9f, 0.2f, 0.2f) },
        new Def { id = "binh_mau_lon", name = "Bình máu lớn", desc = "Cao hổ cốt — hồi nhiều máu.", type = ItemType.Consumable, rarity = ItemRarity.Good,
                  heal = 100, stack = 5, price = 40, icon = Icon.Potion, color = new Color(0.75f, 0.05f, 0.25f) },
        new Def { id = "binh_khi", name = "Bình Khí", desc = "Trà sen — nạp lại Khí để tung chiêu.", type = ItemType.Consumable,
                  energy = 50, stack = 10, price = 20, icon = Icon.Potion, color = new Color(0.25f, 0.6f, 1f) },

        // ── Vũ khí
        new Def { id = "kiem_tre", name = "Kiếm tre", desc = "Nhẹ, sắc, ra đòn hiểm.", type = ItemType.Equipment, slot = EquipSlot.Weapon,
                  mods = new[] { Flat(StatType.Attack, 3), Flat(StatType.CritChance, 0.05f) }, price = 25, icon = Icon.Sword, color = new Color(0.75f, 0.8f, 0.4f) },
        new Def { id = "giao_dong", name = "Giáo đồng", desc = "Giáo của lính đất nung — nặng tay.", type = ItemType.Equipment, slot = EquipSlot.Weapon,
                  rarity = ItemRarity.Good, mods = new[] { Flat(StatType.Attack, 6) }, price = 45, icon = Icon.Art, art = "Giao" },
        new Def { id = "dao_rong", name = "Đao rồng", desc = "Thanh đao chạm rồng lấy từ hang Hồ Tinh.", type = ItemType.Equipment, slot = EquipSlot.Weapon,
                  rarity = ItemRarity.Rare, mods = new[] { Flat(StatType.Attack, 12), Flat(StatType.CritDamage, 0.3f) }, price = 150,
                  icon = Icon.Sword, color = new Color(1f, 0.75f, 0.2f) },
        new Def { id = "chuy_chan_tinh", name = "Chùy đá Chằn Tinh", desc = "Nặng trịch, nện đâu vỡ đó — rơi từ Chằn Tinh.", type = ItemType.Equipment,
                  slot = EquipSlot.Weapon, rarity = ItemRarity.Rare, mods = new[] { Flat(StatType.Attack, 10), Flat(StatType.Defense, 10) }, price = 140,
                  icon = Icon.Sword, color = new Color(0.65f, 0.58f, 0.48f) },

        // ── Giáp
        new Def { id = "ao_giap_tre", name = "Áo giáp tre", desc = "Nan tre đan dày.", type = ItemType.Equipment, slot = EquipSlot.Armor,
                  mods = new[] { Flat(StatType.Defense, 10) }, price = 25, icon = Icon.Armor, color = new Color(0.7f, 0.6f, 0.3f) },
        new Def { id = "khien_dong", name = "Khiên trống đồng", desc = "Mặt khiên chạm hoa văn trống đồng Đông Sơn.", type = ItemType.Equipment,
                  slot = EquipSlot.Armor, rarity = ItemRarity.Good, mods = new[] { Flat(StatType.Defense, 20), Flat(StatType.MaxHealth, 10) },
                  price = 60, icon = Icon.Art, art = "Khien" },
        new Def { id = "giap_vay_thuong_luong", name = "Giáp vảy Thuồng Luồng", desc = "Vảy rắn nước lấp lánh, dao chém trượt — rơi từ Thuồng Luồng.",
                  type = ItemType.Equipment, slot = EquipSlot.Armor, rarity = ItemRarity.Rare,
                  mods = new[] { Flat(StatType.Defense, 28), Flat(StatType.MaxHealth, 20) }, price = 140, icon = Icon.Armor, color = new Color(0.3f, 0.7f, 0.65f) },

        // ── Bùa
        new Def { id = "vong_bac", name = "Vòng bạc trừ tà", desc = "Vòng bạc bà đeo cho cháu từ bé — tà khí tránh xa.", type = ItemType.Equipment,
                  slot = EquipSlot.Charm, mods = new[] { Flat(StatType.Defense, 6), Flat(StatType.MaxHealth, 10) }, price = 45,
                  icon = Icon.Charm, color = new Color(0.85f, 0.88f, 0.92f) },
        new Def { id = "bua_ho_phu", name = "Hổ phù", desc = "Lá bùa hình hổ — người mang ra đòn hăng như chúa sơn lâm.", type = ItemType.Equipment,
                  slot = EquipSlot.Charm, rarity = ItemRarity.Good, mods = new[] { Flat(StatType.Attack, 4), Flat(StatType.CritChance, 0.04f) }, price = 85,
                  icon = Icon.Charm, color = new Color(1f, 0.6f, 0.15f) },
        new Def { id = "bua_ho_menh", name = "Bùa hộ mệnh", desc = "Lá bùa đỏ của thầy đồ làng.", type = ItemType.Equipment, slot = EquipSlot.Charm,
                  rarity = ItemRarity.Good, mods = new[] { Flat(StatType.MaxHealth, 30) }, price = 60, icon = Icon.Charm, color = new Color(0.9f, 0.15f, 0.15f) },
        new Def { id = "bua_khi", name = "Bùa tụ khí", desc = "Khí trời tụ về người mang.", type = ItemType.Equipment, slot = EquipSlot.Charm,
                  rarity = ItemRarity.Rare, mods = new[] { Flat(StatType.MaxEnergy, 25), Flat(StatType.EnergyRegen, 1f) }, price = 90,
                  icon = Icon.Charm, color = new Color(0.3f, 0.6f, 1f) },
        new Def { id = "bua_mat_ba_bi", name = "Bùa mười hai mắt", desc = "Mắt của Ông Ba Bị — nhìn ra chỗ hở của mọi kẻ địch.", type = ItemType.Equipment,
                  slot = EquipSlot.Charm, rarity = ItemRarity.Rare, mods = new[] { Flat(StatType.CritChance, 0.08f), Flat(StatType.CritDamage, 0.25f) },
                  price = 140, icon = Icon.Charm, color = new Color(1f, 0.8f, 0.25f) },

        // ── Ngọc
        new Def { id = "ngoc_trai", name = "Ngọc trai", desc = "Ngọc trai đầm sen — mát lành, Khí hồi đều.", type = ItemType.Equipment,
                  slot = EquipSlot.Gem, mods = new[] { Flat(StatType.MaxEnergy, 10), Flat(StatType.EnergyRegen, 0.3f) }, price = 40,
                  icon = Icon.Gem, color = new Color(0.95f, 0.93f, 0.88f) },
        new Def { id = "ho_phach", name = "Hổ phách", desc = "Nhựa thông nghìn năm hoá đá — đòn chí mạng đau thấu xương.", type = ItemType.Equipment,
                  slot = EquipSlot.Gem, rarity = ItemRarity.Rare, mods = new[] { Flat(StatType.CritDamage, 0.3f), Flat(StatType.MaxHealth, 10) }, price = 120,
                  icon = Icon.Gem, color = new Color(1f, 0.65f, 0.15f) },
        new Def { id = "ngoc_bich", name = "Ngọc bích Lạc Long", desc = "Viên ngọc xanh thẫm dưới đáy hang, khắc hình rồng.", type = ItemType.Equipment,
                  slot = EquipSlot.Gem, rarity = ItemRarity.Epic, mods = new[] { Pct(StatType.Attack, 0.1f), Flat(StatType.MaxHealth, 20) }, price = 220,
                  icon = Icon.Gem, color = new Color(0.15f, 0.75f, 0.5f) },
        new Def { id = "ngoc_do", name = "Ngọc đỏ", desc = "Hồng ngọc ẩn lửa.", type = ItemType.Equipment, slot = EquipSlot.Gem,
                  rarity = ItemRarity.Good, mods = new[] { Pct(StatType.Attack, 0.08f) }, price = 55, icon = Icon.Gem, color = new Color(0.95f, 0.2f, 0.25f) },
        new Def { id = "ngoc_lam", name = "Ngọc lam", desc = "Mắt nhìn thấu chỗ hở.", type = ItemType.Equipment, slot = EquipSlot.Gem,
                  rarity = ItemRarity.Good, mods = new[] { Flat(StatType.CritChance, 0.06f) }, price = 55, icon = Icon.Gem, color = new Color(0.25f, 0.55f, 1f) },
        new Def { id = "ngoc_luc", name = "Ngọc lục", desc = "Lạnh và vững như đá núi.", type = ItemType.Equipment, slot = EquipSlot.Gem,
                  rarity = ItemRarity.Rare, mods = new[] { Flat(StatType.Defense, 12), Flat(StatType.EnergyRegen, 0.5f) }, price = 80,
                  icon = Icon.Gem, color = new Color(0.2f, 0.85f, 0.45f) },

        // ── Nguyên liệu (rèn đồ ở làng — phần sau)
        new Def { id = "manh_gom", name = "Mảnh gốm", desc = "Mảnh vỡ của lính đất nung.", type = ItemType.Material, stack = 99, price = 2,
                  icon = Icon.Shard, color = new Color(0.75f, 0.4f, 0.25f) },
        new Def { id = "vay_bo_gai", name = "Vảy bọ gai", desc = "Cứng như sừng.", type = ItemType.Material, stack = 99, price = 3,
                  icon = Icon.Shard, color = new Color(0.2f, 0.4f, 0.45f) },
        new Def { id = "lua_ma_troi", name = "Lửa ma trơi", desc = "Đốm lửa xanh không bao giờ tắt.", type = ItemType.Material, stack = 99,
                  rarity = ItemRarity.Good, price = 6, icon = Icon.Gem, color = new Color(0.45f, 0.95f, 1f) },
        new Def { id = "da_coc", name = "Da cóc tía", desc = "Sần sùi, dai.", type = ItemType.Material, stack = 99, price = 3,
                  icon = Icon.Shard, color = new Color(0.55f, 0.3f, 0.65f) },
        new Def { id = "da_thach_su", name = "Đá thạch sư", desc = "Mảnh đá còn ấm lửa.", type = ItemType.Material, stack = 99,
                  rarity = ItemRarity.Good, price = 8, icon = Icon.Shard, color = new Color(0.6f, 0.6f, 0.65f) },
        // ── Đồ cho Lò rèn
        new Def { id = "bua_bao_ho", name = "Bùa bảo hộ", desc = "Đập đồ hỏng không bị tụt cấp (dùng ở Lò rèn).", type = ItemType.Material,
                  stack = 20, rarity = ItemRarity.Epic, price = 150, icon = Icon.Charm, color = new Color(0.95f, 0.85f, 0.3f) },
        new Def { id = "ngoc_may_man", name = "Ngọc may mắn", desc = "+10% tỉ lệ đập đồ thành công (dùng ở Lò rèn).", type = ItemType.Material,
                  stack = 20, rarity = ItemRarity.Rare, price = 80, icon = Icon.Gem, color = new Color(0.4f, 1f, 0.6f) },

        // ── Mảnh Đan: gom 3 mảnh luyện thành một viên (I → Tu luyện) — tăng vĩnh viễn máu / Khí tối đa của con giáp đang chơi
        new Def { id = "manh_huyet_dan", name = "Mảnh Huyết Đan", desc = "Gom đủ 3 mảnh để luyện Huyết Đan (+20 máu tối đa).", type = ItemType.Material,
                  stack = 99, rarity = ItemRarity.Rare, price = 50, icon = Icon.Gem, color = new Color(0.85f, 0.1f, 0.15f) },
        new Def { id = "manh_khi_dan", name = "Mảnh Khí Đan", desc = "Gom đủ 3 mảnh để luyện Khí Đan (+20 Khí tối đa).", type = ItemType.Material,
                  stack = 99, rarity = ItemRarity.Rare, price = 50, icon = Icon.Gem, color = new Color(0.2f, 0.65f, 1f) },
        new Def { id = "duoi_ho_tinh", name = "Đuôi Hồ Tinh", desc = "Một trong chín đuôi cáo — toả khí lạ.", type = ItemType.Material, stack = 9,
                  rarity = ItemRarity.Epic, price = 200, icon = Icon.Charm, color = new Color(1f, 0.6f, 0.2f) },
    };

    static Dictionary<string, ItemDefinition> built = new Dictionary<string, ItemDefinition>();

    /// <summary>Vật phẩm theo id (sau khi <see cref="EnsureAll"/>).</summary>
    public static ItemDefinition Get(string id) => built.TryGetValue(id, out var d) ? d : AssetDatabase.LoadAssetAtPath<ItemDefinition>($"{ItemFolder}/{id}.asset");

    public static void EnsureAll()
    {
        ZodiacPrototypeBuilder.EnsureFolder(ItemFolder);
        ZodiacPrototypeBuilder.EnsureFolder(IconFolder);
        ZodiacPrototypeBuilder.EnsureFolder(Path.GetDirectoryName(DatabasePath).Replace('\\', '/'));
        built = new Dictionary<string, ItemDefinition>();
        var list = new List<ItemDefinition>();
        foreach (var d in Defs)
        {
            string path = $"{ItemFolder}/{d.id}.asset";
            var item = AssetDatabase.LoadAssetAtPath<ItemDefinition>(path);
            if (item == null)
            {
                item = ScriptableObject.CreateInstance<ItemDefinition>();
                AssetDatabase.CreateAsset(item, path);
            }
            item.id = d.id;
            item.displayName = d.name;
            item.description = d.desc;
            item.type = d.type;
            item.rarity = d.rarity;
            item.slot = d.type == ItemType.Equipment ? d.slot : EquipSlot.None;
            item.modifiers = d.mods;
            item.heal = d.heal;
            item.energy = d.energy;
            item.maxStack = d.stack;
            item.price = d.price;
            item.icon = d.icon == Icon.Art ? ZodiacEnemyArt.Get(d.art) : EnsureIcon(d);
            EditorUtility.SetDirty(item);
            built[d.id] = item;
            list.Add(item);
        }
        var db = AssetDatabase.LoadAssetAtPath<ItemDatabase>(DatabasePath);
        if (db == null)
        {
            db = ScriptableObject.CreateInstance<ItemDatabase>();
            AssetDatabase.CreateAsset(db, DatabasePath);
        }
        db.items = list.ToArray();
        EditorUtility.SetDirty(db);
        AssetDatabase.SaveAssets();
    }

    // ───────────────────────────── Biểu tượng vẽ bằng code ─────────────────────────────

    const int Size = 128;

    static Sprite EnsureIcon(Def d)
    {
        string path = $"{IconFolder}/{d.id}.png";
        var color = new Color[Size * Size];
        for (int y = 0; y < Size; y++)
        for (int x = 0; x < Size; x++)
        {
            Vector2 p = new Vector2((x + 0.5f) / Size, (y + 0.5f) / Size);
            color[y * Size + x] = Paint(d.icon, d.color, p);
        }
        Outline(color);
        var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
        tex.SetPixels(color);
        tex.Apply();
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(path);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = Size;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    static float Circle(Vector2 p, Vector2 c, float r) => r - Vector2.Distance(p, c);

    static float Box(Vector2 p, Vector2 c, Vector2 half, float angle = 0f)
    {
        Vector2 d = p - c;
        float a = -angle * Mathf.Deg2Rad;
        d = new Vector2(d.x * Mathf.Cos(a) - d.y * Mathf.Sin(a), d.x * Mathf.Sin(a) + d.y * Mathf.Cos(a));
        return Mathf.Min(half.x - Mathf.Abs(d.x), half.y - Mathf.Abs(d.y));
    }

    /// <summary>Đa giác lồi (đỉnh theo chiều nào cũng được): khoảng cách tới cạnh gần nhất, dương bên trong.</summary>
    static float Poly(Vector2 p, params Vector2[] v)
    {
        float area = 0f;
        for (int i = 0; i < v.Length; i++)
        {
            Vector2 a = v[i], b = v[(i + 1) % v.Length];
            area += a.x * b.y - b.x * a.y;
        }
        float sign = Mathf.Sign(area);
        float inside = float.MaxValue;
        for (int i = 0; i < v.Length; i++)
        {
            Vector2 a = v[i], b = v[(i + 1) % v.Length], e = b - a, d = p - a;
            inside = Mathf.Min(inside, sign * (e.x * d.y - e.y * d.x) / e.magnitude);
        }
        return inside;
    }

    static Color Paint(Icon icon, Color c, Vector2 p)
    {
        Color clear = new Color(0, 0, 0, 0);
        Color Shade(Color baseColor, float light) => Color.Lerp(baseColor * 0.55f, Color.Lerp(baseColor, Color.white, 0.35f), light);
        float px = 1f / Size;
        switch (icon)
        {
            case Icon.Potion:
            {
                float body = Circle(p, new Vector2(0.5f, 0.38f), 0.28f);
                float neck = Box(p, new Vector2(0.5f, 0.72f), new Vector2(0.08f, 0.1f));
                float cork = Box(p, new Vector2(0.5f, 0.86f), new Vector2(0.1f, 0.05f));
                if (cork > 0f) return new Color(0.6f, 0.42f, 0.25f);
                if (Mathf.Max(body, neck) <= 0f) return clear;
                Color glass = new Color(0.85f, 0.92f, 1f);
                bool liquid = body > 0f && p.y < 0.48f;
                Color baseC = liquid ? Shade(c, 1f - Vector2.Distance(p, new Vector2(0.42f, 0.32f)) * 2.2f) : glass * 0.9f;
                if (Circle(p, new Vector2(0.4f, 0.45f), 0.06f) > 0f) baseC = Color.white;   // ánh sáng
                return baseC;
            }
            case Icon.Sword:
            {
                float blade = Box(p, new Vector2(0.55f, 0.55f), new Vector2(0.06f, 0.33f), -45f);
                float guard = Box(p, new Vector2(0.3f, 0.3f), new Vector2(0.14f, 0.035f), 45f);
                float grip = Box(p, new Vector2(0.2f, 0.2f), new Vector2(0.035f, 0.1f), -45f);
                if (guard > 0f) return c;
                if (grip > 0f) return new Color(0.4f, 0.22f, 0.12f);
                if (blade > 0f) return Shade(new Color(0.85f, 0.88f, 0.95f), 0.5f + Mathf.Clamp01(blade * 20f) * 0.5f);
                return clear;
            }
            case Icon.Armor:
            {
                float vest = Poly(p, new Vector2(0.22f, 0.15f), new Vector2(0.22f, 0.7f), new Vector2(0.36f, 0.85f), new Vector2(0.64f, 0.85f),
                                  new Vector2(0.78f, 0.7f), new Vector2(0.78f, 0.15f));
                if (vest <= 0f) return clear;
                if (Circle(p, new Vector2(0.5f, 0.85f), 0.1f) > 0f) return clear;   // cổ áo
                bool stripe = Mathf.Repeat(p.y * 8f, 1f) < 0.18f;
                return Shade(c, stripe ? 0.2f : 0.7f);
            }
            case Icon.Charm:
            {
                float card = Box(p, new Vector2(0.5f, 0.52f), new Vector2(0.18f, 0.28f));
                float tassel = Box(p, new Vector2(0.5f, 0.15f), new Vector2(0.03f, 0.1f));
                float ring = Mathf.Abs(Circle(p, new Vector2(0.5f, 0.86f), 0.06f)) - 0.015f;
                if (ring < 0f) return new Color(0.95f, 0.8f, 0.35f);
                if (tassel > 0f) return new Color(0.95f, 0.8f, 0.35f);
                if (card <= 0f) return clear;
                bool glyph = Box(p, new Vector2(0.5f, 0.52f), new Vector2(0.03f, 0.18f)) > 0f || Box(p, new Vector2(0.5f, 0.6f), new Vector2(0.1f, 0.02f)) > 0f;
                return glyph ? new Color(0.98f, 0.85f, 0.4f) : Shade(c, 0.6f);
            }
            case Icon.Gem:
            {
                float gem = Poly(p, new Vector2(0.5f, 0.12f), new Vector2(0.2f, 0.55f), new Vector2(0.32f, 0.78f), new Vector2(0.68f, 0.78f),
                                 new Vector2(0.8f, 0.55f));
                if (gem <= 0f) return clear;
                float facet = p.y > 0.55f ? 0.85f : (p.x < 0.5f ? 0.45f : 0.65f);
                if (Circle(p, new Vector2(0.42f, 0.66f), 0.05f) > 0f) return Color.white;
                return Shade(c, facet);
            }
            default:
            {
                float shard = Poly(p, new Vector2(0.3f, 0.2f), new Vector2(0.18f, 0.55f), new Vector2(0.45f, 0.82f), new Vector2(0.8f, 0.6f),
                                   new Vector2(0.72f, 0.25f));
                if (shard <= 0f) return clear;
                return Shade(c, 0.4f + (p.y - 0.2f) * 0.8f + (Mathf.PerlinNoise(p.x * 12f, p.y * 12f) - 0.5f) * 0.3f);
            }
        }
    }

    /// <summary>Viền tối 2px quanh hình (đọc rõ trên nền ô đồ).</summary>
    static void Outline(Color[] px)
    {
        var src = (Color[])px.Clone();
        for (int y = 0; y < Size; y++)
        for (int x = 0; x < Size; x++)
        {
            if (src[y * Size + x].a > 0.5f) continue;
            bool near = false;
            for (int dy = -2; dy <= 2 && !near; dy++)
            for (int dx = -2; dx <= 2 && !near; dx++)
            {
                int nx = x + dx, ny = y + dy;
                if (nx < 0 || ny < 0 || nx >= Size || ny >= Size) continue;
                near = src[ny * Size + nx].a > 0.5f;
            }
            if (near) px[y * Size + x] = new Color(0.12f, 0.07f, 0.05f, 1f);
        }
    }
}

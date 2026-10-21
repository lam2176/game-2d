using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Định nghĩa 12 con giáp (Tý → Hợi): ảnh, kích thước, chỉ số, nội tại, chiêu thức — và tạo asset ZodiacCharacter + ZodiacRoster.
/// Ảnh trong Assets/Zodiac/Art/Characters đã được cắt gọn từ ảnh gốc (Assets/Characters/...), lật cho mọi con quay mặt sang phải,
/// pivot đặt ở bàn chân (kể cả Khỉ — đã cắt lại cho cùng quy cách).
///
/// Asset đã có thì giữ nguyên chỉnh sửa tay — chỉ điền lại khi asset được tạo bởi bảng số liệu cũ hơn (DataVersion).
/// Menu "Tools/Zodiac/Ghi đè chỉ số 12 con giáp" ghi đè lại toàn bộ theo bảng dưới đây.
/// </summary>
public static class ZodiacCharacterDefs
{
    const string ArtFolder = "Assets/Zodiac/Art/Characters";
    const string DataFolder = "Assets/Zodiac/Characters";
    internal const string RosterPath = DataFolder + "/ZodiacRoster.asset";

    class Def
    {
        public string file, display, zodiac, instrument;
        public Color theme;
        // Ảnh: pivot (chuẩn hoá) ở bàn chân, chiều cao toàn ảnh trong game (unit).
        public Vector2 pivot;
        public float height;
        public Vector2 collider;
        public float colliderOffsetX;
        // Chỉ số
        public int hp = 5, attack = 1;
        public float walk = 5f, run = 8.5f, jump = 3.2f;
        // Nội tại
        public string passive, passiveDesc;
        public int airJumps, lastStand, poison, coinsPerHeal, stompMult = 1;
        public float glide, knockbackRes, magnet;
        public SkillConfig skill;
        public string ultimate, ultimateDesc;
    }

    static SkillConfig Skill(SkillType type, string name, float cooldown, int damage, string desc) =>
        // Mặc định theo thang cũ (tim, ô Khí) như phần còn lại của bảng — Fill quy đổi sang thang ZodiacBalance.
        new SkillConfig { type = type, name = name, cooldown = cooldown, damage = damage, description = desc, energyCost = 3, healAmount = 2 };

    static readonly Def[] Defs =
    {
        new Def
        {
            file = "Rat_Ty", ultimate = "Vạn Ảnh Thử", ultimateDesc = "Hoá vạn bóng chuột: chớp tới từng quái trong khu (tối đa 8) chém một nhát, nhát cuối quật ngã cả đám. Bất tử khi thi triển.", display = "Chuột", zodiac = "Tý", instrument = "Đàn nguyệt", theme = new Color(0.8f, 0.4f, 0.75f),
            pivot = new Vector2(0.550f, 0.004f), height = 1.9f, collider = new Vector2(0.7f, 1.35f),
            hp = 4, walk = 5.5f, run = 9.5f, jump = 3.4f,
            passive = "Lanh lẹ", passiveDesc = "Nhỏ con, chạy nhanh, nhảy cao hơn một chút — dễ luồn lách.",
            skill = Hold(Skill(SkillType.Dash, "Độn Ảnh", 1f, 0,
                "Lướt vụt về phía trước như bóng chuột: bất tử trong lúc lướt, đi xuyên qua quái và đạn.\n(Tham khảo: Dash của Celeste, Shade Cloak của Hollow Knight)"),
                s => { s.energyCost = 2; s.dashSpeed = 22f; s.dashTime = 0.18f; s.dashInvulnerable = true; }),
        },
        new Def
        {
            file = "Ox_Suu", ultimate = "Đại Hồng Chung", ultimateDesc = "Gõ ba hồi đại hồng chung: ba vòng sóng chấn rộng khắp quanh mình, hồi cuối quật ngã; giáp +100 trong 6 giây.", display = "Trâu", zodiac = "Sửu", instrument = "Chiêng", theme = new Color(0.3f, 0.45f, 0.95f),
            pivot = new Vector2(0.618f, 0.002f), height = 2.3f, collider = new Vector2(0.85f, 1.9f),
            hp = 7, walk = 4.3f, run = 7.2f, jump = 3.2f, knockbackRes = 0.75f,
            passive = "Da trâu", passiveDesc = "Máu dày nhất (140) và đứng vững như cột đình — bị đánh gần như không bị đẩy lùi.",
            skill = Hold(Skill(SkillType.Projectile, "Chiêng Chấn Địa", 3f, 2,
                "Gõ chiêng: hai làn sóng chấn chạy dọc mặt đất sang hai bên (quét làn đang đứng), gây 40 sát thương và làm choáng quái 1.2 giây.\n(Tham khảo: sóng chấn của Mega Man, Cuphead)"),
                s => { s.energyCost = 3; s.groundWave = true; s.bothDirections = true; s.projectileSpeed = 11f; s.projectileRange = 9f;
                       s.projectileSize = 0.9f; s.pierce = -1; s.stunTime = 1.2f; s.knockback = 0.5f; }),
        },
        new Def
        {
            file = "Tiger_Dan", ultimate = "Mãnh Hổ Hạ Sơn", ultimateDesc = "Vồ lên cao, đáp xuống giữa đám quái đông nhất (80 sát thương, quật ngã) rồi gầm: choáng cả khu, công +30% trong 8 giây.", display = "Hổ", zodiac = "Dần", instrument = "Trống cơm", theme = new Color(0.95f, 0.5f, 0.1f),
            pivot = new Vector2(0.441f, 0.007f), height = 2.2f, collider = new Vector2(0.8f, 1.75f),
            hp = 5, attack = 2, jump = 3.3f,
            passive = "Mãnh hổ", passiveDesc = "Đòn đánh thường mạnh gấp đôi (40 sát thương).",
            skill = Hold(Skill(SkillType.Dash, "Hổ Vồ", 2.5f, 3,
                "Vồ chéo lên phía trước, xé 60 sát thương; vồ trúng thì bật nảy lên để vồ tiếp.\n(Tham khảo: Dash Slash của Hollow Knight)"),
                s => { s.energyCost = 3; s.dashSpeed = 17f; s.dashTime = 0.3f; s.dashAngle = 28f; s.dashInvulnerable = true;
                       s.dashBounceOnHit = true; s.knockback = 1.5f; }),
        },
        new Def
        {
            file = "Cat_Mao", ultimate = "Cửu Mệnh Huyền Âm", ultimateDesc = "Hồi 30% máu và thêm một mạng giữ lại; tiếng nhị huyền âm làm cả khu choáng 2.5 giây.", display = "Mèo", zodiac = "Mão", instrument = "Đàn nhị", theme = new Color(0.9f, 0.25f, 0.35f),
            pivot = new Vector2(0.443f, 0.016f), height = 2.0f, collider = new Vector2(0.75f, 1.6f),
            hp = 4, walk = 5.2f, run = 9f, jump = 3.6f, lastStand = 1,
            passive = "Chín mạng", passiveDesc = "Mỗi màn được thoát chết một lần: đòn kết liễu chỉ để lại 1 máu.",
            skill = Hold(Skill(SkillType.Projectile, "Tiếng Nhị Xuyên Tâm", 1.6f, 2,
                "Kéo nhị bắn một sợi âm sắc lẹm bay thẳng, xuyên qua mọi kẻ địch trên đường (40 sát thương).\n(Tham khảo: Charge Shot của Mega Man)"),
                s => { s.energyCost = 2; s.projectileSpeed = 18f; s.projectileRange = 13f; s.projectileSize = 0.5f; s.pierce = -1; }),
        },
        new Def
        {
            file = "Dragon_Thin", ultimate = "Long Thần Hoả", ultimateDesc = "Đứng phun lửa rồng 1.6 giây: 22 cầu lửa xuyên thấu toả theo chiều sâu.", display = "Rồng", zodiac = "Thìn", instrument = "Đàn tranh", theme = new Color(0.95f, 0.75f, 0.2f),
            pivot = new Vector2(0.631f, 0.004f), height = 2.3f, collider = new Vector2(0.85f, 1.8f),
            hp = 6, jump = 4f,
            passive = "Thăng long", passiveDesc = "Nhảy cao nhất trong 12 con giáp — đá bay từ trên cao — và có 120 máu.",
            skill = Hold(Skill(SkillType.Projectile, "Long Hoả", 2.2f, 1,
                "Gảy đàn tranh phun 5 cầu lửa toả hình quạt — đứng càng gần càng trúng nhiều.\n(Tham khảo: Spread Shot của Contra, Cuphead)"),
                s => { s.energyCost = 3; s.projectileCount = 5; s.spreadAngle = 50f; s.projectileSpeed = 12f; s.projectileRange = 8f;
                       s.projectileSize = 0.55f; }),
        },
        new Def
        {
            file = "Snake_Ty", ultimate = "Vạn Xà Ẩn Hình", ultimateDesc = "Cả khu nhiễm độc; quấn lấy con khoẻ nhất hút máu 2 giây, hồi cho mình nửa lượng máu hút được.", display = "Rắn", zodiac = "Tỵ", instrument = "Áo choàng bóng đêm", theme = new Color(0.2f, 0.65f, 0.45f),
            pivot = new Vector2(0.613f, 0.003f), height = 2.05f, collider = new Vector2(0.9f, 1.35f),
            hp = 4, walk = 4.8f, poison = 2,
            passive = "Nọc độc", passiveDesc = "Đòn đánh thường tẩm độc, gây thêm 40 sát thương theo thời gian.",
            skill = Hold(Skill(SkillType.Stealth, "Xà Ảnh", 6f, 0,
                "Ẩn vào bóng tối 3 giây: bất tử, quái mất dấu. Đòn đánh đầu tiên khi đang ẩn gây sát thương x3.\n(Tham khảo: Mark of the Ninja, Dead Cells)"),
                s => { s.energyCost = 4; s.stealthDuration = 3f; s.backstabMultiplier = 3f; }),
        },
        new Def
        {
            file = "Horse_Ngo", ultimate = "Thiên Mã Hành Không", ultimateDesc = "Phi qua lại hết khu ba lượt, húc mọi quái trên đường; lượt cuối quật ngã. Bất tử khi phi.", display = "Ngựa", zodiac = "Ngọ", instrument = "Đàn tam", theme = new Color(0.3f, 0.75f, 0.8f),
            pivot = new Vector2(0.640f, 0.004f), height = 2.3f, collider = new Vector2(0.75f, 1.9f),
            hp = 5, walk = 5.5f, run = 11f, jump = 3.3f,
            passive = "Thiên lý mã", passiveDesc = "Chạy nhanh nhất (11 ô/giây khi giữ Shift) — đòn lao khi chạy đi rất xa.",
            skill = Hold(Skill(SkillType.Dash, "Vó Ngựa Phi", 2.5f, 2,
                "Phi nước đại một quãng dài, húc văng mọi quái trên đường (40 sát thương, bất tử khi phi).\n(Tham khảo: Spin Dash của Sonic)"),
                s => { s.energyCost = 3; s.dashSpeed = 19f; s.dashTime = 0.45f; s.dashInvulnerable = true; s.knockback = 2f; }),
        },
        new Def
        {
            file = "Goat_Mui", ultimate = "Xuân Hồi Đại Địa", ultimateDesc = "Hồi đầy máu, đẩy lùi quái quanh mình; +60 giáp và hồi máu dần trong 10 giây.", display = "Dê", zodiac = "Mùi", instrument = "Tỳ bà", theme = new Color(0.45f, 0.8f, 0.45f),
            pivot = new Vector2(0.560f, 0.003f), height = 2.2f, collider = new Vector2(0.75f, 1.7f),
            hp = 5, jump = 3.4f, coinsPerHeal = 6,
            passive = "Hiền hoà", passiveDesc = "Cứ nhặt 6 đồng xu lại hồi 20 máu.",
            skill = Hold(Skill(SkillType.Heal, "Khúc Hồi Xuân", 8f, 0,
                "Đứng yên gảy tỳ bà 0.8 giây để hồi 40 máu — bị đánh trúng thì bị ngắt.\n(Tham khảo: Focus của Hollow Knight)"),
                s => { s.energyCost = 4; s.channelTime = 0.8f; s.healAmount = 2; }),
        },
        new Def
        {
            file = "Monkey_Than", ultimate = "Tề Thiên Đại Thánh", ultimateDesc = "Nhổ lông thổi ra 3 phân thân vàng tự lao vào đánh quái trong 8 giây — Khỉ vẫn đánh tiếp được.", display = "Khỉ", zodiac = "Thân", instrument = "Chũm chọe", theme = new Color(0.95f, 0.7f, 0.25f),
            pivot = new Vector2(0.365f, 0.003f), height = 2.46f, collider = new Vector2(0.8f, 1.9f), colliderOffsetX = 0.15f,
            hp = 5, airJumps = 1,
            passive = "Tề Thiên", passiveDesc = "Nhảy thêm 1 lần trên không — đá bay liên hoàn, tung hứng quái trên không.",
            skill = Hold(Skill(SkillType.Projectile, "Chũm Chọe Bay", 1.8f, 1,
                "Ném chũm chọe xoay tít bay ra rồi quay về tay, chém mọi quái trên đường đi lẫn đường về.\n(Tham khảo: Metal Blade của Mega Man, boomerang của Zelda)"),
                s => { s.energyCost = 2; s.boomerang = true; s.projectileSpeed = 13f; s.projectileRange = 10f; s.projectileSize = 0.7f; s.pierce = -1; }),
        },
        new Def
        {
            file = "Rooster_Dau", ultimate = "Kim Kê Báo Hiệu", ultimateDesc = "Bình minh bừng sáng cả khu: choáng 3.5 giây, xoá sạch đạn địch, quái nhận thêm 50% sát thương trong 6 giây.", display = "Gà", zodiac = "Dậu", instrument = "Kèn", theme = new Color(0.9f, 0.3f, 0.2f),
            pivot = new Vector2(0.432f, 0.057f), height = 1.95f, collider = new Vector2(0.8f, 1.45f),
            hp = 5, glide = 2.2f,
            passive = "Cánh phượng", passiveDesc = "Giữ Space khi đang rơi để xoè cánh lượn chậm — lơ lửng lâu, đá bay vào lúc quái sơ hở.",
            skill = Hold(Skill(SkillType.Burst, "Kèn Hiệu Bình Minh", 5f, 1,
                "Thổi kèn: sóng âm toả tròn làm choáng mọi quái quanh mình 2 giây, gây 20 sát thương và thổi tan đạn địch.\n(Tham khảo: Howling Wraiths của Hollow Knight)"),
                s => { s.energyCost = 4; s.radius = 4f; s.stunTime = 2f; s.knockback = 1f; s.clearsProjectiles = true; }),
        },
        new Def
        {
            file = "Dog_Tuat", ultimate = "Thiên Cẩu Hống", ultimateDesc = "Tiếng hống trời giáng quật ngã cả khu (50 sát thương); công +25% trong 8 giây.", display = "Chó", zodiac = "Tuất", instrument = "Đàn đáy", theme = new Color(0.85f, 0.6f, 0.25f),
            pivot = new Vector2(0.327f, 0.005f), height = 2.1f, collider = new Vector2(0.8f, 1.8f),
            hp = 5, walk = 5.2f, run = 9f, jump = 3.3f, magnet = 4.5f,
            passive = "Mũi thính", passiveDesc = "Đồng xu trong bán kính 4.5 ô tự bay về phía Chó.",
            skill = Hold(Skill(SkillType.Burst, "Khuyển Hống", 2f, 2,
                "Sủa vang: sóng âm phía trước gây 40 sát thương và hất văng quái ra xa.\n(Tham khảo: Force Push, Bash của Ori)"),
                s => { s.energyCost = 2; s.radius = 2.2f; s.forwardOffset = 1.6f; s.knockback = 2.5f; s.stunTime = 0.6f; }),
        },
        new Def
        {
            file = "Pig_Hoi", ultimate = "Bát Giới Trấn Sơn", ultimateDesc = "Hút cả khu về trước mặt rồi bật lên dậm đất: 90 sát thương, quật ngã mọi quái quanh mình.", display = "Heo", zodiac = "Hợi", instrument = "Sáo trúc", theme = new Color(0.95f, 0.5f, 0.6f),
            pivot = new Vector2(0.519f, 0.018f), height = 1.95f, collider = new Vector2(1f, 1.45f),
            hp = 6, walk = 4.5f, run = 7.5f, stompMult = 2,
            passive = "Phúc hậu", passiveDesc = "120 máu; đá bay (nhảy + Q) gây gấp đôi sát thương.",
            skill = Hold(Skill(SkillType.GroundPound, "Heo Dậm Đất", 3f, 3,
                "Bật lên rồi lao thẳng xuống dậm đất: sóng chấn quanh chỗ đáp gây 60 sát thương và làm choáng.\n(Tham khảo: Ground Pound của Mario, Stone của Kirby)"),
                s => { s.energyCost = 3; s.diveSpeed = 26f; s.radius = 2.8f; s.stunTime = 1f; s.knockback = 1.2f; }),
        },
    };

    /// <summary>Tiếng nhạc cụ của chiêu (Assets/Zodiac/Audio/Synth).</summary>
    static readonly System.Collections.Generic.Dictionary<string, string> SkillSounds = new System.Collections.Generic.Dictionary<string, string>
    {
        { "Rat_Ty", "skill_rat_nguyet" }, { "Ox_Suu", "skill_ox_gong" }, { "Tiger_Dan", "skill_tiger_drum" },
        { "Cat_Mao", "skill_cat_nhi" }, { "Dragon_Thin", "skill_dragon_tranh" }, { "Snake_Ty", "skill_snake_cloak" },
        { "Horse_Ngo", "skill_horse_tam" }, { "Goat_Mui", "skill_goat_tyba" }, { "Monkey_Than", "skill_monkey_cymbal" },
        { "Rooster_Dau", "skill_rooster_ken" }, { "Dog_Tuat", "skill_dog_day" }, { "Pig_Hoi", "skill_pig_flute" },
    };

    static SkillConfig Hold(SkillConfig s, System.Action<SkillConfig> setup)
    {
        setup(s);
        return s;
    }

    [MenuItem("Tools/Zodiac/Ghi đè chỉ số 12 con giáp", true)]
    static bool CanOverwrite() => !EditorApplication.isPlayingOrWillChangePlaymode;

    [MenuItem("Tools/Zodiac/Ghi đè chỉ số 12 con giáp")]
    static void OverwriteInteractive()
    {
        if (!EditorUtility.DisplayDialog("Ghi đè chỉ số", "Mọi chỉnh sửa tay trong 12 asset nhân vật sẽ bị thay bằng giá trị mặc định.", "Ghi đè", "Huỷ"))
            return;
        EnsureRoster(overwrite: true);
        Debug.Log("Đã ghi đè chỉ số 12 con giáp.");
    }

    /// <summary>Import ảnh, tạo/điền 12 asset nhân vật và roster. Trả về roster.</summary>
    public static ZodiacRoster EnsureRoster(bool overwrite = false)
    {
        ZodiacPrototypeBuilder.EnsureFolder(DataFolder);
        var characters = Defs.Select(d => EnsureCharacter(d, overwrite)).ToArray();

        var roster = AssetDatabase.LoadAssetAtPath<ZodiacRoster>(RosterPath);
        if (roster == null)
        {
            roster = ScriptableObject.CreateInstance<ZodiacRoster>();
            AssetDatabase.CreateAsset(roster, RosterPath);
        }
        roster.characters = characters;
        EditorUtility.SetDirty(roster);
        AssetDatabase.SaveAssets();
        return roster;
    }

    /// <summary>Tăng khi bảng số liệu đổi cấu trúc (vd. thêm Khí) — asset cũ hơn sẽ được điền lại.</summary>
    // 4: đổi sang thang số ZodiacBalance (máu, công, chiêu ×20; Khí 6 ô → 100).
    // 5: mô tả theo thang số mới và lối chơi beat 'em up (đá bay thay đạp đầu, bỏ vực).
    const int DataVersion = 7;

    static ZodiacCharacter EnsureCharacter(Def d, bool overwrite)
    {
        string spritePath = $"{ArtFolder}/{d.file}.png";
        if (d.height > 0f) ConfigureImporter(spritePath, d);
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
        if (sprite == null) Debug.LogError("Không tìm thấy sprite " + spritePath);

        string path = $"{DataFolder}/{d.file}.asset";
        var data = AssetDatabase.LoadAssetAtPath<ZodiacCharacter>(path);
        bool created = data == null;
        if (created)
        {
            data = ScriptableObject.CreateInstance<ZodiacCharacter>();
            AssetDatabase.CreateAsset(data, path);
        }
        if (created || overwrite || data.dataVersion < DataVersion) Fill(data, d, sprite);
        if (data.skillSound == null && SkillSounds.TryGetValue(d.file, out string sound))
        {
            data.skillSound = ZodiacVisualSetup.Clip("Synth/" + sound);
            EditorUtility.SetDirty(data);
        }
        return data;
    }

    static void Fill(ZodiacCharacter c, Def d, Sprite sprite)
    {
        c.displayName = d.display;
        c.zodiacName = d.zodiac;
        c.instrument = d.instrument;
        c.sprite = sprite;
        c.themeColor = d.theme;
        c.colliderSize = d.collider;
        c.colliderOffset = new Vector2(d.colliderOffsetX, d.collider.y / 2f);
        // Bảng trên viết theo tim / ô Khí của thang cũ cho dễ đọc → quy đổi sang thang ZodiacBalance
        c.maxHealth = d.hp * ZodiacBalance.Unit;
        c.walkSpeed = d.walk;
        c.runSpeed = d.run;
        c.jumpHeight = d.jump;
        c.attackDamage = d.attack * ZodiacBalance.Unit;
        c.defense = 0;
        c.maxEnergy = ZodiacBalance.BaseEnergy;
        c.energyRegen = 2.5f;
        c.passiveName = d.passive;
        c.passiveDescription = d.passiveDesc;
        c.airJumps = d.airJumps;
        c.glideFallSpeed = d.glide;
        c.knockbackResistance = d.knockbackRes;
        c.lastStandCharges = d.lastStand;
        c.poisonDamage = d.poison;
        c.coinMagnetRadius = d.magnet;
        c.coinsPerHeal = d.coinsPerHeal;
        c.stompDamageMultiplier = d.stompMult;
        c.skill = JsonUtility.FromJson<SkillConfig>(JsonUtility.ToJson(d.skill));   // bản sao, không dùng chung object
        c.skill.damage *= ZodiacBalance.Unit;
        c.skill.healAmount *= ZodiacBalance.Unit;
        c.skill.energyCost = ZodiacBalance.EnergyCost(c.skill.energyCost);
        c.ultimateName = d.ultimate;
        c.ultimateDescription = d.ultimateDesc;
        c.dataVersion = DataVersion;
        EditorUtility.SetDirty(c);
    }

    /// <summary>
    /// Sprite đơn, pivot ở chân, PPU tính sao cho cả ảnh cao đúng d.height unit. Chỉ reimport khi thiết lập khác.
    /// Ảnh 2048px hiển thị ~250–500px → bật mipmap (lọc Kaiser giữ nét, bias âm nhẹ) để thu nhỏ không bị nhoè/lấp lánh;
    /// nén chất lượng cao (BC7) để màu áo, hoa văn không bị bệt.
    /// </summary>
    static void ConfigureImporter(string path, Def d)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) return;
        importer.GetSourceTextureWidthAndHeight(out _, out int height);
        float ppu = Mathf.Round(height / d.height);

        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        bool same = importer.textureType == TextureImporterType.Sprite &&
                    settings.spriteMode == (int)SpriteImportMode.Single &&
                    settings.spriteAlignment == (int)SpriteAlignment.Custom &&
                    Mathf.Approximately(settings.spritePixelsPerUnit, ppu) &&
                    (settings.spritePivot - d.pivot).sqrMagnitude < 1e-6f &&
                    importer.mipmapEnabled && importer.mipmapFilter == TextureImporterMipFilter.KaiserFilter &&
                    importer.alphaIsTransparency && importer.textureCompression == TextureImporterCompression.CompressedHQ &&
                    importer.maxTextureSize >= 2048;
        if (same) return;

        importer.textureType = TextureImporterType.Sprite;
        importer.ReadTextureSettings(settings);
        settings.spriteMode = (int)SpriteImportMode.Single;
        settings.spriteAlignment = (int)SpriteAlignment.Custom;
        settings.spritePivot = d.pivot;
        settings.spritePixelsPerUnit = ppu;
        settings.spriteMeshType = SpriteMeshType.Tight;
        settings.mipmapEnabled = true;
        settings.mipmapFilter = TextureImporterMipFilter.KaiserFilter;
        settings.mipmapBias = -0.25f;
        settings.alphaIsTransparency = true;
        settings.filterMode = FilterMode.Trilinear;
        settings.aniso = 1;
        importer.SetTextureSettings(settings);
        importer.maxTextureSize = 2048;   // ảnh nguồn 2048px: nét cả ở màn hình 1440p/4K
        importer.textureCompression = TextureImporterCompression.CompressedHQ;
        importer.SaveAndReimport();
    }
}

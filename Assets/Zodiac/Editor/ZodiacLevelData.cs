using UnityEngine;

/// <summary>
/// Dữ liệu 8 màn beat 'em up 2.5D (Hồi 1: Làng Quê → … → Động Hồ Tinh).
/// Tên scene giữ như cũ cho các màn có từ trước (Level01/02/03), màn mới là Level04…08 — thứ tự chơi là thứ tự trong mảng. Mỗi màn là một chuỗi <b>khu</b> (<see cref="Stage"/>), mỗi khu vừa khít một khung hình
/// (<see cref="RoomWidth"/> unit dọc trục X, các làn theo chiều sâu Z từ <see cref="LevelDef.laneMin"/> tới <see cref="LevelDef.laneMax"/>).
/// Camera đứng yên trong khu nên luôn thấy hết quái; dọn xong đi ra mép phải thì màn hình tối lại và chuyển sang khu kế.
/// Khu cuối có cổng đích (hiện khi dọn xong). Sửa màn: đổi số liệu ở đây rồi chạy lại Tools/Zodiac/Dựng prototype.
///
/// Mỗi đợt là một chuỗi mã quái cách nhau dấu cách — quái hiện ngay trong khu (có vòng khói báo trước):
///   W  Lính Đất Nung (cầm giáo — còn nửa máu thì văng giáo)
///   Q  Lính Khiên (đỡ đòn phía trước — đỡ 4 đòn hoặc còn nửa máu thì vỡ khiên)
///   S  Bọ Gai (lăn dọc làn)     F  Ma Trơi (bay)     H  Cóc Tía (nhảy đè)
///   T  Thạch Sư (tượng phun lửa — đứng ở làn trong cùng)
/// Thêm "!" trước mã (vd. "!W") = quái trùm (<see cref="MiniBoss"/>): to, trâu, thanh máu riêng, chắc chắn rơi mảnh Đan.
/// Khu có boss = true thì boss (bossKind: Hồ Tinh, Thuồng Luồng, Ông Ba Bị, Chằn Tinh) đứng chờ sẵn.
/// Khu có secret = true: góc trong mép phải có vật đập vỡ được (bụi tre rậm / vách đá nứt) che lối sang khu bí mật
/// nằm trong khoảng trống trước khu kế — có rương báu và các đợt quái riêng (secretWaves).
///
/// Nhịp màn: mỗi loại quái mới có biển hướng dẫn ở đầu khu giới thiệu nó, rồi mới trộn với loại khác.
/// Đầu mỗi khu có đèn lồng — thua thì hồi sinh ở đầu khu đang đánh.
/// </summary>
public static class ZodiacLevelData
{
    /// <summary>Kiểu cảnh — mỗi màn mới có bộ cảnh riêng (xem ZodiacLevelBuilder.Themes).</summary>
    public enum Backdrop { Village, Bamboo, Cave, Field, Pond, Market, Limestone, Lakeshore }

    /// <summary>Bề ngang một khu (unit) — bằng khung hình 16:9 tại làn giữa.</summary>
    public const float RoomWidth = 15f;
    /// <summary>Khoảng cách giữa mép trái hai khu liền nhau (khu kế nằm ngoài tầm nhìn).</summary>
    public const float RoomSpacing = 36f;

    /// <summary>Boss đứng chờ ở khu có boss = true.</summary>
    public enum BossKind { HoTinh, ThuongLuong, OngBaBi, ChanTinh }

    public class Stage
    {
        public string title;
        public string[] waves;
        /// <summary>Biển hướng dẫn đầu khu (trống = không có).</summary>
        public string sign;
        /// <summary>Bãi chông / than hồng giữa khu, chắn nửa mặt đường.</summary>
        public bool hazard;
        /// <summary>Số cụm 3 đồng xu trong khu.</summary>
        public int coinRows = 1;
        public bool boss;
        public BossKind bossKind = BossKind.HoTinh;
        /// <summary>Có khu bí mật (lối ẩn sau vật đập vỡ được ở góc khu). Không dùng cho khu cuối.</summary>
        public bool secret;
        public string secretTitle = "";
        public string[] secretWaves = System.Array.Empty<string>();

        public Stage(string title, params string[] waves)
        {
            this.title = title;
            this.waves = waves;
        }
    }

    public class LevelDef
    {
        public string sceneName, title, subtitle, nextScene;
        public Backdrop backdrop;
        public Color sky, far, mid, soil, grass, stone, plank;
        public float laneMin = -3f, laneMax = 3f;
        public Stage[] stages = System.Array.Empty<Stage>();
        /// <summary>Làng Trung Tâm: không có khu / quái, có các điểm Lò rèn, Tiệm thuốc, Bàn thờ, Cổng làng.</summary>
        public bool hub;
        public float hubLength = 36f;

        /// <summary>Mép trái khu thứ i.</summary>
        public float RoomX(int i) => 4f + i * RoomSpacing;
        /// <summary>Chiều dài cả con đường.</summary>
        public float Length => hub ? hubLength : RoomX(stages.Length - 1) + RoomWidth + 6f;
    }

    /// <summary>Làng Trung Tâm — nghỉ chân giữa các màn: đập đồ, mua thuốc, tu luyện, chọn màn.</summary>
    public static readonly LevelDef Village = new LevelDef
    {
        sceneName = "Zodiac_Village", title = "Làng Trung Tâm", subtitle = "Lò rèn, tiệm thuốc, bàn thờ tổ — nghỉ chân trước khi lên đường",
        backdrop = Backdrop.Village, hub = true,
        sky = new Color(0.98f, 0.86f, 0.66f), far = new Color(0.62f, 0.72f, 0.70f), mid = new Color(0.55f, 0.70f, 0.40f),
        soil = new Color(0.66f, 0.5f, 0.34f), grass = new Color(0.42f, 0.68f, 0.32f), stone = new Color(0.68f, 0.6f, 0.5f),
        plank = new Color(0.80f, 0.62f, 0.32f),
    };

    public static readonly LevelDef[] Levels =
    {
        new LevelDef
        {
            sceneName = "Zodiac_Level01", title = "Màn 1 — Làng Quê", subtitle = "Đường làng, luỹ tre và cổng làng",
            nextScene = "Zodiac_Level04", backdrop = Backdrop.Village,
            sky = new Color(0.98f, 0.88f, 0.70f), far = new Color(0.62f, 0.72f, 0.70f), mid = new Color(0.55f, 0.70f, 0.40f),
            soil = new Color(0.62f, 0.45f, 0.30f), grass = new Color(0.42f, 0.68f, 0.32f), stone = new Color(0.66f, 0.56f, 0.46f),
            plank = new Color(0.80f, 0.62f, 0.32f),
            stages = new[]
            {
                new Stage("Đầu làng", "W W", "W W W")
                {
                    sign = "{Move} đi · {Lane} đổi làn · {Sprint} chạy · {Jump} nhảy\n{Attack} đánh (3 đòn — đòn cuối quật ngã) · {Dodge} lăn né · {Skill} chiêu · {Ultimate} tuyệt kỹ",
                },
                new Stage("Giếng làng", "Q W", "W Q W")
                {
                    sign = "Lính Khiên đỡ đòn phía trước — vòng ra sau lưng hoặc đá bay ({Jump} rồi {Attack}).\nĐỡ 4 đòn là vỡ khiên!",
                    coinRows = 2, secret = true, secretTitle = "Giếng cạn",
                },
                new Stage("Bờ tre", "S W", "S W S")
                {
                    sign = "Bọ Gai rung người là sắp lăn thẳng một đường —\nđổi làn ({Lane}) hoặc nhảy qua, rồi đánh lúc nó choáng",
                    hazard = true,
                },
                new Stage("Cổng làng", "W Q W", "S W Q W") { coinRows = 2 },
            },
        },
        new LevelDef
        {
            sceneName = "Zodiac_Level04", title = "Màn 2 — Cánh Đồng Lúa", subtitle = "Lúa chín vàng, đống rơm và miếu thổ địa",
            nextScene = "Zodiac_Level02", backdrop = Backdrop.Field,
            sky = new Color(1.00f, 0.80f, 0.55f), far = new Color(0.76f, 0.68f, 0.55f), mid = new Color(0.72f, 0.64f, 0.30f),
            soil = new Color(0.58f, 0.42f, 0.26f), grass = new Color(0.78f, 0.68f, 0.28f), stone = new Color(0.70f, 0.60f, 0.46f),
            plank = new Color(0.82f, 0.64f, 0.34f),
            stages = new[]
            {
                new Stage("Bờ ruộng", "W W S", "W Q W")
                {
                    sign = "Lính đất nung giẫm nát cả ruộng lúa sắp gặt…\nĐập vỡ hũ sành, rương gỗ để kiếm thêm đồng và bình thuốc",
                },
                new Stage("Đống rơm", "S S W", "Q W S")
                {
                    sign = "Bụi tre rậm ở góc khu trông lạ lắm…\nĐánh vỡ nó, dọn sạch quái rồi đi vào lối sáng tím — khu bí mật!",
                    coinRows = 2, secret = true, secretTitle = "Hầm rơm",
                },
                new Stage("Cối giã gạo", "W W W Q", "S W S") { hazard = true },
                new Stage("Miếu thổ địa", "W Q", "!W")
                {
                    sign = "Quái trùm: to, trâu, có thanh máu riêng, không túm được.\nHạ nó là chắc chắn có mảnh Đan!",
                },
                new Stage("Đường đê", "S Q W W", "W S Q S") { coinRows = 2 },
            },
        },
        new LevelDef
        {
            sceneName = "Zodiac_Level02", title = "Màn 3 — Rừng Trúc", subtitle = "Sương mù, ma trơi và cóc tía",
            nextScene = "Zodiac_Level05", backdrop = Backdrop.Bamboo,
            sky = new Color(0.78f, 0.88f, 0.80f), far = new Color(0.55f, 0.70f, 0.60f), mid = new Color(0.35f, 0.55f, 0.38f),
            soil = new Color(0.45f, 0.38f, 0.28f), grass = new Color(0.34f, 0.58f, 0.34f), stone = new Color(0.50f, 0.50f, 0.44f),
            plank = new Color(0.72f, 0.75f, 0.38f),
            stages = new[]
            {
                new Stage("Bìa rừng", "F W", "F F W")
                {
                    sign = "Ma Trơi bay ngang tầm đầu — nhảy lên rồi {Attack} (đá bay),\nhoặc chờ nó sáng lên lao xuống thì chém",
                },
                new Stage("Suối trúc", "H W", "H H W")
                {
                    sign = "Cóc Tía: vòng đỏ dưới đất là chỗ nó sắp đáp —\nchạy ra khỏi vòng, hoặc đang nhảy lúc nó đáp",
                    coinRows = 2,
                },
                new Stage("Lối mòn sương", "S F W", "H Q F")
                {
                    hazard = true, secret = true, secretTitle = "Hang cáo cũ", secretWaves = new[] { "F F", "H" },
                },
                new Stage("Miếu hoang", "W W F", "H S Q F", "!H") { coinRows = 2 },
            },
        },
        new LevelDef
        {
            sceneName = "Zodiac_Level05", title = "Màn 4 — Đầm Sen", subtitle = "Đêm trên đầm, cầu tre và thuyền đắm",
            nextScene = "Zodiac_Level06", backdrop = Backdrop.Pond,
            sky = new Color(0.14f, 0.18f, 0.32f), far = new Color(0.20f, 0.28f, 0.38f), mid = new Color(0.18f, 0.36f, 0.34f),
            soil = new Color(0.32f, 0.32f, 0.28f), grass = new Color(0.26f, 0.48f, 0.38f), stone = new Color(0.42f, 0.46f, 0.48f),
            plank = new Color(0.56f, 0.58f, 0.38f),
            stages = new[]
            {
                new Stage("Bờ đầm", "F F W", "F H W"),
                new Stage("Cầu tre", "H H", "F H Q") { hazard = true },
                new Stage("Bãi sậy", "S F S", "H F W Q")
                {
                    coinRows = 2, secret = true, secretTitle = "Thuyền đắm", secretWaves = new[] { "F", "!F" },
                },
                new Stage("Đầm sen nở", "F F F", "H S Q W"),
                new Stage("Đình bỏ hoang", "W Q F", "H H S", "!Q") { coinRows = 2 },
                new Stage("Giữa đầm sâu")
                {
                    boss = true, bossKind = BossKind.ThuongLuong, coinRows = 0,
                    sign = "Thuồng Luồng: gợn sóng bám theo là nó sắp trồi lên — chạy khỏi vòng sáng.\nĐuôi rực là sắp quật sát đất — nhảy! Ngóc cổ là sắp phun — đổi làn.",
                },
            },
        },
        new LevelDef
        {
            sceneName = "Zodiac_Level06", title = "Màn 5 — Chợ Phiên Ma", subtitle = "Chợ đêm Ba Mươi vắng tanh, chỉ còn lính tuần",
            nextScene = "Zodiac_Level07", backdrop = Backdrop.Market,
            sky = new Color(0.56f, 0.40f, 0.56f), far = new Color(0.44f, 0.38f, 0.52f), mid = new Color(0.42f, 0.42f, 0.40f),
            soil = new Color(0.48f, 0.38f, 0.32f), grass = new Color(0.42f, 0.50f, 0.34f), stone = new Color(0.60f, 0.52f, 0.52f),
            plank = new Color(0.72f, 0.50f, 0.32f),
            stages = new[]
            {
                new Stage("Đầu chợ", "W W Q", "Q W W"),
                new Stage("Hàng vải", "Q Q W", "S W Q S") { hazard = true },
                new Stage("Sạp gốm", "S W S W", "F F H")
                {
                    coinRows = 2, secret = true, secretTitle = "Kho hàng", secretWaves = new[] { "Q W", "!W" },
                },
                new Stage("Giếng cổ", "F H W Q", "H S F Q"),
                new Stage("Cổng chợ", "W W Q", "S S F", "!S") { coinRows = 2 },
                new Stage("Sân đình chợ")
                {
                    boss = true, bossKind = BossKind.OngBaBi, coinRows = 0,
                    sign = "Ông Ba Bị: vòng đỏ là chỗ bao tải rơi — trúng là bị trùm bao!\nKhom người là sắp vồ — đổi làn. Than hồng hắt ra cháy một lúc, đi vòng.",
                },
            },
        },
        new LevelDef
        {
            sceneName = "Zodiac_Level07", title = "Màn 6 — Hang Đá Vôi", subtitle = "Nhũ đá, mạch nước ngầm và đền thờ đá",
            nextScene = "Zodiac_Level08", backdrop = Backdrop.Limestone,
            sky = new Color(0.30f, 0.22f, 0.16f), far = new Color(0.45f, 0.34f, 0.24f), mid = new Color(0.52f, 0.40f, 0.28f),
            soil = new Color(0.50f, 0.40f, 0.30f), grass = new Color(0.56f, 0.48f, 0.30f), stone = new Color(0.66f, 0.56f, 0.44f),
            plank = new Color(0.62f, 0.44f, 0.28f),
            stages = new[]
            {
                new Stage("Cửa hang", "T W", "W Q W")
                {
                    sign = "Nhũ đá trên trần rung lên là sắp rơi — vòng đỏ dưới chân báo chỗ rơi.\nLăn ra khỏi vòng, hoặc dụ quái đứng vào đó!",
                },
                new Stage("Nhũ đá", "H S T", "F W W") { hazard = true },
                new Stage("Mạch nước ngầm", "F F W Q", "S H F")
                {
                    coinRows = 2, secret = true, secretTitle = "Hang thạch nhũ", secretWaves = new[] { "S S", "!S" },
                },
                new Stage("Đền thờ đá", "T T Q W", "F H Q W"),
                new Stage("Bệ đá cổ", "T W W", "Q F H", "!W") { coinRows = 2 },
                new Stage("Hang Chằn Tinh")
                {
                    boss = true, bossKind = BossKind.ChanTinh, coinRows = 0,
                    sign = "Chằn Tinh: giơ chùy là sắp nện — lùi ra, nhảy qua sóng chấn.\nCào chân là sắp húc — đổi làn; nó đâm vách thì choáng, đánh ngay!",
                },
            },
        },
        new LevelDef
        {
            sceneName = "Zodiac_Level08", title = "Màn 7 — Bờ Hồ Tây", subtitle = "Đêm ven hồ, bãi lau và chùa cổ",
            nextScene = "Zodiac_Level03", backdrop = Backdrop.Lakeshore,
            sky = new Color(0.12f, 0.14f, 0.26f), far = new Color(0.18f, 0.24f, 0.36f), mid = new Color(0.22f, 0.32f, 0.32f),
            soil = new Color(0.36f, 0.32f, 0.30f), grass = new Color(0.28f, 0.42f, 0.34f), stone = new Color(0.46f, 0.46f, 0.52f),
            plank = new Color(0.58f, 0.46f, 0.30f),
            stages = new[]
            {
                new Stage("Đường ven hồ", "W Q F", "S W Q W"),
                new Stage("Bãi lau", "F F H", "S Q F W") { hazard = true },
                new Stage("Bến thuyền", "H W Q", "F F S H")
                {
                    coinRows = 2, secret = true, secretTitle = "Đáy thuyền rồng", secretWaves = new[] { "F F", "!H" },
                },
                new Stage("Chùa cổ ven hồ", "T Q W", "W W Q F", "!W"),
                new Stage("Cửa hồ", "Q Q W W", "S F H Q") { coinRows = 2 },
            },
        },
        new LevelDef
        {
            sceneName = "Zodiac_Level03", title = "Màn 8 — Động Hồ Tinh", subtitle = "Hang đá, tượng phun lửa và cáo chín đuôi",
            nextScene = "", backdrop = Backdrop.Cave,
            sky = new Color(0.16f, 0.12f, 0.22f), far = new Color(0.24f, 0.18f, 0.32f), mid = new Color(0.30f, 0.22f, 0.36f),
            soil = new Color(0.36f, 0.32f, 0.40f), grass = new Color(0.45f, 0.35f, 0.55f), stone = new Color(0.46f, 0.42f, 0.52f),
            plank = new Color(0.60f, 0.42f, 0.30f),
            stages = new[]
            {
                new Stage("Cửa động", "T W W", "Q F W")
                {
                    sign = "Thạch Sư phun lửa ngang các làn — đổi làn, nhảy, hoặc chém tan cầu lửa",
                },
                new Stage("Hang pha lê", "H S T", "W W Q F")
                {
                    hazard = true, coinRows = 2, secret = true, secretTitle = "Mạch pha lê", secretWaves = new[] { "Q W", "!Q" },
                },
                new Stage("Đền đá", "T T W Q", "F F H"),
                new Stage("Động Hồ Tinh")
                {
                    boss = true, coinRows = 0,
                    sign = "Hồ Tinh: đuôi sáng xanh là sắp ra đòn — đổi làn tránh cú vồ,\nchạy khỏi vòng xanh, nhảy qua sóng chấn",
                },
            },
        },
    };
}

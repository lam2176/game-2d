using UnityEngine;

/// <summary>
/// Dữ liệu 3 màn beat 'em up 2.5D. Mỗi màn là một con đường chạy dọc trục X (dài <see cref="LevelDef.length"/> unit),
/// rộng theo chiều sâu Z từ <see cref="LevelDef.laneMin"/> (sát camera) tới <see cref="LevelDef.laneMax"/> (trong cùng).
/// Sửa màn: đổi số liệu ở đây rồi chạy lại Tools/Zodiac/Dựng prototype.
///
/// Vùng giao tranh (<see cref="Encounter"/>): đi tới thì tường vô hình đóng lại, quái kéo tới theo từng đợt.
/// Mỗi đợt là một chuỗi mã quái cách nhau dấu cách — quái lần lượt chạy vào từ mép phải, mép trái, xen kẽ các làn:
///   W  Lính Đất Nung      Q  Lính Khiên (đỡ đòn phía trước)      S  Bọ Gai (lăn dọc làn)
///   F  Ma Trơi (bay)      H  Cóc Tía (nhảy đè)                    T  Thạch Sư (tượng phun lửa — đặt sẵn ở làn trong cùng)
/// Vùng có boss = true thì boss Hồ Tinh đứng chờ sẵn trong đấu trường.
///
/// Nhịp màn: dạy từng loại quái một (kèm biển hướng dẫn ngay trước), rồi trộn lại; đèn lồng sau mỗi 1–2 vùng.
/// </summary>
public static class ZodiacLevelData
{
    public enum Backdrop { Village, Bamboo, Cave }

    public class Encounter
    {
        /// <summary>Mép trái đấu trường (X) và bề ngang (vừa một khung hình ~16 unit).</summary>
        public float x, width = 16f;
        public string[] waves = System.Array.Empty<string>();
        public bool boss;

        public Encounter(float x, params string[] waves)
        {
            this.x = x;
            this.waves = waves;
        }
    }

    public class Sign
    {
        public float x;
        public string text;

        public Sign(float x, string text)
        {
            this.x = x;
            this.text = text;
        }
    }

    public class LevelDef
    {
        public string sceneName, title, subtitle, nextScene;
        public Backdrop backdrop;
        public Color sky, far, mid, soil, grass, stone, plank;
        public float length = 130f;
        public float laneMin = -3f, laneMax = 3f;
        public float startX = 3f, goalX = 125f;
        public Encounter[] encounters = System.Array.Empty<Encounter>();
        /// <summary>X các đèn lồng (đặt ở mép trong con đường).</summary>
        public float[] checkpoints = System.Array.Empty<float>();
        /// <summary>X các cụm 3 đồng xu (làn chọn ngẫu nhiên theo tên màn — dựng lại vẫn y nguyên).</summary>
        public float[] coins = System.Array.Empty<float>();
        /// <summary>X các bãi chông / than hồng (rộng 2.5 unit, chắn một nửa mặt đường — đi vòng hoặc nhảy qua).</summary>
        public float[] hazards = System.Array.Empty<float>();
        public Sign[] signs = System.Array.Empty<Sign>();
    }

    public static readonly LevelDef[] Levels =
    {
        new LevelDef
        {
            sceneName = "Zodiac_Level01", title = "Màn 1 — Làng Quê", subtitle = "Đường làng, luỹ tre và cổng làng",
            nextScene = "Zodiac_Level02", backdrop = Backdrop.Village,
            sky = new Color(0.98f, 0.88f, 0.70f), far = new Color(0.62f, 0.72f, 0.70f), mid = new Color(0.55f, 0.70f, 0.40f),
            soil = new Color(0.62f, 0.45f, 0.30f), grass = new Color(0.42f, 0.68f, 0.32f), stone = new Color(0.66f, 0.56f, 0.46f),
            plank = new Color(0.80f, 0.62f, 0.32f),
            length = 130f, goalX = 126f,
            signs = new[]
            {
                new Sign(7f, "← → đi · ↑ ↓ đổi làn · Shift chạy · Space nhảy\nQ đánh (3 đòn — đòn cuối quật ngã) · W lăn né · E chiêu · R tuyệt kỹ"),
                new Sign(36f, "Lính Khiên đỡ mọi đòn phía trước:\nđổi làn vòng ra sau lưng, hoặc nhảy lên rồi Q (đá bay)"),
                new Sign(66f, "Bọ Gai rung người là sắp lăn thẳng một đường —\nđổi làn (↑ ↓) hoặc nhảy qua, rồi đánh lúc nó choáng"),
                new Sign(93f, "Bãi chông! Nhảy qua hoặc đi vòng làn ngoài"),
            },
            encounters = new[]
            {
                new Encounter(14f, "W W", "W W W"),
                new Encounter(44f, "Q W", "W Q W"),
                new Encounter(72f, "S W", "S W S"),
                new Encounter(102f, "W Q W", "S W Q W") { width = 18f },
            },
            checkpoints = new[] { 40f, 97f },
            coins = new[] { 10f, 32f, 58f, 66f, 88f, 120f },
            hazards = new[] { 95f },
        },
        new LevelDef
        {
            sceneName = "Zodiac_Level02", title = "Màn 2 — Rừng Trúc", subtitle = "Sương mù, ma trơi và cóc tía",
            nextScene = "Zodiac_Level03", backdrop = Backdrop.Bamboo,
            sky = new Color(0.78f, 0.88f, 0.80f), far = new Color(0.55f, 0.70f, 0.60f), mid = new Color(0.35f, 0.55f, 0.38f),
            soil = new Color(0.45f, 0.38f, 0.28f), grass = new Color(0.34f, 0.58f, 0.34f), stone = new Color(0.50f, 0.50f, 0.44f),
            plank = new Color(0.72f, 0.75f, 0.38f),
            length = 140f, goalX = 136f,
            signs = new[]
            {
                new Sign(8f, "Ma Trơi bay ngang tầm đầu — nhảy lên rồi Q (đá bay),\nhoặc chờ nó sáng lên lao xuống thì chém"),
                new Sign(40f, "Cóc Tía: vòng đỏ dưới đất là chỗ nó sắp đáp —\nchạy ra khỏi vòng, hoặc đang nhảy lúc nó đáp"),
            },
            encounters = new[]
            {
                new Encounter(14f, "F W", "F F W"),
                new Encounter(46f, "H W", "H H W"),
                new Encounter(78f, "S F W", "H Q F"),
                new Encounter(110f, "W W F", "H S Q F") { width = 18f },
            },
            checkpoints = new[] { 42f, 104f },
            coins = new[] { 9f, 34f, 64f, 70f, 96f, 130f },
            hazards = new[] { 68f, 98f },
        },
        new LevelDef
        {
            sceneName = "Zodiac_Level03", title = "Màn 3 — Động Hồ Tinh", subtitle = "Hang đá, tượng phun lửa và cáo chín đuôi",
            nextScene = "", backdrop = Backdrop.Cave,
            sky = new Color(0.16f, 0.12f, 0.22f), far = new Color(0.24f, 0.18f, 0.32f), mid = new Color(0.30f, 0.22f, 0.36f),
            soil = new Color(0.36f, 0.32f, 0.40f), grass = new Color(0.45f, 0.35f, 0.55f), stone = new Color(0.46f, 0.42f, 0.52f),
            plank = new Color(0.60f, 0.42f, 0.30f),
            length = 130f, goalX = 127f,
            signs = new[]
            {
                new Sign(8f, "Thạch Sư phun lửa ngang các làn — đổi làn, nhảy, hoặc chém tan cầu lửa"),
                new Sign(95f, "Phía trước là động Hồ Tinh. Đèn lồng đã thắp — chết thì hồi sinh tại đây"),
            },
            encounters = new[]
            {
                new Encounter(14f, "T W W", "Q F W"),
                new Encounter(44f, "H S T", "W W Q F"),
                new Encounter(74f, "T T W Q", "F F H"),
                new Encounter(102f) { width = 22f, boss = true },
            },
            checkpoints = new[] { 40f, 97f },
            coins = new[] { 10f, 36f, 64f, 90f },
            hazards = new[] { 68f },
        },
    };
}

using UnityEngine;

/// <summary>
/// Bản đồ 3 màn dạng ký tự (mỗi ký tự = 1 ô 1×1 unit, dòng đầu là trên cùng; dòng ngắn tự đệm khoảng trắng).
/// Sửa bản đồ: đổi ký tự ở đây rồi chạy lại Tools/Zodiac/Dựng prototype.
///
///   #  đất (có cỏ khi bên trên trống)        B  đá / gạch (không cỏ)
///   =  bục tre một chiều (nhảy xuyên từ dưới)  ^  gai (mất 1 máu, về đèn lồng)
///   M  bục di động ngang, '-' phía sau = quãng đi (ô)   V  bục di động dọc, '|' phía trên = quãng đi
///   o  đồng xu      P  điểm xuất phát      K  đèn lồng (checkpoint)      G  cổng đích
///   i  biển hướng dẫn (lấy lần lượt từ mảng Hints)
///   W  Lính Đất Nung   S  Bọ Gai   F  Ma Trơi   H  Cóc Tía   T  Thạch Sư   X  boss Hồ Tinh
///   A  cổng đấu trường boss (đóng lại khi người chơi bước vào, đấu trường kéo tới hết bản đồ bên phải)
///
/// Quy ước thiết kế (độ cao nhảy mặc định 3.2 ô, chạy 8.5 ô/giây): bậc leo ≤ 3 ô, vực đi bộ ≤ 4 ô, vực phải chạy ≤ 6 ô.
/// Bản đồ được sinh bằng script rồi dán vào đây — nhưng sửa tay trực tiếp vẫn được.
/// </summary>
public static class ZodiacLevelData
{
    public enum Backdrop { Village, Bamboo, Cave }

    public class LevelDef
    {
        public string sceneName, title, subtitle, nextScene;
        public string[] rows, hints;
        public Backdrop backdrop;
        public Color sky, far, mid, soil, grass, stone, plank;
    }

    // 172 x 15
    static readonly string[] Level1 =
    {
        "",
        "",
        "",
        "",
        "                                           ooo                                                                                                              oo",
        "                                                                                    oo ooo",
        "                                          =====",
        "                                    ooo                      oo       ooo                                                                   F               BB",
        "                       oo   ooo                                                  BBBBB                       ooo             o o o                         BBB",
        "                                   =====                                        BBBBBB                                                                    BBBB",
        "         o o o         BB                                    ##                BBBBBBB                                                                   BBBBB",
        "   P  i            W   BB                W       i   S  K    ##     i    H    BBBBBBBB           W      W    ^^^    K                             H     BBBBBB      G",
        "############################   ###########################   ##   ####################################################### MMM------- #######################################",
        "############################   ###########################   ##   #######################################################            #######################################",
        "############################   ###########################   ##   #######################################################            #######################################",
    };
    static readonly string[] Level1Hints =
    {
        "A/D: đi · Shift: chạy · Space: nhảy (giữ lâu nhảy cao)\nJ: đánh · K: tung chiêu · Đạp lên đầu quái để hạ chúng!",
        "Bọ Gai có gai trên lưng — đừng đạp!\nHãy đánh (J) hoặc dùng chiêu (K).",
        "Mỗi con giáp có một chiêu riêng (K) và một nội tại.\nĐèn lồng: điểm hồi sinh + hồi 1 máu. Rơi vực chỉ mất 1 máu.",
    };
    // 196 x 26
    static readonly string[] Level2 =
    {
        "",
        "",
        "",
        "",
        "                                                                                                                                         F",
        "",
        "                                                                                                                                             oo",
        "",
        "                                                                                                                                            ====  #######",
        "                                                                                                                                                  #######",
        "            F                       oo            o o o                                                                                           #######",
        "                                                                                                                                       ====       #######",
        "                                                                                                                                                  #######",
        "                              W     ^^     K                   S                                                                                  #######",
        "               ====== ######################## MMM------ ##############                                                          ====       |     #######",
        "          ooo         ########################           ##############                                                                     |     #######",
        "                      ########################           ##############                                     F       F                       |     #######",
        "         =====        ########################                         ###            F                                               ====  |     #######",
        "                ooo   ########################                         ###                              ooo     ooo     ooo                 |     #######                ooo",
        "                      ########################                         ###                                                                  |     #######",
        "               =====  ########################                         ######                          =====   =====   =====    ====        |     #######         ooo   =====",
        "                      ########################                         ######       ooooo                                                   |     #######",
        "  P  i    H           ########################                         ######     H       H       K     ^^^     ^^^     ^^^       K         VVV   #######     K      W     S     H          G",
        "##############################################                         #############################################################################################################################",
        "##############################################                         #############################################################################################################################",
        "##############################################                         #############################################################################################################################",
    };
    static readonly string[] Level2Hints =
    {
        "Bục tre (=): nhảy xuyên từ dưới lên được.\nGiữ ↓ rồi bấm Space để rơi xuống qua bục.",
    };
    // 178 x 19
    static readonly string[] Level3 =
    {
        "BBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBB",
        "BBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBB",
        "BBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBB",
        "                                                                                                                                                                                BB",
        "                                                                                                                                                                                BB",
        "                                                                                                                                                                                BB",
        "                                                                            F                                                                                                   BB",
        "                                                                                                                                                                                BB",
        "                                                                                                                                                                                BB",
        "                                                                        oo    ooo                                     F                                                         BB",
        "                                                 o  o    o  o                                    oooo                                                                           BB",
        "                                          T                                  =====                                                                                              BB",
        "                    ooo                 BBBB                                                     BBBB                                          =====               =====        BB",
        "                                        BBBB                           ====                      BBBB  H                                                                        BB",
        "                                        BBBB           MMM---                                BBBBBBBBBBBBBB                                                                     BB",
        "  P  i         T             W       W  BBBB                    K   S   ^^^^^^^^^        T   BBBBBBBBBBBBBB     H           K  i        A                             X    G    BB",
        "BBBBBBBBBBBBBBBBBBB    BBBBBBBBBBBBBBBBBBBBBBB MMM---          BBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBB",
        "BBBBBBBBBBBBBBBBBBB    BBBBBBBBBBBBBBBBBBBBBBB                 BBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBB",
        "BBBBBBBBBBBBBBBBBBB    BBBBBBBBBBBBBBBBBBBBBBB                 BBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBB",
    };
    static readonly string[] Level3Hints =
    {
        "Thạch Sư phun lửa: nấp sau địa hình hoặc phá tượng.\nĐòn đánh thường (J) chém tan được cầu lửa!",
        "Hồ Tinh ở phía trước… Cổng sẽ đóng sau lưng bạn.\nQuan sát đuôi sáng lên để đoán đòn.",
    };

    public static readonly LevelDef[] Levels =
    {
        new LevelDef
        {
            sceneName = "Zodiac_Level01", title = "Màn 1 — Làng Quê", subtitle = "Đồng lúa, luỹ tre và cổng làng",
            nextScene = "Zodiac_Level02", rows = Level1, hints = Level1Hints, backdrop = Backdrop.Village,
            sky = new Color(0.98f, 0.88f, 0.70f), far = new Color(0.62f, 0.72f, 0.70f), mid = new Color(0.55f, 0.70f, 0.40f),
            soil = new Color(0.55f, 0.36f, 0.22f), grass = new Color(0.36f, 0.68f, 0.30f), stone = new Color(0.62f, 0.50f, 0.40f),
            plank = new Color(0.80f, 0.62f, 0.32f),
        },
        new LevelDef
        {
            sceneName = "Zodiac_Level02", title = "Màn 2 — Rừng Trúc", subtitle = "Sương mù, bục tre và ma trơi",
            nextScene = "Zodiac_Level03", rows = Level2, hints = Level2Hints, backdrop = Backdrop.Bamboo,
            sky = new Color(0.78f, 0.88f, 0.80f), far = new Color(0.55f, 0.70f, 0.60f), mid = new Color(0.35f, 0.55f, 0.38f),
            soil = new Color(0.36f, 0.30f, 0.22f), grass = new Color(0.30f, 0.58f, 0.32f), stone = new Color(0.45f, 0.45f, 0.40f),
            plank = new Color(0.72f, 0.75f, 0.38f),
        },
        new LevelDef
        {
            sceneName = "Zodiac_Level03", title = "Màn 3 — Động Hồ Tinh", subtitle = "Hang đá, tượng phun lửa và cáo chín đuôi",
            nextScene = "", rows = Level3, hints = Level3Hints, backdrop = Backdrop.Cave,
            sky = new Color(0.16f, 0.12f, 0.22f), far = new Color(0.24f, 0.18f, 0.32f), mid = new Color(0.30f, 0.22f, 0.36f),
            soil = new Color(0.30f, 0.26f, 0.32f), grass = new Color(0.45f, 0.35f, 0.55f), stone = new Color(0.42f, 0.38f, 0.48f),
            plank = new Color(0.60f, 0.42f, 0.30f),
        },
    };
}

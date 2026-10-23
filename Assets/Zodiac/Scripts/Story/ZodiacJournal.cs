using UnityEngine;

/// <summary>
/// Nội dung Sổ ghi chép: các loại quái (tên, nơi gặp, truyện, mẹo đánh) theo thứ tự xuất hiện. Quái nhận ra theo tên prefab
/// (bỏ "(Clone)") — số đã hạ lưu trong <see cref="PlayerProfile.kills"/>. Giao diện ở trang Sổ ghi chép của hành trang.
/// </summary>
public static class ZodiacJournal
{
    public class Beast
    {
        public string id, name, art, where, lore, tip;
    }

    public static readonly Beast[] Beasts =
    {
        new Beast
        {
            id = "Enemy_LinhDatNung", name = "Lính Đất Nung", art = "LinhDatNung", where = "Làng Quê · Cánh Đồng · Chợ Phiên…",
            lore = "Tượng lính nặn bằng đất sét đỏ trong lăng cổ. Đêm Ba Mươi, oán khí của Hồ Tinh thổi hồn vào, chúng thức dậy cầm giáo đi tuần như còn sống.",
            tip = "Đánh đủ 3 đòn để quật ngã. Lúc nó choáng thì túm ({Interact}) rồi ném vào cả đám.",
        },
        new Beast
        {
            id = "Enemy_LinhKhien", name = "Lính Khiên", art = "LinhKhien", where = "Làng Quê · Chợ Phiên · Bờ Hồ Tây…",
            lore = "Lính đất nung cầm khiên đồng — ngày trước canh cửa lăng, nay canh đường làng cho Hồ Tinh.",
            tip = "Khiên chặn mọi đòn phía trước. Vòng ra sau lưng ({Lane}), đá bay từ trên xuống ({Jump} rồi {Attack}), hoặc đánh vỡ khiên sau 4 đòn.",
        },
        new Beast
        {
            id = "Enemy_BoGaiLan", name = "Bọ Gai", art = "BoGai", where = "Cánh Đồng · Chợ Phiên · Hang Đá Vôi…",
            lore = "Con bọ vỏ đầy gai bò ra từ bờ tre. Cuộn tròn lại thì lăn như hòn đá lở.",
            tip = "Rung người là sắp lăn thẳng một làn — đổi làn hoặc nhảy qua, rồi đánh lúc nó nằm choáng.",
        },
        new Beast
        {
            id = "Enemy_MaTroi", name = "Ma Trơi", art = "MaTroi", where = "Rừng Trúc · Đầm Sen · Bờ Hồ Tây",
            lore = "Đốm lửa xanh lập loè giữa rừng trúc đêm. Người xưa bảo đó là hồn lạc chưa tìm được đường về.",
            tip = "Bay ngang tầm đầu: nhảy lên rồi {Attack} để đá bay, hoặc chờ nó sáng lên lao xuống thì chém.",
        },
        new Beast
        {
            id = "Enemy_CocTia", name = "Cóc Tía", art = "CocTia", where = "Rừng Trúc · Đầm Sen · Bờ Hồ Tây",
            lore = "Cóc tía trồi lên từ suối, to như cái nong. Mỗi cú nhảy làm đất rung cả khoảnh.",
            tip = "Vòng đỏ dưới đất là chỗ nó sắp đáp — chạy khỏi vòng, hoặc đang nhảy đúng lúc nó đáp.",
        },
        new Beast
        {
            id = "Enemy_ThachSu", name = "Thạch Sư", art = "ThachSu", where = "Hang Đá Vôi · Bờ Hồ Tây · Động Hồ Tinh",
            lore = "Sư tử đá canh cửa đền, bị yểm bùa nên phun lửa vào bất cứ ai đi qua.",
            tip = "Đứng yên một chỗ, không quật ngã được. Đổi làn hoặc nhảy tránh lửa, chém tan cầu lửa rồi áp sát đánh.",
        },
        new Beast
        {
            id = "Boss_ThuongLuong", name = "Thuồng Luồng", art = "-", where = "Đầm Sen",
            lore = "Rắn nước khổng lồ có sừng trong truyện cổ, nằm dưới đáy đầm sâu, hay lật thuyền người qua. Hồ Tinh đánh thức nó bằng tiếng trống vỡ.",
            tip = "Gợn sóng bám theo là sắp trồi lên — chạy khỏi vòng. Trồi xong nó nằm phơi mình: đánh lúc đó. Đuôi rực sáng thì {Jump}.",
        },
        new Beast
        {
            id = "Boss_OngBaBi", name = "Ông Ba Bị", art = "-", where = "Chợ Phiên Ma",
            lore = "\"Ông ba bị chín quai mười hai con mắt\" — lão vác bao tải đi bắt trẻ hư trong lời doạ của bà. Đêm nay lão có thật.",
            tip = "Vòng đỏ là chỗ bao tải rơi. Khom người là sắp vồ — đổi làn ({Lane}). Biến mất trong khói là sắp hiện sau lưng — {Dodge} ngay.",
        },
        new Beast
        {
            id = "Boss_ChanTinh", name = "Chằn Tinh", art = "-", where = "Hang Đá Vôi",
            lore = "Yêu tinh khổng lồ trong truyện Thạch Sanh, từng bắt dân làng nộp người hằng năm. Hồ Tinh hứa trả lại cho nó cả một năm không có ban ngày.",
            tip = "Giơ chùy là sắp nện — lùi ra rồi nhảy qua sóng chấn. Để nó húc vào vách: nó choáng, tha hồ đánh.",
        },
        new Beast
        {
            id = "Boss_HoTinh", name = "Hồ Tinh", art = "", where = "Động Hồ Tinh (dưới Hồ Tây)",
            lore = "Cáo chín đuôi bị Lạc Long Quân đánh chìm dưới đáy Hồ Tây từ ngàn năm trước. Sống lại nhờ oán khí, nó đập vỡ Trống Đồng Thiên Âm thành mười hai Âm Phù để năm cũ không bao giờ hết.",
            tip = "Đuôi sáng xanh là sắp ra đòn: đổi làn tránh cú vồ, chạy khỏi vòng xanh, nhảy qua sóng chấn.",
        },
    };

    /// <summary>Id sổ ghi chép của một quái: tên object bỏ "(Clone)" và số thứ tự " (1)".</summary>
    public static string IdOf(GameObject go)
    {
        string n = go.name;
        int cut = n.IndexOf('(');
        if (cut > 0) n = n.Substring(0, cut);
        return n.Trim();
    }

    /// <summary>Ảnh của quái: ảnh vẽ trong Art/Enemies (qua <see cref="ZodiacResources.enemyArt"/>); Hồ Tinh dùng hình vẽ bằng code.</summary>
    public static Sprite Art(Beast b)
    {
        if (string.IsNullOrEmpty(b.art)) return StoryArt.Icon(StoryArt.Kind.Fox);
        if (b.art == "-") return null;   // boss dựng bằng hình khối, chưa có ảnh vẽ
        var res = ZodiacResources.Get;
        if (res == null || res.enemyArt == null) return null;
        foreach (var s in res.enemyArt)
            if (s != null && s.name == b.art) return s;
        return null;
    }
}

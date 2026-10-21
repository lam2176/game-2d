using System.Collections.Generic;

/// <summary>
/// Nội dung cốt truyện Hồi 1 (xem docs/COT_TRUYEN.md): tranh mở đầu, thoại khi vào khu, lời Hồ Tinh,
/// kết thúc riêng 12 con giáp và kết thúc thật. Chỉ là dữ liệu — <see cref="StoryUi"/> hiển thị,
/// <see cref="ZodiacGameManager"/> gọi đúng lúc, <see cref="PlayerProfile"/> nhớ đoạn nào đã xem.
///
/// Người nói: <see cref="Me"/> = con giáp đang chơi, <see cref="Elder"/> = Già làng, <see cref="Fox"/> = Hồ Tinh,
/// <see cref="Narrator"/> = lời kể (không chân dung), hoặc tên asset của một con giáp (vd "Cat_Mao").
/// Dòng có <see cref="Line.only"/> chỉ hiện khi đang chơi con đó.
/// </summary>
public static class ZodiacStory
{
    public const string Me = "@me", Elder = "@elder", Fox = "@fox", Narrator = "@narr";

    public struct Line
    {
        public string speaker, text, only;
        public Line(string speaker, string text, string only = "")
        {
            this.speaker = speaker;
            this.text = text;
            this.only = only;
        }
    }

    /// <summary>Một khung tranh kể chuyện: tiêu đề, lời kể, hình minh hoạ (<see cref="StoryArt"/>).</summary>
    public struct Panel
    {
        public string title, text;
        public StoryArt.Kind art;
        /// <summary>Con giáp vẽ trong khung (rỗng = theo art; "@me" = con đang chơi; "@all" = cả 12).</summary>
        public string figure;
        public Panel(string title, string text, StoryArt.Kind art, string figure = "")
        {
            this.title = title;
            this.text = text;
            this.art = art;
            this.figure = figure;
        }
    }

    static Line L(string speaker, string text, string only = "") => new Line(speaker, text, only);

    // ───────── Tranh mở đầu ─────────

    public static readonly Panel[] Intro =
    {
        new Panel("Khúc Giao Thừa",
            "Mỗi năm, đúng khắc Giao Thừa, mười hai con giáp cùng tấu Khúc Giao Thừa trên mười hai nhạc cụ, trao năm cũ cho năm mới.",
            StoryArt.Kind.None, "@all"),
        new Panel("Trống Đồng Thiên Âm",
            "Bản nhạc ấy được giữ trong Trống Đồng Thiên Âm — mặt khắc mặt trời và chim Lạc, đặt ở đình làng Trung Tâm.",
            StoryArt.Kind.Drum),
        new Panel("Cô gái áo đỏ",
            "Đêm Ba Mươi, một cô gái áo đỏ bước vào đình. Đó là Hồ Tinh — con cáo chín đuôi bị Lạc Long Quân đánh chìm dưới đáy Hồ Tây ngàn năm trước.",
            StoryArt.Kind.Fox),
        new Panel("Mười hai Âm Phù",
            "Nó đập vỡ trống đồng thành mười hai Âm Phù, rải khắp nơi. Thời gian đứng lại ở khắc cuối của năm cũ. Lính đất nung trong lăng cổ thức dậy, ma trơi bay khỏi rừng trúc.",
            StoryArt.Kind.Shards),
        new Panel("Lên đường",
            "Nhạc cụ của mười hai con giáp đều câm lặng. Tìm lại đủ mười hai Âm Phù thì Khúc Giao Thừa mới vang lên — và năm mới mới đến được.",
            StoryArt.Kind.None, "@me"),
    };

    // ───────── Thoại khi vào khu: khoá "L{màn}-R{khu}" (đếm từ 0) ─────────

    static readonly Dictionary<string, Line[]> rooms = new Dictionary<string, Line[]>
    {
        ["L0-R0"] = new[]
        {
            L(Elder, "Đêm Ba Mươi mà trống đồng vỡ rồi, con ơi… Mười hai Âm Phù văng khắp nơi."),
            L(Me, "Cả làng bảo thấy một bóng nhỏ chạy khỏi đình… Không phải tôi! Tôi sẽ tìm ra kẻ đổ tội.", "Rat_Ty"),
            L(Me, "Chiêng câm thì sáng mai ai gọi làng ra đồng? Tôi đi.", "Ox_Suu"),
            L(Me, "Một con cáo dám vào làng của chúa sơn lâm? Để ta xử nó!", "Tiger_Dan"),
            L(Me, "Không có tiếng nhị, đêm nay trẻ con sẽ gặp ác mộng… Ta đi.", "Cat_Mao"),
            L(Me, "Phong ấn của tổ tiên bị phá trong đời ta. Đó là việc của ta.", "Dragon_Thin"),
            L(Me, "Ai cũng nhìn ta như kẻ đồng loã… Được thôi. Ta sẽ tìm kẻ mạo danh.", "Snake_Ty"),
            L(Me, "Phải chạy báo cho mọi làng: năm mới đang bị đánh cắp!", "Horse_Ngo"),
            L(Me, "Người làng bị ma trơi hớp hồn… Tiếng tỳ bà của tôi gọi họ về được. Tôi… tôi sẽ cố.", "Goat_Mui"),
            L(Me, "Đi tìm Âm Phù à? Tề Thiên Đại Thánh đây nhận việc! Cả làng sẽ nhớ tên ta.", "Monkey_Than"),
            L(Me, "Lần đầu tiên trong đời ta không gáy được. Không có bình minh nếu không có ta.", "Rooster_Dau"),
            L(Me, "Ta đã bảo mà! Mùi cáo trong đình từ chiều! Không ai tin… Theo dấu thôi!", "Dog_Tuat"),
            L(Me, "Quái phá mất nồi bánh chưng của làng rồi! Tết mà không có bánh chưng thì không được!", "Pig_Hoi"),
            L(Elder, "Lính đất nung trong lăng cổ thức dậy cả rồi. Cẩn thận, đánh vỡ khiên của chúng trước!"),
        },
        ["L0-R2"] = new[]
        {
            L(Me, "Dấu chân cáo trên bờ tre… nó đi về phía cổng làng."),
            L(Me, "Mùi cáo! Mỗi lúc một gần hơn.", "Dog_Tuat"),
        },
        ["L0-R3"] = new[]
        {
            L(Me, "Có tiếng ngân rất khẽ ở cổng làng… Âm Phù đầu tiên ở đây!"),
            L(Fox, "Hi hi… nhanh chân đấy. Nhưng lính của ta đông lắm."),
        },
        ["L1-R0"] = new[]
        {
            L(Me, "Sương dày quá… ma trơi bay khắp rừng trúc."),
            L(Me, "Những đốm lửa này… là hồn người làng. Mình phải gọi họ về.", "Goat_Mui"),
        },
        ["L1-R1"] = new[]
        {
            L("Cat_Mao", "Hừ. Lại là ngươi. Lần trước ngươi \"quên\" gọi ta dậy, nhớ không?", "Rat_Ty"),
            L(Me, "Ta… ta xin lỗi mà. Đánh cùng nhau một trận rồi tính, được không?", "Rat_Ty"),
            L("Rat_Ty", "Ơ… Mèo! Ngươi cũng ở đây à? Đừng nhìn ta như thế…", "Cat_Mao"),
            L(Me, "Ta không giận vì thua. Ta giận vì bị bỏ lại. Thôi, đánh đã.", "Cat_Mao"),
            L(Me, "Suối trúc trơn lắm. Cụ ơi, bám chắc vào lưng tôi.", "Ox_Suu"),
        },
        ["L1-R3"] = new[]
        {
            L(Fox, "Cứ đi tiếp đi, con giáp nhỏ. Động của ta dưới Hồ Tây đang chờ."),
            L(Me, "Một mình cũng được. Mình làm được mà.", "Goat_Mui"),
            L(Fox, "Này chú khỉ… đổi chũm chọe lấy một pháp bảo thật sự không? Người ta sẽ nhớ tên chú mãi.", "Monkey_Than"),
            L(Me, "…Nghe hay đấy. Nhưng thôi. Bạn bè ta còn đang chờ tiếng chũm chọe này.", "Monkey_Than"),
        },
        ["L2-R0"] = new[]
        {
            L(Me, "Hơi lạnh từ đáy Hồ Tây… đây là động của nó."),
            L(Me, "Phong ấn của tổ tiên… vỡ thật rồi.", "Dragon_Thin"),
            L(Me, "Không được để nến tắt trước khi tới đáy động. Chạy!", "Horse_Ngo"),
        },
        ["L2-R2"] = new[]
        {
            L(Me, "Một mình ta là đủ! Không cần ai giúp!", "Tiger_Dan"),
            L(Me, "Đền đá… cứ đi trong bóng tối, chúng sẽ không thấy ta.", "Snake_Ty"),
            L(Me, "Ở trên mái đình, Chó và ta đã giữ làng qua một đêm. Đêm nay cũng vậy.", "Rooster_Dau"),
            L(Me, "Có mấy đứa trẻ lạc vào đây… Ta ngửi thấy chúng ở gần!", "Dog_Tuat"),
        },
        ["L2-R3"] = new[]
        {
            L(Fox, "Ngàn năm dưới đáy hồ tối om… Các ngươi có biết bóng tối lạnh thế nào không?"),
            L(Fox, "Tổ tiên ngươi dìm ta xuống đây vì ta muốn được nhìn mặt trời. Ngươi có biết điều đó không, cháu Lạc Long Quân?", "Dragon_Thin"),
            L(Me, "…Ta không biết. Nhưng năm mới không thể chết theo nỗi hận của ngươi.", "Dragon_Thin"),
            L(Fox, "Hoá ra con rắn kia không phải ta. Buồn cười nhỉ — chúng nghi ngươi trước cả nghi ta.", "Snake_Ty"),
            L(Me, "Ngươi mượn hình ta để vào đình. Hôm nay ta lấy lại.", "Snake_Ty"),
            L(Fox, "Đêm nay sẽ không bao giờ sáng!"),
        },
    };

    /// <summary>Thoại khi vào khu (đã lọc theo con đang chơi). Không có thì mảng rỗng.</summary>
    public static Line[] Room(int level, int room, string character) => Filter(rooms.TryGetValue($"L{level}-R{room}", out var l) ? l : null, character);

    // ───────── Thắng màn ─────────

    static readonly Line[][] levelWins =
    {
        new[] { L(Narrator, "Âm Phù thứ nhất cất tiếng — một nhạc cụ đã tìm lại được giọng của mình."), L(Me, "Còn mười một… Đi tiếp vào rừng trúc!") },
        new[] { L(Narrator, "Âm Phù thứ hai ngân lên giữa miếu hoang. Sương rừng trúc tan dần."), L(Me, "Dấu cáo dẫn xuống Hồ Tây.") },
        new[]
        {
            L(Fox, "Ba đuôi… ta mất ba đuôi rồi. Nhưng ta vẫn còn sáu đuôi nữa, con giáp ạ."),
            L(Fox, "Năm mới của các ngươi… chưa chắc đã đến đâu."),
            L(Narrator, "Âm Phù thứ ba sáng lên trong tay. Hồ Tinh tan vào làn nước đen, bỏ chạy."),
        },
    };

    public static Line[] LevelWin(int level, string character) =>
        Filter(level >= 0 && level < levelWins.Length ? levelWins[level] : null, character);

    // ───────── Kết thúc ─────────

    static readonly Dictionary<string, Panel[]> endings = new Dictionary<string, Panel[]>
    {
        ["Rat_Ty"] = new[]
        {
            new Panel("Lời xin lỗi đầu năm", "Chuột mang giải nhất cuộc đua năm xưa trả lại cho Trâu, rồi đứng giữa sân đình xin lỗi Mèo trước cả làng.", StoryArt.Kind.None, "Rat_Ty"),
            new Panel("Năm Tý", "Từ đó, năm Tý luôn mở đầu bằng một lời xin lỗi — và tiếng đàn nguyệt lanh lảnh khắp xóm.", StoryArt.Kind.Village, "Rat_Ty"),
        },
        ["Ox_Suu"] = new[]
        {
            new Panel("Luống cày đầu năm", "Trâu về làng, cày luống đầu tiên của năm mới rồi gõ chiêng khai hạ.", StoryArt.Kind.Field, "Ox_Suu"),
            new Panel("Năm Sửu", "Không cần ai cảm ơn. Tiếng chiêng vang khắp đồng, và cả làng lại ra ruộng.", StoryArt.Kind.Village, "Ox_Suu"),
        },
        ["Tiger_Dan"] = new[]
        {
            new Panel("Hội đấu vật", "Hổ đánh trống mở hội đấu vật đầu xuân. Mãnh hổ nan địch quần hồ — Hổ đã học được điều đó ở Đền đá.", StoryArt.Kind.Village, "Tiger_Dan"),
            new Panel("Năm Dần", "Lần đầu tiên, chúa sơn lâm nhường giải nhất cho một đứa trẻ trong làng.", StoryArt.Kind.None, "Tiger_Dan"),
        },
        ["Cat_Mao"] = new[]
        {
            new Panel("Tiếng nhị ru", "Mèo kéo nhị ru cả làng ngủ yên đêm đầu năm. Không đứa trẻ nào gặp ác mộng.", StoryArt.Kind.Night, "Cat_Mao"),
            new Panel("Năm Mão", "Chuột ngồi cạnh nghe, lần đầu tiên không nói câu nào.", StoryArt.Kind.Night, "Cat_Mao"),
        },
        ["Dragon_Thin"] = new[]
        {
            new Panel("Ngọn đèn dưới đáy hồ", "Rồng không giết Hồ Tinh. Rồng dựng lại phong ấn của tổ tiên — kèm một ngọn đèn, để nó không phải ở trong bóng tối nữa.", StoryArt.Kind.Lake, "Dragon_Thin"),
            new Panel("Năm Thìn", "Tiếng đàn tranh ngân trên mặt Hồ Tây. Có lẽ một ngày nào đó, ngọn đèn ấy sẽ đủ.", StoryArt.Kind.Lake, "Dragon_Thin"),
        },
        ["Snake_Ty"] = new[]
        {
            new Panel("Áo choàng bóng đêm", "Rắn trả áo choàng bóng đêm cho đình, làm tấm che cho ngọn đèn thờ.", StoryArt.Kind.Village, "Snake_Ty"),
            new Panel("Năm Tỵ", "Lần đầu tiên, làng mời Rắn ở lại ăn Tết.", StoryArt.Kind.Village, "Snake_Ty"),
        },
        ["Horse_Ngo"] = new[]
        {
            new Panel("Tin vui ngàn dặm", "Ngựa phi qua mọi làng báo tin năm mới đã đến. Tiếng đàn tam rộn khắp đường.", StoryArt.Kind.Field, "Horse_Ngo"),
            new Panel("Năm Ngọ", "Đến đâu, pháo nổ đến đó.", StoryArt.Kind.Village, "Horse_Ngo"),
        },
        ["Goat_Mui"] = new[]
        {
            new Panel("Hồn về", "Tiếng tỳ bà gọi những người bị ma trơi hớp hồn trở về. Dê đã một mình đi qua rừng trúc.", StoryArt.Kind.Night, "Goat_Mui"),
            new Panel("Năm Mùi", "Dê mở lớp dạy nhạc cho trẻ con trong làng — và không còn nhút nhát nữa.", StoryArt.Kind.Village, "Goat_Mui"),
        },
        ["Monkey_Than"] = new[]
        {
            new Panel("Múa lân", "Khỉ diễn trò múa lân mở hội đầu xuân, chũm chọe xoảng xoảng.", StoryArt.Kind.Village, "Monkey_Than"),
            new Panel("Năm Thân", "Lần này không phải để được khen — mà để cả làng cùng vui.", StoryArt.Kind.Village, "Monkey_Than"),
        },
        ["Rooster_Dau"] = new[]
        {
            new Panel("Tiếng gáy đầu năm", "Trên mái đình, Gà thổi kèn rồi cất tiếng gáy đầu tiên của năm mới.", StoryArt.Kind.Dawn, "Rooster_Dau"),
            new Panel("Năm Dậu", "Mặt trời mọc trên Hồ Tây. Thời gian lại trôi.", StoryArt.Kind.Dawn, "Rooster_Dau"),
        },
        ["Dog_Tuat"] = new[]
        {
            new Panel("Đưa trẻ về nhà", "Chó đưa từng đứa trẻ lạc trong rừng trúc về tận cửa nhà.", StoryArt.Kind.Night, "Dog_Tuat"),
            new Panel("Năm Tuất", "Đêm đầu năm, Chó nằm canh cổng làng. Lần này, ai cũng tin cái mũi của nó.", StoryArt.Kind.Village, "Dog_Tuat"),
        },
        ["Pig_Hoi"] = new[]
        {
            new Panel("Nồi bánh chưng", "Heo nấu nồi bánh chưng lớn nhất lịch sử làng.", StoryArt.Kind.Village, "Pig_Hoi"),
            new Panel("Năm Hợi", "Bên bếp lửa đêm Giao Thừa, tiếng sáo trúc vi vu. Ai cũng được một miếng.", StoryArt.Kind.Night, "Pig_Hoi"),
        },
    };

    static readonly Panel[] endingCommon =
    {
        new Panel("Năm mới đến muộn một khắc",
            "Hồ Tinh đã bị đánh lui. Nhưng một tiếng nhạc không đủ cho cả Khúc Giao Thừa — năm mới đến muộn một khắc.",
            StoryArt.Kind.Dawn, "@me"),
    };

    /// <summary>Kết thúc riêng của con giáp (mở đầu bằng khung chung).</summary>
    public static Panel[] Ending(string character)
    {
        var list = new List<Panel>(endingCommon);
        if (endings.TryGetValue(character, out var own)) list.AddRange(own);
        list.Add(new Panel("Còn sáu đuôi", "Phá đảo với cả mười hai con giáp để nghe trọn Khúc Giao Thừa.", StoryArt.Kind.Shards));
        return list.ToArray();
    }

    public static readonly Panel[] TrueEnding =
    {
        new Panel("Khúc Giao Thừa", "Mười hai con giáp đứng giữa sân đình. Mười hai nhạc cụ cùng cất tiếng — Khúc Giao Thừa vang lên trọn vẹn.", StoryArt.Kind.None, "@all"),
        new Panel("Bình minh", "Gà gáy. Ánh bình minh đầu năm chiếu xuống tận đáy động — lần đầu tiên sau ngàn năm, Hồ Tinh thấy mặt trời.", StoryArt.Kind.Dawn),
        new Panel("Hồ Tây", "Hồ Tinh được phong ấn lại dưới Hồ Tây, có ngọn đèn của Rồng bên cạnh. Nước hồ trong trở lại.", StoryArt.Kind.Lake),
        new Panel("Chúc mừng năm mới", "Pháo hoa, bánh chưng, câu đối đỏ. Năm mới đã đến.", StoryArt.Kind.Village, "@all"),
    };

    static Line[] Filter(Line[] lines, string character)
    {
        if (lines == null) return System.Array.Empty<Line>();
        var list = new List<Line>();
        foreach (var l in lines)
            if (string.IsNullOrEmpty(l.only) || l.only == character) list.Add(l);
        return list.ToArray();
    }
}

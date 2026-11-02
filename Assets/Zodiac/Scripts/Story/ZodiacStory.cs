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
        // ───── Màn 2 — Cánh Đồng Lúa ─────
        ["L1-R0"] = new[]
        {
            L(Me, "Lúa chín vàng sắp gặt mà lính đất nung giẫm nát cả rồi…"),
            L(Me, "Ruộng của làng! Ai giẫm lên luống cày của ta thì đừng hòng đi tiếp.", "Ox_Suu"),
            L(Me, "Đồng lúa thế này mà không ai được ăn Tết… không được!", "Pig_Hoi"),
        },
        ["L1-R1"] = new[]
        {
            L(Me, "Đống rơm này… có gió lùa ra từ phía sau. Lạ thật."),
            L(Me, "Ta ngửi thấy có gì đó giấu sau bụi tre kia!", "Dog_Tuat"),
        },
        ["L1-R3"] = new[]
        {
            L(Fox, "Đội trưởng của ta canh miếu thổ địa. Hắn không dễ chịu như lũ lính đâu."),
            L(Me, "Thổ địa ơi, cho con mượn sân miếu một lát."),
        },
        // ───── Màn 4 — Đầm Sen ─────
        ["L3-R0"] = new[]
        {
            L(Me, "Trời tối hẳn rồi… đầm sen lập loè toàn ma trơi."),
            L(Me, "Tiếng tỳ bà của tôi… có lẽ gọi được những đốm lửa này về.", "Goat_Mui"),
            L(Me, "Đêm tối thế này, mắt mèo nhìn rõ hơn ai hết.", "Cat_Mao"),
        },
        ["L3-R2"] = new[]
        {
            L(Me, "Có ánh sáng tím hắt ra từ bãi sậy… như có chiếc thuyền đắm ở đó."),
            L(Me, "Thuyền rồng! Ngày xưa tổ tiên ta đi thuyền này.", "Dragon_Thin"),
        },
        ["L3-R4"] = new[]
        {
            L(Fox, "Sen tàn cả rồi, con giáp nhỏ. Năm cũ cũng sẽ không bao giờ tàn."),
            L(Me, "Năm cũ phải qua thì năm mới mới tới. Ai cũng biết điều đó."),
        },
        ["L3-R5"] = new[]
        {
            L(Me, "Mặt đầm sủi bọt… có cái gì rất to đang bơi dưới kia."),
            L(Fox, "Thuồng Luồng ngủ dưới đầm này trăm năm rồi. Ta vừa gọi nó dậy giúp ngươi đấy."),
            L(Me, "Trâu ta lội nước cả đời, sợ gì một con rắn!", "Ox_Suu"),
            L(Me, "Rồng còn chưa sợ, huống chi thuồng luồng.", "Dragon_Thin"),
        },
        // ───── Màn 5 — Chợ Phiên Ma ─────
        ["L4-R0"] = new[]
        {
            L(Me, "Chợ phiên đêm Ba Mươi mà vắng tanh… không một tiếng rao."),
            L(Me, "Không ai bán lá dong, bánh chưng của ta biết gói bằng gì!", "Pig_Hoi"),
            L(Me, "Phải phi thật nhanh báo cho các làng: chợ Tết bị chiếm rồi!", "Horse_Ngo"),
            L(Me, "Chợ vắng thế này… dễ trộm lắm. Ơ, không, ta chỉ nói thế thôi!", "Rat_Ty"),
        },
        ["L4-R2"] = new[]
        {
            L(Me, "Sạp gốm đổ ngổn ngang… sau đống hàng có cánh cửa kho."),
            L(Me, "Kho hàng! Chắc chắn có thứ gì quý giá. Tề Thiên đi trước!", "Monkey_Than"),
        },
        ["L4-R4"] = new[]
        {
            L(Fox, "Nghe thấy không? Tiếng trống đồng đang yếu dần… như tim một kẻ sắp chết."),
            L(Me, "Vậy thì phải nhanh lên."),
        },
        ["L4-R5"] = new[]
        {
            L(Narrator, "Giữa sân đình chợ, một lão già gù lưng vác bao tải. Mười hai con mắt cùng mở ra một lúc."),
            L(Me, "Ông… Ông Ba Bị? Bà bảo ông chỉ có trong chuyện thôi mà!"),
            L(Me, "Ta là chuột, đâu phải trẻ con! …Có phải không?", "Rat_Ty"),
            L(Me, "Cái bao đó đựng đầy lá dong của chợ! Trả đây!", "Pig_Hoi"),
        },
        // ───── Màn 6 — Hang Đá Vôi ─────
        ["L5-R0"] = new[]
        {
            L(Me, "Nước nhỏ giọt từ nhũ đá… thạch sư canh khắp lối vào."),
            L(Me, "Hang tối thì cứ đi sát vách. Ta quen rồi.", "Snake_Ty"),
        },
        ["L5-R5"] = new[]
        {
            L(Narrator, "Hang rung lên từng nhịp. Một bóng khổng lồ vác chùy đá bước ra từ bóng tối."),
            L(Me, "Chằn Tinh… con yêu tinh trong chuyện Thạch Sanh!"),
            L(Me, "Một mình ta là đủ cho mi!", "Tiger_Dan"),
            L(Me, "Mi to thật đấy. Nhưng to thì chậm.", "Monkey_Than"),
        },
        ["L5-R3"] = new[]
        {
            L(Me, "Bức chạm trên vách… một người cưỡi rồng đuổi con cáo chín đuôi."),
            L(Me, "Lạc Long Quân… tổ tiên ta. Ngài đã đánh nó ở chính nơi này.", "Dragon_Thin"),
            L(Me, "Gáy một tiếng thật to cho cả hang biết ta tới!", "Rooster_Dau"),
        },
        // ───── Màn 7 — Bờ Hồ Tây ─────
        ["L6-R0"] = new[]
        {
            L(Me, "Hồ Tây đây rồi. Mặt nước đen như mực, không một gợn sóng."),
            L(Me, "Mùi cáo nồng nặc! Nó ở ngay dưới mặt hồ!", "Dog_Tuat"),
            L(Me, "Chúa sơn lâm không sợ nước. Ừ… không sợ lắm.", "Tiger_Dan"),
        },
        ["L6-R3"] = new[]
        {
            L(Me, "Chùa cổ ven hồ… chuông chùa cũng câm lặng rồi."),
            L(Me, "Tiếng chiêng của ta sẽ thay chuông chùa đêm nay.", "Ox_Suu"),
        },
        ["L6-R4"] = new[]
        {
            L(Fox, "Xuống đây đi. Động của ta chờ các ngươi lâu lắm rồi."),
        },
        ["L2-R0"] = new[]
        {
            L(Me, "Sương dày quá… ma trơi bay khắp rừng trúc."),
            L(Me, "Những đốm lửa này… là hồn người làng. Mình phải gọi họ về.", "Goat_Mui"),
        },
        ["L2-R1"] = new[]
        {
            L("Cat_Mao", "Hừ. Lại là ngươi. Lần trước ngươi \"quên\" gọi ta dậy, nhớ không?", "Rat_Ty"),
            L(Me, "Ta… ta xin lỗi mà. Đánh cùng nhau một trận rồi tính, được không?", "Rat_Ty"),
            L("Rat_Ty", "Ơ… Mèo! Ngươi cũng ở đây à? Đừng nhìn ta như thế…", "Cat_Mao"),
            L(Me, "Ta không giận vì thua. Ta giận vì bị bỏ lại. Thôi, đánh đã.", "Cat_Mao"),
            L(Me, "Suối trúc trơn lắm. Cụ ơi, bám chắc vào lưng tôi.", "Ox_Suu"),
        },
        ["L2-R3"] = new[]
        {
            L(Fox, "Cứ đi tiếp đi, con giáp nhỏ. Động của ta dưới Hồ Tây đang chờ."),
            L(Me, "Một mình cũng được. Mình làm được mà.", "Goat_Mui"),
            L(Fox, "Này chú khỉ… đổi chũm chọe lấy một pháp bảo thật sự không? Người ta sẽ nhớ tên chú mãi.", "Monkey_Than"),
            L(Me, "…Nghe hay đấy. Nhưng thôi. Bạn bè ta còn đang chờ tiếng chũm chọe này.", "Monkey_Than"),
        },
        // ───── Hồi 2 — Kinh thành Thăng Long (màn 9–11 = chỉ số 8–10) ─────
        ["L8-R0"] = new[]
        {
            L(Narrator, "Hồi 2 — Kinh thành Thăng Long. Hồ Tinh bỏ chạy ngược sông Hồng, mang theo sáu đuôi và những Âm Phù cuối cùng."),
            L(Narrator, "Kinh thành vẫn đứng ở khắc cuối của năm cũ. Đèn lồng phố cổ đỏ rực, nhưng không một bóng người."),
            L(Me, "Mùi cáo lẫn trong mùi trầm hương… nó trốn đâu đó giữa ba mươi sáu phố phường."),
            L(Me, "Phố đông, ngõ nhỏ, chỗ nào cũng có ngách để luồn. Ta thích rồi đấy!", "Rat_Ty"),
            L(Me, "Kinh thành này tổ tiên ta dựng lên — rồng bay lên từ đây. Không để nó vấy bẩn được.", "Dragon_Thin"),
        },
        ["L8-R2"] = new[]
        {
            L(Fox, "Bạc, vàng, ngọc… người kinh thành giữ của kỹ thật. Nhưng có giữ được năm cũ không?"),
            L(Me, "Ánh bạc này lấp lánh quá… Không! Tập trung nào.", "Monkey_Than"),
        },
        ["L8-R4"] = new[]
        {
            L(Me, "Chợ Đồng Xuân… lính đất nung đứng chật cả gian hàng."),
            L(Me, "Chợ Tết mà không ai bán bánh chưng thì buồn lắm.", "Pig_Hoi"),
        },
        ["L9-R0"] = new[]
        {
            L(Me, "Hoàng Thành. Tường đỏ, ngói vàng — nơi vua ngự ngày xưa."),
            L(Narrator, "Ánh ban mai không tới. Bầu trời đứng yên như một bức tranh cũ."),
            L(Me, "Đường thẳng tắp tới cổng thành. Phi một mạch thôi!", "Horse_Ngo"),
        },
        ["L9-R2"] = new[]
        {
            L(Fox, "Điện Kính Thiên… nơi các ngươi tôn vinh kẻ đã dìm ta xuống đáy hồ."),
            L(Me, "Lạc Long Quân không dìm ngươi vì ghét ngươi. Ngươi đã bắt người làng ăn thịt, Hồ Tinh.", "Dragon_Thin"),
            L(Me, "Lại định đổ tội cho rắn à? Lần này ta đi trước, ngươi theo sau.", "Snake_Ty"),
        },
        ["L9-R4"] = new[]
        {
            L(Me, "Từ lầu canh thấy rõ cổng Đoan Môn… có thứ gì to lớn đang đứng chắn ở đó."),
            L(Me, "To thì sao? Ta là chúa sơn lâm! …Nhưng lần này ta không đi một mình nữa.", "Tiger_Dan"),
        },
        ["L10-R0"] = new[]
        {
            L(Me, "Cổng Đoan Môn. Mặt đất rung lên từng nhịp như trống trận."),
            L(Fox, "Nhớ con chằn tinh trong hang đá chứ? Ta mượn xác nó một chút thôi."),
        },
        ["L10-R2"] = new[]
        {
            L(Me, "Sân rồng… mười hai bậc đá, mười hai con giáp."),
            L(Me, "Trời sắp sáng chưa? Cổ ta ngứa lắm rồi, chỉ chờ được gáy thôi.", "Rooster_Dau"),
        },
        ["L10-R3"] = new[]
        {
            L(Fox, "Đánh đi, con giáp nhỏ. Mỗi đòn vào nó là một đòn vào kẻ từng bị Thạch Sanh tha mạng."),
            L(Me, "Ra khỏi xác nó đi, Hồ Tinh! Đừng nấp sau lưng kẻ khác nữa."),
            L(Fox, "Hi hi… vậy thì nghiền nát ta xem!"),
        },
        // ───── Hồi 3 — Biển Đông và Long Cung (màn 12–14 = chỉ số 11–13) ─────
        ["L11-R0"] = new[]
        {
            L(Narrator, "Hồi 3 — Biển Đông. Sông Hồng đổ ra biển ở Cửa Thần Phù, nơi người xưa đặt tên cho những con sóng dữ."),
            L(Narrator, "Thuyền thúng úp mạn trên cát, lưới còn phơi trên cọc. Cả làng chài đã trốn vào bờ."),
            L(Me, "Mùi cáo lẫn trong gió mặn… Nó đi về phía biển thật rồi."),
            L(Me, "Biển… quê nội của ta. Mẹ Âu Cơ lên núi, cha Lạc Long Quân xuống biển — năm mươi người con theo cha.", "Dragon_Thin"),
            L(Me, "Ta không thích nước đâu nhé! …Nhưng thôi, đi thì đi.", "Cat_Mao"),
        },
        ["L11-R2"] = new[]
        {
            L(Fox, "Dân chài sợ sóng, sợ gió, sợ cả bóng tối ngoài khơi. Ta chỉ thổi thêm một chút thôi."),
            L(Me, "Làng không người, nhưng bếp còn ấm. Họ sẽ về ăn Tết — ta hứa.", "Dog_Tuat"),
            L(Me, "Lưới còn nguyên cá khô… không được ăn vụng, không được ăn vụng…", "Pig_Hoi"),
        },
        ["L11-R3"] = new[]
        {
            L(Narrator, "Đền Cá Ông — nơi ngư dân thờ cá voi đã bao đời cứu thuyền gặp nạn."),
            L(Me, "Bát hương nguội lạnh. Ông Nam Hải ơi, cho con mượn chút gió thuận."),
        },
        ["L12-R0"] = new[]
        {
            L(Narrator, "Hoàng hôn đỏ như lửa trên bãi đá ngầm. Ngọn hải đăng ngoài mũi đá tối om."),
            L(Me, "Mặt trời lặn mà không có ai gáy tiễn. Buồn thật.", "Rooster_Dau"),
            L(Me, "Ghềnh đá trơn — bước chậm, chắc chân, như trâu cày ruộng.", "Ox_Suu"),
        },
        ["L12-R2"] = new[]
        {
            L(Fox, "Hải đăng ư? Thuyền nào lạc thì cứ lạc. Tối hơn một chút, Biển Đông mới đẹp."),
            L(Me, "Ngươi dập tắt cả đèn dẫn đường cho người đi biển? Ta sẽ thắp lại!"),
            L(Me, "Leo lên hải đăng thì cứ để ta. Trèo cao là nghề của khỉ!", "Monkey_Than"),
        },
        ["L12-R3"] = new[]
        {
            L(Me, "Dưới mũi đá… nước sâu xanh thẳm, có ánh ngọc le lói tận đáy."),
            L(Me, "Long Cung. Ta cảm thấy… tổ tiên đang gọi.", "Dragon_Thin"),
        },
        ["L13-R0"] = new[]
        {
            L(Narrator, "Long Cung dưới đáy Biển Đông. Ngói ngọc, cột san hô, ngọc trai to bằng cái nồi đồng."),
            L(Narrator, "Không một bóng quan quân thuỷ phủ. Chỉ có Ma Trơi biển lượn lờ như đàn sứa."),
            L(Me, "Cha Lạc Long Quân từng ngự ở đây… Ta về rồi.", "Dragon_Thin"),
            L(Me, "Ở dưới nước mà vẫn thở được? Chắc nhờ phép của Long Vương.", "Snake_Ty"),
        },
        ["L13-R1"] = new[]
        {
            L(Fox, "Đẹp không? Long Vương già đi chơi xa rồi. Giờ Long Cung là của ta."),
            L(Me, "Hồ Tinh! Ngươi đã làm gì Thuồng Luồng?"),
            L(Fox, "Ta chỉ hát cho nó nghe một bài ru… một bài rất dài."),
        },
        ["L13-R3"] = new[]
        {
            L(Narrator, "Trên ngai Long Vương, Thuồng Luồng Biển Đông cuộn mình, mắt đỏ rực bùa mê."),
            L(Me, "Tỉnh lại đi! Ngươi là thần giữ biển, không phải đầy tớ của Hồ Tinh!"),
            L(Me, "Ta là con cháu Lạc Long Quân. Nhìn ta mà nhớ lại đi!", "Dragon_Thin"),
            L(Fox, "Nó không nghe đâu. Cắn đi, rắn biển của ta!"),
        },
        // ───── Hồi 4 — Núi Tản Viên và Thiên Đình (màn 15–17 = chỉ số 14–16) ─────
        ["L14-R0"] = new[]
        {
            L(Narrator, "Hồi 4 — Núi Tản Viên. Ngọn núi của Sơn Tinh, nơi xưa kia nước dâng bao nhiêu thì núi cao bấy nhiêu."),
            L(Narrator, "Trên đỉnh núi, mây xoáy thành một con đường dẫn thẳng lên trời. Hồ Tinh đã đi lối ấy."),
            L(Me, "Khắc Giao Thừa sắp điểm. Nếu nó giữ được khắc ấy, năm cũ sẽ kéo dài mãi mãi."),
            L(Me, "Leo núi thì nhà Dê ta có nghề! Theo ta, bước nào chắc bước ấy.", "Goat_Mui"),
            L(Me, "Núi cao, đường dốc, vách đá… Hổ về rừng rồi!", "Tiger_Dan"),
        },
        ["L14-R2"] = new[]
        {
            L(Narrator, "Sau màn nước trắng của thác, có tiếng chiêng đồng vọng ra — xa xăm như từ thời Hùng Vương."),
            L(Me, "Tiếng chiêng của ta… không, của tổ tiên ta. Họ đang cổ vũ ta.", "Ox_Suu"),
        },
        ["L14-R3"] = new[]
        {
            L(Narrator, "Đền Hạ thờ Đức Thánh Tản. Hương vẫn cháy, như có ai vừa thắp."),
            L(Me, "Đức Thánh Tản ơi, cho con mượn sức núi một lần này thôi."),
            L(Fox, "Sơn Tinh ngủ rồi, con giáp nhỏ. Trên kia có bạn cũ của ta đang chờ."),
        },
        ["L15-R0"] = new[]
        {
            L(Me, "Gió trên này rít như tiếng sáo. Mây trôi ngang tầm mắt."),
            L(Me, "Gió to thế này… lông ta dựng hết cả lên.", "Rooster_Dau"),
        },
        ["L15-R2"] = new[]
        {
            L(Narrator, "Trên vách đá, lông vũ vàng rụng khắp nơi — mỗi chiếc dài bằng cả con người."),
            L(Me, "Đại Bàng Tinh… con chim đã quắp công chúa trong truyện Thạch Sanh."),
            L(Me, "Ta bé nhất, nhưng ta biết chim lớn sợ gì: sợ những thứ nó không bắt kịp!", "Rat_Ty"),
        },
        ["L15-R3"] = new[]
        {
            L(Narrator, "Một tiếng rít xé trời. Bóng cánh khổng lồ che kín đỉnh núi."),
            L(Fox, "Bạn cũ của ta đây. Thạch Sanh bắn nó một mũi tên — nó nhớ mãi. Hôm nay nó trả cho các ngươi."),
            L(Me, "Con đường lên trời ở ngay sau lưng nó. Không lùi được nữa!"),
        },
        ["L16-R0"] = new[]
        {
            L(Narrator, "Thiên Đình. Biển mây trải đến chân trời, cầu vồng bắc qua, đèn trời trôi lững lờ."),
            L(Narrator, "Nhưng điện ngọc lặng ngắt. Đồng hồ trời đứng yên ở khắc cuối cùng của năm cũ."),
            L(Me, "Đi trên mây mà chân vẫn chạm đất… lạ thật."),
            L(Me, "Trên này ăn gì nhỉ? Đào tiên chắc ngon lắm.", "Pig_Hoi"),
        },
        ["L16-R1"] = new[]
        {
            L(Fox, "Nam Thiên Môn mở cho ta, đóng cho các ngươi. Thiên binh đâu, giữ cổng!"),
            L(Me, "Thiên binh gì mà toàn tượng đất nung với ma trơi? Lại là trò của ngươi."),
            L(Me, "Ta trông nhà giỏi nhất. Không ai được đóng cổng trước mặt ta!", "Dog_Tuat"),
        },
        ["L16-R2"] = new[]
        {
            L(Me, "Vườn đào tiên… ba nghìn năm mới chín một lần."),
            L(Me, "Ta chỉ ngắm thôi! …Ừ thì một quả. Một quả thôi.", "Monkey_Than"),
        },
        ["L16-R3"] = new[]
        {
            L(Narrator, "Điện Lăng Tiêu. Hồ Tinh ngồi trên ngai ngọc, chín đuôi xoè như ngọn lửa xanh."),
            L(Fox, "Chín đuôi. Đủ cả rồi. Ngàn năm dưới đáy hồ, ta chỉ đợi một khắc này."),
            L(Fox, "Khi khắc Giao Thừa không bao giờ điểm, sẽ chẳng còn năm mới, chẳng còn con giáp nào thay ca. Chỉ còn ta."),
            L(Me, "Mười hai Âm Phù đã cất tiếng. Năm mới sẽ đến — dù ngươi có muốn hay không!"),
            L(Me, "Ngàn năm trước tổ tiên ta đã thắng ngươi một lần. Hôm nay ta làm lại.", "Dragon_Thin"),
            L(Me, "Ngươi đổ cho ta tội cắn người trong cuộc đua năm ấy. Hôm nay cả trời sẽ thấy ai mới là kẻ dối.", "Snake_Ty"),
        },
        ["L7-R0"] = new[]
        {
            L(Me, "Hơi lạnh từ đáy Hồ Tây… đây là động của nó."),
            L(Me, "Phong ấn của tổ tiên… vỡ thật rồi.", "Dragon_Thin"),
            L(Me, "Không được để nến tắt trước khi tới đáy động. Chạy!", "Horse_Ngo"),
        },
        ["L7-R2"] = new[]
        {
            L(Me, "Một mình ta là đủ! Không cần ai giúp!", "Tiger_Dan"),
            L(Me, "Đền đá… cứ đi trong bóng tối, chúng sẽ không thấy ta.", "Snake_Ty"),
            L(Me, "Ở trên mái đình, Chó và ta đã giữ làng qua một đêm. Đêm nay cũng vậy.", "Rooster_Dau"),
            L(Me, "Có mấy đứa trẻ lạc vào đây… Ta ngửi thấy chúng ở gần!", "Dog_Tuat"),
        },
        ["L7-R3"] = new[]
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
        new[] { L(Narrator, "Âm Phù thứ nhất cất tiếng — một nhạc cụ đã tìm lại được giọng của mình."), L(Me, "Còn mười một… Dấu cáo dẫn ra cánh đồng!") },
        new[] { L(Narrator, "Âm Phù thứ hai sáng lên giữa sân miếu thổ địa. Gió đồng thổi thơm mùi lúa."), L(Me, "Dấu chân cáo đi vào rừng trúc.") },
        new[] { L(Narrator, "Âm Phù thứ ba ngân lên giữa miếu hoang. Sương rừng trúc tan dần."), L(Me, "Có tiếng ếch nhái từ đầm sen phía trước…") },
        new[] { L(Narrator, "Thuồng Luồng chìm xuống đáy đầm, nhả ra Âm Phù thứ tư. Ma trơi tan như sương sớm."), L(Me, "Đi tiếp tới chợ phiên!") },
        new[] { L(Narrator, "Ông Ba Bị tan thành làn khói, bao tải rơi xuống — bên trong là Âm Phù thứ năm."), L(Me, "Dấu cáo dẫn vào núi đá vôi.") },
        new[] { L(Narrator, "Chằn Tinh đổ sập như một ngọn núi nhỏ. Âm Phù thứ sáu sáng rực giữa hang."), L(Me, "Mạch nước ngầm này chảy ra Hồ Tây… nó ở đó.") },
        new[] { L(Narrator, "Âm Phù thứ bảy. Mặt hồ bỗng gợn sóng — có gì đó đang thở dưới đáy."), L(Me, "Động Hồ Tinh. Đi thôi!") },
        new[]
        {
            L(Fox, "Ba đuôi… ta mất ba đuôi rồi. Nhưng ta vẫn còn sáu đuôi nữa, con giáp ạ."),
            L(Fox, "Năm mới của các ngươi… chưa chắc đã đến đâu."),
            L(Narrator, "Âm Phù thứ tám sáng lên trong tay. Hồ Tinh tan vào làn nước đen, bỏ chạy."),
        },
        new[] { L(Narrator, "Trong gian hàng cuối chợ Đồng Xuân, Âm Phù thứ chín rơi ra từ chiếc mõ gỗ của lính gác."), L(Me, "Dấu cáo dẫn về phía Hoàng Thành.") },
        new[] { L(Narrator, "Âm Phù thứ mười nằm dưới bậc thềm Điện Kính Thiên, sáng như một ngọn nến."), L(Me, "Cổng Đoan Môn… Âm Phù cuối của kinh thành ở đó.") },
        new[]
        {
            L(Fox, "Lại một đuôi nữa… Ta còn năm đuôi. Xác chằn tinh này cũng chẳng giữ được ta."),
            L(Fox, "Biển Đông rộng lắm, con giáp ạ. Xuống Long Cung mà tìm ta!"),
            L(Narrator, "Chằn Tinh đổ sập. Hồn cáo bay ra như làn khói đỏ về phía biển. Âm Phù thứ mười một sáng lên giữa cổng thành."),
        },
        new[]
        {
            L(Narrator, "Trên bàn thờ Cá Ông, một chiếc vảy rồng ánh vàng nằm giữa bát hương nguội — dấu Long Cung gửi lên bờ."),
            L(Me, "Dấu cáo đi dọc ghềnh đá, về phía mặt trời lặn."),
        },
        new[]
        {
            L(Narrator, "Ngọn hải đăng bừng sáng trở lại. Dưới mũi đá, nước biển rẽ đôi thành một lối bậc ngọc dẫn xuống sâu."),
            L(Me, "Lối xuống Long Cung! Hít một hơi thật sâu nào."),
        },
        new[]
        {
            L(Fox, "Lại mất một đuôi… Thuồng Luồng vô dụng! Trả Âm Phù đây!"),
            L(Narrator, "Thuồng Luồng tỉnh khỏi bùa mê, cúi đầu nhả ra Âm Phù thứ mười hai."),
            L(Fox, "Đủ mười hai thì đã sao? Ta sẽ gom lại đủ chín đuôi và giữ chặt khắc Giao Thừa trên Thiên Đình!"),
        },
        new[]
        {
            L(Narrator, "Hương trên Đền Hạ bỗng bốc cao thành một cột khói thẳng tắp, chỉ lên đỉnh núi."),
            L(Me, "Đức Thánh Tản chỉ đường rồi. Lên đỉnh thôi!"),
        },
        new[]
        {
            L(Narrator, "Đại Bàng Tinh rơi xuống vực mây, để lại chiếc lông vàng cứng như thép."),
            L(Narrator, "Mây trên đỉnh Tản Viên xoáy lại thành bậc thang trắng dẫn thẳng lên trời."),
            L(Me, "Thiên Đình. Trận cuối cùng."),
        },
        new[]
        {
            L(Fox, "Không… chín đuôi… ngàn năm…"),
            L(Narrator, "Chín đuôi lửa xanh tắt dần từng chiếc. Đồng hồ trời rung lên — kim nhích sang khắc Giao Thừa."),
            L(Narrator, "Mười hai Âm Phù bay lên, ghép lại thành Trống Đồng Thiên Âm. Tiếng trống đầu tiên của năm mới vang khắp ba cõi."),
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

    /// <summary>Kết Hồi 1 (thắng Hồ Tinh dưới đáy Hồ Tây) — chưa phải phá đảo.</summary>
    public static readonly Panel[] Act1Ending =
    {
        endingCommon[0],
        new Panel("Còn sáu đuôi",
            "Hồ Tinh để lại sáu chiếc đuôi rồi trốn lên kinh thành. Mười một Âm Phù vẫn nằm trong tay yêu quái.",
            StoryArt.Kind.Shards),
        new Panel("Hết Hồi 1",
            "Hồi 2 — Kinh thành Thăng Long — đang chờ phía trước. Về làng nghỉ chân, đập đồ rồi lên đường nhé!",
            StoryArt.Kind.Village),
    };

    /// <summary>Kết thúc riêng của con giáp — xem sau trận cuối Hồi 4.</summary>
    public static Panel[] Ending(string character)
    {
        var list = new List<Panel>();
        if (endings.TryGetValue(character, out var own)) list.AddRange(own);
        list.Add(new Panel("Mười hai canh giờ", "Phá đảo với cả mười hai con giáp để nghe trọn Khúc Giao Thừa.", StoryArt.Kind.Shards));
        return list.ToArray();
    }

    /// <summary>Kết Hồi 2 (thắng Cổng Đoan Môn).</summary>
    public static readonly Panel[] Act2Ending =
    {
        new Panel("Kinh thành thức giấc",
            "Âm Phù thứ mười một ngân lên. Chuông chùa Một Cột đổ một hồi dài — đèn lồng ba mươi sáu phố phường cùng sáng.",
            StoryArt.Kind.Village, "@me"),
        new Panel("Còn một Âm Phù",
            "Mười một nhạc cụ đã có lại tiếng. Âm Phù cuối cùng theo Hồ Tinh xuống tận Long Cung dưới Biển Đông.",
            StoryArt.Kind.Shards),
        new Panel("Hết Hồi 2",
            "Hồi 3 — Biển Đông và Long Cung — đang chờ phía trước. Về làng nghỉ chân, đập đồ, ghé chợ phiên và thử thách hằng ngày nhé!",
            StoryArt.Kind.Lake),
    };

    /// <summary>Kết Hồi 3 (thắng Thuồng Luồng ở Long Cung).</summary>
    public static readonly Panel[] Act3Ending =
    {
        new Panel("Long Cung bừng sáng",
            "Thuồng Luồng tỉnh lại, cúi đầu tạ lỗi. Ngọc trai khắp Long Cung sáng lên — hải đăng trên bờ cũng sáng theo.",
            StoryArt.Kind.Lake, "@me"),
        new Panel("Đủ mười hai Âm Phù",
            "Mười hai nhạc cụ đã có lại tiếng. Nhưng Hồ Tinh gom lại đủ chín đuôi, bay về núi Tản Viên — đường lên Thiên Đình.",
            StoryArt.Kind.Shards),
        new Panel("Hết Hồi 3",
            "Hồi 4 — Núi Tản Viên và Thiên Đình: trận cuối với Hồ Tinh Cửu Vĩ. Về làng chuẩn bị thật kỹ nhé!",
            StoryArt.Kind.Night),
    };

    /// <summary>Kết Hồi 4 — trận cuối (thắng Hồ Tinh Cửu Vĩ ở Thiên Đình).</summary>
    public static readonly Panel[] Act4Ending =
    {
        new Panel("Giao Thừa",
            "Trống Đồng Thiên Âm vang lên giữa điện Lăng Tiêu. Đồng hồ trời điểm khắc Giao Thừa — năm cũ qua, năm mới đến.",
            StoryArt.Kind.Drum, "@me"),
        new Panel("Hồ Tinh bị phong ấn",
            "Hồ Tinh hoá thành một con cáo nhỏ lông xám, bị phong ấn trở lại đáy Hồ Tây. Lần này, nước hồ trong vắt.",
            StoryArt.Kind.Lake),
        new Panel("Pháo hoa trên làng",
            "Dưới trần gian, pháo hoa nở rộ. Người làng ra sân đình, bánh chưng đã chín, câu đối đỏ dán trên mọi cửa.",
            StoryArt.Kind.Dawn, "@me"),
        new Panel("Hết Hồi 4",
            "Đã phá đảo trọn bốn hồi! Độ khó Ác Mộng đã mở.",
            StoryArt.Kind.Village),
    };

    /// <summary>Kết hồi theo số hồi.</summary>
    public static Panel[] ActEnding(int act) => act switch
    {
        1 => Act1Ending,
        2 => Act2Ending,
        3 => Act3Ending,
        4 => Act4Ending,
        _ => null,
    };

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

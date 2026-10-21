# 12 Con Giáp — Tài liệu thiết kế (prototype)

Beat 'em up 2.5D (kiểu *Streets of Rage*, *Castle Crashers*). Người chơi chọn 1 trong 12 con giáp, đánh qua 3 màn và hạ boss Hồ Tinh.
Cả 12 nhân vật đều cầm một nhạc cụ dân tộc, nên chiêu thức lấy âm thanh làm chủ đề: sóng âm, tiếng chiêng, tiếng kèn…

Thế giới 3D (Universal Renderer, Physics 3D): con đường chạy dọc trục X, các **làn** theo chiều sâu Z, nhảy theo trục Y.
Nhân vật và quái vẫn là ảnh phẳng nhưng nhận đèn thật, đổ bóng xuống đất, chìm trong sương mù theo khoảng cách
(shader `Zodiac/Sprite Lit 3D`), có bóng tròn dưới chân để canh làn.

Dựng lại toàn bộ: **Tools ▸ Zodiac ▸ Dựng prototype** (tạo 12 nhân vật, prefab quái, 4 scene, Build Settings).
Bấm Play ở `Zodiac_Start`.

**Chơi thử bản riêng (không cần mở Unity)**: tắt Unity Editor rồi bấm đúp `ChoiThu.bat` ở thư mục gốc
(hoặc chạy `.\tools\choi-thu.ps1` trong PowerShell) — build ra `Builds\Windows\12ConGiap.exe` rồi mở game trong cửa sổ 1600×900.
`-Rebuild`: dựng lại scene trước khi build (sau khi sửa bản đồ / chỉ số / builder) · `-SkipBuild`: mở lại bản cũ · `-NoRun`: chỉ build.
Lần build đầu lâu (biên dịch shader), các lần sau nhanh hơn. Đang mở Unity thì dùng menu **Tools ▸ Zodiac ▸ Xuất bản chơi thử (Windows)**.

## Điều khiển

Tay phải mũi tên, tay trái Space/Shift + QWER (như game hành động online):

| Phím | Hành động |
|---|---|
| ← → (hoặc A/D) | Đi dọc đường — giữ Shift để chạy |
| ↑ ↓ | Đổi làn (vào sâu / ra ngoài) — chậm hơn đi ngang |
| Space | Nhảy (giữ lâu nhảy cao) |
| **Q** (J, chuột trái) | Đánh — bấm liên tiếp ra combo 3 đòn, đòn cuối **quật ngã**; đang nhảy: **đá bay**; đang chạy: **đòn lao** |
| **W** (L, Ctrl trái, LB) | Lăn né theo hướng phím (cả chéo) — bất tử, xuyên quái và đạn |
| **E** (K, chuột phải, RB) | Chiêu riêng của con giáp (tốn Khí) |
| **R** (RT) | **Tuyệt kỹ** — cần đầy thanh Khí (100), tốn hết |
| **F** (Y) | Túm quái đang choáng (Q thúc gối, F / Q ném) · ở làng: dùng điểm gần đó |
| **1 / 2** | Dùng nhanh đồ đã gán (bình máu, bình Khí) |
| **I / Tab** | Hành trang · trang Tu luyện |
| **Esc** | Tạm dừng: tiếp tục, chơi lại, về màn hình chính, âm lượng, toàn màn hình |
| F5 | Chơi lại màn |

## Luật chung (tham khảo)

- **Canh làn**: đòn đánh chỉ trúng quái cùng làn (lệch tối đa ~0.5 unit). Né bằng cách đổi làn, lăn, hoặc nhảy.
- **Màn chia thành khu**: mỗi khu vừa khít một khung hình, camera đứng yên nên luôn thấy hết quái. Quái hiện ngay trong khu
  (vòng khói báo trước) theo từng đợt. Dọn xong hiện **ĐI TIẾP →** và cột sáng ở mép phải; đi vào là màn hình tối lại
  ("Đang tới khu tiếp theo…") rồi sang khu kế. Khu cuối có cổng đích (màn 3: boss).
- **Vây đánh có lượt** (*Sifu*, *Batman Arkham*): tối đa 2 quái tấn công cùng lúc, số còn lại đứng vòng ngoài chờ.
- **Quật ngã**: đòn kết combo, đá bay, đòn lao làm quái văng lên nằm đất (bất tử khi nằm); đánh trúng lúc còn đang bay thì tung hứng tiếp.
- **Quái không gây sát thương khi chạm** — mọi đòn của quái đều **có báo hiệu trước** (thân bẹp xuống, phát sáng, vòng đỏ dưới đất).
  Quái không bao giờ đứng đè lên người chơi (giữ khoảng cách tối thiểu).
- **Lính cầm đồ mất đồ**: Lính Khiên đỡ 4 đòn (hoặc còn nửa máu) thì vỡ khiên; Lính Đất Nung còn nửa máu thì văng giáo.
  Mất đồ thì thành lính thường: hết đỡ đòn, đánh yếu và ngắn tầm hơn nhưng nhanh hơn.
- **Đèn lồng = checkpoint**: mỗi khu có một đèn lồng ở đầu khu; thắp đèn hồi 1 tim (20 máu). Thua thì "Hồi sinh tại đèn lồng"
  bắt đầu lại từ đầu khu đang đánh, các khu trước coi như đã dọn.
- **Bãi chông / than hồng** chắn nửa mặt đường: đi vòng làn kia hoặc nhảy qua.

## 12 con giáp

Mỗi con có chỉ số riêng (máu, tốc độ, độ cao nhảy, sức đánh), 1 nội tại và 1 chiêu (phím E).
Số liệu nằm trong `Assets/Zodiac/Editor/ZodiacCharacterDefs.cs` và được chép vào asset `Assets/Zodiac/Characters/*.asset`; chỉnh trực tiếp trong Inspector cũng được.

| Con giáp | Nhạc cụ | Nội tại | Chiêu (E) | Hồi chiêu | Tham khảo |
|---|---|---|---|---|---|
| Tý — Chuột | Đàn nguyệt | **Lanh lẹ**: nhỏ, nhanh, nhảy cao hơn | **Độn Ảnh**: lướt ngang, bất tử, xuyên qua quái và đạn | 1s | Dash của *Celeste*, Shade Cloak (*Hollow Knight*) |
| Sửu — Trâu | Chiêng | **Da trâu**: 140 máu, gần như không bị đẩy lùi | **Chiêng Chấn Địa**: 2 sóng chấn chạy dọc mặt đất sang 2 bên, 40 sát thương + choáng | 3s | Sóng chấn của *Mega Man*, *Cuphead* |
| Dần — Hổ | Trống cơm | **Mãnh hổ**: đánh thường 40 sát thương | **Hổ Vồ**: vồ chéo lên trước, 60 sát thương, trúng thì nảy lên vồ tiếp | 2.5s | Dash Slash (*Hollow Knight*) |
| Mão — Mèo | Đàn nhị | **Chín mạng**: mỗi màn thoát chết 1 lần (còn 1 máu) | **Tiếng Nhị Xuyên Tâm**: tia âm bay thẳng xuyên mọi kẻ địch, 40 sát thương | 1.6s | Charge Shot (*Mega Man*) |
| Thìn — Rồng | Đàn tranh | **Thăng long**: nhảy cao nhất, 120 máu | **Long Hoả**: 5 cầu lửa toả hình quạt | 2.2s | Spread Shot (*Contra*, *Cuphead*) |
| Tỵ — Rắn | Áo choàng bóng đêm | **Nọc độc**: đòn thường gây độc thêm 40 sát thương | **Xà Ảnh**: tàng hình 3s (bất tử, quái mất dấu), đòn đầu tiên x3 | 6s | *Mark of the Ninja*, *Dead Cells* |
| Ngọ — Ngựa | Đàn tam | **Thiên lý mã**: chạy nhanh nhất (11 ô/s) | **Vó Ngựa Phi**: phi nước đại dài, húc văng quái, 40 sát thương | 2.5s | Spin Dash (*Sonic*) |
| Mùi — Dê | Tỳ bà | **Hiền hoà**: cứ 6 đồng xu hồi 20 máu | **Khúc Hồi Xuân**: đứng gảy đàn 0.8s để hồi 40 máu (bị đánh thì ngắt) | 8s | Focus (*Hollow Knight*) |
| Thân — Khỉ | Chũm chọe | **Tề Thiên**: nhảy đôi — đá bay liên hoàn | **Chũm Chọe Bay**: ném chũm chọe bay ra rồi quay về, chém cả đi lẫn về | 1.8s | Metal Blade (*Mega Man*), boomerang (*Zelda*) |
| Dậu — Gà | Kèn | **Cánh phượng**: giữ Space khi rơi để lượn | **Kèn Hiệu Bình Minh**: sóng âm tròn làm choáng mọi quái quanh mình 2s và thổi tan đạn | 5s | Howling Wraiths (*Hollow Knight*) |
| Tuất — Chó | Đàn đáy | **Mũi thính**: hút đồng xu trong bán kính 4.5 ô | **Khuyển Hống**: tiếng sủa phía trước, 40 sát thương, hất văng quái | 2s | Bash (*Ori*) |
| Hợi — Heo | Sáo trúc | **Phúc hậu**: 120 máu, đá bay gấp đôi sát thương | **Heo Dậm Đất**: bật lên rồi lao xuống, sóng chấn 60 sát thương + choáng | 3s | Ground Pound (*Mario*), Stone (*Kirby*) |

Ảnh nhân vật: cắt từ `Assets/Characters/drive-download-.../IMG_*.png` (ảnh gốc giữ nguyên), lật cho mọi con quay mặt sang phải,
pivot ở bàn chân → `Assets/Zodiac/Art/Characters/<Con>_<Chi>.png`.

## Quái

Lấy cảm hứng từ truyện cổ và đình chùa Việt Nam. Ảnh quái vẽ bằng AI (Figma AI) theo nét tranh Tết của 12 con giáp, tách nền bằng
**Tools ▸ Zodiac ▸ Tách nền ảnh quái** (ảnh gốc nền trắng đặt ở `Assets/Zodiac/Art/Enemies/Raw`, kết quả ở `Assets/Zodiac/Art/Enemies`).
Giáo và khiên là ảnh riêng để văng ra khi lính mất đồ. Boss Hồ Tinh chưa có ảnh (hết credit AI) — vẫn ghép hình khối.

| Quái | Mã đợt | Hành vi | Cách hạ | Tham khảo |
|---|---|---|---|---|
| **Lính Đất Nung** | `W` | Xáp lại đứng ngang hàng (cùng làn), bẹp người gồng đòn rồi lao đâm | Đổi làn khi nó gồng, đánh ngắt | Lính thường của *Streets of Rage* |
| **Lính Khiên** | `Q` | Như Lính Đất Nung nhưng cầm khiên đỡ mọi đòn phía trước, đánh đau (quật ngã) | Đổi làn vòng ra sau lưng, đá bay từ trên xuống, chiêu gây choáng | Lính khiên của *Dead Cells* |
| **Bọ Gai** | `S` | Bò về đúng làn người chơi, rung người rồi cuộn tròn lăn thẳng dọc đường; lăn xong choáng váng | Đổi làn / nhảy qua, đánh lúc choáng | Bowling của *Castle Crashers* |
| **Ma Trơi** | `F` | Bay ngang tầm đầu (đánh trên đất không với tới), sáng rực rồi lao chéo xuống | Đá bay; chém lúc nó lao xuống | Vengefly (*Hollow Knight*) |
| **Cóc Tía** | `H` | Vòng đỏ hiện dưới chân người chơi → nhảy đè xuống tạo sóng chấn tròn | Chạy ra khỏi vòng hoặc đang nhảy lúc nó đáp | *Hades*, *Castle Crashers* |
| **Thạch Sư** | `T` | Tượng đá đứng ở làn trong cùng, miệng nạp sáng rồi phun cầu lửa bay ngang các làn | Đổi làn, nhảy, chém tan cầu lửa; phá tượng | Tháp pháo (*Mega Man*) |
| **Hồ Tinh** (boss) | — | Cáo chín đuôi. Lao vồ dọc làn (dạt sang làn người chơi trước), nhảy đập (vòng báo chỗ đáp + sóng toả tròn sát đất), chùm lửa hình quạt quét các làn. Dưới 50% máu: nhanh hơn, sóng/lửa dày hơn, gọi thêm Ma Trơi | Né theo báo hiệu rồi phản công | Truyền thuyết Lạc Long Quân diệt Hồ Tinh |

## Vật phẩm & hành trang

- **I / Tab** (nút Select tay cầm) mở hành trang, game tạm dừng: 5 ô trang bị (Vũ khí · Giáp · Bùa · Ngọc · Ngọc), bảng chỉ số,
  túi 6 × 5 ô (viền màu theo độ hiếm: trắng Thường · lục Tốt · lam Hiếm · tím Sử thi · cam Huyền thoại). Bấm vào ô để xem;
  nút Trang bị / Tháo · Dùng · Gán phím 1 · Gán phím 2 · Vứt · Xếp gọn.
- **Phím 1 / 2**: dùng nhanh đồ đã gán (bắt đầu game có 3 Bình máu nhỏ ở phím 1). Thanh dùng nhanh + số đồng ở góc dưới trái.
- **Rơi đồ**: mỗi loại quái có bảng rơi riêng (nguyên liệu, bình thuốc, đôi khi trang bị) và đồng xu; boss rơi Đao rồng + Đuôi Hồ Tinh.
  Đồ nảy ra trên đất, có cột sáng màu độ hiếm, lại gần là tự hút vào túi. Giáo / khiên lính đánh rơi có 20% thành vật phẩm.
- Trang bị cộng chỉ số qua `CharacterStats` (Công, Giáp, Máu/Khí tối đa, Hồi Khí, Chí mạng). Nguyên liệu để dành cho thợ rèn ở làng (phần sau).
- Đồ, trang bị, phím dùng nhanh và tiền giữ qua các màn, lưu ở `zodiac_profile.json` (thư mục `Application.persistentDataPath`).
- Bảng vật phẩm: `Assets/Zodiac/Editor/ZodiacItemDefs.cs` (biểu tượng vẽ bằng code, giáo/khiên dùng ảnh quái).
  Bảng rơi đồ từng quái: `ZodiacEnemyFactory.cs`.

## Tiến trình (cấp, tu luyện, Đan, màn)

- **Màn hình chính:** Bắt đầu → bảng chọn màn (màn chưa mở bị khoá; thắng màn N mở màn N+1). **Chơi mới** xoá hồ sơ (có hỏi lại).
- **Cấp & XP riêng từng con giáp** (tối đa cấp 30): hạ quái được XP theo máu tối đa của quái (boss ×4). Lên cấp hồi đầy máu/Khí,
  được 3 điểm tu luyện. Cấp hiện ở góc dưới trái và trong bảng thông tin màn hình chính.
- **Tu luyện** (I → Tu luyện): Sinh lực +12 máu · Nội khí +6 Khí, +0.15 hồi Khí/giây · Sức mạnh +2 Công · Thân pháp +1% chí mạng, +3 giáp.
- **Huyết Đan / Khí Đan:** gom 3 mảnh (rơi từ Thạch Sư, Lính Khiên, Ma Trơi, boss) → luyện thành một viên, +20 máu / Khí tối đa vĩnh viễn
  cho con giáp đang chơi.
- **Đồ trang bị là từng món riêng:** có cấp đập (+0 → +10, mỗi cấp +8% chỉ số gốc — đập ở Lò rèn, giai đoạn B) và 0–3 dòng phụ ngẫu nhiên
  theo độ hiếm (Tốt 1 · Hiếm 2 · Sử thi trở lên 3). Đồ, tiền dùng chung cho 12 con; cấp và Đan riêng từng con.
- File lưu có trường `version`: file lưu cũ (v1) tự tách đồ trang bị đang chồng thành từng món.

## Làng Trung Tâm, Lò rèn, Tiệm thuốc

Màn hình chính → **Bắt đầu** vào thẳng **Làng Trung Tâm** (scene `Zodiac_Village`, không có quái). Thắng / thua đều có nút **Về làng**.
Đứng gần một điểm thì hiện "F: …", bấm **F** (tay cầm: nút Y / North):

| Điểm | Chức năng |
|---|---|
| **Bàn thờ tổ** | Mở trang Tu luyện (cộng điểm, luyện Đan) |
| **Tiệm thuốc bắc** | Mua bình thuốc, Ngọc may mắn, Bùa bảo hộ · Bán đồ (nửa giá, đồ đập cao bán đắt hơn) · **Luyện đan** từ nguyên liệu |
| **Lò rèn** | **Đập đồ** +0 → +10 (cả đồ đang mặc) · **Phân giải** đồ thừa ra nguyên liệu + đồng |
| **Cổng làng** | Chọn màn đã mở · chọn **độ khó** |

**Đập đồ** (`Forge`): tỉ lệ +1…+3 chắc chắn, rồi 85 · 70 · 55 · 40 · 30 · 20 · 12%. Mỗi lần hỏng liên tiếp +5% cho lần sau.
Hỏng khi lên +1…+5 giữ cấp; lên **+6 trở lên hỏng thì tụt 1 cấp** — trừ khi dùng **Bùa bảo hộ**. **Ngọc may mắn** +10%.
Tốn `10 × cấp mới` đồng + nguyên liệu theo ô đồ (vũ khí Mảnh gốm · giáp Vảy bọ gai · bùa/ngọc Lửa ma trơi), từ +6 thêm Đá thạch sư.
Mỗi cấp +8% chỉ số gốc của món (+10 = gấp đôi).

**Luyện đan** (`Shop.Recipes`): Bình máu lớn, 2 Bình Khí, Mảnh Huyết Đan, Mảnh Khí Đan, Ngọc may mắn — từ nguyên liệu quái rơi.

## Chiều sâu chiến đấu (giai đoạn C1)

**Độ khó** (`ZodiacDifficulty`, chọn ở Cổng làng, lưu trong hồ sơ):

| | Máu quái | Đòn quái | Tinh anh | Đồ rơi / xu | XP |
|---|---|---|---|---|---|
| Thường | ×1 | ×1 | 8% | ×1 | ×1 |
| Khó | ×1.5 | ×1.3 | 20% | ×1.25 | ×1.3 |
| Ác Mộng (mở khi phá đảo màn cuối) | ×2.2 | ×1.7 | 35% | ×1.6 | ×1.7 |

**Quái tinh anh** (`EliteAffix`): to hơn, vòng hào quang màu dưới chân, chữ "Tinh anh · …" trên đầu, máu ×2.2, XP và đồ rơi ×3,
40% rơi thêm Bùa bảo hộ / Ngọc may mắn / mảnh Đan. Một trong 5 đặc tính:
**Cuồng nộ** (nhanh ×1.45, đòn mạnh hơn, choáng ngắn) · **Giáp đá** (+80 giáp, khó đẩy lùi) · **Độc** (đòn trúng gây độc 3 lần) ·
**Hồi máu** (2 giây không bị đánh thì hồi 4%/giây) · **Phân thân** (chết tách 2 con nhỏ — phải hạ hết mới dọn xong khu).

**Chuỗi đòn & xếp hạng khu** (`ComboCounter`): mỗi đòn trúng quái cộng chuỗi (đứt sau 2.5 giây không trúng hoặc khi bị đánh),
hiện số lớn ở mép phải từ 3 đòn. Dọn xong khu thì chấm **S / A / B / C** theo thời gian (so với 6 + 3.5 giây mỗi quái),
máu mất và chuỗi dài nhất. **S**: +10 xu và một món hiếm (mảnh Đan, Ngọc may mắn, Bình máu lớn) · **A**: +5 xu.

**Hũ sành, rương gỗ** (`Breakable`): mỗi khu 2–3 hũ (2 đòn vỡ, rơi xu, đôi khi bình máu / lửa ma trơi), khu lẻ có rương
(4 lần sát thương, nhiều xu, bình máu / bình Khí / mảnh Đan / Ngọc may mắn). Không tính vào việc dọn khu.

**Túm & ném** (`PlayerGrab`): đứng sát một quái đang choáng (vừa trúng đòn, bị sóng âm…) bấm **F** (tay cầm: Y) để túm.
Đang túm: **Q** thúc gối (tối đa 3 lần), **Q** lần nữa hoặc **F** thì ném theo hướng đang nhìn (bấm ← / → để quay).
Quái bị ném mất 2× sát thương khi đập đất, ai nó đâm phải (quái khác, hũ, rương) cũng mất 2× và bị quật ngã.
Giữ quá 2.5 giây hoặc bị đánh trúng thì quái tuột ra. Không túm được boss, quái bay, Thạch Sư.

**Nội tại gom về một chỗ** (`PlayerPassives`): độc, đá bay, hút xu, hồi máu bằng xu (giờ tính cả xu quái rơi) đều đi qua đây.
Bốn con vốn có nội tại kiểu đi cảnh được thêm hiệu ứng chiến đấu:

| Con giáp | Nội tại | Thêm |
|---|---|---|
| Chuột | Lanh lẹ | Lăn hồi nhanh gấp đôi, +10% chí mạng |
| Rồng | Thăng long | Đòn kết combo phun thêm một cầu lửa nhỏ |
| Khỉ | Tề Thiên | Đá bay trúng quái hồi thêm 6 Khí |
| Gà | Cánh phượng | Đá bay trúng quái choáng thêm 0.6 giây |

## Bản đồ

Mỗi màn khai báo trong `Assets/Zodiac/Editor/ZodiacLevelData.cs` là một danh sách **khu**: tên khu, các đợt quái viết bằng mã
(ví dụ `"W Q W"`), biển hướng dẫn, bãi chông, số cụm đồng xu, có boss không. Khu rộng 15 unit (vừa khung hình 16:9),
các khu cách nhau 36 unit dọc con đường nên không nhìn thấy nhau. Sửa số liệu rồi dựng lại là xong.
Cảnh hai bên đường (nhà, tre, vách đá) sinh theo chủ đề với số ngẫu nhiên cố định theo tên màn.

Nhịp màn: mỗi loại quái mới có biển hướng dẫn ở đầu khu giới thiệu nó, rồi mới trộn với loại khác.

1. **Làng Quê** — Đầu làng (Lính Đất Nung) → Giếng làng (Lính Khiên) → Bờ tre (Bọ Gai, bãi chông) → Cổng làng (trộn, cổng đích).
2. **Rừng Trúc** — Bìa rừng (Ma Trơi) → Suối trúc (Cóc Tía) → Lối mòn sương (trộn, bãi chông) → Miếu hoang (trộn, cổng đích).
3. **Động Hồ Tinh** — Cửa động (Thạch Sư) → Hang pha lê (trộn) → Đền đá (trộn) → Động Hồ Tinh (**boss**: thanh máu boss trên HUD,
   hạ Hồ Tinh thì cổng đền hiện ra).

## Ghi chú kỹ thuật

- Vật lý 3D: người chơi `Rigidbody` + `CapsuleCollider` tự tính trọng lực; quái có collider bề dày theo làn, không va chạm với người chơi
  (sát thương do đòn đánh quét `Physics.OverlapBox`).
- Hình nhân vật/quái: Root (vật lý) → Facing (lật trái/phải, chứa hitbox) → Tilt (`Billboard`, ngả theo góc cúi camera) → Visual.
- Camera phối cảnh cố định góc cúi (`ZodiacCameraFollow`), khoá trong khu đang đánh (`ZodiacGameManager.EnterRoom`).
- Chiêu thức: `PlayerSkill` đọc `SkillConfig` của nhân vật (6 kiểu: Dash, Projectile, Burst, Heal, Stealth, GroundPound) —
  thêm chiêu mới chủ yếu là chỉnh thông số. Lướt theo hướng phím (cả chéo), đạn toả quạt theo chiều sâu.
- Quái kế thừa `EnemyBase` (bị đánh, quật ngã, lượt vây đánh, chết); mỗi loại chỉ viết AI trong `Think()`.
- Chỉ số người chơi đi qua `CharacterStats` (gốc + modifier của trang bị/nâng cấp); thang số trong `ZodiacBalance` (1 tim = 20 máu).
- Asset nhân vật đã có thì builder giữ nguyên chỉnh sửa tay. Muốn lấy lại mặc định: dùng **Tools ▸ Zodiac ▸ Ghi đè chỉ số 12 con giáp**.
  Prefab quái và scene được tạo lại mỗi lần dựng.

## Âm thanh & hình ảnh (đợt 1 — game feel)

- **Âm thanh hiệu ứng**: [Kenney.nl](https://kenney.nl) — gói *Impact Sounds*, *RPG Audio* — giấy phép **CC0** (dùng tự do, không cần ghi công)
  → `Assets/Zodiac/Audio/SFX`.
- **Tiếng nhạc cụ khi tung chiêu, nhạc nền ngũ cung** (5 bài: menu, làng, rừng trúc, hang, boss): **tổng hợp bằng code**
  (numpy: chiêng, kèn, đàn nhị, đàn tranh, sáo, trống…) → `Assets/Zodiac/Audio/Synth`, `Assets/Zodiac/Audio/Music`.
  Không vướng bản quyền; muốn thay bằng bản thu thật chỉ cần thay file cùng tên.
- **Ánh sáng**: URP **2D Renderer** + Light2D — đèn lồng chập chờn, cầu lửa và ma trơi tự phát sáng, hang tối có đèn quanh người.
- **Hậu kỳ**: bloom, vignette, chỉnh màu riêng từng màn (`Assets/Zodiac/Settings/Volume_*.asset`).
- **Game feel**: đứng hình khi trúng đòn (hitstop), rung camera theo trauma, bụi chân / tia lửa / mảnh vỡ (Particle System),
  màn mờ khi chuyển cảnh và hồi sinh, loé đỏ khi bị thương, trọng lực nhẹ ở đỉnh cú nhảy (apex hang).
- **Animation bằng code** (`CharacterAnimator`): nhịp bước khớp tốc độ, nghiêng theo gia tốc, co giãn khi nhảy/đáp,
  vung người theo đòn, lộn vòng khi nhảy đôi, chao khi lượn.
- **Chiến đấu**: combo 3 đòn (đòn 3 gấp đôi sát thương), ↑ + J chém lên, trên không ↓ + J chém xuống — trúng quái/gai thì nảy lên (pogo).

## Chiêu & quái (đợt 2)

- **Khí** (6 ô xanh dưới thanh máu, như Soul của *Hollow Knight*): vào màn đầy Khí; đánh trúng +1, đòn kết combo +2, đạp đầu +1;
  tự hồi 1 ô mỗi 6 giây. Mỗi chiêu tốn 2–4 Khí (cột "Khí" trong bảng dưới), vẫn có hồi chiêu ngắn.
  Ô chiêu trên HUD tối lại và hiện chữ "Khí" khi thiếu.

  | Chiêu | Khí | | Chiêu | Khí |
  |---|---|---|---|---|
  | Độn Ảnh (Chuột) | 2 | | Vó Ngựa Phi (Ngựa) | 3 |
  | Chiêng Chấn Địa (Trâu) | 3 | | Khúc Hồi Xuân (Dê) | 4 |
  | Hổ Vồ (Hổ) | 3 | | Chũm Chọe Bay (Khỉ) | 2 |
  | Tiếng Nhị Xuyên Tâm (Mèo) | 2 | | Kèn Hiệu Bình Minh (Gà) | 4 |
  | Long Hoả (Rồng) | 3 | | Khuyển Hống (Chó) | 2 |
  | Xà Ảnh (Rắn) | 4 | | Heo Dậm Đất (Heo) | 3 |

- **Lăn né** (mọi con giáp): lăn ~3.5 ô, bất tử, lộn một vòng; hồi 0.45 giây.
- **Lính Khiên** (`Q` trên bản đồ, màn 2–3): đỡ mọi đòn từ phía trước (tia lửa, tiếng "keng", người chơi bật lại).
  Hạ bằng cách lăn ra sau lưng, chém xuống / đạp từ trên đầu, hoặc làm choáng bằng chiêu (khiên hạ xuống). Cắn 2 máu.
- **Quái phản ứng rõ hơn**: dấu "!" vàng khi phát hiện người chơi, thân bẹp/nảy khi trúng đòn, chết thì văng lên lộn vòng rồi rơi.

## Đồ hoạ (đợt 3 — sửa hình bị mờ)

Nguyên nhân hình mờ và cách sửa:
- **Bloom** ngưỡng 0.85 làm cả bầu trời sáng loá → nâng ngưỡng lên 1.05 (chỉ đèn lồng, lửa — sáng hơn trắng — mới loá), giảm vignette.
- **Ảnh nhân vật** 1024px hiển thị chỉ ~250px mà không có mipmap → bật mipmap (lọc Kaiser, bias âm nhẹ), lọc Trilinear,
  nén chất lượng cao (BC7). Khỉ được cắt lại sát hình như 11 con kia (trước đây chỉ ~700px là hình con khỉ).
- **Khử răng cưa** MSAA 4x; camera zoom gần hơn (orthographic size 6 → 5.5) để nhân vật to và rõ nét vẽ hơn.

Texture vẽ bằng code (`ZodiacTextureFactory`, lưu ở `Assets/Zodiac/Art/Generated`), tô màu theo từng màn:
đất có vân và sỏi, gạch đá có mạch vữa/vết sứt, viền cỏ có ngọn (chân nhân vật lẫn trong cỏ), bục tre có đốt,
dãy núi và đồi có đường sống lưng tự nhiên + lớp mù chân núi, sương mù rừng trúc, vách hang xếp gạch, bầu trời chuyển màu.

## Thanh chiêu & Tuyệt kỹ (đợt 4)

- **HUD kiểu game online** (Ngọc Rồng…): góc trái trên là chân dung tròn + tên + máu + 6 ô Khí + nội tại;
  giữa đáy là 4 ô tròn **Q Đánh · W Lướt · E Chiêu · R Tuyệt kỹ** — biểu tượng, nhãn phím, số Khí cần,
  lớp tối quét tròn theo thời gian hồi, ô nảy lên khi bấm; ô R viền vàng nhấp nháy khi đầy Khí.
- **Tuyệt kỹ riêng** (giai đoạn C2, `PlayerSkill.Ultimates.cs`) — mỗi con một chiêu khác hẳn, không còn là bản phóng to
  của chiêu thường. Khi tung có cảnh cắt: dải đen chéo màn hình, chân dung trượt vào, tên tuyệt kỹ chữ lớn, chậm thời gian 0.75 giây.
  "Cả khu" = mọi quái còn sống trong khu đang đánh.

  | Con giáp | Tuyệt kỹ | Tác dụng |
  |---|---|---|
  | Chuột | Vạn Ảnh Thử | Chớp tới từng quái trong khu (tối đa 8) chém một nhát, nhát cuối quật ngã cả đám; bất tử |
  | Trâu | Đại Hồng Chung | Ba hồi chuông, ba vòng sóng chấn bán kính 6, hồi cuối quật ngã; +100 giáp 6 giây |
  | Hổ | Mãnh Hổ Hạ Sơn | Vồ tới giữa đám đông nhất (4× sát thương, quật ngã), gầm choáng cả khu; +30% công 8 giây |
  | Mèo | Cửu Mệnh Huyền Âm | Hồi 30% máu, +1 mạng giữ lại, cả khu choáng 2.5 giây |
  | Rồng | Long Thần Hoả | Đứng phun 22 cầu lửa xuyên thấu toả theo chiều sâu trong 1.6 giây |
  | Rắn | Vạn Xà Ẩn Hình | Cả khu nhiễm độc; quấn con khoẻ nhất hút máu 2 giây, hồi nửa lượng hút được |
  | Ngựa | Thiên Mã Hành Không | Phi qua lại hết khu ba lượt, lượt cuối quật ngã; bất tử khi phi |
  | Dê | Xuân Hồi Đại Địa | Hồi đầy máu, đẩy lùi quái quanh mình; +60 giáp và hồi máu dần 10 giây |
  | Khỉ | Tề Thiên Đại Thánh | 3 phân thân vàng tự đánh quái 8 giây, Khỉ vẫn đánh tiếp |
  | Gà | Kim Kê Báo Hiệu | Cả khu choáng 3.5 giây, xoá đạn địch, quái nhận thêm 50% sát thương 6 giây |
  | Chó | Thiên Cẩu Hống | Quật ngã cả khu (2.5× sát thương); +25% công 8 giây |
  | Heo | Bát Giới Trấn Sơn | Hút cả khu về trước mặt rồi bật lên dậm đất (4.5×, quật ngã) |

- **Độ nét**: ảnh nhân vật 2048px; texture vẽ bằng code lấy mẫu dày gấp đôi (trước bị phóng to 1.5–3 lần khi hiển thị);
  hình tròn placeholder 256px (trước 64px bị kéo ra vài trăm px).

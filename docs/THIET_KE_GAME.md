# 12 Con Giáp — Tài liệu thiết kế (prototype)

Platformer 2D. Người chơi chọn 1 trong 12 con giáp, vượt 3 màn và hạ boss Hồ Tinh.
Cả 12 nhân vật đều cầm một nhạc cụ dân tộc, nên chiêu thức lấy âm thanh làm chủ đề: sóng âm, tiếng chiêng, tiếng kèn…

Dựng lại toàn bộ: **Tools ▸ Zodiac ▸ Dựng prototype** (tạo 12 nhân vật, prefab quái, 4 scene, Build Settings).
Bấm Play ở `Zodiac_Start`.

## Điều khiển

| Phím | Hành động |
|---|---|
| A/D, ←/→ | Đi (giữ Shift để chạy) |
| Space | Nhảy (giữ lâu thì nhảy cao, thả sớm thì nhảy thấp). ↓ + Space: rơi xuống qua bục tre |
| J / chuột trái | Đánh thường (chém tan được đạn của quái) |
| K / chuột phải / RB | Chiêu riêng của con giáp (tốn Khí) |
| L / Ctrl trái / LB | Lăn né — bất tử, xuyên qua quái và đạn; trên không né được 1 lần |
| R | Chơi lại màn |

## Luật chung (tham khảo)

- **Đạp đầu quái** (Mario), được nảy lên; Khỉ nảy xong còn được nhảy đôi lại (pogo của *Hollow Knight*).
- **Đèn lồng = checkpoint** (ghế nghỉ của *Hollow Knight*): thắp đèn thì hồi 1 máu và trở thành điểm hồi sinh.
- **Rơi vực / dính gai chỉ mất 1 máu** rồi về đèn lồng gần nhất (*Celeste*, *Hollow Knight*). Chỉ thua khi hết máu,
  lúc đó có nút "Hồi sinh tại đèn lồng".
- Mọi đòn của quái đều **có báo hiệu trước** (thân bẹp xuống, phát sáng) để người chơi kịp né — triết lý của *Hollow Knight* / *Cuphead*.

## 12 con giáp

Mỗi con có chỉ số riêng (máu, tốc độ, độ cao nhảy, sức đánh), 1 nội tại và 1 chiêu (phím K).
Số liệu nằm trong `Assets/Zodiac/Editor/ZodiacCharacterDefs.cs` và được chép vào asset `Assets/Zodiac/Characters/*.asset`; chỉnh trực tiếp trong Inspector cũng được.

| Con giáp | Nhạc cụ | Nội tại | Chiêu (K) | Hồi chiêu | Tham khảo |
|---|---|---|---|---|---|
| Tý — Chuột | Đàn nguyệt | **Lanh lẹ**: nhỏ, nhanh, nhảy cao hơn | **Độn Ảnh**: lướt ngang, bất tử, xuyên qua quái và đạn | 1s | Dash của *Celeste*, Shade Cloak (*Hollow Knight*) |
| Sửu — Trâu | Chiêng | **Da trâu**: 7 máu, gần như không bị đẩy lùi | **Chiêng Chấn Địa**: 2 sóng chấn chạy dọc mặt đất sang 2 bên, 2 sát thương + choáng | 3s | Sóng chấn của *Mega Man*, *Cuphead* |
| Dần — Hổ | Trống cơm | **Mãnh hổ**: đánh thường 2 sát thương | **Hổ Vồ**: vồ chéo lên trước, 3 sát thương, trúng thì nảy lên vồ tiếp | 2.5s | Dash Slash (*Hollow Knight*) |
| Mão — Mèo | Đàn nhị | **Chín mạng**: mỗi màn thoát chết 1 lần (còn 1 máu) | **Tiếng Nhị Xuyên Tâm**: tia âm bay thẳng xuyên mọi kẻ địch, 2 sát thương | 1.6s | Charge Shot (*Mega Man*) |
| Thìn — Rồng | Đàn tranh | **Thăng long**: nhảy cao nhất (4 ô), 6 máu | **Long Hoả**: 5 cầu lửa toả hình quạt | 2.2s | Spread Shot (*Contra*, *Cuphead*) |
| Tỵ — Rắn | Áo choàng bóng đêm | **Nọc độc**: đòn thường gây độc thêm 2 sát thương | **Xà Ảnh**: tàng hình 3s (bất tử, quái mất dấu), đòn đầu tiên x3 | 6s | *Mark of the Ninja*, *Dead Cells* |
| Ngọ — Ngựa | Đàn tam | **Thiên lý mã**: chạy nhanh nhất (11 ô/s) | **Vó Ngựa Phi**: phi nước đại dài, húc văng quái, 2 sát thương | 2.5s | Spin Dash (*Sonic*) |
| Mùi — Dê | Tỳ bà | **Hiền hoà**: cứ 6 đồng xu hồi 1 máu | **Khúc Hồi Xuân**: đứng gảy đàn 0.8s để hồi 2 máu (bị đánh thì ngắt) | 8s | Focus (*Hollow Knight*) |
| Thân — Khỉ | Chũm chọe | **Tề Thiên**: nhảy đôi | **Chũm Chọe Bay**: ném chũm chọe bay ra rồi quay về, chém cả đi lẫn về | 1.8s | Metal Blade (*Mega Man*), boomerang (*Zelda*) |
| Dậu — Gà | Kèn | **Cánh phượng**: giữ Space khi rơi để lượn | **Kèn Hiệu Bình Minh**: sóng âm tròn làm choáng mọi quái quanh mình 2s và thổi tan đạn | 5s | Howling Wraiths (*Hollow Knight*) |
| Tuất — Chó | Đàn đáy | **Mũi thính**: hút đồng xu trong bán kính 4.5 ô | **Khuyển Hống**: tiếng sủa phía trước, 2 sát thương, hất văng quái | 2s | Bash (*Ori*) |
| Hợi — Heo | Sáo trúc | **Phúc hậu**: 6 máu, đạp đầu gấp đôi sát thương | **Heo Dậm Đất**: bật lên rồi lao xuống, sóng chấn 3 sát thương + choáng | 3s | Ground Pound (*Mario*), Stone (*Kirby*) |

Ảnh nhân vật: cắt từ `Assets/Characters/drive-download-.../IMG_*.png` (ảnh gốc giữ nguyên), lật cho mọi con quay mặt sang phải,
pivot ở bàn chân → `Assets/Zodiac/Art/Characters/<Con>_<Chi>.png`.

## Quái

Lấy cảm hứng từ truyện cổ và đình chùa Việt Nam; hình dáng tạm ghép từ hình khối (chưa có sprite).

| Quái | Ký tự bản đồ | Hành vi | Cách hạ | Tham khảo |
|---|---|---|---|---|
| **Lính Đất Nung** | `W` | Tuần tra; thấy người thì đuổi, bẹp người gồng đòn rồi lao cắn | Đạp đầu, đánh | Husk (*Hollow Knight*), Goomba |
| **Bọ Gai** | `S` | Chỉ tuần tra, lưng đầy gai | **Không đạp được** (bị đâm) — phải đánh/dùng chiêu | Spiny (*Mario*) |
| **Ma Trơi** | `F` | Bay lơ lửng; thấy người thì sáng rực rồi lao thẳng tới, xong bay về chỗ cũ | Đạp (pogo), đánh, bắn | Vengefly (*Hollow Knight*), Boo |
| **Cóc Tía** | `H` | Ngồi thụp xuống rồi nhảy vòng cung về phía người chơi, không nhảy xuống vực | Đạp, đánh | Cóc của *Mario* |
| **Thạch Sư** | `T` | Tượng sư tử đá đứng yên, miệng nạp sáng rồi phun cầu lửa ngắm vào người chơi | Phá tượng; nấp sau địa hình; chém tan cầu lửa | Tháp pháo (*Mega Man*, *Castlevania*) |
| **Hồ Tinh** (boss) | `X` | Cáo chín đuôi. 3 đòn: lao vồ ngang, nhảy đập tạo sóng chấn, bắn chùm lửa hình quạt. Dưới 50% máu: nhanh hơn, lửa dày hơn, gọi thêm Ma Trơi | Né theo báo hiệu (đuôi sáng xanh, thân cúi thấp) rồi phản công | Truyền thuyết Lạc Long Quân diệt Hồ Tinh; nhịp boss của *Hollow Knight*, *Cuphead* |

## Bản đồ

Bản đồ viết bằng ký tự trong `Assets/Zodiac/Editor/ZodiacLevelData.cs` (mỗi ký tự = 1 ô) — sửa chữ rồi dựng lại là xong,
giống cách làm màn của *Spelunky* / *Mario Maker*. Nền parallax nhiều lớp tạo chiều sâu (*Ori*, *Hollow Knight*).

Ký hiệu: `#` đất · `B` đá · `=` bục tre một chiều · `^` gai · `M`/`V` bục di động ngang/dọc (`-`/`|` = quãng đi) ·
`o` đồng xu · `P` xuất phát · `K` đèn lồng · `G` cổng đích · `i` biển hướng dẫn · `A` cổng đấu trường boss.

Quy ước độ khó (nhảy cao 3.2 ô, chạy 8.5 ô/s): bậc leo ≤ 3 ô, vực đi bộ ≤ 4 ô, vực phải chạy ≤ 6 ô.

1. **Làng Quê** (172 × 15) — dạy luật theo kiểu World 1-1 của *Mario*: đoạn đầu an toàn → quái đầu tiên trên đất bằng →
   vực nhỏ → bục tre có xu → Bọ Gai (có biển nhắc "đừng đạp") → đèn lồng → cầu thang + vực rộng → bãi gai →
   bục di động qua vực 10 ô → Ma Trơi đầu tiên → cầu thang lên **cổng làng**.
2. **Rừng Trúc** (196 × 26) — màn leo dọc kiểu *Celeste* / Greenpath: leo vách bằng bục tre, qua vực sâu bằng bè tre di động,
   hành lang gai có bục phía trên và Ma Trơi canh, tháp trúc cao 18 ô (có thang máy tre cho ai trượt chân).
3. **Động Hồ Tinh** (178 × 19) — hang đá có trần: Thạch Sư phun lửa, hai bè đá di động trên vực, bãi gai dài,
   rồi **đấu trường boss**: cổng đá đóng sau lưng, camera khoá trong đấu trường, thanh máu boss hiện ở đáy màn hình;
   hạ Hồ Tinh thì cổng đền hiện ra.

## Ghi chú kỹ thuật

- Địa hình gộp thành một `CompositeCollider2D` → nhân vật không vấp ở chỗ nối các khối.
- Chiêu thức: `PlayerSkill` đọc `SkillConfig` của nhân vật (6 kiểu: Dash, Projectile, Burst, Heal, Stealth, GroundPound) —
  thêm chiêu mới chủ yếu là chỉnh thông số.
- Quái kế thừa `EnemyBase` (va chạm, đạp đầu, choáng, chết); mỗi loại chỉ viết AI trong `Think()`.
- Prefab quái và asset nhân vật đã có thì builder giữ nguyên chỉnh sửa tay. Muốn lấy lại mặc định: xoá prefab, hoặc dùng
  **Tools ▸ Zodiac ▸ Ghi đè chỉ số 12 con giáp**.

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

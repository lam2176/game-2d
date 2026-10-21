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
| F5 | Chơi lại màn |

## Luật chung (tham khảo)

- **Canh làn**: đòn đánh chỉ trúng quái cùng làn (lệch tối đa ~0.5 unit). Né bằng cách đổi làn, lăn, hoặc nhảy.
- **Vùng giao tranh** (*Streets of Rage*): đi tới thì tường vô hình đóng hai đầu, camera khoá khung, quái kéo tới theo từng đợt.
  Dọn xong thì hiện **ĐI TIẾP →**.
- **Vây đánh có lượt** (*Sifu*, *Batman Arkham*): tối đa 2 quái tấn công cùng lúc, số còn lại đứng vòng ngoài chờ.
- **Quật ngã**: đòn kết combo, đá bay, đòn lao làm quái văng lên nằm đất (bất tử khi nằm); đánh trúng lúc còn đang bay thì tung hứng tiếp.
- **Quái không gây sát thương khi chạm** — mọi đòn của quái đều **có báo hiệu trước** (thân bẹp xuống, phát sáng, vòng đỏ dưới đất).
- **Đèn lồng = checkpoint**: đi ngang qua thì thắp, hồi 1 tim (20 máu) và trở thành điểm hồi sinh khi bấm "Hồi sinh tại đèn lồng".
  Các vùng giao tranh phía sau đèn lồng coi như đã dọn.
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

Lấy cảm hứng từ truyện cổ và đình chùa Việt Nam; hình dáng tạm ghép từ hình khối (chưa có sprite).

| Quái | Mã đợt | Hành vi | Cách hạ | Tham khảo |
|---|---|---|---|---|
| **Lính Đất Nung** | `W` | Xáp lại đứng ngang hàng (cùng làn), bẹp người gồng đòn rồi lao đâm | Đổi làn khi nó gồng, đánh ngắt | Lính thường của *Streets of Rage* |
| **Lính Khiên** | `Q` | Như Lính Đất Nung nhưng cầm khiên đỡ mọi đòn phía trước, đánh đau (quật ngã) | Đổi làn vòng ra sau lưng, đá bay từ trên xuống, chiêu gây choáng | Lính khiên của *Dead Cells* |
| **Bọ Gai** | `S` | Bò về đúng làn người chơi, rung người rồi cuộn tròn lăn thẳng dọc đường; lăn xong choáng váng | Đổi làn / nhảy qua, đánh lúc choáng | Bowling của *Castle Crashers* |
| **Ma Trơi** | `F` | Bay ngang tầm đầu (đánh trên đất không với tới), sáng rực rồi lao chéo xuống | Đá bay; chém lúc nó lao xuống | Vengefly (*Hollow Knight*) |
| **Cóc Tía** | `H` | Vòng đỏ hiện dưới chân người chơi → nhảy đè xuống tạo sóng chấn tròn | Chạy ra khỏi vòng hoặc đang nhảy lúc nó đáp | *Hades*, *Castle Crashers* |
| **Thạch Sư** | `T` | Tượng đá đứng ở làn trong cùng, miệng nạp sáng rồi phun cầu lửa bay ngang các làn | Đổi làn, nhảy, chém tan cầu lửa; phá tượng | Tháp pháo (*Mega Man*) |
| **Hồ Tinh** (boss) | — | Cáo chín đuôi. Lao vồ dọc làn (dạt sang làn người chơi trước), nhảy đập (vòng báo chỗ đáp + sóng toả tròn sát đất), chùm lửa hình quạt quét các làn. Dưới 50% máu: nhanh hơn, sóng/lửa dày hơn, gọi thêm Ma Trơi | Né theo báo hiệu rồi phản công | Truyền thuyết Lạc Long Quân diệt Hồ Tinh |

## Bản đồ

Mỗi màn khai báo trong `Assets/Zodiac/Editor/ZodiacLevelData.cs`: chiều dài con đường, các **vùng giao tranh**
(vị trí, bề ngang, danh sách đợt quái viết bằng mã — ví dụ `"W Q W"`), đèn lồng, cụm đồng xu, bãi chông, biển hướng dẫn, cổng đích.
Sửa số liệu rồi dựng lại là xong. Cảnh hai bên đường (nhà, tre, vách đá) sinh theo chủ đề với số ngẫu nhiên cố định theo tên màn.

Nhịp màn: mỗi loại quái mới có biển hướng dẫn ngay trước vùng giới thiệu nó, rồi mới trộn với loại khác; đèn lồng sau 1–2 vùng.

1. **Làng Quê** (130 unit) — đường đất giữa dãy nhà vách gỗ mái ngói, luỹ tre, đồng lúa và núi xa. Dạy đánh/đổi làn với
   Lính Đất Nung → Lính Khiên → Bọ Gai → bãi chông → vùng trộn cuối, rồi **cổng làng**.
2. **Rừng Trúc** (140 unit) — rừng tre dày nhiều tầng, sương mù đặc. Ma Trơi (đá bay) → Cóc Tía (vòng đỏ) → các vùng trộn.
3. **Động Hồ Tinh** (130 unit) — hang tối, ánh sáng từ pha lê và chậu lửa. Thạch Sư phun lửa, các vùng trộn, rồi
   **đấu trường boss**: tường đóng, camera khoá, thanh máu boss hiện trên HUD; hạ Hồ Tinh thì cổng đền hiện ra.

## Ghi chú kỹ thuật

- Vật lý 3D: người chơi `Rigidbody` + `CapsuleCollider` tự tính trọng lực; quái có collider bề dày theo làn, không va chạm với người chơi
  (sát thương do đòn đánh quét `Physics.OverlapBox`).
- Hình nhân vật/quái: Root (vật lý) → Facing (lật trái/phải, chứa hitbox) → Tilt (`Billboard`, ngả theo góc cúi camera) → Visual.
- Camera phối cảnh cố định góc cúi (`ZodiacCameraFollow`), bám trục X, khoá trong đấu trường khi đánh đợt quái.
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
- **Tuyệt kỹ** = bản cường hoá của chiêu (`SkillConfig.Empowered`): sát thương ×2, vùng ×1.7, nhiều đạn hơn, lướt xa hơn,
  choáng lâu hơn, hồi đầy máu (Dê), tàng hình lâu + đánh lén ×5 (Rắn). Khi tung có cảnh cắt: dải đen chéo màn hình,
  chân dung trượt vào, tên tuyệt kỹ chữ lớn, thời gian chậm lại 0.75 giây.

  | Con giáp | Tuyệt kỹ | | Con giáp | Tuyệt kỹ |
  |---|---|---|---|---|
  | Chuột | Vạn Ảnh Thử | | Ngựa | Thiên Mã Hành Không |
  | Trâu | Đại Hồng Chung | | Dê | Xuân Hồi Đại Địa |
  | Hổ | Mãnh Hổ Hạ Sơn | | Khỉ | Tề Thiên Đại Thánh |
  | Mèo | Cửu Mệnh Huyền Âm | | Gà | Kim Kê Báo Hiểu |
  | Rồng | Long Thần Hoả | | Chó | Thiên Cẩu Hống |
  | Rắn | Vạn Xà Ẩn Hình | | Heo | Bát Giới Trấn Sơn |

- **Độ nét**: ảnh nhân vật 2048px; texture vẽ bằng code lấy mẫu dày gấp đôi (trước bị phóng to 1.5–3 lần khi hiển thị);
  hình tròn placeholder 256px (trước 64px bị kéo ra vài trăm px).

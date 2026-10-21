# Kế hoạch nâng cấp & phát hành — 12 Con Giáp

Lập ngày 21/10/2026, sau khi đã có: beat 'em up 2.5D theo khu, 3 màn + boss, 12 con giáp, lính mất đồ, ảnh quái,
vật phẩm + hành trang + trang bị. Đọc kèm `PHAN_TICH_THIET_KE.md` (hiện trạng) và `COT_TRUYEN.md` (truyện).

**Mục tiêu:** từ prototype đi tới một bản phát hành được trên PC — demo miễn phí trên itch.io trước, rồi trang Steam.

**Giả định** (đổi được ở mục 6): PC Windows trước; tiếng Việt trước, tiếng Anh sau; chơi một người, co-op cùng máy làm sau;
một người làm code cùng Claude; ảnh vẽ bằng AI + tự chỉnh.

---

## 1. Thứ tự ưu tiên

Phân tích thiết kế (mục 6 của `PHAN_TICH_THIET_KE.md`) chỉ ra: lớp RPG (trang bị, chỉ số) đã có
nhưng **chưa có đủ phút chơi để người ta xây đồ** — không chọn màn, không lưu tiến trình, không có làng.
Vì vậy thứ tự là:

1. **Nền tảng tiến trình** (lưu màn, chọn màn, lên cấp) — để mọi hệ sau có chỗ bám.
2. **Vòng lặp nâng cấp** (làng, đập đồ, cửa hàng, luyện đan) — lý do để chơi lại.
3. **Chiều sâu chiến đấu** (tuyệt kỹ riêng, quái tinh anh, độ khó) — để chơi lại không nhàm.
4. **Cốt truyện** — lý do để chơi tiếp và chơi đủ 12 con.
5. **Nội dung mới** (màn, boss, chế độ chơi) — sau khi các hệ đã chắc.
6. **Hoàn thiện & phát hành**.

Thêm nội dung trước khi có 1–3 thì mỗi màn mới chỉ là "thêm một lần dọn phòng".

---

## 2. Hệ thống mới

### 2.1 Đập đồ (cường hoá trang bị) ★

Trang bị nâng từ **+0 đến +10** ở **Lò rèn** trong làng. Mỗi cấp tăng chỉ số gốc của món đồ; tên hiện "Giáo đồng +7",
viền ô đồ và đồ trên người phát sáng theo cấp (+4 lam, +7 tím, +10 vàng cháy).

| Lên cấp | Tỉ lệ thành công | Thất bại | Tốn |
|---|---|---|---|
| +1 → +3 | 100% | — | ít đồng + nguyên liệu thường |
| +4 → +5 | 85% → 70% | giữ nguyên cấp | thêm nguyên liệu "Tốt" |
| +6 → +7 | 55% → 40% | **tụt 1 cấp** | nguyên liệu hiếm (Lửa ma trơi, Đá thạch sư) |
| +8 → +10 | 30% → 12% | **tụt 1 cấp** | + Đuôi Hồ Tinh / nguyên liệu boss |

- **Chống ức chế:** mỗi lần thất bại cộng ẩn +5% cho lần sau với món đó; đập thành công thì xoá.
- **Bùa bảo hộ:** thất bại không tụt cấp. **Ngọc may mắn:** +10% tỉ lệ. Hai thứ này rơi từ quái tinh anh, boss, hoặc mua trong làng.
- Hiệu ứng: búa đập, tia lửa, rung màn hình; thành công thì đồ loé sáng + chữ to; thất bại thì khói và tiếng rè.
- **Mỗi cấp tăng 8% chỉ số gốc của món** (+10 = gấp đôi chỉ số gốc), để đồ thường đập cao vẫn đáng giá.
- Nguyên liệu theo loại: vũ khí cần đồ từ lính (Mảnh gốm, Đá thạch sư), giáp cần đồ từ thú (Vảy bọ gai, Da cóc),
  bùa/ngọc cần đồ từ ma (Lửa ma trơi). Nguyên liệu nào cũng đã rơi sẵn trong game hiện tại.

**Kỹ thuật:** `ItemStack` hiện chỉ có `id + count`. Cần thêm `level` (cấp đập) và `affixes` (dòng phụ ngẫu nhiên, mục 2.2),
nghĩa là đồ trang bị trở thành **từng món riêng** (không chồng). File lưu cần trường phiên bản để đọc được file cũ.

### 2.2 Dòng phụ ngẫu nhiên, khảm ngọc, phân giải

- **Dòng phụ:** đồ rơi ra có 0–3 dòng chỉ số ngẫu nhiên theo độ hiếm (Tốt 1, Hiếm 2, Sử thi 3). Ví dụ "Kiếm tre: +3 Công, +5% Chí mạng, **+12 Máu**".
- **Khảm ngọc:** vũ khí và giáp có 1–2 lỗ khảm; ngọc đang là trang bị riêng sẽ chuyển thành vật khảm (giải phóng 2 ô trang bị cho bùa/nhẫn).
- **Phân giải:** đồ thừa → nguyên liệu (thay cho việc vứt). Giải quyết luôn chuyện túi đầy đồ trùng.

### 2.3 Làng trung tâm (Hub)

Một scene làng giữa các màn (tái dụng cảnh Làng Quê). Đi lại tự do, không có quái.

| Nơi | Chức năng |
|---|---|
| **Đình làng** | Chọn màn / hồi, xem cốt truyện, sổ ghi chép, đổi con giáp |
| **Lò rèn** (bác thợ rèn) | Đập đồ, khảm ngọc, phân giải |
| **Tiệm thuốc bắc** | Mua bình máu/Khí; **luyện đan** từ nguyên liệu |
| **Bàn thờ tổ tiên** | Dùng Huyết Đan / Khí Đan; cộng điểm lên cấp |
| **Rương nhà** | Kho đồ chung cho 12 con giáp |
| **Chợ phiên** (mở khoá sau) | Thương nhân bán đồ ngẫu nhiên mỗi lần về làng |

### 2.4 Lên cấp & Đan (đã có trong lộ trình cũ)

- XP từ quái và từ dọn khu; lên cấp được điểm cộng Sinh lực / Nội khí / Sức mạnh / Thân pháp.
- **Huyết Đan / Khí Đan:** gom 3 mảnh (rơi từ boss, khu bí mật, quái tinh anh) → +1 nấc máu/Khí tối đa vĩnh viễn.
- **Cấp và Đan riêng cho từng con**, trang bị và tiền dùng chung — chơi con mới không phải bắt đầu nghèo, nhưng vẫn phải luyện.

### 2.5 Chiều sâu chiến đấu

- **Tuyệt kỹ riêng cho 12 con** (giải quyết mục 9.1 của bản phân tích): thay `Empowered()` bằng một chiêu khác hẳn.
  Ví dụ: Rắn khoá một quái và hút máu, Dê hồi máu + tăng giáp cả khu, Khỉ gọi 3 phân thân, Gà làm cả khu "sáng"
  (quái choáng + lộ điểm yếu), Heo hút quái về giữa rồi dậm đất.
- **Nội tại gom về một chỗ** (`IPassive` hoặc component riêng mỗi con) trước khi thêm nội tại mới — mục 9.2.
- **Bắt & ném quái** (đặc trưng beat 'em up): áp sát + bấm đánh khi quái choáng → túm, ném vào quái khác.
- **Vũ khí nhặt trên đất:** chum vại, đòn gánh, đèn lồng — dùng vài lần rồi vỡ.
- **Đỡ đòn / phản đòn:** bấm lăn đúng lúc quái ra đòn → chậm thời gian, phản đòn chí mạng.
- **Đếm combo + xếp hạng khu** (S/A/B/C theo thời gian, combo, máu mất) — thưởng thêm đồ khi đạt S.
- **Quái tinh anh** (viền màu, tên có tiền tố): Cuồng nộ (nhanh), Giáp đá (giảm sát thương), Độc, Hồi máu, Phân thân.
  Rơi đồ tốt hơn và Bùa bảo hộ / Ngọc may mắn.
- **Độ khó theo màn + chế độ khó:** Thường / Khó / Ác Mộng (mở sau khi phá đảo) — quái đông hơn, có affix, đồ rơi hiếm hơn.

### 2.6 Nội dung & chế độ chơi

- **Hũ sành, rương, đèn đá đập vỡ được** trong khu — rơi xu, bình thuốc.
- **Khu bí mật:** lối ẩn sau bụi tre / tường nứt, có rương và mảnh Đan.
- **Mini-boss giữa mỗi màn** và **boss mới cho mỗi hồi**: Chằn Tinh, Thuồng Luồng, Đại Bàng Tinh, Hồ Tinh Cửu Vĩ (xem `COT_TRUYEN.md`).
- **Tháp Mười Hai Canh Giờ:** chế độ vượt tầng vô tận, mỗi tầng một khu ngẫu nhiên + một luật đặc biệt
  (quái nhanh gấp đôi, chỉ dùng chiêu, sương mù…). Bảng xếp hạng cục bộ.
- **Thử thách hằng ngày:** một khu cố định trong ngày, phần thưởng nguyên liệu hiếm.
- **Co-op 2 người cùng máy** (bàn phím + tay cầm): hợp chất beat 'em up, nhưng đụng tới camera, HUD, lượt vây quái —
  để sau khi các hệ đã ổn định.

---

## 3. Chuẩn bị phát hành ("up game")

### 3.1 Những thứ game bắt buộc phải có trước khi ai khác chơi
- Menu tạm dừng (Esc): tiếp tục, cài đặt, về làng, thoát.
- **Cài đặt:** âm lượng nhạc/hiệu ứng, độ phân giải, toàn màn hình, chất lượng đồ hoạ, **đổi phím**.
- Hỗ trợ tay cầm đầy đủ (đã có một phần qua Input System) + hiện đúng biểu tượng nút.
- **Lưu nhiều ô** + nút "Chơi mới" (hiện mới có một file hồ sơ).
- Màn hình hướng dẫn / màn chơi thử đầu tiên thay cho biển chữ.
- Hiệu năng: 60 khung/giây ổn định trên máy tầm thấp (card tích hợp) — đo, giảm bóng đổ / hạt nếu cần.
- **Đa ngôn ngữ:** tách toàn bộ chữ trong code ra bảng chữ (VN trước, EN sau). Chữ đang nằm rải trong code và builder.
- Font có đủ dấu tiếng Việt và nét đẹp hơn font mặc định.

### 3.2 Pháp lý & bản quyền
- **Âm thanh:** Kenney (CC0) và nhạc tự tổng hợp — dùng thương mại được; giữ danh sách nguồn trong `docs/`.
- **Ảnh vẽ bằng AI:** Steam yêu cầu **khai báo nội dung tạo bằng AI** khi nộp game. Nên tự chỉnh sửa tay / vẽ lại dần
  các ảnh chính (nhân vật, boss) để hình đồng nhất và giảm rủi ro.
- **Ảnh 12 con giáp gốc** (`Assets/Characters/drive-download-...`): cần xác nhận ai vẽ và có quyền dùng thương mại.
- Kiểm tra tên game "12 Con Giáp" có trùng thương hiệu / game khác trên Steam không; cân nhắc tên riêng
  (ví dụ "Giao Thừa: 12 Con Giáp").

### 3.3 Trang cửa hàng
- Ảnh bìa (capsule) các kích thước theo Steam, 5–10 ảnh chụp đẹp, **trailer 30–60 giây**, mô tả ngắn + dài (VN/EN).
- itch.io: trang demo miễn phí, nút tải bản Windows, link Discord/Facebook để nhận góp ý.

### 3.4 Các mốc phát hành
1. **Bản chơi thử nội bộ** (bạn bè): 1 hồi đầy đủ + làng + đập đồ. Thu góp ý bằng biểu mẫu.
2. **Demo itch.io miễn phí:** Hồi 1 + 4 con giáp mở sẵn. Mục tiêu: người lạ chơi hết demo không cần hướng dẫn.
3. **Trang Steam "Sắp ra mắt"** (phí Steam Direct 100 USD/game): bắt đầu gom wishlist càng sớm càng tốt.
4. **Steam Next Fest** (lễ hội demo, vài lần mỗi năm) với bản demo đã chỉnh từ góp ý itch.io.
5. **Early Access hoặc bản đầy đủ** khi có ít nhất 2 hồi + Tháp + kết thúc thật.

---

## 4. Lộ trình

Mỗi giai đoạn một hoặc vài nhánh `feature/...`, merge sau khi chơi thử. Thời gian là ước lượng cho một người làm cùng Claude.

| Giai đoạn | Nội dung | Ước lượng |
|---|---|---|
| **A. Nền tảng tiến trình** | Lưu tiến trình màn + nhiều ô lưu, chọn màn, menu tạm dừng & cài đặt, `ItemStack` → món đồ riêng (cấp + dòng phụ), XP/lên cấp, Huyết/Khí Đan | 2–3 tuần |
| **B. Làng & đập đồ** | Scene làng, Lò rèn (đập đồ, phân giải), Tiệm thuốc (mua, luyện đan), Bàn thờ, Rương nhà | 2–3 tuần |
| **C. Chiều sâu chiến đấu** | Tuyệt kỹ riêng 12 con, gom nội tại, quái tinh anh + affix, độ khó, hũ/rương đập vỡ, bắt & ném, xếp hạng khu | 3 tuần |
| **D. Cốt truyện Hồi 1** | Hệ hội thoại + tranh kể chuyện, mở đầu, cảnh giữa hồi, 12 kết thúc riêng, ảnh boss Hồ Tinh (sau 01/11 khi Figma có lại credit) | 3 tuần |
| **E. Hoàn thiện demo** | Đổi phím, tay cầm, đa ngôn ngữ, hiệu năng, font, màn hướng dẫn, âm thanh làng/lò rèn | 2 tuần |
| **F. Demo itch.io** | Build, trang itch.io, chơi thử với người ngoài, sửa theo góp ý | 1–2 tuần |
| **G. Steam** | Trang Steam, trailer, Hồi 2 + Tháp Mười Hai Canh Giờ, Next Fest | 4–6 tuần |

Tổng tới demo itch.io: **khoảng 13–16 tuần**. Tới trang Steam có nội dung đủ cho Next Fest: thêm **4–6 tuần**.

---

## 5. Rủi ro

| Rủi ro | Ảnh hưởng | Cách giảm |
|---|---|---|
| Phình phạm vi (muốn thêm mọi thứ) | Không bao giờ phát hành | Khoá phạm vi demo = Hồi 1; ý mới ghi vào danh sách "sau demo" |
| Hình nhân vật vẫn là một tấm ảnh | Đòn khó đọc, trông kém chuyên nghiệp | Rig xương 2D cho 4 con mở sẵn trong demo trước, 8 con sau |
| Ảnh AI không đồng nhất, chính sách Steam | Hình lủng củng, bị hỏi khi duyệt | Chỉnh tay, khai báo đúng, thay dần ảnh chính |
| Đập đồ làm mất cân bằng | Người chơi quá mạnh / bực vì tụt cấp | Giới hạn +10, chống ức chế, Bùa bảo hộ; cân bằng bằng bot chơi thử |
| Đổi cấu trúc file lưu | Mất đồ của người chơi | Trường phiên bản + đọc được file cũ ngay từ giai đoạn A |
| Máy dev yếu RAM | Không chạy được bot chơi thử | Chạy kiểm thử từng màn, đóng ứng dụng khác khi chạy |

---

## 6. Cần bạn chốt

1. **Nền tảng:** PC (Steam + itch.io) như giả định, hay muốn cả điện thoại? Điện thoại cần điều khiển cảm ứng và giảm đồ hoạ — thêm 3–4 tuần.
2. **Giá:** demo miễn phí + bản đầy đủ trả phí (gợi ý 80.000–120.000đ / 5–8 USD), hay miễn phí hoàn toàn?
3. **Đập đồ có tụt cấp khi thất bại từ +6?** (kế hoạch: có, kèm Bùa bảo hộ) — hay chỉ mất nguyên liệu cho nhẹ nhàng?
4. **12 con mở sẵn hay mở khoá dần theo truyện?** (gợi ý: demo mở 4 con, bản đầy đủ mở dần qua các hồi)
5. **Co-op 2 người:** có cần cho bản phát hành đầu không?
6. **Tên game** khi lên cửa hàng.

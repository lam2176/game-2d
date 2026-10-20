# Kế hoạch cải thiện: gameplay, animation, độ chân thật

Tài liệu này lập kế hoạch từ bản prototype hiện tại (12 con giáp, 3 màn, boss Hồ Tinh — xem `THIET_KE_GAME.md`)
lên một bản chơi có cảm giác "thật": nhân vật chuyển động có khung hình, đòn đánh có lực, màn chơi có ánh sáng, âm thanh.

Ước lượng công: **S** ≈ ½–1 ngày · **M** ≈ 2–4 ngày · **L** ≈ 1–2 tuần (1 người).
Cột "Ai làm": **Code** (lập trình), **Art** (hoạ sĩ), **Audio** (âm thanh).

---

## Tiến độ

- [x] **Đợt 1** (nhánh `feature/game-feel-dot-1`): toàn bộ Giai đoạn 1 (game feel), 2.1 combo + 2.2 pogo, 4.1–4.4 (2D Renderer, Light2D,
  hậu kỳ, hạt môi trường), 5.1–5.5 âm thanh (Kenney CC0 + tổng hợp bằng code). Animation nhân vật làm bằng code (không có hoạ sĩ).
- [x] **Đợt 2** (nhánh `feature/chieu-va-quai-dot-2`): 2.3 Khí, 2.4 lăn né, 2.5 quái phản ứng + Lính Khiên.
- [x] **Đợt 3** (nhánh `feature/do-hoa-dot-3`): sửa hình mờ (bloom, mipmap, MSAA, zoom), 4.5–4.6 bằng texture vẽ bằng code.
- [x] **Đợt 4** (nhánh `feature/hud-qwer-dot-4`): phím QWER, HUD thanh chiêu kiểu Ngọc Rồng, Tuyệt kỹ + cảnh cắt, ảnh/texture độ phân giải cao.
- [ ] Còn lại: 2.6 cân bằng, 4.7 vật thể sống, Giai đoạn 6–7. Giai đoạn 3 (rig xương) cần ảnh tách lớp — tạm thay bằng `CharacterAnimator`.

## 1. Hiện trạng — vì sao game chưa "thật"

| Vấn đề | Nguyên nhân trong code/asset hiện tại | Ảnh hưởng |
|---|---|---|
| Nhân vật như "tấm ảnh trượt trên sàn" | Mỗi con giáp chỉ có **1 ảnh minh hoạ tĩnh**; animation Idle/Walk/Run/Jump chỉ nhún, nghiêng, co giãn transform (`ZodiacPrototypeBuilder.EnsureAnimatorController`) | Không thấy chân bước, tay vung, không đọc được tư thế |
| Ảnh có cảnh nền dính kèm | Ảnh gốc vẽ kèm cây thông, trúc, hoa cúc, thảm (Heo), cành mai | Hitbox và hình không khớp, không tách bộ phận để làm animation được |
| Đòn đánh không có lực | Đánh thường chỉ hiện một hình tròn mờ (`SlashEffect`), không hitstop, không rung màn hình, không âm thanh | Đánh trúng hay trượt đều "nhạt" như nhau |
| Quái là khối hình học | `ZodiacEnemyFactory` ghép hình vuông/tròn | Không có biểu cảm, không có animation chết/bị đánh |
| Không có âm thanh | Chưa có AudioSource / nhạc / SFX nào | Mất một nửa cảm giác của game |
| Màn chơi phẳng màu | Địa hình là khối màu (`ZodiacLevelBuilder`), nền parallax là hình học | Không có chiều sâu, ánh sáng, thời tiết |
| Chưa có ánh sáng 2D | URP đang dùng **Universal Renderer (3D)**, chưa có **2D Renderer** → không dùng được Light2D | Không có đèn lồng toả sáng, hang tối, bóng đổ |
| Hiệu ứng tạm | `ZodiacFx` vẽ vòng tròn/bụi bằng sprite, không dùng Particle System | Thiếu bụi, tia lửa, lá rơi… |

---

## 2. Quyết định cần chốt trước (chặn các giai đoạn sau)

### Q1. Cách làm animation nhân vật — **đề xuất: rig xương 2D (2D Animation + PSD Importer)**

| Phương án | Cách làm | Ưu | Nhược | Chi phí art (12 con) |
|---|---|---|---|---|
| **A. Rig xương 2D** (đề xuất) | Hoạ sĩ tách nhân vật hiện có thành lớp (đầu, thân, 2 tay, 2 chân, đuôi, nhạc cụ, vạt áo) trong file **.psb**, Unity gắn xương và làm animation | Tận dụng đúng nét vẽ hiện có; 1 bộ chuyển động có thể dùng lại cho nhiều con cùng dáng; sửa animation dễ | Chuyển động hơi "giấy cắt" nếu rig kém; cần học package 2D Animation | Tách lớp: ~1–2 ngày/con |
| B. Vẽ frame-by-frame | Vẽ lại từng khung hình (8–12 khung × ~10 animation × 12 con) | Đẹp, mượt nhất (Hollow Knight, Cuphead) | Rất tốn: ~1000+ khung hình | ~2–3 tuần/con |
| C. Sinh ảnh bằng AI | Dùng AI tạo khung hình | Nhanh | Mỗi khung khác nét, khó giữ nhất quán với bộ tranh gốc | Thấp nhưng rủi ro chất lượng |

→ Làm **thử với 1 con (Khỉ)** trước khi nhân ra 12 (xem Giai đoạn 3).

### Q2. Phong cách âm thanh
Đề xuất: nhạc nền **ngũ cung, nhạc cụ dân tộc** (khớp chủ đề mỗi con một nhạc cụ): Làng Quê — sáo trúc + đàn bầu; Rừng Trúc — đàn tranh, tiếng gió; Động Hồ Tinh — trống, cồng chiêng.
Nguồn: tự sáng tác / thuê / thư viện có giấy phép (freesound CC0, Unity Asset Store).

### Q3. Mức "chân thật"
Đề xuất **"tranh dân gian sống động"** (giữ nét vẽ Đông Hồ / Hàng Trống của bộ nhân vật) chứ không phải hiện thực ảnh chụp — ánh sáng, chuyển động và âm thanh mới là thứ tạo cảm giác thật.

---

## 3. Lộ trình theo giai đoạn

Thứ tự ưu tiên: làm trước những thứ **rẻ mà tác động lớn** (Giai đoạn 1 chỉ cần code), song song chuẩn bị art cho Giai đoạn 3.

```
Tuần:        1    2    3    4    5    6    7    8    9    10
GĐ1 Game feel  ████
GĐ2 Combat          ██████
GĐ3 Anim (thử)   ░░░░██████            (░ = art chuẩn bị song song)
GĐ3 Anim (12 con)              ██████████████
GĐ4 Ánh sáng/không khí     ██████
GĐ5 Âm thanh            ░░░░████
GĐ6 Quái & màn có art                  ██████████
GĐ7 Hoàn thiện                                   ██████
```

---

### Giai đoạn 1 — "Game feel": làm mọi hành động có lực (chỉ code, ~1 tuần)

Tham khảo: bài nói *"Juice it or lose it"*, *Celeste* (Maddy Thorson về tay cảm), sách *Game Feel* (Steve Swink).

| # | Việc | Chi tiết | Ai | Công |
|---|---|---|---|---|
| 1.1 | **Hitstop** (đứng hình khi trúng đòn) | Dừng `Time.timeScale` 0.04–0.08s khi đánh trúng / bị đánh; boss trúng đòn nặng 0.1s. Thêm `HitStop.Freeze(seconds)` gọi từ `Health.Damaged` | Code | S |
| 1.2 | **Rung màn hình** | `CameraShake` trong `ZodiacCameraFollow`: biên độ theo cường độ (đánh thường nhỏ, dậm đất/boss đáp lớn), giảm dần theo trauma² | Code | S |
| 1.3 | **Co giãn khi nhảy/đáp** (squash & stretch) | Khi bật nhảy: kéo dài dọc 1.15; khi chạm đất: bẹp 0.8 trong 0.1s, tỉ lệ theo tốc độ rơi | Code | S |
| 1.4 | **Particle System** thay `ZodiacFx` | Bụi chân khi chạy/đáp/đổi hướng, tia lửa khi trúng đòn, mảnh vỡ khi quái chết, lông/lá khi lướt; prefab hiệu ứng dùng chung + object pool | Code | M |
| 1.5 | **Phản hồi trúng đòn rõ ràng** | Quái nháy trắng (đã có) + bị đẩy + số sát thương nhỏ bay lên; người chơi trúng đòn: màn hình loé đỏ viền, chậm 0.1s | Code | S |
| 1.6 | **Chuyển cảnh** | Fade đen giữa các màn, khi hồi sinh ở đèn lồng; banner tên màn trượt vào | Code | S |
| 1.7 | **Tinh chỉnh điều khiển** | Gia tốc đổi hướng khi chạy, "apex hang" (trọng lực giảm ở đỉnh cú nhảy như Celeste), giới hạn tốc độ rơi khi giữ ↓ nhanh hơn; thêm bảng số liệu để thử từng con | Code | S |

**Tiêu chí xong:** quay video 30s đánh 1 quái — nhìn tắt tiếng vẫn thấy rõ lúc trúng đòn; người thử nói "đánh sướng tay hơn".

---

### Giai đoạn 2 — Chiều sâu chiến đấu (~1.5 tuần)

| # | Việc | Chi tiết | Tham khảo | Ai | Công |
|---|---|---|---|---|---|
| 2.1 | **Combo 3 đòn** | J liên tiếp: chém – chém ngược – đòn mạnh (đẩy văng). Cửa sổ nối đòn 0.35s; đòn 3 chậm hơn, mạnh hơn | Dead Cells, Hollow Knight | Code | M |
| 2.2 | **Đánh lên / đánh xuống (pogo)** | Giữ ↑ + J chém lên; đang trên không giữ ↓ + J chém xuống, trúng quái/gai thì nảy lên | Hollow Knight | Code | M |
| 2.3 | **Năng lượng "Khí" cho chiêu** | Thay hồi chiêu thuần thời gian: đánh trúng nạp khí, chiêu tốn khí (vẫn giữ hồi chiêu ngắn). Khuyến khích đánh áp sát thay vì chờ | Soul của Hollow Knight | Code | M |
| 2.4 | **Né / lăn cho mọi con** | Phím L: lăn ngắn bất tử 0.25s (Chuột vẫn giữ chiêu lướt riêng, mạnh hơn) | Dead Cells | Code | S |
| 2.5 | **Quái phản ứng tốt hơn** | Trạng thái bị đánh (khựng), bị hất tung khi nhận đòn mạnh, ngã chết có animation; một số quái có giáp (đỡ đòn phía trước) | Hollow Knight | Code | M |
| 2.6 | **Cân bằng 12 con** | Bảng số liệu sát thương/giây, thời gian qua màn cho từng con; chỉnh chỉ số trong `ZodiacCharacterDefs` | — | Code | S |

**Tiêu chí xong:** mỗi con giáp có ít nhất 2 cách xử lý một con quái; test tự động thêm combo + pogo.

---

### Giai đoạn 3 — Animation nhân vật thật (việc lớn nhất)

#### 3a. Chuẩn bị kỹ thuật (Code, ~3 ngày)
- Cài package **2D Animation**, **2D PSD Importer**, **2D IK** (Package Manager).
- Animator mới, dùng chung cho cả 12 con qua **AnimatorOverrideController** (mỗi con chỉ thay clip):

```
Locomotion: Idle ⇄ Walk ⇄ Run ⇄ Skid(đổi hướng)
Air:        JumpUp → Apex → Fall → Land        Glide (Gà) · DoubleJump (Khỉ)
Combat:     Attack1 → Attack2 → Attack3 · AttackUp · AttackDown · Skill · SkillLoop
Reaction:   Hurt · Death · Respawn · Victory
```

- **Animation Event** điều khiển hitbox: bật hitbox đúng khung hình vung tay (thay bộ đếm `startupTime/activeTime` trong `PlayerMeleeAttack`), sinh đạn đúng lúc nhả đòn trong `PlayerSkill`.
- Animation chân khớp tốc độ chạy (tham số Speed → playback speed) để chân không "trượt".

#### 3b. Chuẩn bị art (Art, song song)
- **Vẽ lại bản sạch** (không cây/hoa/thảm) và **tách lớp** từng con thành file `.psb`:
  đầu · thân · cánh tay trên/dưới ×2 · bàn tay ×2 · đùi/cẳng chân ×2 · bàn chân ×2 · đuôi (nếu có) · vạt áo trước/sau · nhạc cụ.
- Vẽ thêm phần bị che (phần thân sau tay, chân sau) để khi xoay khớp không lộ lỗ.
- Quy ước: quay mặt sang phải, đứng thẳng (T-pose nhẹ), cùng tỉ lệ cho cả 12 con.

#### 3c. Thử nghiệm với Khỉ (~1 tuần)
Rig + 12 animation tối thiểu: Idle, Walk, Run, JumpUp, Fall, Land, Attack1–3, Skill (ném chũm chọe), Hurt, Death.
**Tiêu chí:** đạt chất lượng nhìn được → nhân rộng; không đạt → cân nhắc phương án B cho các đòn quan trọng.

#### 3d. Nhân rộng 11 con (~3–4 tuần)
Gom theo dáng để dùng lại animation: **dáng người đứng** (Hổ, Mèo, Chó, Ngựa, Dê, Trâu, Khỉ, Chuột, Rồng) chung bộ khung; **dáng riêng**: Rắn (trườn), Gà (cánh), Heo (to tròn, ngồi thảm → cho đứng). Mỗi con thêm 1–2 animation riêng cho chiêu (gõ chiêng, thổi kèn, gảy đàn…).

---

### Giai đoạn 4 — Ánh sáng & không khí (~1.5 tuần)

| # | Việc | Chi tiết | Ai | Công |
|---|---|---|---|---|
| 4.1 | **Chuyển sang URP 2D Renderer** | Tạo `Renderer2DData`, gán vào PC/Mobile RP asset; kiểm tra lại mọi sprite (material Sprite-Lit-Default) | Code | S |
| 4.2 | **Light2D** | Ánh sáng toàn cục theo màn (chiều vàng / rừng xanh mờ / hang tối); đèn lồng checkpoint là Point Light nhấp nháy; ma trơi, cầu lửa, tinh thể hang tự phát sáng; nhân vật có đèn nhỏ quanh mình trong hang | Code | M |
| 4.3 | **Post-processing** | Bloom (đèn lồng, lửa), Vignette nhẹ, Color grading riêng từng màn, Chromatic aberration thoáng qua khi trúng đòn nặng | Code | S |
| 4.4 | **Thời tiết & hạt môi trường** | Làng: lúa lay, chim bay, khói bếp; Rừng: sương trôi, lá trúc rơi, đom đóm; Hang: bụi lơ lửng, nước nhỏ giọt | Code + Art | M |
| 4.5 | **Nền parallax có art** | Thay hình học bằng tranh nền nhiều lớp (núi, đình làng, đồng lúa, rừng trúc, vách hang) theo phong cách tranh dân gian | Art | L |
| 4.6 | **Tilemap cho địa hình** | Đổi khối màu thành **Tilemap + Rule Tile** (đất có cỏ, đá có rêu, mép vực, góc) — builder vẫn đọc bản đồ ký tự nhưng xuất ra Tilemap | Code + Art | M |
| 4.7 | **Vật thể sống** | Cỏ/lúa rẽ ra khi đi qua, bục tre rung khi đáp, đèn lồng đung đưa, nước có phản chiếu | Code | M |

**Tiêu chí xong:** ảnh chụp màn hình mỗi màn nhìn ra "nơi chốn" mà không cần biết tên màn.

---

### Giai đoạn 5 — Âm thanh (~1 tuần code + thời gian sản xuất audio)

| # | Việc | Chi tiết | Ai | Công |
|---|---|---|---|---|
| 5.1 | Hệ thống âm thanh | `AudioManager`: nhạc nền chuyển mượt giữa màn/boss, SFX có pool, ngẫu nhiên cao độ ±5% để không lặp nhàm, Audio Mixer (Nhạc / Hiệu ứng / Môi trường) | Code | M |
| 5.2 | SFX nhân vật | Bước chân theo mặt đất (đất/đá/tre), nhảy, đáp, vung đòn, trúng đòn, bị thương, chết | Audio | M |
| 5.3 | SFX chiêu theo nhạc cụ | Mỗi chiêu một âm nhạc cụ thật: tiếng chiêng (Trâu), kèn (Gà), đàn nhị (Mèo), chũm chọe (Khỉ), đàn tranh (Rồng)… — điểm nhấn bản sắc của game | Audio | M |
| 5.4 | Nhạc nền | 3 màn + boss (2 phase) + màn hình chọn nhân vật | Audio | L |
| 5.5 | Âm thanh môi trường | Gió, côn trùng, chim, nước nhỏ giọt, tiếng vọng trong hang (Reverb Zone) | Audio | S |

---

### Giai đoạn 6 — Quái & màn chơi có art (~2 tuần)

| # | Việc | Chi tiết | Ai | Công |
|---|---|---|---|---|
| 6.1 | Art + rig cho 5 loại quái | Lính Đất Nung, Bọ Gai, Ma Trơi, Cóc Tía, Thạch Sư — cùng phong cách nhân vật; animation: đi, báo hiệu đòn, tấn công, bị đánh, chết | Art + Code | L |
| 6.2 | Boss Hồ Tinh | Rig lớn: 9 đuôi chuyển động độc lập (IK/xương chuỗi), biểu cảm mặt theo đòn, đoạn mở màn (cutscene ngắn) và đoạn chuyển phase hoá hình | Art + Code | L |
| 6.3 | Thêm quái theo truyền thuyết | Ngư Tinh (màn sông nước), Mộc Tinh (rừng) — 2 boss còn lại trong truyện Lạc Long Quân | Thiết kế | M |
| 6.4 | Tinh chỉnh màn chơi | Ghi lại đường đi người chơi thử (heatmap chết), sửa chỗ khó vô lý, thêm lối bí mật + đồng xu ẩn | Code | M |

---

### Giai đoạn 7 — Hoàn thiện (~1.5 tuần)

| # | Việc | Ai | Công |
|---|---|---|---|
| 7.1 | Menu tạm dừng (Esc), cài đặt âm lượng / đổi phím / độ phân giải | Code | M |
| 7.2 | Lưu tiến trình: màn đã qua, xu, con giáp đã mở khoá | Code | S |
| 7.3 | Chọn màn, màn hướng dẫn tương tác thay biển chữ | Code | M |
| 7.4 | Hỗ trợ tay cầm đầy đủ + rung tay cầm khi trúng đòn | Code | S |
| 7.5 | Hiệu năng: object pool cho đạn/hiệu ứng, Sprite Atlas, kiểm tra 60fps máy yếu | Code | S |
| 7.6 | Mở rộng test tự động: combo, pogo, animation event, tải 3 màn liên tiếp; CI chạy test | Code | S |

---

## 4. Việc nên làm ngay (2 tuần đầu)

1. **Giai đoạn 1 toàn bộ** (hitstop, rung, squash/stretch, particle, chuyển cảnh) — chỉ cần code, thấy khác biệt ngay.
2. **4.1 + 4.2 + 4.3** chuyển 2D Renderer, đèn lồng/lửa phát sáng, bloom — rẻ mà đổi hẳn không khí.
3. **2.1 + 2.2** combo và pogo.
4. Song song: nhờ hoạ sĩ **tách lớp nhân vật Khỉ** theo quy ước ở 3b để thử rig.

## 5. Rủi ro

| Rủi ro | Giảm thiểu |
|---|---|
| Thiếu nguồn lực art cho 12 con + quái | Thử 1 con trước; dùng chung bộ xương/animation theo nhóm dáng; ưu tiên 4–6 con "chủ lực" nếu phải cắt |
| Rig xương trông cứng | Thêm vài khung vẽ tay cho khoảnh khắc quan trọng (vung đòn mạnh, chiêu) kết hợp xương |
| Chuyển 2D Renderer làm sai màu sprite cũ | Làm trên nhánh riêng, so ảnh chụp trước/sau |
| Thêm combo/khí làm lệch cân bằng 12 con | Bảng số liệu + test tự động đo thời gian hạ quái cho từng con |

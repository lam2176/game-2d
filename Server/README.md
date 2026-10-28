# Máy chủ tài khoản 12 Con Giáp

ASP.NET Core (.NET 10) + SQLite. Lưu tài khoản, con giáp đã chọn (mỗi tài khoản một con, không đổi được) và hồ sơ
người chơi (túi đồ, cấp, màn đã mở… dạng JSON). Có trang quản trị web.

## Chạy

```
cd Server/ZodiacServer
dotnet run
```

- Lắng nghe `http://0.0.0.0:5080` (đổi bằng `dotnet run --urls http://0.0.0.0:6000`).
- Dữ liệu: `Server/ZodiacServer/data/zodiac.db` (sao lưu = chép file này khi máy chủ đang tắt). Thư mục `data/` không đưa lên git.
- Trang quản trị: <http://localhost:5080/admin>. Mật khẩu nằm trong `data/admin.txt` (tự tạo lần chạy đầu),
  hoặc đặt biến môi trường `ZODIAC_ADMIN_PASSWORD`.

## Cho điện thoại kết nối

1. Điện thoại và máy tính cùng một mạng Wi-Fi.
2. Mở cổng 5080 trên tường lửa Windows (PowerShell **chạy với quyền Administrator**, làm một lần):
   ```
   New-NetFirewallRule -DisplayName "12 Con Giap - may chu tai khoan (5080)" -Direction Inbound -Protocol TCP -LocalPort 5080 -Action Allow -Profile Private,Public
   ```
3. Trong game, ô **Máy chủ** ở màn đăng nhập mặc định là `http://192.168.1.202:5080` (IP máy tính lúc làm).
   IP đổi thì sửa ô này — xem IP bằng `ipconfig` (dòng IPv4 Address).

Cắm cáp USB thì không cần tường lửa: `adb reverse tcp:5080 tcp:5080` rồi để ô Máy chủ là `http://localhost:5080`.

## Trang quản trị

- Danh sách tài khoản: con giáp, cấp, đồng, màn đã mở, giờ chơi, lần đăng nhập / lưu cuối; tìm theo tên; đếm số người theo từng con giáp.
- Bấm vào một dòng: khoá / mở khoá (kèm lý do — người chơi bị đá ra ngay), đặt lại mật khẩu, đổi con giáp,
  xem / sửa / tải hồ sơ JSON, xoá tài khoản. Sửa hồ sơ, đổi mật khẩu hay đổi con giáp sẽ đăng xuất người chơi
  để lần vào sau game tải bản mới.

## API cho game (`POST /api/<hàm>`, JSON)

| Hàm | Vào | Ra |
|---|---|---|
| `register`, `login` | `p_username`, `p_password` | tài khoản + `token` |
| `resume` | `p_token` | tài khoản (gia hạn phiên 30 ngày) |
| `choose_character` | `p_token`, `p_character` | tài khoản — chỉ được một lần |
| `save_progress` | `p_token`, `p_profile` (object), `p_level`, `p_coins`, `p_unlocked_levels`, `p_play_seconds` | `{}` |
| `logout` | `p_token` | `{}` |

Lỗi: HTTP 400 `{"message": "<mã>"}` — `bad_username`, `bad_password`, `username_taken`, `wrong_login`, `locked`
(sai 5 lần → khoá 5 phút), `banned:<lý do>`, `session_expired`, `character_locked`, `no_character`.
Mật khẩu băm PBKDF2-SHA256; phiên là chuỗi ngẫu nhiên, DB chỉ giữ bản băm SHA-256.

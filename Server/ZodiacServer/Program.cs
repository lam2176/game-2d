// Máy chủ tài khoản 12 Con Giáp: đăng ký / đăng nhập, mỗi tài khoản một con giáp, lưu hồ sơ người chơi (JSON)
// vào SQLite (data/zodiac.db). Trang quản trị ở /admin (mật khẩu trong data/admin.txt hoặc biến ZODIAC_ADMIN_PASSWORD).
//
// Game gọi POST /api/<hàm> với thân JSON; lỗi trả 400 {"message":"<mã lỗi>"} (game tự dịch ra tiếng Việt).

using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Microsoft.Data.Sqlite;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls(builder.Configuration["urls"] ?? "http://0.0.0.0:5080");
builder.WebHost.ConfigureKestrel(k => k.Limits.MaxRequestBodySize = 1024 * 1024);
var app = builder.Build();

string dataDir = Path.GetFullPath(builder.Configuration["data"] ?? Path.Combine(builder.Environment.ContentRootPath, "data"));
Directory.CreateDirectory(dataDir);
var db = new Db(Path.Combine(dataDir, "zodiac.db"));
string adminPassword = AdminPassword(dataDir);

app.Logger.LogInformation("Dữ liệu: {Dir}", dataDir);
app.Logger.LogInformation("Trang quản trị: http://localhost:5080/admin (mật khẩu trong {File})", Path.Combine(dataDir, "admin.txt"));

// ───────── API cho game ─────────

app.MapPost("/api/register", (JsonObject body) =>
{
    string name = Str(body, "p_username").Trim().ToLowerInvariant();
    string password = Str(body, "p_password");
    if (!Rules.Username.IsMatch(name)) return Fail("bad_username");
    if (password.Length < 6 || password.Length > 72) return Fail("bad_password");
    using var c = db.Open();
    if (c.Scalar("select count(*) from accounts where username = $u", ("$u", name)) is long n && n > 0) return Fail("username_taken");
    long id = (long)c.Scalar("insert into accounts (username, password_hash, created_at, last_login_at) values ($u, $h, $t, $t) returning id",
                             ("$u", name), ("$h", Passwords.Hash(password)), ("$t", Now()))!;
    return Results.Ok(db.AccountJson(c, id, db.NewSession(c, id)));
});

app.MapPost("/api/login", (JsonObject body) =>
{
    string name = Str(body, "p_username").Trim().ToLowerInvariant();
    using var c = db.Open();
    var row = c.Row("select id, password_hash, banned, ban_reason, failed_logins, locked_until from accounts where username = $u", ("$u", name));
    if (row == null) return Fail("wrong_login");
    long id = (long)row["id"]!;
    if (row["locked_until"] is string until && DateTime.Parse(until).ToUniversalTime() > DateTime.UtcNow) return Fail("locked");
    if (!Passwords.Verify(Str(body, "p_password"), (string)row["password_hash"]!))
    {
        // Sai 5 lần liền → khoá đăng nhập 5 phút
        long failed = (long)row["failed_logins"]! + 1;
        c.Exec("update accounts set failed_logins = $f, locked_until = $l where id = $id",
               ("$f", failed), ("$l", failed >= 5 ? DateTime.UtcNow.AddMinutes(5).ToString("o") : null), ("$id", id));
        return Fail("wrong_login");
    }
    if ((long)row["banned"]! != 0) return Fail("banned:" + row["ban_reason"]);
    c.Exec("update accounts set failed_logins = 0, locked_until = null, last_login_at = $t where id = $id", ("$t", Now()), ("$id", id));
    return Results.Ok(db.AccountJson(c, id, db.NewSession(c, id)));
});

app.MapPost("/api/resume", (JsonObject body) =>
{
    string token = Str(body, "p_token");
    using var c = db.Open();
    var (id, error) = db.AccountOf(c, token);
    if (error != null) return Fail(error);
    c.Exec("update accounts set last_login_at = $t where id = $id", ("$t", Now()), ("$id", id));
    c.Exec("update sessions set expires_at = $e where token_hash = $h", ("$e", DateTime.UtcNow.AddDays(30).ToString("o")), ("$h", Db.Sha256(token)));
    return Results.Ok(db.AccountJson(c, id, token));
});

app.MapPost("/api/logout", (JsonObject body) =>
{
    using var c = db.Open();
    c.Exec("delete from sessions where token_hash = $h", ("$h", Db.Sha256(Str(body, "p_token"))));
    return Results.Ok(new { });
});

app.MapPost("/api/choose_character", (JsonObject body) =>
{
    string token = Str(body, "p_token");
    string character = Str(body, "p_character");
    if (!Rules.Characters.Contains(character)) return Fail("bad_character");
    using var c = db.Open();
    var (id, error) = db.AccountOf(c, token);
    if (error != null) return Fail(error);
    // Chỉ chọn được một lần: điều kiện "character is null" nằm ngay trong câu update
    if (c.Exec("update accounts set character = $c where id = $id and character is null", ("$c", character), ("$id", id)) == 0)
        return Fail("character_locked");
    return Results.Ok(db.AccountJson(c, id, token));
});

app.MapPost("/api/save_progress", (JsonObject body) =>
{
    using var c = db.Open();
    var (id, error) = db.AccountOf(c, Str(body, "p_token"));
    if (error != null) return Fail(error);
    if (c.Scalar("select character from accounts where id = $id", ("$id", id)) is not string) return Fail("no_character");
    if (body["p_profile"] is not JsonObject profile) return Fail("bad_profile");
    c.Exec("""
           update accounts set profile = $p, level = max(1, $lv), coins = max(0, $co), unlocked_levels = max(1, $ul),
                  play_seconds = max(0, $ps), saved_at = $t
           where id = $id
           """,
           ("$p", profile.ToJsonString()), ("$lv", Int(body, "p_level")), ("$co", Int(body, "p_coins")),
           ("$ul", Int(body, "p_unlocked_levels")), ("$ps", Int(body, "p_play_seconds")), ("$t", Now()), ("$id", id));
    return Results.Ok(new { });
});

// ───────── Trang quản trị ─────────

app.MapGet("/", () => Results.Redirect("/admin"));
app.MapGet("/admin", () => Results.File(Path.Combine(builder.Environment.ContentRootPath, "admin.html"), "text/html; charset=utf-8"));

var admin = app.MapGroup("/admin/api").AddEndpointFilter(async (ctx, next) =>
{
    var key = ctx.HttpContext.Request.Headers["X-Admin-Key"].ToString();
    if (!AdminGate.Check(key, adminPassword)) return Results.Json(new { message = "Sai mật khẩu quản trị" }, statusCode: 401);
    return await next(ctx);
});

admin.MapGet("/accounts", (string? q) =>
{
    using var c = db.Open();
    var list = c.Rows("""
        select id, username, character, level, coins, unlocked_levels, play_seconds, banned, ban_reason,
               created_at, last_login_at, saved_at, length(profile) as profile_size
        from accounts where $q is null or username like '%' || $q || '%'
        order by coalesce(saved_at, created_at) desc
        """, ("$q", string.IsNullOrWhiteSpace(q) ? null : q.Trim().ToLowerInvariant()));
    var stats = c.Rows("select character, count(*) as count from accounts group by character");
    return Results.Ok(new { accounts = list, stats });
});

admin.MapGet("/accounts/{id:long}", (long id) =>
{
    using var c = db.Open();
    var row = c.Row("""
        select id, username, character, level, coins, unlocked_levels, play_seconds, banned, ban_reason, failed_logins,
               locked_until, created_at, last_login_at, saved_at, profile,
               (select count(*) from sessions s where s.account_id = accounts.id and s.expires_at > $now) as sessions
        from accounts where id = $id
        """, ("$id", id), ("$now", DateTime.UtcNow.ToString("o")));
    return row == null ? Results.NotFound() : Results.Ok(row);
});

admin.MapPost("/accounts/{id:long}/ban", (long id, JsonObject body) =>
{
    bool banned = body["banned"]?.GetValue<bool>() ?? false;
    using var c = db.Open();
    c.Exec("update accounts set banned = $b, ban_reason = $r where id = $id",
           ("$b", banned ? 1 : 0), ("$r", banned ? Str(body, "reason") : null), ("$id", id));
    if (banned) c.Exec("delete from sessions where account_id = $id", ("$id", id));   // đá ra ngay
    return Results.Ok(new { });
});

admin.MapPost("/accounts/{id:long}/password", (long id, JsonObject body) =>
{
    string password = Str(body, "password");
    if (password.Length < 6 || password.Length > 72) return Results.BadRequest(new { message = "Mật khẩu 6–72 ký tự" });
    using var c = db.Open();
    c.Exec("update accounts set password_hash = $h, failed_logins = 0, locked_until = null where id = $id",
           ("$h", Passwords.Hash(password)), ("$id", id));
    c.Exec("delete from sessions where account_id = $id", ("$id", id));
    return Results.Ok(new { });
});

admin.MapPost("/accounts/{id:long}/character", (long id, JsonObject body) =>
{
    string? character = Str(body, "character") is { Length: > 0 } s ? s : null;
    if (character != null && !Rules.Characters.Contains(character)) return Results.BadRequest(new { message = "Con giáp không hợp lệ" });
    using var c = db.Open();
    c.Exec("update accounts set character = $c where id = $id", ("$c", character), ("$id", id));
    c.Exec("delete from sessions where account_id = $id", ("$id", id));
    return Results.Ok(new { });
});

admin.MapPost("/accounts/{id:long}/profile", (long id, JsonObject body) =>
{
    // Sửa hồ sơ tay: đăng xuất người chơi để lần vào sau game tải bản này về (không ghi đè bằng bản trên máy)
    string? json = null;
    if (body["profile"] is JsonValue v && v.GetValue<string>() is { Length: > 0 } text)
    {
        try
        {
            if (JsonNode.Parse(text) is not JsonObject parsed) throw new FormatException();
            json = parsed.ToJsonString();
        }
        catch (Exception)
        {
            return Results.BadRequest(new { message = "Hồ sơ không phải JSON hợp lệ" });
        }
    }
    using var c = db.Open();
    c.Exec("update accounts set profile = $p, saved_at = $t where id = $id", ("$p", json), ("$t", Now()), ("$id", id));
    c.Exec("delete from sessions where account_id = $id", ("$id", id));
    return Results.Ok(new { });
});

admin.MapDelete("/accounts/{id:long}", (long id) =>
{
    using var c = db.Open();
    c.Exec("delete from sessions where account_id = $id", ("$id", id));
    c.Exec("delete from accounts where id = $id", ("$id", id));
    return Results.Ok(new { });
});

app.Run();

// ───────── Trợ giúp ─────────

static IResult Fail(string code) => Results.BadRequest(new { message = code });
static string Now() => DateTime.UtcNow.ToString("o");
static string Str(JsonObject o, string key) => o[key] is JsonValue v && v.TryGetValue<string>(out var s) ? s : "";
static long Int(JsonObject o, string key) => o[key] is JsonValue v && v.TryGetValue<long>(out var n) ? n : 0;

static string AdminPassword(string dataDir)
{
    var env = Environment.GetEnvironmentVariable("ZODIAC_ADMIN_PASSWORD");
    if (!string.IsNullOrEmpty(env)) return env;
    string file = Path.Combine(dataDir, "admin.txt");
    if (File.Exists(file)) return File.ReadAllText(file).Trim();
    string generated = Convert.ToHexString(RandomNumberGenerator.GetBytes(8)).ToLowerInvariant();
    File.WriteAllText(file, generated);
    return generated;
}

static class Rules
{
    public static readonly Regex Username = new("^[a-z0-9_]{3,20}$");
    public static readonly HashSet<string> Characters = new()
    {
        "Rat_Ty", "Ox_Suu", "Tiger_Dan", "Cat_Mao", "Dragon_Thin", "Snake_Ty",
        "Horse_Ngo", "Goat_Mui", "Monkey_Than", "Rooster_Dau", "Dog_Tuat", "Pig_Hoi",
    };
}

/// <summary>Mật khẩu: PBKDF2-SHA256, 100.000 vòng, muối 16 byte — "pbkdf2$vòng$muối$băm".</summary>
static class Passwords
{
    const int Iterations = 100_000;

    public static string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, 32);
        return $"pbkdf2${Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    public static bool Verify(string password, string stored)
    {
        var parts = stored.Split('$');
        if (parts.Length != 4 || parts[0] != "pbkdf2") return false;
        var salt = Convert.FromBase64String(parts[2]);
        var expected = Convert.FromBase64String(parts[3]);
        var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, int.Parse(parts[1]), HashAlgorithmName.SHA256, expected.Length);
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }
}

/// <summary>Mật khẩu quản trị, chặn dò: sai 10 lần trong một phút thì từ chối hết tới phút sau.</summary>
static class AdminGate
{
    static readonly object Lock = new();
    static int failures;
    static DateTime window = DateTime.UtcNow;

    public static bool Check(string given, string expected)
    {
        lock (Lock)
        {
            if (DateTime.UtcNow - window > TimeSpan.FromMinutes(1))
            {
                window = DateTime.UtcNow;
                failures = 0;
            }
            if (failures >= 10) return false;
            bool ok = CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(given), Encoding.UTF8.GetBytes(expected));
            if (!ok) failures++;
            return ok;
        }
    }
}

class Db
{
    readonly string connectionString;

    public Db(string path)
    {
        connectionString = new SqliteConnectionStringBuilder { DataSource = path, Pooling = true }.ToString();
        using var c = Open();
        c.Exec("pragma journal_mode = wal");
        c.Exec("""
            create table if not exists accounts (
                id              integer primary key autoincrement,
                username        text not null unique,
                password_hash   text not null,
                character       text,
                level           integer not null default 1,
                coins           integer not null default 0,
                unlocked_levels integer not null default 1,
                play_seconds    integer not null default 0,
                profile         text,
                banned          integer not null default 0,
                ban_reason      text,
                failed_logins   integer not null default 0,
                locked_until    text,
                created_at      text not null,
                last_login_at   text,
                saved_at        text
            );
            create table if not exists sessions (
                token_hash  text primary key,
                account_id  integer not null references accounts (id) on delete cascade,
                created_at  text not null,
                expires_at  text not null
            );
            create index if not exists sessions_account_idx on sessions (account_id);
            """);
    }

    public SqliteConnection Open()
    {
        var c = new SqliteConnection(connectionString);
        c.Open();
        c.Exec("pragma foreign_keys = on");
        return c;
    }

    public static string Sha256(string s) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(s))).ToLowerInvariant();

    /// <summary>Phiên mới (30 ngày): trả token gốc, trong DB chỉ giữ bản băm.</summary>
    public string NewSession(SqliteConnection c, long account)
    {
        string token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
        c.Exec("delete from sessions where account_id = $a and expires_at < $now", ("$a", account), ("$now", Now()));
        c.Exec("insert into sessions (token_hash, account_id, created_at, expires_at) values ($h, $a, $now, $e)",
               ("$h", Sha256(token)), ("$a", account), ("$now", Now()), ("$e", DateTime.UtcNow.AddDays(30).ToString("o")));
        return token;
    }

    /// <summary>Token → id tài khoản, hoặc mã lỗi (hết phiên / bị khoá).</summary>
    public (long id, string? error) AccountOf(SqliteConnection c, string token)
    {
        var row = c.Row("""
            select a.id, a.banned, a.ban_reason from sessions s join accounts a on a.id = s.account_id
            where s.token_hash = $h and s.expires_at > $now
            """, ("$h", Sha256(token)), ("$now", Now()));
        if (row == null) return (0, "session_expired");
        if ((long)row["banned"]! != 0) return (0, "banned:" + row["ban_reason"]);
        return ((long)row["id"]!, null);
    }

    public object AccountJson(SqliteConnection c, long id, string token)
    {
        var row = c.Row("select username, character, level, profile from accounts where id = $id", ("$id", id))!;
        return new
        {
            token,
            username = row["username"],
            character = row["character"] ?? "",
            level = row["level"],
            profile = row["profile"] ?? "",
        };
    }

    static string Now() => DateTime.UtcNow.ToString("o");
}

static class SqliteExtensions
{
    static SqliteCommand Command(SqliteConnection c, string sql, (string name, object? value)[] args)
    {
        var cmd = c.CreateCommand();
        cmd.CommandText = sql;
        foreach (var (name, value) in args) cmd.Parameters.AddWithValue(name, value ?? DBNull.Value);
        return cmd;
    }

    public static int Exec(this SqliteConnection c, string sql, params (string, object?)[] args)
    {
        using var cmd = Command(c, sql, args);
        return cmd.ExecuteNonQuery();
    }

    public static object? Scalar(this SqliteConnection c, string sql, params (string, object?)[] args)
    {
        using var cmd = Command(c, sql, args);
        var v = cmd.ExecuteScalar();
        return v is DBNull ? null : v;
    }

    public static List<Dictionary<string, object?>> Rows(this SqliteConnection c, string sql, params (string, object?)[] args)
    {
        using var cmd = Command(c, sql, args);
        using var r = cmd.ExecuteReader();
        var list = new List<Dictionary<string, object?>>();
        while (r.Read())
        {
            var row = new Dictionary<string, object?>();
            for (int i = 0; i < r.FieldCount; i++) row[r.GetName(i)] = r.IsDBNull(i) ? null : r.GetValue(i);
            list.Add(row);
        }
        return list;
    }

    public static Dictionary<string, object?>? Row(this SqliteConnection c, string sql, params (string, object?)[] args) =>
        c.Rows(sql, args).FirstOrDefault();
}

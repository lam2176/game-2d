using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

/// <summary>
/// Tài khoản online trên máy chủ riêng (Server/ZodiacServer — ASP.NET Core + SQLite): đăng ký / đăng nhập / vào lại
/// bằng phiên đã lưu, chọn con giáp (một lần), đẩy hồ sơ (<see cref="PlayerProfile"/>) lên máy chủ qua POST /api/&lt;hàm&gt;.
/// Địa chỉ máy chủ đổi được ở màn đăng nhập (<see cref="Server"/>). Hồ sơ vẫn lưu file trên máy như cũ (bản nháp),
/// mỗi lần lưu đánh dấu "chưa đẩy" — gom lại đẩy mỗi <see cref="UploadInterval"/> giây, khi đổi scene và khi
/// ứng dụng xuống nền. Mất mạng thì giữ dấu "chưa đẩy" (cả khi tắt game) và lần đăng nhập sau đẩy bản trên máy lên.
/// </summary>
public class ZodiacCloud : MonoBehaviour
{
    // Máy chủ mặc định: máy tính chạy Server/ZodiacServer trong mạng Wi-Fi nhà (đổi được ở màn đăng nhập)
#if UNITY_EDITOR
    const string DefaultServer = "http://localhost:5080";
#else
    const string DefaultServer = "http://192.168.1.202:5080";
#endif
    const string ServerKey = "zodiac.server";

    const float UploadInterval = 8f;
    /// <summary>Báo "đang online" cho trang quản trị.</summary>
    const float PingInterval = 60f;
    const string TokenKey = "zodiac.session";

    [Serializable]
    public class Account
    {
        public string token = "", username = "", character = "", profile = "";
        public int level;
    }

    [Serializable]
    class ErrorBody
    {
        public string message = "";
    }

    /// <summary>Bật tài khoản online — tắt thì game chạy như cũ (ô lưu trên máy, không đăng nhập).</summary>
    public static bool Configured => true;

    /// <summary>Địa chỉ máy chủ, vd. http://192.168.1.202:5080.</summary>
    public static string Server
    {
        get => PlayerPrefs.GetString(ServerKey, DefaultServer);
        set
        {
            string v = (value ?? "").Trim().TrimEnd('/');
            if (v.Length > 0 && !v.StartsWith("http://") && !v.StartsWith("https://")) v = "http://" + v;
            if (v.Length == 0 || v == DefaultServer) PlayerPrefs.DeleteKey(ServerKey);
            else PlayerPrefs.SetString(ServerKey, v);
            PlayerPrefs.Save();
        }
    }
    public static Account Current { get; private set; }
    public static bool LoggedIn => Current != null;
    public static bool HasCharacter => LoggedIn && !string.IsNullOrEmpty(Current.character);
    public const string Offline = "Không kết nối được máy chủ — kiểm tra mạng rồi thử lại";
    public static bool HasSavedSession => !string.IsNullOrEmpty(PlayerPrefs.GetString(TokenKey, ""));
    /// <summary>Phiên hết hạn / tài khoản bị khoá giữa chừng: (lời báo).</summary>
    public static event Action<string> SessionLost;

    static ZodiacCloud runner;
    static int dirtyStamp, sentStamp;
    float nextUpload, nextPing;
    bool uploading;

    static string DirtyKey => "zodiac.cloudDirty." + Current.username;

    // ───────── Tài khoản ─────────

    public static void Register(string username, string password, Action<string> done) =>
        Call("register", Body(("p_username", username), ("p_password", password)), AccountReply(done, true));

    public static void Login(string username, string password, Action<string> done) =>
        Call("login", Body(("p_username", username), ("p_password", password)), AccountReply(done, true));

    /// <summary>Vào lại bằng phiên lưu trên máy (không phải gõ mật khẩu).</summary>
    public static void Resume(Action<string> done)
    {
        if (!HasSavedSession)
        {
            done?.Invoke("Chưa đăng nhập");
            return;
        }
        Call("resume", Body(("p_token", PlayerPrefs.GetString(TokenKey))), (ok, body) =>
        {
            if (!ok && body != Offline) ForgetSession();   // hết hạn / bị khoá: bỏ phiên cũ để hiện màn đăng nhập
            AccountReply(done, true)(ok, body);
        });
    }

    /// <summary>Chọn con giáp cho tài khoản (chỉ được một lần, máy chủ kiểm tra).</summary>
    public static void ChooseCharacter(string character, Action<string> done)
    {
        if (!LoggedIn)
        {
            done?.Invoke("Chưa đăng nhập");
            return;
        }
        Call("choose_character", Body(("p_token", Current.token), ("p_character", character)), AccountReply(done, false));
    }

    /// <summary>Đẩy nốt hồ sơ rồi đăng xuất; hồ sơ trên máy trở về ô lưu thường.</summary>
    public static void Logout()
    {
        if (!LoggedIn) return;
        var token = Current.token;
        if (runner != null) runner.Upload(() => Call("logout", Body(("p_token", token)), null));
        Current = null;
        ForgetSession();
        PlayerProfile.LeaveAccount();
    }

    static void ForgetSession()
    {
        PlayerPrefs.DeleteKey(TokenKey);
        PlayerPrefs.Save();
    }

    /// <summary>Phản hồi đăng nhập / đăng ký / vào lại / chọn con giáp: nạp tài khoản và hồ sơ của nó.</summary>
    static Action<bool, string> AccountReply(Action<string> done, bool loadProfile) => (ok, body) =>
    {
        if (!ok)
        {
            done?.Invoke(body);
            return;
        }
        var account = JsonUtility.FromJson<Account>(body);
        Current = account;
        PlayerPrefs.SetString(TokenKey, account.token);
        PlayerPrefs.Save();
        if (loadProfile)
        {
            // Bản trên máy chưa kịp đẩy (mất mạng / tắt ngang) thì mới hơn bản trên máy chủ: dùng nó và đẩy lên
            bool localNewer = PlayerPrefs.GetInt(DirtyKey, 0) == 1;
            PlayerProfile.UseAccount(account.username, account.profile, localNewer);
            if (localNewer) MarkDirty();
        }
        Ensure();
        done?.Invoke(null);
    };

    // ───────── Đẩy hồ sơ ─────────

    /// <summary>Hồ sơ vừa lưu xuống máy — đẩy lên ở lượt gom kế tiếp.</summary>
    public static void MarkDirty()
    {
        if (!LoggedIn || !HasCharacter) return;
        dirtyStamp++;
        PlayerPrefs.SetInt(DirtyKey, 1);
        Ensure();
    }

    static void Ensure()
    {
        if (runner != null) return;
        var go = new GameObject("ZodiacCloud");
        DontDestroyOnLoad(go);
        runner = go.AddComponent<ZodiacCloud>();
    }

    void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;
    void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;
    void OnSceneLoaded(Scene scene, LoadSceneMode mode) => Upload(null);
    void OnApplicationPause(bool paused)
    {
        if (paused) Upload(null);
    }
    void OnApplicationQuit() => Upload(null);

    void Update()
    {
        if (LoggedIn && Time.unscaledTime >= nextPing)
        {
            nextPing = Time.unscaledTime + PingInterval;
            Call("ping", Body(("p_token", Current.token)), null);
        }
        if (Time.unscaledTime < nextUpload) return;
        nextUpload = Time.unscaledTime + UploadInterval;
        Upload(null);
    }

    void Upload(Action then)
    {
        if (uploading || !HasCharacter || dirtyStamp == sentStamp)
        {
            then?.Invoke();
            return;
        }
        var p = PlayerProfile.Current;
        int stamp = dirtyStamp;
        string user = Current.username;
        var body = new StringBuilder("{");
        body.Append("\"p_token\":").Append(Quote(Current.token));
        body.Append(",\"p_profile\":").Append(JsonUtility.ToJson(p));
        body.Append(",\"p_level\":").Append(p.Progress(Current.character).level);
        body.Append(",\"p_coins\":").Append(p.coins);
        body.Append(",\"p_unlocked_levels\":").Append(p.unlockedLevels);
        body.Append(",\"p_play_seconds\":").Append(Mathf.FloorToInt(p.playSeconds));
        body.Append('}');
        uploading = true;
        Call("save_progress", body.ToString(), (ok, reply) =>
        {
            uploading = false;
            if (ok)
            {
                sentStamp = stamp;
                if (stamp == dirtyStamp) PlayerPrefs.SetInt("zodiac.cloudDirty." + user, 0);
            }
            else if (reply == Messages["session_expired"] || reply.StartsWith(Messages["banned"]))
            {
                // Quản trị vừa sửa hồ sơ / khoá tài khoản: bản trên máy chủ là bản đúng, bỏ dấu "chưa đẩy"
                PlayerPrefs.SetInt("zodiac.cloudDirty." + user, 0);
                Current = null;
                ForgetSession();
                SessionLost?.Invoke(reply);
            }
            else Debug.LogWarning("Chưa đẩy được hồ sơ lên máy chủ: " + reply);
            then?.Invoke();
        });
    }

    // ───────── Gọi máy chủ ─────────

    static void Call(string function, string json, Action<bool, string> done)
    {
        Ensure();
        runner.StartCoroutine(Send(function, json, done));
    }

    static IEnumerator Send(string function, string json, Action<bool, string> done)
    {
        using var req = new UnityWebRequest(Server + "/api/" + function, "POST");
        req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        req.timeout = 15;
        yield return req.SendWebRequest();
        if (req.result == UnityWebRequest.Result.Success)
        {
            done?.Invoke(true, req.downloadHandler.text);
            yield break;
        }
        if (req.result == UnityWebRequest.Result.ConnectionError || req.responseCode == 0)
        {
            done?.Invoke(false, Offline);
            yield break;
        }
        string message = "";
        try
        {
            message = JsonUtility.FromJson<ErrorBody>(req.downloadHandler.text)?.message ?? "";
        }
        catch (Exception)
        {
            // thân lỗi không phải JSON
        }
        Debug.LogWarning($"rpc/{function} lỗi {req.responseCode}: {req.downloadHandler.text}");
        done?.Invoke(false, Translate(message, req.responseCode));
    }

    static readonly System.Collections.Generic.Dictionary<string, string> Messages = new System.Collections.Generic.Dictionary<string, string>
    {
        { "bad_username", "Tên đăng nhập 3–20 ký tự: chữ thường không dấu, số hoặc _" },
        { "bad_password", "Mật khẩu từ 6 đến 72 ký tự" },
        { "username_taken", "Tên đăng nhập này đã có người dùng" },
        { "wrong_login", "Sai tên đăng nhập hoặc mật khẩu" },
        { "locked", "Sai mật khẩu quá nhiều lần — thử lại sau 5 phút" },
        { "banned", "Tài khoản đã bị khoá" },
        { "session_expired", "Phiên đăng nhập đã hết hạn — đăng nhập lại" },
        { "character_locked", "Tài khoản này đã chọn con giáp rồi" },
        { "no_character", "Chưa chọn con giáp" },
        { "bad_character", "Con giáp không hợp lệ" },
    };

    static string Translate(string message, long code)
    {
        if (message.StartsWith("banned:"))
        {
            string reason = message.Substring(7).Trim();
            return Messages["banned"] + (reason.Length > 0 ? ": " + reason : "");
        }
        return Messages.TryGetValue(message, out var text) ? text : $"Máy chủ báo lỗi ({code})";
    }

    // ───────── JSON ─────────

    static string Body(params (string key, string value)[] fields)
    {
        var sb = new StringBuilder("{");
        for (int i = 0; i < fields.Length; i++)
        {
            if (i > 0) sb.Append(',');
            sb.Append(Quote(fields[i].key)).Append(':').Append(Quote(fields[i].value));
        }
        return sb.Append('}').ToString();
    }

    static string Quote(string s)
    {
        var sb = new StringBuilder("\"");
        foreach (char c in s ?? "")
        {
            switch (c)
            {
                case '"': sb.Append("\\\""); break;
                case '\\': sb.Append("\\\\"); break;
                case '\n': sb.Append("\\n"); break;
                case '\r': sb.Append("\\r"); break;
                case '\t': sb.Append("\\t"); break;
                default:
                    if (c < ' ') sb.Append($"\\u{(int)c:x4}");
                    else sb.Append(c);
                    break;
            }
        }
        return sb.Append('"').ToString();
    }
}

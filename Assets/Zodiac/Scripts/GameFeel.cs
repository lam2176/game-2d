using System.Collections;
using UnityEngine;

/// <summary>
/// "Game feel" dùng chung (tham khảo bài nói "Juice it or lose it", Celeste, Hollow Knight):
/// hitstop (đứng hình vài phần trăm giây khi trúng đòn), rung camera, và gói "trúng đòn" kết hợp
/// đứng hình + rung + tia lửa + âm thanh để mọi cú đánh có lực như nhau.
/// </summary>
public class GameFeel : MonoBehaviour
{
    static GameFeel host;
    float freezeUntil;
    bool freezing;

    static GameFeel Host
    {
        get
        {
            if (host != null) return host;
            var go = new GameObject("GameFeel");
            DontDestroyOnLoad(go);
            host = go.AddComponent<GameFeel>();
            return host;
        }
    }

    /// <summary>Đứng hình (thời gian thực) — gọi chồng nhau thì lấy mốc kết thúc xa nhất.</summary>
    public static void HitStop(float seconds)
    {
        if (!Application.isPlaying || seconds <= 0f) return;
        var h = Host;
        h.freezeUntil = Mathf.Max(h.freezeUntil, Time.unscaledTime + seconds);
        if (!h.freezing) h.StartCoroutine(h.Freeze());
    }

    IEnumerator Freeze()
    {
        freezing = true;
        Time.timeScale = 0.02f;
        while (Time.unscaledTime < freezeUntil) yield return null;
        Time.timeScale = 1f;
        freezing = false;
    }

    /// <summary>Rung camera: 0.1 nhẹ (đánh thường) … 0.6 mạnh (boss đập đất).</summary>
    public static void Shake(float trauma) => ZodiacCameraFollow.Main?.AddTrauma(trauma);

    /// <summary>Đánh trúng: đứng hình + rung + tia lửa + tiếng va chạm. heavy cho đòn kết combo / chiêu mạnh.</summary>
    public static void Impact(Vector3 point, Color sparkColor, bool heavy)
    {
        HitStop(heavy ? 0.09f : 0.05f);
        Shake(heavy ? 0.32f : 0.16f);
        ZodiacParticles.Sparks(point, sparkColor, heavy ? 16 : 9);
        var r = ZodiacResources.Get;
        if (r != null) ZodiacAudio.Play(heavy ? r.hitHeavy : r.hit, heavy ? 1f : 0.8f, 0.08f, point);
    }

    void OnDisable()
    {
        if (freezing) Time.timeScale = 1f;
    }
}

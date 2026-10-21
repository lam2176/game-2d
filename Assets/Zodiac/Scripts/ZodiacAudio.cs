using System.Collections;
using UnityEngine;

/// <summary>
/// Phát âm thanh: hiệu ứng (một nhóm clip → chọn ngẫu nhiên, lệch cao độ ±pitchVariation cho đỡ lặp nhàm)
/// và nhạc nền (hai nguồn luân phiên để chuyển bài mượt). Tự tạo object giữ qua các scene khi gọi lần đầu.
/// </summary>
public class ZodiacAudio : MonoBehaviour
{
    const string MusicKey = "Zodiac.MusicVolume", SfxKey = "Zodiac.SfxVolume";
    static float? sfxVolume, musicVolume;
    // Đọc PlayerPrefs lúc dùng lần đầu (không được gọi API Unity trong khởi tạo field tĩnh của MonoBehaviour)
    public static float SfxVolume
    {
        get => sfxVolume ??= PlayerPrefs.GetFloat(SfxKey, 0.9f);
        private set => sfxVolume = value;
    }
    public static float MusicVolume
    {
        get => musicVolume ??= PlayerPrefs.GetFloat(MusicKey, 0.55f);
        private set => musicVolume = value;
    }

    /// <summary>Đổi âm lượng nhạc (menu tạm dừng) — áp ngay cho bài đang phát và lưu lại.</summary>
    public static void SetMusicVolume(float value)
    {
        MusicVolume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(MusicKey, MusicVolume);
        if (host != null && host.music != null && host.music[host.activeMusic].clip != null)
            host.music[host.activeMusic].volume = MusicVolume;
    }

    /// <summary>Đổi âm lượng hiệu ứng và lưu lại.</summary>
    public static void SetSfxVolume(float value)
    {
        SfxVolume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(SfxKey, SfxVolume);
    }

    const int Voices = 20;
    static ZodiacAudio host;

    AudioSource[] voices;
    AudioSource[] music;
    int nextVoice, activeMusic;
    Coroutine musicFade;

    static ZodiacAudio Host
    {
        get
        {
            if (host != null) return host;
            var go = new GameObject("ZodiacAudio");
            DontDestroyOnLoad(go);
            host = go.AddComponent<ZodiacAudio>();
            return host;
        }
    }

    void Awake()
    {
        voices = new AudioSource[Voices];
        for (int i = 0; i < Voices; i++)
        {
            voices[i] = gameObject.AddComponent<AudioSource>();
            voices[i].playOnAwake = false;
        }
        music = new AudioSource[2];
        for (int i = 0; i < 2; i++)
        {
            music[i] = gameObject.AddComponent<AudioSource>();
            music[i].loop = true;
            music[i].playOnAwake = false;
            music[i].volume = 0f;
        }
    }

    /// <summary>Phát một clip ngẫu nhiên trong nhóm. position = null → không lệch trái/phải theo vị trí.</summary>
    public static void Play(AudioClip[] group, float volume = 1f, float pitchVariation = 0.06f, Vector3? position = null)
    {
        if (group == null || group.Length == 0) return;
        Play(group[Random.Range(0, group.Length)], volume, pitchVariation, position);
    }

    public static void Play(AudioClip clip, float volume = 1f, float pitchVariation = 0.06f, Vector3? position = null)
    {
        if (clip == null || !Application.isPlaying) return;
        var h = Host;
        var source = h.voices[h.nextVoice];
        h.nextVoice = (h.nextVoice + 1) % Voices;
        source.pitch = 1f + Random.Range(-pitchVariation, pitchVariation);
        source.panStereo = 0f;
        if (position.HasValue && Camera.main != null)
        {
            // Lệch tai trái/phải nhẹ theo vị trí so với camera, nhỏ dần khi ở xa theo chiều ngang
            // (camera luôn lùi ra sau ~14 unit nên không tính khoảng cách 3D đầy đủ)
            float dx = position.Value.x - Camera.main.transform.position.x;
            source.panStereo = Mathf.Clamp(dx / 14f, -0.6f, 0.6f);
            volume *= Mathf.Clamp01(1.3f - Mathf.Abs(dx) / 22f);
        }
        source.PlayOneShot(clip, volume * SfxVolume);
    }

    /// <summary>Chuyển nhạc nền (mờ dần bài cũ, rõ dần bài mới). Cùng bài đang phát thì bỏ qua.</summary>
    public static void PlayMusic(AudioClip clip, float fadeTime = 1.2f)
    {
        if (!Application.isPlaying) return;
        var h = Host;
        var current = h.music[h.activeMusic];
        if (clip != null && current.clip == clip && current.isPlaying) return;
        if (h.musicFade != null) h.StopCoroutine(h.musicFade);
        h.activeMusic = 1 - h.activeMusic;
        var next = h.music[h.activeMusic];
        next.clip = clip;
        next.volume = 0f;
        if (clip != null) next.Play();
        h.musicFade = h.StartCoroutine(h.Crossfade(current, next, fadeTime));
    }

    IEnumerator Crossfade(AudioSource from, AudioSource to, float time)
    {
        float startFrom = from.volume;
        for (float t = 0f; t < time; t += Time.unscaledDeltaTime)
        {
            float k = t / time;
            from.volume = startFrom * (1f - k);
            to.volume = to.clip != null ? MusicVolume * k : 0f;
            yield return null;
        }
        from.Stop();
        from.volume = 0f;
        to.volume = to.clip != null ? MusicVolume : 0f;
    }
}

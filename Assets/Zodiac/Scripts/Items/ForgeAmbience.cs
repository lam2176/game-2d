using UnityEngine;

/// <summary>
/// Tiếng lò rèn trong làng: lửa reo lặp liên tục và bác thợ rèn thỉnh thoảng gõ búa (vài nhát liền nhau).
/// Âm lượng / lệch tai theo khoảng cách ngang tới camera giống <see cref="ZodiacAudio.Play(AudioClip, float, float, Vector3?)"/>.
/// <see cref="VillageStation"/> loại Lò rèn tự gắn khi vào làng.
/// </summary>
public class ForgeAmbience : MonoBehaviour
{
    [Range(0f, 1f)] public float fireVolume = 0.35f;
    [Range(0f, 1f)] public float hammerVolume = 0.3f;
    public Vector2 hammerInterval = new Vector2(3.5f, 7f);

    AudioSource fire;
    float nextBurst;
    int strikesLeft;
    float nextStrike;

    void Start()
    {
        var res = ZodiacResources.Get;
        if (res != null && res.fireLoop != null)
        {
            fire = gameObject.AddComponent<AudioSource>();
            fire.clip = res.fireLoop;
            fire.loop = true;
            fire.playOnAwake = false;
            fire.spatialBlend = 0f;
            fire.volume = 0f;
            fire.time = Random.Range(0f, res.fireLoop.length);
            fire.Play();
        }
        nextBurst = Time.time + Random.Range(1f, hammerInterval.y);
    }

    /// <summary>Hệ số âm lượng theo khoảng cách ngang tới camera (camera lùi xa cố định nên chỉ tính trục X).</summary>
    float Falloff(out float pan)
    {
        pan = 0f;
        var cam = Camera.main;
        if (cam == null) return 1f;
        float dx = transform.position.x - cam.transform.position.x;
        pan = Mathf.Clamp(dx / 14f, -0.6f, 0.6f);
        return Mathf.Clamp01(1.2f - Mathf.Abs(dx) / 16f);
    }

    void Update()
    {
        float k = Falloff(out float pan);
        if (fire != null)
        {
            fire.volume = fireVolume * k * ZodiacAudio.SfxVolume;
            fire.panStereo = pan;
        }

        // Đang mở cửa sổ lò rèn (game dừng) thì bác thợ rèn nghỉ tay — để tiếng búa của người chơi nghe rõ
        if (VillageWindow.IsOpen || Time.timeScale <= 0f) return;
        if (strikesLeft <= 0 && Time.time >= nextBurst)
        {
            strikesLeft = Random.Range(2, 5);
            nextStrike = Time.time;
        }
        if (strikesLeft > 0 && Time.time >= nextStrike)
        {
            strikesLeft--;
            nextStrike = Time.time + Random.Range(0.35f, 0.5f);
            if (strikesLeft == 0) nextBurst = Time.time + Random.Range(hammerInterval.x, hammerInterval.y);
            var res = ZodiacResources.Get;
            if (res != null && k > 0f) ZodiacAudio.Play(res.anvil, hammerVolume, 0.08f, transform.position);
        }
    }
}

using System;
using UnityEngine;

/// <summary>
/// Vật phẩm thu thập (đồng xu). Nhấp nhô tại chỗ, người chơi chạm vào thì biến mất.
/// Nhân vật có nội tại hút xu (Chó) thì đồng xu trong bán kính bay về phía người chơi.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class Collectible : MonoBehaviour
{
    public float bobHeight = 0.15f;
    public float bobSpeed = 2.5f;
    public float magnetSpeed = 12f;

    public event Action<Collectible> Collected;

    Vector3 restPosition;
    float phase;
    bool attracted;

    void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
        restPosition = transform.localPosition;
        phase = transform.position.x;
    }

    void Update()
    {
        var player = ZodiacPlayerController.Current;
        float magnet = player != null && player.Character != null ? player.Character.coinMagnetRadius : 0f;
        if (magnet > 0f && player.ControlEnabled)
        {
            Vector3 target = player.transform.position + Vector3.up;
            if (!attracted && (target - transform.position).sqrMagnitude <= magnet * magnet) attracted = true;
            if (attracted)
            {
                transform.position = Vector3.MoveTowards(transform.position, target, magnetSpeed * Time.deltaTime);
                return;
            }
        }
        transform.localPosition = restPosition + Vector3.up * (Mathf.Sin(Time.time * bobSpeed + phase) * bobHeight);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (ZodiacPlayerController.FromCollider(other) == null) return;
        gameObject.SetActive(false);
        var res = ZodiacResources.Get;
        if (res != null) ZodiacAudio.Play(res.coin, 0.5f, 0.05f);
        ZodiacParticles.Sparkle(transform.position, new Color(1f, 0.85f, 0.3f), 6, 0.2f);
        Collected?.Invoke(this);
    }
}

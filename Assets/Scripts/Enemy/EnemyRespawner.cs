using System.Collections;
using UnityEngine;

/// <summary>
/// Quái bị tắt (SetActive false) khi chết nên không tự chạy coroutine được;
/// object này sống suốt game và lo việc hồi sinh. Tự tạo khi cần, không phải đặt vào scene.
/// </summary>
public class EnemyRespawner : MonoBehaviour
{
    static EnemyRespawner instance;

    public static void Schedule(EnemyAI enemy, float delay)
    {
        if (instance == null)
            instance = new GameObject(nameof(EnemyRespawner)).AddComponent<EnemyRespawner>();
        instance.StartCoroutine(instance.RespawnAfter(enemy, delay));
    }

    IEnumerator RespawnAfter(EnemyAI enemy, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (enemy != null) enemy.Respawn();
    }
}

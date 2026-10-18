using UnityEngine;

/// <summary>
/// HUD tạm bằng IMGUI để test nhanh: thanh HP, KI, sức mạnh, tiềm năng. Gắn vào nhân vật người chơi.
/// Sau này có thể thay bằng uGUI/UI Toolkit.
/// </summary>
[RequireComponent(typeof(CharacterStats))]
public class PlayerHUD : MonoBehaviour
{
    CharacterStats stats;

    void Awake() => stats = GetComponent<CharacterStats>();

    void OnGUI()
    {
        DrawBar(new Rect(10, 10, 220, 18), (float)stats.Hp / stats.maxHp, Color.red, $"HP {stats.Hp}/{stats.maxHp}");
        DrawBar(new Rect(10, 32, 220, 18), stats.Ki / stats.maxKi, new Color(0.2f, 0.6f, 1f), $"KI {(int)stats.Ki}/{stats.maxKi}");
        GUI.Label(new Rect(10, 54, 300, 20), $"Sức mạnh: {stats.power:N0}   Tiềm năng: {stats.potential:N0}");
        if (stats.IsDead) GUI.Label(new Rect(10, 76, 300, 20), "Bạn đã bị hạ gục!");
    }

    static void DrawBar(Rect r, float t, Color c, string label)
    {
        GUI.color = Color.black;
        GUI.DrawTexture(r, Texture2D.whiteTexture);
        GUI.color = c;
        GUI.DrawTexture(new Rect(r.x, r.y, r.width * Mathf.Clamp01(t), r.height), Texture2D.whiteTexture);
        GUI.color = Color.white;
        GUI.Label(new Rect(r.x + 4, r.y, r.width, r.height), label);
    }
}

using UnityEngine;

/// <summary>Danh sách 12 con giáp theo thứ tự Tý → Hợi. Màn chọn nhân vật và người chơi trong màn dùng chung.</summary>
[CreateAssetMenu(menuName = "Zodiac/Roster", fileName = "ZodiacRoster")]
public class ZodiacRoster : ScriptableObject
{
    public ZodiacCharacter[] characters;

    public int Count => characters != null ? characters.Length : 0;

    public ZodiacCharacter Get(int index) =>
        Count == 0 ? null : characters[((index % Count) + Count) % Count];
}

/// <summary>Nhân vật đang chọn — giữ qua các scene và lưu vào PlayerPrefs.</summary>
public static class ZodiacSelection
{
    const string Key = "Zodiac.SelectedCharacter";

    public static int Index
    {
        get => PlayerPrefs.GetInt(Key, 8);   // mặc định: Khỉ (Thân)
        set
        {
            PlayerPrefs.SetInt(Key, value);
            PlayerPrefs.Save();
        }
    }
}

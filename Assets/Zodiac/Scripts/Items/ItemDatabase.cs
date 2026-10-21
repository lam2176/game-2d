using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Danh sách mọi vật phẩm (Assets/Zodiac/Resources/ItemDatabase.asset, builder tự tạo) — tra vật phẩm theo id khi đọc file lưu.
/// </summary>
[CreateAssetMenu(menuName = "Zodiac/Item Database", fileName = "ItemDatabase")]
public class ItemDatabase : ScriptableObject
{
    public ItemDefinition[] items = System.Array.Empty<ItemDefinition>();

    static ItemDatabase instance;
    Dictionary<string, ItemDefinition> byId;

    public static ItemDatabase Instance
    {
        get
        {
            if (instance == null) instance = Resources.Load<ItemDatabase>("ItemDatabase");
            return instance;
        }
    }

    /// <summary>Vật phẩm theo id (null nếu không có — ví dụ file lưu cũ chứa đồ đã bị xoá).</summary>
    public static ItemDefinition Get(string id)
    {
        var db = Instance;
        if (db == null || string.IsNullOrEmpty(id)) return null;
        if (db.byId == null)
        {
            db.byId = new Dictionary<string, ItemDefinition>();
            foreach (var item in db.items)
                if (item != null && !string.IsNullOrEmpty(item.id)) db.byId[item.id] = item;
        }
        return db.byId.TryGetValue(id, out var def) ? def : null;
    }
}

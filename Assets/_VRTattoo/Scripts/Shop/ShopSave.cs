using System;
using System.IO;
using UnityEngine;

// What survives between play sessions: the day the shop is on and the money in the bank.
[Serializable]
public class ShopSaveData
{
    public int day = 1;
    public int money;
}

// A small JSON file in Application.persistentDataPath. Written by ShopDayManager at the end of
// each day (and on Restart), so quitting mid-day replays that day from the money it started with.
public static class ShopSave
{
    public static string FilePath => Path.Combine(Application.persistentDataPath, "vrtattoo_save.json");

    public static ShopSaveData Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                var data = JsonUtility.FromJson<ShopSaveData>(File.ReadAllText(FilePath));
                if (data != null)
                {
                    data.day = Mathf.Max(1, data.day);
                    data.money = Mathf.Max(0, data.money);
                    return data;
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[ShopSave] Could not read {FilePath}: {e.Message}");
        }
        return new ShopSaveData();
    }

    public static void Save(ShopSaveData data)
    {
        try
        {
            File.WriteAllText(FilePath, JsonUtility.ToJson(data, true));
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[ShopSave] Could not write {FilePath}: {e.Message}");
        }
    }
}

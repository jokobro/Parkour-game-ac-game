using System.IO;
using UnityEngine;

[System.Serializable]
public class SaveData
{
    public int coins;
    public int level;
    public bool questCompleted;
}

public static class SaveManager
{
    private static string savePath = Application.persistentDataPath + "/save.json";

    public static void SaveGame(SaveData data)
    {
        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(savePath, json);
        Debug.Log("Game Saved: " + savePath);
    }

    public static SaveData LoadGame()
    {
        if (File.Exists(savePath))
        {
            string json = File.ReadAllText(savePath);
            return JsonUtility.FromJson<SaveData>(json);
        }
        else
        {
            Debug.Log("No save file found, returning new SaveData");
            return new SaveData();
        }
    }
}

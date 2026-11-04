using System;
using System.IO;
using UnityEngine;

namespace VirtualOffice.Core
{
    [Serializable]
    public class SaveData
    {
        public CampDay currentDay = CampDay.Setup1;
        public string[] completedTaskIds = Array.Empty<string>();
    }

    /// Single-user local save — no server, no network. Plain JSON next to the
    /// user's own save folder (Application.persistentDataPath).
    public static class SaveSystem
    {
        private const string FileName = "save.json";

        private static string FilePath => Path.Combine(Application.persistentDataPath, FileName);

        public static void Save(SaveData data)
        {
            try
            {
                string json = JsonUtility.ToJson(data, true);
                File.WriteAllText(FilePath, json);
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveSystem] 저장 실패: {e.Message}");
            }
        }

        public static bool TryLoad(out SaveData data)
        {
            data = null;
            try
            {
                if (!File.Exists(FilePath)) return false;

                string json = File.ReadAllText(FilePath);
                data = JsonUtility.FromJson<SaveData>(json);
                return data != null;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveSystem] 불러오기 실패: {e.Message}");
                return false;
            }
        }
    }
}

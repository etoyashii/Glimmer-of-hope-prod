using System;
using System.IO;
using UnityEngine;
using GlimmerOfHope.Core.Services;

namespace GlimmerOfHope.Core.Save
{
    public class SaveManager : IService
    {
        private const string SaveFileName = "save.json";

        public SaveData CurrentSave { get; private set; }
        public bool HasSave => File.Exists(SavePath);

        private string SavePath => Path.Combine(Application.persistentDataPath, SaveFileName);

        public void Initialize()
        {
            if (HasSave)
            {
                Load();
            }
            else
            {
                CurrentSave = new SaveData();
            }
        }

        public void Shutdown()
        {
            Save();
        }

        public void Save()
        {
            try
            {
                CurrentSave.timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                var json = JsonUtility.ToJson(CurrentSave, true);
                File.WriteAllText(SavePath, json);
                Debug.Log("[SaveManager] Game saved successfully."   + SavePath);
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveManager] Failed to save: {e.Message}");
            }
        }

        public void Load()
        {
            try
            {
                if (!HasSave)
                {
                    CurrentSave = new SaveData();
                    return;
                }

                var json = File.ReadAllText(SavePath);
                CurrentSave = JsonUtility.FromJson<SaveData>(json);
                Debug.Log("[SaveManager] Game loaded successfully.");
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveManager] Failed to load: {e.Message}");
                CurrentSave = new SaveData();
            }
        }

        public void Delete()
        {
            if (HasSave)
            {
                File.Delete(SavePath);
                CurrentSave = new SaveData();
                Debug.Log("[SaveManager] Save deleted.");
            }
        }

        public void NewGame()
        {
            CurrentSave = new SaveData();
            Save();
        }
    }
}

using GlimmerOfHope.Core.Services;
using System;
using System.IO;
using UnityEngine;

namespace GlimmerOfHope.Core.Save
{
    public class SaveManager : IService
    {
        private const string ProgressionSaveFileName = "ProgressionSave.json";
        private const string PreferencesSaveFileName = "PreferencecesSave.json";
        //private const string SaveFileName = "save.json";

        public SaveData CurrentSave { get; private set; }
        public bool HasSave => File.Exists(ProgressionSavePath)&& File.Exists(PreferencesSavePath);

        private string ProgressionSavePath => Path.Combine(Application.persistentDataPath, ProgressionSaveFileName);
        private string PreferencesSavePath => Path.Combine(Application.persistentDataPath, PreferencesSaveFileName);


        virtual public void Initialize()
        {
            if (HasSave)
            {
                LoadAll();
            }
            else
            {
                CurrentSave = new SaveData();
            }
        }

        virtual public void Shutdown()
        {
            SaveAll();
        }

        virtual public void SaveProgression()
        {
            try
            {
                CurrentSave.timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                var json = JsonUtility.ToJson(CurrentSave.progression, true);
                File.WriteAllText(ProgressionSavePath, json);
                Debug.Log("[SaveManager] Game saved successfully." + ProgressionSavePath);
        
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveManager] Failed to save: {e.Message}");
            }

        }

        virtual public void SavePreferences()
        {
            try
            {
                var json = JsonUtility.ToJson(CurrentSave.preferences, true);
                File.WriteAllText(PreferencesSavePath, json);
                Debug.Log("[SaveManager] Game saved successfully." + PreferencesSavePath);

            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveManager] Failed to save: {e.Message}");
            }

        }
        virtual public void SaveAll()
        {
            SaveProgression();
            SavePreferences();
        }

        virtual public bool LoadAll()
        {
            try
            {
                if (!HasSave)
                {
                    CurrentSave = new SaveData();
                    return true;
                }

                var json = File.ReadAllText(ProgressionSavePath);
                CurrentSave.progression = JsonUtility.FromJson<ProgressionData>(json);

                var json2 = File.ReadAllText(PreferencesSavePath);
                CurrentSave.preferences = JsonUtility.FromJson<PreferencesData>(json2);

                Debug.Log("[SaveManager] Game loaded successfully.");
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveManager] Failed to load: {e.Message}");
                CurrentSave = new SaveData();
                return false;

            }
            return true;

        }

        virtual public void Delete()
        {
            if (HasSave) 
            {
                File.Delete(ProgressionSavePath);
                File.Delete(PreferencesSavePath);

                CurrentSave = new SaveData();
                Debug.Log("[SaveManager] Save deleted.");
            }
        }

        virtual public void NewGame()
        {
            CurrentSave = new SaveData();
            SaveAll();
        }
    }
}

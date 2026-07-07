using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace GlimmerOfHope.Core.Save
{
    /// <summary>
    /// Secure save manager with encryption and checksum validation.
    /// Post-MVP implementation (ADR-006).
    /// </summary>
    public class SecureSaveManager : SaveManager
    {
        private const string ProgressionSaveFileName = "ProgressionSave.dat";
        private const string PreferencesSaveFileName = "PreferencecesSave.dat";

        //private const string SaveFileName = "save.dat";

        private const string ChecksumSuffix = ".checksum";

        // Production: use secure key storage (keychain, etc.)
        private readonly byte[] _encryptionKey;
        private readonly byte[] _iv;

        new public SaveData CurrentSave { get; private set; }
        new public bool HasSave => File.Exists(ProgressionSavePath) && File.Exists(PreferencesSavePath);

        private string ProgressionSavePath => Path.Combine(Application.persistentDataPath, ProgressionSaveFileName);
        private string PreferencesSavePath => Path.Combine(Application.persistentDataPath, PreferencesSaveFileName);

        private string ChecksumPath1 => ProgressionSavePath + ChecksumSuffix;
        private string ChecksumPath2 => PreferencesSavePath + ChecksumSuffix;

        public SecureSaveManager()
        {
            // Derive key from device identifier
            // In production, use more secure key derivation
            var keySource = SystemInfo.deviceUniqueIdentifier;
            using var sha256 = SHA256.Create();
            _encryptionKey = sha256.ComputeHash(Encoding.UTF8.GetBytes(keySource));
            _iv = new byte[16];
            Array.Copy(_encryptionKey, _iv, 16);
        }

        override public  void Initialize() 
        {
            if (HasSave)
            {
                if (!LoadAll())
                {
                    Debug.LogWarning("[SecureSaveManager] Save corrupted or tampered. Starting fresh.");
                    CurrentSave = new SaveData();
                }
            }
            else
            {
                CurrentSave = new SaveData();
            }
        }

        override public void Shutdown() 
        {
            SaveAll();
        }

        override public void SaveProgression()
        {
            try
            {
                CurrentSave.timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                var json = JsonUtility.ToJson(CurrentSave.progression);
                File.WriteAllText(ProgressionSavePath, json);
               
                var encrypted = Encrypt(json);
                var checksum = ComputeChecksum(encrypted);
               
                File.WriteAllBytes(ProgressionSavePath, encrypted);
                File.WriteAllText(ChecksumPath1, checksum);            

                Debug.Log("[SecureSaveManager] Game saved securely.");
            }
            catch (Exception e)
            {
                Debug.LogError($"[SecureSaveManager] Failed to save: {e.Message}");
            }

        }

        override public void SavePreferences()
        {
            try
            {
                CurrentSave.timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                var json = JsonUtility.ToJson(CurrentSave.preferences);
                File.WriteAllText(PreferencesSavePath, json);

                var encrypted = Encrypt(json);
                var checksum = ComputeChecksum(encrypted);

                File.WriteAllBytes(PreferencesSavePath, encrypted);
                File.WriteAllText(ChecksumPath2, checksum);

                Debug.Log("[SecureSaveManager] Game saved securely.");

            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveManager] Failed to save: {e.Message}");
            }

        }

        override public void SaveAll()
        {
            this.SaveProgression();
            this.SavePreferences();
        }

        override public bool LoadAll()
        {
            try
            {
                /// SAVE PROGRESSION
                {
                    if (!HasSave)
                    {
                        CurrentSave = new SaveData();
                        return true;
                    }

                    var encrypted = File.ReadAllBytes(ProgressionSavePath);

                    // Verify checksum
                    if (File.Exists(ChecksumPath1))
                    {
                        var storedChecksum = File.ReadAllText(ChecksumPath1);
                        var computedChecksum = ComputeChecksum(encrypted);

                        if (storedChecksum != computedChecksum)
                        {
                            Debug.LogWarning("[SecureSaveManager] Checksum mismatch. Save may be corrupted.");
                            return false;
                        }
                    }

                    var json = Decrypt(encrypted);
                    CurrentSave.progression = JsonUtility.FromJson<ProgressionData>(json);

                    Debug.Log("[SecureSaveManager] Game loaded securely.");
                }
                /// SAVE PREFERENCES
                {
                    if (!HasSave)
                    {
                        CurrentSave = new SaveData();
                        return true;
                    }

                    var encrypted = File.ReadAllBytes(PreferencesSavePath);

                    // Verify checksum
                    if (File.Exists(ChecksumPath2))
                    {
                        var storedChecksum = File.ReadAllText(ChecksumPath2);
                        var computedChecksum = ComputeChecksum(encrypted);

                        if (storedChecksum != computedChecksum)
                        {
                            Debug.LogWarning("[SecureSaveManager] Checksum mismatch. Save may be corrupted.");
                            return false;
                        }
                    }

                    var json = Decrypt(encrypted);
                    CurrentSave.preferences = JsonUtility.FromJson<PreferencesData>(json);

                    Debug.Log("[SecureSaveManager] Game loaded securely.");
                }

                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SecureSaveManager] Failed to load: {e.Message}");
                return false;
            }
        }
        override public void NewGame()
        {
            CurrentSave = new SaveData();
            this.SaveAll();
        }
        private byte[] Encrypt(string plainText)
        {
            using var aes = Aes.Create();
            aes.Key = _encryptionKey;
            aes.IV = _iv;

            using var encryptor = aes.CreateEncryptor();
            var plainBytes = Encoding.UTF8.GetBytes(plainText);
            return encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
        }

        private string Decrypt(byte[] cipherBytes)
        {
            using var aes = Aes.Create();
            aes.Key = _encryptionKey;
            aes.IV = _iv;

            using var decryptor = aes.CreateDecryptor();
            var plainBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);
            return Encoding.UTF8.GetString(plainBytes);
        }

        private string ComputeChecksum(byte[] data)
        {
            using var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(data);
            return Convert.ToBase64String(hash);
        }
    }
}

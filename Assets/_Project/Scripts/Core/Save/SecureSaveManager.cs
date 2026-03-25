using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using GlimmerOfHope.Core.Services;

namespace GlimmerOfHope.Core.Save
{
    /// <summary>
    /// Secure save manager with encryption and checksum validation.
    /// Post-MVP implementation (ADR-006).
    /// </summary>
    public class SecureSaveManager : IService
    {
        private const string SaveFileName = "save.dat";
        private const string ChecksumSuffix = ".checksum";

        // Production: use secure key storage (keychain, etc.)
        private readonly byte[] _encryptionKey;
        private readonly byte[] _iv;

        public SaveData CurrentSave { get; private set; }
        public bool HasSave => File.Exists(SavePath);

        private string SavePath => Path.Combine(Application.persistentDataPath, SaveFileName);
        private string ChecksumPath => SavePath + ChecksumSuffix;

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

        public void Initialize()
        {
            if (HasSave)
            {
                if (!Load())
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

        public void Shutdown()
        {
            Save();
        }

        public void Save()
        {
            try
            {
                CurrentSave.timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                var json = JsonUtility.ToJson(CurrentSave);
                var encrypted = Encrypt(json);
                var checksum = ComputeChecksum(encrypted);

                File.WriteAllBytes(SavePath, encrypted);
                File.WriteAllText(ChecksumPath, checksum);

                Debug.Log("[SecureSaveManager] Game saved securely.");
            }
            catch (Exception e)
            {
                Debug.LogError($"[SecureSaveManager] Failed to save: {e.Message}");
            }
        }

        public bool Load()
        {
            try
            {
                if (!HasSave)
                {
                    CurrentSave = new SaveData();
                    return true;
                }

                var encrypted = File.ReadAllBytes(SavePath);

                // Verify checksum
                if (File.Exists(ChecksumPath))
                {
                    var storedChecksum = File.ReadAllText(ChecksumPath);
                    var computedChecksum = ComputeChecksum(encrypted);

                    if (storedChecksum != computedChecksum)
                    {
                        Debug.LogWarning("[SecureSaveManager] Checksum mismatch. Save may be corrupted.");
                        return false;
                    }
                }

                var json = Decrypt(encrypted);
                CurrentSave = JsonUtility.FromJson<SaveData>(json);

                Debug.Log("[SecureSaveManager] Game loaded securely.");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SecureSaveManager] Failed to load: {e.Message}");
                return false;
            }
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

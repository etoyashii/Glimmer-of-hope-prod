using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace GlimmerOfHope.Editor.Tools
{
    [Serializable]
    public sealed class SpriteMaterialManifestEntry
    {
        public string sourceGuid;
        public string materialGuid;
    }

    [Serializable]
    public sealed class SpriteMaterialManifestData
    {
        public List<SpriteMaterialManifestEntry> entries = new List<SpriteMaterialManifestEntry>();
    }

    public sealed class SpriteMaterialManifest
    {
        private readonly Dictionary<string, string> _map = new Dictionary<string, string>();
        private readonly string _assetPath;

        public SpriteMaterialManifest(string outputFolder)
        {
            _assetPath = outputFolder + "/" + SpriteMaterialSettings.MANIFEST_FILE;
            Load();
        }

        public string FindMaterialPath(string sourceGuid)
        {
            if (string.IsNullOrEmpty(sourceGuid))
                return null;

            if (!_map.TryGetValue(sourceGuid, out string materialGuid))
                return null;

            string path = AssetDatabase.GUIDToAssetPath(materialGuid);

            if (string.IsNullOrEmpty(path))
                return null;

            if (AssetDatabase.LoadAssetAtPath<Material>(path) == null)
                return null;

            return path;
        }

        public void Record(string sourceGuid, string materialPath)
        {
            if (string.IsNullOrEmpty(sourceGuid) || string.IsNullOrEmpty(materialPath))
                return;

            string materialGuid = AssetDatabase.AssetPathToGUID(materialPath);

            if (string.IsNullOrEmpty(materialGuid))
                return;

            _map[sourceGuid] = materialGuid;
        }

        public void Save()
        {
            var data = new SpriteMaterialManifestData();

            foreach (var pair in _map)
            {
                data.entries.Add(new SpriteMaterialManifestEntry
                {
                    sourceGuid = pair.Key,
                    materialGuid = pair.Value
                });
            }

            data.entries.Sort((a, b) => string.CompareOrdinal(a.sourceGuid, b.sourceGuid));

            string json = JsonUtility.ToJson(data, true);
            string absolute = ToAbsolutePath(_assetPath);

            if (File.Exists(absolute) && File.ReadAllText(absolute) == json)
                return;

            File.WriteAllText(absolute, json);
            AssetDatabase.ImportAsset(_assetPath);
        }

        private void Load()
        {
            string absolute = ToAbsolutePath(_assetPath);

            if (!File.Exists(absolute))
                return;

            var data = JsonUtility.FromJson<SpriteMaterialManifestData>(File.ReadAllText(absolute));

            if (data == null || data.entries == null)
                return;

            foreach (var entry in data.entries)
            {
                if (string.IsNullOrEmpty(entry.sourceGuid) || string.IsNullOrEmpty(entry.materialGuid))
                    continue;

                _map[entry.sourceGuid] = entry.materialGuid;
            }
        }

        private static string ToAbsolutePath(string assetPath)
        {
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            return Path.Combine(projectRoot, assetPath.Replace('/', Path.DirectorySeparatorChar));
        }
    }
}

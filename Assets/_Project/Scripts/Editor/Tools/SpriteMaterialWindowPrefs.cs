using UnityEditor;
using UnityEngine;

namespace GlimmerOfHope.Editor.Tools
{
    public sealed partial class SpriteMaterialWindow
    {
        private const string PREF_ROOT = "GlimmerOfHope.SpriteMaterial.";

        private void LoadPrefs()
        {
            _shader = LoadAsset<Shader>(PREF_ROOT + "Shader", _shader);
            _textureFolder = LoadAsset<DefaultAsset>(PREF_ROOT + "TextureFolder", _textureFolder);
            _outputFolder = LoadAsset<DefaultAsset>(PREF_ROOT + "OutputFolder", _outputFolder);

            _textureProperty = EditorPrefs.GetString(
                PREF_ROOT + "Property",
                SpriteMaterialSettings.DEFAULT_TEXTURE_PROPERTY);

            _recursive = EditorPrefs.GetBool(PREF_ROOT + "Recursive", true);
            _overwrite = EditorPrefs.GetBool(PREF_ROOT + "Overwrite", false);
            _excludedSuffixes = EditorPrefs.GetString(PREF_ROOT + "Excluded", string.Empty);
        }

        private void SavePrefs()
        {
            SaveAsset(PREF_ROOT + "Shader", _shader);
            SaveAsset(PREF_ROOT + "TextureFolder", _textureFolder);
            SaveAsset(PREF_ROOT + "OutputFolder", _outputFolder);

            EditorPrefs.SetString(PREF_ROOT + "Property", _textureProperty);
            EditorPrefs.SetBool(PREF_ROOT + "Recursive", _recursive);
            EditorPrefs.SetBool(PREF_ROOT + "Overwrite", _overwrite);
            EditorPrefs.SetString(PREF_ROOT + "Excluded", _excludedSuffixes);
        }

        private static T LoadAsset<T>(string key, T current) where T : Object
        {
            if (current != null)
                return current;

            string guid = EditorPrefs.GetString(key, string.Empty);

            if (string.IsNullOrEmpty(guid))
                return null;

            string path = AssetDatabase.GUIDToAssetPath(guid);

            if (string.IsNullOrEmpty(path))
                return null;

            return AssetDatabase.LoadAssetAtPath<T>(path);
        }

        private static void SaveAsset(string key, Object asset)
        {
            if (asset == null)
            {
                EditorPrefs.DeleteKey(key);
                return;
            }

            string path = AssetDatabase.GetAssetPath(asset);
            EditorPrefs.SetString(key, AssetDatabase.AssetPathToGUID(path));
        }
    }
}

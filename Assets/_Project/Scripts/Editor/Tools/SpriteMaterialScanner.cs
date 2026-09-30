using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;

namespace GlimmerOfHope.Editor.Tools
{
    public sealed class SpriteMaterialScanner
    {
        private readonly SpriteMaterialRequest _request;
        private readonly SpriteMaterialManifest _manifest;
        private readonly Dictionary<string, string> _claimed = new Dictionary<string, string>();

        public SpriteMaterialScanner(SpriteMaterialRequest request, SpriteMaterialManifest manifest)
        {
            _request = request;
            _manifest = manifest;
        }

        public static string Validate(SpriteMaterialRequest request)
        {
            if (request == null)
                return "Requete vide.";

            if (!AssetDatabase.IsValidFolder(request.TextureFolder))
                return "Le dossier de textures n'est pas un dossier Unity valide.";

            if (!AssetDatabase.IsValidFolder(request.OutputFolder))
                return "Le dossier de sortie n'existe pas. Cree-le dans la fenetre Project avant de generer.";

            if (IsInsideScannedTree(request))
                return "Le dossier de sortie est a l'interieur du dossier scanne. "
                    + "Un generateur ne doit jamais ecrire dans l'espace qu'il relit (ADR-009).";

            return null;
        }

        public List<SpriteMaterialEntry> Scan()
        {
            var entries = new List<SpriteMaterialEntry>();
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { _request.TextureFolder });

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);

                if (!IsInScope(path))
                    continue;

                entries.Add(BuildEntry(guid, path));
            }

            entries.Sort((a, b) => string.CompareOrdinal(a.MaterialName, b.MaterialName));

            return entries;
        }

        private SpriteMaterialEntry BuildEntry(string guid, string path)
        {
            string textureName = Path.GetFileNameWithoutExtension(path);

            var entry = new SpriteMaterialEntry
            {
                TextureGuid = guid,
                TexturePath = path,
                TextureName = textureName,
                MaterialPath = SpriteMaterialNaming.ToMaterialPath(_request.OutputFolder, textureName)
            };

            string tracked = _manifest.FindMaterialPath(guid);

            if (!string.IsNullOrEmpty(tracked))
                entry.MaterialPath = tracked;

            entry.MaterialName = Path.GetFileNameWithoutExtension(entry.MaterialPath);

            Classify(entry, tracked);

            return entry;
        }

        private void Classify(SpriteMaterialEntry entry, string tracked)
        {
            if (_claimed.TryGetValue(entry.MaterialPath, out string owner))
            {
                entry.Action = SpriteMaterialAction.Skip;
                entry.Enabled = false;
                entry.Reason = "collision de nom avec " + owner;
                return;
            }

            _claimed[entry.MaterialPath] = entry.TexturePath;

            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Material>(entry.MaterialPath) == null)
            {
                entry.Action = SpriteMaterialAction.Create;
                entry.Reason = "nouveau";
                return;
            }

            if (!_request.Overwrite)
            {
                entry.Action = SpriteMaterialAction.Skip;
                entry.Enabled = false;
                entry.Reason = "deja present, coche Mettre a jour l'existant pour le reassigner";
                return;
            }

            entry.Action = SpriteMaterialAction.Update;
            entry.Reason = string.IsNullOrEmpty(tracked)
                ? "mis a jour sur place, GUID conserve"
                : "suivi par le manifeste, mis a jour sur place";
        }

        private bool IsInScope(string path)
        {
            if (!_request.Recursive)
            {
                string folder = (Path.GetDirectoryName(path) ?? string.Empty).Replace('\\', '/');

                if (!string.Equals(folder, _request.TextureFolder, StringComparison.Ordinal))
                    return false;
            }

            return !HasExcludedSuffix(Path.GetFileNameWithoutExtension(path), _request.ExcludedSuffixes);
        }

        private static bool HasExcludedSuffix(string textureName, string[] suffixes)
        {
            if (suffixes == null)
                return false;

            foreach (string suffix in suffixes)
            {
                if (string.IsNullOrEmpty(suffix))
                    continue;

                if (textureName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private static bool IsInsideScannedTree(SpriteMaterialRequest request)
        {
            if (string.Equals(request.OutputFolder, request.TextureFolder, StringComparison.Ordinal))
                return true;

            if (!request.Recursive)
                return false;

            return request.OutputFolder.StartsWith(request.TextureFolder + "/", StringComparison.Ordinal);
        }
    }
}

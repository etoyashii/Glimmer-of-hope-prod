using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using GlimmerOfHope.Gameplay.Characters;

namespace GlimmerOfHope.Editor.Characters
{
    public static class CharacterThumbnailGenerator
    {
        private const string REGISTRY_PATH = "Assets/_Project/Data/Characters/_Registry.asset";
        private const string THUMB_ROOT = "Assets/_Project/Art/Textures/Chara/Thumbnails";
        private const int SIZE = 256;

        [MenuItem("Tools/GlimmerOfHope/Generer miniatures des parts")]
        public static void GenerateAll()
        {
            var registry = AssetDatabase.LoadAssetAtPath<CharacterRegistrySO>(REGISTRY_PATH);
            if (registry == null || registry.MasterCharacterPrefab == null)
            {
                Debug.LogError("[CharacterThumbnailGenerator] Registry ou MasterCharacterPrefab introuvable.");
                return;
            }

            if (!AssetDatabase.IsValidFolder(THUMB_ROOT))
            {
                Directory.CreateDirectory(THUMB_ROOT);
                AssetDatabase.Refresh();
            }

            var preview = new PreviewRenderUtility();
            preview.camera.fieldOfView = 20f;
            preview.camera.nearClipPlane = 0.01f;
            preview.camera.farClipPlane = 50f;
            preview.camera.clearFlags = CameraClearFlags.SolidColor;
            preview.camera.backgroundColor = new Color(0f, 0f, 0f, 0f);
            preview.lights[0].intensity = 1.6f;
            preview.lights[0].transform.rotation = Quaternion.Euler(30f, 150f, 0f);
            preview.lights[1].intensity = 1.1f;
            preview.ambientColor = new Color(0.8f, 0.8f, 0.8f);

            var rig = preview.InstantiatePrefabInScene(registry.MasterCharacterPrefab);
            rig.transform.rotation = Quaternion.Euler(0f, 180f, 0f);

            var smrs = new Dictionary<string, SkinnedMeshRenderer>();
            foreach (var smr in rig.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (smr.sharedMesh != null)
                    smrs[smr.sharedMesh.name] = smr;
            }
            foreach (var mr in rig.GetComponentsInChildren<MeshRenderer>(true))
                mr.enabled = false;

            int done = 0;
            var paths = new List<string>();
            var targets = new List<CharacterPartSO>();
            try
            {
                foreach (var category in registry.GetAllLeafCategories())
                {
                    if (category == null) continue;
                    foreach (var part in category.Parts)
                    {
                        if (part == null || part.Mesh == null) continue;
                        if (!smrs.TryGetValue(part.Mesh.name, out var target)) continue;

                        foreach (var smr in smrs.Values)
                            smr.enabled = smr == target;
                        if (part.HasValidMaterials)
                            target.sharedMaterials = part.Materials;

                        var png = Render(preview, target);
                        var path = THUMB_ROOT + "/T_" + part.PartID.Replace(" ", "_") + ".png";
                        File.WriteAllBytes(path, png);
                        paths.Add(path);
                        targets.Add(part);
                        done++;
                    }
                }
            }
            finally
            {
                preview.Cleanup();
            }

            AssetDatabase.Refresh();
            int missing = 0;
            for (int i = 0; i < paths.Count; i++)
            {
                var importer = (TextureImporter)AssetImporter.GetAtPath(paths[i]);
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.SaveAndReimport();

                Sprite sprite = null;
                foreach (var obj in AssetDatabase.LoadAllAssetsAtPath(paths[i]))
                {
                    if (obj is Sprite sp)
                        sprite = sp;
                }
                if (sprite == null)
                {
                    missing++;
                    continue;
                }

                var so = new SerializedObject(targets[i]);
                so.FindProperty("_thumbnail").objectReferenceValue = sprite;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(targets[i]);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[CharacterThumbnailGenerator] Miniatures generees : " + done + ", sans sprite : " + missing);
        }

        private static byte[] Render(PreviewRenderUtility preview, SkinnedMeshRenderer target)
        {
            var mesh = new Mesh();
            target.BakeMesh(mesh, true);
            var b = ToWorld(target.transform, mesh.bounds);
            Object.DestroyImmediate(mesh);

            float radius = Mathf.Max(b.extents.x, b.extents.y, 0.05f);
            float dist = radius / Mathf.Tan(preview.camera.fieldOfView * 0.5f * Mathf.Deg2Rad) * 1.15f;
            preview.camera.transform.position = b.center + new Vector3(0f, 0f, -dist - b.extents.z);
            preview.camera.transform.LookAt(b.center);

            var onBlack = Shot(preview, Color.black);
            var onWhite = Shot(preview, Color.white);
            var b0 = onBlack.GetPixels();
            var w0 = onWhite.GetPixels();
            var px = new Color[b0.Length];
            for (int i = 0; i < px.Length; i++)
            {
                float a = 1f - ((w0[i].r - b0[i].r) + (w0[i].g - b0[i].g) + (w0[i].b - b0[i].b)) / 3f;
                a = Mathf.Clamp01(a);
                px[i] = a > 0.001f ? new Color(b0[i].r / a, b0[i].g / a, b0[i].b / a, a) : Color.clear;
            }
            for (int y = 0; y < SIZE; y++)
            {
                bool bar = true;
                for (int x = 0; x < SIZE && bar; x++)
                {
                    var c = w0[y * SIZE + x];
                    if (c.r + c.g + c.b > 0.03f)
                        bar = false;
                }
                if (!bar) continue;
                for (int x = 0; x < SIZE; x++)
                    px[y * SIZE + x] = Color.clear;
            }
            var tex = new Texture2D(SIZE, SIZE, TextureFormat.RGBA32, false);
            tex.SetPixels(px);
            tex.Apply();
            var png = tex.EncodeToPNG();
            Object.DestroyImmediate(tex);
            Object.DestroyImmediate(onBlack);
            Object.DestroyImmediate(onWhite);
            return png;
        }

        private static Texture2D Shot(PreviewRenderUtility preview, Color background)
        {
            preview.camera.backgroundColor = background;
            preview.BeginStaticPreview(new Rect(0, 0, SIZE, SIZE));
            preview.Render(true);
            var src = preview.EndStaticPreview();
            var rt = RenderTexture.GetTemporary(SIZE, SIZE, 0, RenderTextureFormat.ARGB32);
            Graphics.Blit(src, rt);
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            var copy = new Texture2D(SIZE, SIZE, TextureFormat.RGBA32, false);
            copy.ReadPixels(new Rect(0, 0, SIZE, SIZE), 0, 0);
            copy.Apply();
            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(rt);
            Object.DestroyImmediate(src);
            return copy;
        }

        private static Bounds ToWorld(Transform t, Bounds local)
        {
            var min = local.min;
            var max = local.max;
            var b = new Bounds(t.TransformPoint(min), Vector3.zero);
            for (int i = 0; i < 8; i++)
            {
                var p = new Vector3((i & 1) == 0 ? min.x : max.x, (i & 2) == 0 ? min.y : max.y, (i & 4) == 0 ? min.z : max.z);
                b.Encapsulate(t.TransformPoint(p));
            }
            return b;
        }
    }
}

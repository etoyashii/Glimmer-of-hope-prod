using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GlimmerOfHope.Editor.Tools
{
    public sealed class SpriteMaterialApplyContext
    {
        public Shader Shader;
        public string TextureProperty;
        public SpriteMaterialManifest Manifest;
    }

    public static class SpriteMaterialApplier
    {
        public static SpriteMaterialReport Apply(
            List<SpriteMaterialEntry> entries,
            SpriteMaterialApplyContext context)
        {
            var report = new SpriteMaterialReport();

            try
            {
                AssetDatabase.StartAssetEditing();
                Run(entries, context, report);
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                EditorUtility.ClearProgressBar();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            RecordApplied(entries, context);

            return report;
        }

        private static void Run(
            List<SpriteMaterialEntry> entries,
            SpriteMaterialApplyContext context,
            SpriteMaterialReport report)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                SpriteMaterialEntry entry = entries[i];

                if (!entry.Enabled || entry.Action == SpriteMaterialAction.Skip)
                {
                    report.Skipped++;
                    continue;
                }

                if (IsCancelled(i, entries.Count, entry))
                {
                    report.Cancelled = true;
                    return;
                }

                ApplyEntry(entry, context, report);
            }
        }

        private static bool IsCancelled(int index, int total, SpriteMaterialEntry entry)
        {
            return EditorUtility.DisplayCancelableProgressBar(
                "Sprite Material Generator",
                entry.MaterialName,
                total == 0 ? 1f : (float)index / total);
        }

        private static void ApplyEntry(
            SpriteMaterialEntry entry,
            SpriteMaterialApplyContext context,
            SpriteMaterialReport report)
        {
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(entry.TexturePath);

            if (texture == null)
            {
                Fail(report, entry, "texture illisible");
                return;
            }

            if (entry.Action == SpriteMaterialAction.Update)
            {
                UpdateExisting(entry, texture, context, report);
                return;
            }

            CreateNew(entry, texture, context, report);
        }

        private static void UpdateExisting(
            SpriteMaterialEntry entry,
            Texture2D texture,
            SpriteMaterialApplyContext context,
            SpriteMaterialReport report)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(entry.MaterialPath);

            if (material == null)
            {
                CreateNew(entry, texture, context, report);
                return;
            }

            if (material.shader != context.Shader)
                material.shader = context.Shader;

            material.SetTexture(context.TextureProperty, texture);
            EditorUtility.SetDirty(material);

            entry.Applied = true;
            report.Updated++;
        }

        private static void CreateNew(
            SpriteMaterialEntry entry,
            Texture2D texture,
            SpriteMaterialApplyContext context,
            SpriteMaterialReport report)
        {
            var material = new Material(context.Shader);
            material.SetTexture(context.TextureProperty, texture);

            AssetDatabase.CreateAsset(material, entry.MaterialPath);

            entry.Applied = true;
            report.Created++;
        }

        private static void RecordApplied(
            List<SpriteMaterialEntry> entries,
            SpriteMaterialApplyContext context)
        {
            foreach (SpriteMaterialEntry entry in entries)
            {
                if (entry.Applied)
                    context.Manifest.Record(entry.TextureGuid, entry.MaterialPath);
            }

            context.Manifest.Save();
        }

        private static void Fail(SpriteMaterialReport report, SpriteMaterialEntry entry, string reason)
        {
            report.Failed++;

            if (report.Errors.Count < SpriteMaterialSettings.MAX_ERRORS_LOGGED)
                report.Errors.Add(entry.TexturePath + " : " + reason);
        }
    }
}

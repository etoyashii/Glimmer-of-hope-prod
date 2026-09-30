using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using GlimmerOfHope.Gameplay;
using GlimmerOfHope.Gameplay.Firefly;
using GlimmerOfHope.Gameplay.Puzzles;

namespace GlimmerOfHope.Editor.Tools
{
    public static class PuzzleLucioleScanner
    {
        private const string FIREFLY_PREFIX = "Luciole";
        private const string BROKEN_LANTERN_PREFIX = "Lanterne_Casser";

        public static List<PuzzleLucioleSpot> Scan()
        {
            List<PuzzleLucioleSpot> spots = new List<PuzzleLucioleSpot>();
            Scene scene = SceneManager.GetActiveScene();

            if (!scene.IsValid()) return spots;

            GameObject[] roots = scene.GetRootGameObjects();

            for (int i = 0; i < roots.Length; i++)
            {
                Collect(roots[i].transform, spots);
            }

            return spots;
        }

        private static void Collect(Transform current, List<PuzzleLucioleSpot> spots)
        {
            PuzzleLucioleSpot spot = Inspect(current);

            if (spot != null)
            {
                spots.Add(spot);
            }

            for (int i = 0; i < current.childCount; i++)
            {
                Collect(current.GetChild(i), spots);
            }
        }

        private static PuzzleLucioleSpot Inspect(Transform root)
        {
            Transform firefly = FindChild(root, FIREFLY_PREFIX);
            Transform lantern = FindChild(root, BROKEN_LANTERN_PREFIX);

            if (firefly == null) return null;
            if (lantern == null) return null;

            return new PuzzleLucioleSpot
            {
                Root = root,
                Firefly = firefly,
                BrokenLantern = lantern,
                GrayZone = FindGrayZone(root),
                HasManager = root.GetComponent<PuzzleManager>() != null,
                HasFireflyAI = firefly.GetComponent<FireflyAI>() != null,
                HasLanternElement = lantern.GetComponent<LanternPuzzleElement>() != null,
                HasSwarm = firefly.GetComponent<FireflySwarm>() != null,
                HasRepairedVisual = HasRepairedVisual(lantern),
                Path = BuildPath(root),
                SuggestedId = BuildId(root)
            };
        }

        private static bool HasRepairedVisual(Transform lantern)
        {
            LanternPuzzleElement element = lantern.GetComponent<LanternPuzzleElement>();

            if (element == null) return false;

            SerializedObject so = new SerializedObject(element);

            return so.FindProperty("_repairedVisual").objectReferenceValue != null;
        }

        private static Transform FindChild(Transform root, string prefix)
        {
            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);

                if (!child.name.StartsWith(prefix)) continue;
                if (prefix == FIREFLY_PREFIX && child.name.StartsWith(BROKEN_LANTERN_PREFIX)) continue;

                return child;
            }

            return null;
        }

        private static Transform FindGrayZone(Transform root)
        {
            GrayZone zone = root.GetComponentInChildren<GrayZone>(true);

            return zone == null ? null : zone.transform;
        }

        public static string BuildPath(Transform target)
        {
            StringBuilder builder = new StringBuilder(target.name);
            Transform current = target.parent;

            while (current != null)
            {
                builder.Insert(0, current.name + "/");
                current = current.parent;
            }

            return builder.ToString();
        }

        private static string BuildId(Transform root)
        {
            StringBuilder builder = new StringBuilder("puzzle_luciole");
            Transform current = root;
            int depth = 0;

            while (current != null && depth < 2)
            {
                builder.Append('_');
                builder.Append(Sanitize(current.name));

                current = current.parent;
                depth++;
            }

            return builder.ToString().ToLowerInvariant();
        }

        private static string Sanitize(string raw)
        {
            StringBuilder builder = new StringBuilder();

            for (int i = 0; i < raw.Length; i++)
            {
                char c = raw[i];

                if (char.IsLetterOrDigit(c))
                {
                    builder.Append(c);
                }
            }

            return builder.Length == 0 ? "zone" : builder.ToString();
        }
    }
}

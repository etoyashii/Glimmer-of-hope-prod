using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using GlimmerOfHope.Gameplay;
using GlimmerOfHope.Gameplay.Firefly;
using GlimmerOfHope.Gameplay.Puzzles;

namespace GlimmerOfHope.Editor.Tools
{
    public static class PuzzleLucioleApplier
    {
        private const string UNDO_GROUP = "Cablage puzzle luciole";
        private const string GLOW_FOLDER = "Assets/_Project/Art/Materials/Puzzle";
        private const string GLOW_PATH = GLOW_FOLDER + "/M_FireflyGlow.mat";
        private const string REPAIRED_PREFAB = "Assets/_Project/Prefabs/LDForest/Lanterne.prefab";
        private const string REPAIRED_NAME = "Lanterne_Reparee";

        public static int ApplyAll(System.Collections.Generic.List<PuzzleLucioleSpot> spots, bool wireGrayZone)
        {
            int applied = 0;

            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName(UNDO_GROUP);

            int group = Undo.GetCurrentGroup();

            for (int i = 0; i < spots.Count; i++)
            {
                if (spots[i].IsWired) continue;
                if (!Apply(spots[i], wireGrayZone)) continue;

                applied++;
            }

            Undo.CollapseUndoOperations(group);

            if (applied > 0)
            {
                EditorSceneManager.MarkSceneDirty(spots[0].Root.gameObject.scene);
            }

            return applied;
        }

        public static bool Apply(PuzzleLucioleSpot spot, bool wireGrayZone)
        {
            if (spot.Root == null) return false;
            if (spot.Firefly == null) return false;
            if (spot.BrokenLantern == null) return false;

            FireflyAI firefly = EnsureFirefly(spot);
            EnsureSwarm(spot);
            PuzzleLucioleTuning.Apply(spot);
            LanternPuzzleElement element = EnsureLanternElement(spot, firefly);
            EnsureRepairedVisual(spot, element);
            PuzzleManager manager = EnsureManager(spot);

            RegisterElement(manager, element);

            if (wireGrayZone && spot.GrayZone != null)
            {
                WireGrayZone(manager, spot.GrayZone);
            }

            return true;
        }

        private static FireflyAI EnsureFirefly(PuzzleLucioleSpot spot)
        {
            FireflyAI firefly = spot.Firefly.GetComponent<FireflyAI>();

            if (firefly == null)
            {
                firefly = Undo.AddComponent<FireflyAI>(spot.Firefly.gameObject);
            }

            SerializedObject so = new SerializedObject(firefly);

            so.FindProperty("_lanternTransform").objectReferenceValue = spot.BrokenLantern;
            so.ApplyModifiedProperties();

            return firefly;
        }

        private static void EnsureSwarm(PuzzleLucioleSpot spot)
        {
            FireflySwarm swarm = spot.Firefly.GetComponent<FireflySwarm>();

            if (swarm == null)
            {
                swarm = Undo.AddComponent<FireflySwarm>(spot.Firefly.gameObject);
            }

            SerializedObject so = new SerializedObject(swarm);
            SerializedProperty mat = so.FindProperty("_dotMaterial");

            if (mat.objectReferenceValue == null)
            {
                mat.objectReferenceValue = EnsureGlowMaterial();
            }

            so.ApplyModifiedProperties();
        }

        private static Material EnsureGlowMaterial()
        {
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(GLOW_PATH);

            if (mat != null) return mat;

            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");

            if (shader == null)
            {
                Debug.LogWarning("[PuzzleLucioleApplier] URP Unlit introuvable, l'essaim garde le materiau de la luciole.");
                return null;
            }

            if (!AssetDatabase.IsValidFolder(GLOW_FOLDER))
            {
                AssetDatabase.CreateFolder("Assets/_Project/Art/Materials", "Puzzle");
            }

            mat = new Material(shader);
            mat.SetColor("_BaseColor", new Color(1.0f, 0.92f, 0.45f));

            AssetDatabase.CreateAsset(mat, GLOW_PATH);
            AssetDatabase.SaveAssets();

            return mat;
        }

        private static void EnsureRepairedVisual(PuzzleLucioleSpot spot, LanternPuzzleElement element)
        {
            SerializedObject so = new SerializedObject(element);
            SerializedProperty visual = so.FindProperty("_repairedVisual");

            if (visual.objectReferenceValue != null) return;

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(REPAIRED_PREFAB);

            if (prefab == null)
            {
                Debug.LogWarning("[PuzzleLucioleApplier] " + REPAIRED_PREFAB + " introuvable, pas de lanterne reparee.");
                return;
            }

            GameObject repaired = (GameObject)PrefabUtility.InstantiatePrefab(prefab, spot.Root);

            Undo.RegisterCreatedObjectUndo(repaired, UNDO_GROUP);

            repaired.name = REPAIRED_NAME;
            repaired.transform.SetPositionAndRotation(spot.BrokenLantern.position, spot.BrokenLantern.rotation);
            repaired.SetActive(false);

            visual.objectReferenceValue = repaired;
            so.ApplyModifiedProperties();
        }

        private static LanternPuzzleElement EnsureLanternElement(PuzzleLucioleSpot spot, FireflyAI firefly)
        {
            LanternPuzzleElement element = spot.BrokenLantern.GetComponent<LanternPuzzleElement>();

            if (element == null)
            {
                element = Undo.AddComponent<LanternPuzzleElement>(spot.BrokenLantern.gameObject);
            }

            SerializedObject so = new SerializedObject(element);

            so.FindProperty("_firefly").objectReferenceValue = firefly;
            so.FindProperty("_elementName").stringValue = spot.SuggestedId;
            so.ApplyModifiedProperties();

            return element;
        }

        private static PuzzleManager EnsureManager(PuzzleLucioleSpot spot)
        {
            PuzzleManager manager = spot.Root.GetComponent<PuzzleManager>();

            if (manager == null)
            {
                manager = Undo.AddComponent<PuzzleManager>(spot.Root.gameObject);
            }

            SerializedObject so = new SerializedObject(manager);
            SerializedProperty id = so.FindProperty("_puzzleId");

            if (IsDefaultId(id.stringValue))
            {
                id.stringValue = spot.SuggestedId;
                so.FindProperty("_puzzleName").stringValue = spot.Root.name + " Luciole";
            }

            so.FindProperty("_solveMode").enumValueIndex = 0;
            so.ApplyModifiedProperties();

            return manager;
        }

        private static bool IsDefaultId(string current)
        {
            if (string.IsNullOrEmpty(current)) return true;

            return current == "puzzle_01";
        }

        private static void RegisterElement(PuzzleManager manager, LanternPuzzleElement element)
        {
            SerializedObject so = new SerializedObject(manager);
            SerializedProperty list = so.FindProperty("_elements");

            for (int i = 0; i < list.arraySize; i++)
            {
                if (list.GetArrayElementAtIndex(i).objectReferenceValue == element) return;
            }

            int index = list.arraySize;

            list.arraySize = index + 1;
            list.GetArrayElementAtIndex(index).objectReferenceValue = element;

            so.ApplyModifiedProperties();
        }

        private static void WireGrayZone(PuzzleManager manager, Transform grayZoneTransform)
        {
            GrayZone zone = grayZoneTransform.GetComponent<GrayZone>();

            if (zone == null) return;
            if (HasRepaintListener(manager, zone)) return;

            Undo.RecordObject(manager, UNDO_GROUP);

            UnityEventTools.AddVoidPersistentListener(manager.OnPuzzleSolved, new UnityAction(zone.Repaint));

            EditorUtility.SetDirty(manager);
        }

        private static bool HasRepaintListener(PuzzleManager manager, GrayZone zone)
        {
            int count = manager.OnPuzzleSolved.GetPersistentEventCount();

            for (int i = 0; i < count; i++)
            {
                if (manager.OnPuzzleSolved.GetPersistentTarget(i) != zone) continue;
                if (manager.OnPuzzleSolved.GetPersistentMethodName(i) != "Repaint") continue;

                return true;
            }

            return false;
        }
    }
}

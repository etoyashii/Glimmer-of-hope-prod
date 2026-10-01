using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using GlimmerOfHope.Gameplay.Firefly;

namespace GlimmerOfHope.Editor.Tools
{
    public static class PuzzleLucioleTuning
    {
        private const string UNDO_NAME = "Reglage taille lucioles";

        private const float FIREFLY_SCALE = 0.15f;
        private const float WAKE_RADIUS = 1.5f;
        private const float ORBIT_RADIUS = 0.25f;
        private const float ORBIT_SPEED = 75.0f;
        private const float ORBIT_HEIGHT_OFFSET = 0.0f;
        private const float ORBIT_WAVE_HEIGHT = 0.2f;
        private const float ORBIT_WAVE_COUNT = 3.0f;
        private const float LANTERN_RADIUS = 1.0f;
        private const float ARRIVE_THRESHOLD = 0.05f;
        private const float HOVER_HEIGHT = 0.05f;
        private const float HOVER_MAX_SPEED = 0.3f;
        private const float SWARM_SPREAD = 0.5f;
        private const float SWARM_DOT_SIZE = 0.07f;

        public static int ApplyAll(List<PuzzleLucioleSpot> spots)
        {
            int count = 0;

            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName(UNDO_NAME);

            int group = Undo.GetCurrentGroup();

            for (int i = 0; i < spots.Count; i++)
            {
                if (!Apply(spots[i])) continue;

                count++;
            }

            Undo.CollapseUndoOperations(group);

            if (count > 0)
            {
                EditorSceneManager.MarkSceneDirty(spots[0].Root.gameObject.scene);
            }

            return count;
        }

        public static bool Apply(PuzzleLucioleSpot spot)
        {
            if (spot.Firefly == null) return false;

            FireflyAI firefly = spot.Firefly.GetComponent<FireflyAI>();

            if (firefly == null) return false;

            Undo.RecordObject(spot.Firefly, UNDO_NAME);
            spot.Firefly.localScale = Vector3.one * FIREFLY_SCALE;

            SerializedObject so = new SerializedObject(firefly);

            so.FindProperty("_wakeRadius").floatValue = WAKE_RADIUS;
            so.FindProperty("_orbitRadius").floatValue = ORBIT_RADIUS;
            so.FindProperty("_orbitSpeed").floatValue = ORBIT_SPEED;
            so.FindProperty("_orbitHeightOffset").floatValue = ORBIT_HEIGHT_OFFSET;
            so.FindProperty("_orbitWaveHeight").floatValue = ORBIT_WAVE_HEIGHT;
            so.FindProperty("_orbitWaveCount").floatValue = ORBIT_WAVE_COUNT;
            so.FindProperty("_lanternRadius").floatValue = LANTERN_RADIUS;
            so.FindProperty("_arriveThreshold").floatValue = ARRIVE_THRESHOLD;
            so.FindProperty("_hoverHeight").floatValue = HOVER_HEIGHT;
            so.FindProperty("_hoverMaxSpeed").floatValue = HOVER_MAX_SPEED;
            so.ApplyModifiedProperties();

            FireflySwarm swarm = spot.Firefly.GetComponent<FireflySwarm>();

            if (swarm == null) return true;

            SerializedObject swarmSo = new SerializedObject(swarm);

            swarmSo.FindProperty("_spread").floatValue = SWARM_SPREAD;
            swarmSo.FindProperty("_dotSize").floatValue = SWARM_DOT_SIZE;
            swarmSo.ApplyModifiedProperties();

            return true;
        }
    }
}

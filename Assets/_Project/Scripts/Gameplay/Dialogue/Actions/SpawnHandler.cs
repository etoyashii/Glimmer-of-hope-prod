using UnityEngine;

namespace GlimmerOfHope.Gameplay.Dialogue.Actions
{
    /// <summary>
    /// Handles Spawn actions during dialogue.
    /// STANDBY: Awaiting SpawnManager implementation from Gameplay Dev.
    ///
    /// Expected parameter formats:
    /// - "prefabId" (spawns at default position)
    /// - "prefabId@x,y,z" (spawns at world position)
    /// - "prefabId@marker:markerName" (spawns at named marker)
    /// </summary>
    public class SpawnHandler : IDialogueActionHandler
    {
        public DialogueActionType HandledType => DialogueActionType.Spawn;

        public void Execute(string parameter, float delay)
        {
            if (string.IsNullOrEmpty(parameter)) return;

            // TODO: Uncomment when SpawnManager is available
            // if (!ServiceLocator.TryGet<ISpawnManager>(out var spawnManager))
            // {
            //     Debug.LogWarning("[SpawnHandler] SpawnManager not available.");
            //     return;
            // }
            //
            // ParseAndSpawn(parameter, spawnManager);

            Debug.LogWarning($"[SpawnHandler] STUB - SpawnManager not yet implemented. Would spawn: {parameter}");
        }

        // private void ParseAndSpawn(string parameter, ISpawnManager spawnManager)
        // {
        //     if (parameter.Contains("@marker:"))
        //     {
        //         var parts = parameter.Split(new[] { "@marker:" }, StringSplitOptions.None);
        //         spawnManager.Spawn(parts[0], parts[1]);
        //     }
        //     else if (parameter.Contains("@"))
        //     {
        //         var parts = parameter.Split('@');
        //         var coords = parts[1].Split(',');
        //         var pos = new Vector3(float.Parse(coords[0]), float.Parse(coords[1]), float.Parse(coords[2]));
        //         spawnManager.Spawn(parts[0], pos);
        //     }
        //     else
        //     {
        //         spawnManager.Spawn(parameter, Vector3.zero);
        //     }
        // }
    }
}

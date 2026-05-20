using UnityEngine;

namespace GlimmerOfHope.Gameplay.Dialogue.Actions
{
    /// <summary>
    /// Handles Despawn actions during dialogue.
    /// STANDBY: Awaiting SpawnManager implementation from Gameplay Dev.
    ///
    /// Expected parameter format:
    /// - "spawnId" (despawns entity by ID returned from Spawn)
    /// </summary>
    public class DespawnHandler : IDialogueActionHandler
    {
        public DialogueActionType HandledType => DialogueActionType.Despawn;

        public void Execute(string parameter, float delay)
        {
            if (string.IsNullOrEmpty(parameter)) return;

            // TODO: Uncomment when SpawnManager is available
            // if (!ServiceLocator.TryGet<ISpawnManager>(out var spawnManager))
            // {
            //     Debug.LogWarning("[DespawnHandler] SpawnManager not available.");
            //     return;
            // }
            //
            // spawnManager.Despawn(parameter);

            Debug.LogWarning($"[DespawnHandler] STUB - SpawnManager not yet implemented. Would despawn: {parameter}");
        }
    }
}

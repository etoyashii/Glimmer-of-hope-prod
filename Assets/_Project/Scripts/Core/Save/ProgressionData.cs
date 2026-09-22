using System;
using System.Collections.Generic;

namespace GlimmerOfHope.Core.Save
{
    [Serializable]
    public class ProgressionData
    {
        public string currentWorld;
        public int currentLevel;
        public List<string> unlockedWorlds = new();
        public List<string> completedLevels = new();
        public List<string> collectedItems = new();
        public List<string> dialogueFlags = new();
        public Dictionary<string, int> statistics = new();
        public List<CharacterSaveEntry> characterSelections = new();
        public List<CharacterColorEntry> characterColors = new();

        public bool IsWorldUnlocked(string worldId)
        {
            return unlockedWorlds.Contains(worldId);
        }

        public bool IsLevelCompleted(string levelId)
        {
            return completedLevels.Contains(levelId);
        }

        public void UnlockWorld(string worldId)
        {
            if (!unlockedWorlds.Contains(worldId))
                unlockedWorlds.Add(worldId);
        }

        public void CompleteLevel(string levelId)
        {
            if (!completedLevels.Contains(levelId))
                completedLevels.Add(levelId);
        }
    }
}

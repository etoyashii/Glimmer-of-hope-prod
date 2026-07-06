using System;
using System.Collections.Generic;
using UnityEngine;

namespace GlimmerOfHope.Core.Save
{
    [Serializable]
    public class ProgressionData
    {
        public string currentWorld;
        public int currentLevel;
        public List<string> unlockedWorlds = new(); // PAS SURe
        public List<string> completedLevels = new();   // PAS SURe
        public List<string> collectedItems = new();        /// collectibles débloqué(secrets)
        public List<string> dialogueFlags = new();        /// dialogues joué ???
        public Dictionary<string, int> statistics = new();


        /////////////////////////////////
        public Vector3 pos;        /// pos joueur

        public List<string> Inventory = new();/// inventaire
        public List<string> UnlockedItems = new(); //rien pour l'instant



        /// <summary>
        /// save manquant :
        /// ----------------
        /// /// Hub(objet debloquer et posés + tenues débloqué et porté)
        /// états énigmes ,cinématiques jouer ou non
        /// </summary>


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

using System.Collections.Generic;

namespace GlimmerOfHope.Gameplay.NewDialogue
{
    /// <summary>
    /// Global registry of boolean flags. Any script can call DialogueFlags.Set(...) to unlock a
    /// Gate or a if node in "Flag" mode, without needing a direct reference to the dialogue 
    /// </summary>
    public static class DialogueFlags
    {
        #region Private Fields

        private static readonly Dictionary<string, bool> Flags = new Dictionary<string, bool>();

        #endregion

        #region Public Methods

        public static void Set(string name, bool value)
        {
            if (string.IsNullOrEmpty(name)) return;
            Flags[name] = value;
        }

        public static bool Get(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            return Flags.TryGetValue(name, out var value) && value;
        }

        public static void Clear(string name)
        {
            if (string.IsNullOrEmpty(name)) return;
            Flags.Remove(name);
        }

        public static void ClearAll() => Flags.Clear();

        #endregion
    }
}

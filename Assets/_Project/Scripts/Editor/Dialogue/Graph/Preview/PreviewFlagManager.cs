using System.Collections.Generic;

namespace GlimmerOfHope.Editor.Dialogue.Graph
{
    public class PreviewFlagManager
    {
        #region Private Fields

        private readonly HashSet<string> _flags = new();

        #endregion

        #region Properties

        public IReadOnlyCollection<string> ActiveFlags => _flags;
        public int FlagCount => _flags.Count;

        #endregion

        #region Public Methods

        public void SetFlag(string flag)
        {
            if (!string.IsNullOrEmpty(flag))
                _flags.Add(flag);
        }

        public void UnsetFlag(string flag)
        {
            _flags.Remove(flag);
        }

        public bool HasFlag(string flag)
        {
            return !string.IsNullOrEmpty(flag) && _flags.Contains(flag);
        }

        public void ToggleFlag(string flag)
        {
            if (HasFlag(flag))
                UnsetFlag(flag);
            else
                SetFlag(flag);
        }

        public void ClearAll()
        {
            _flags.Clear();
        }

        public void LoadFlags(IEnumerable<string> flags)
        {
            _flags.Clear();
            foreach (var flag in flags)
            {
                if (!string.IsNullOrEmpty(flag))
                    _flags.Add(flag);
            }
        }

        #endregion
    }
}

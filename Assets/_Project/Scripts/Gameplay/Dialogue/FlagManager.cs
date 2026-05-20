using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using GlimmerOfHope.Core.Services;
using GlimmerOfHope.Core.Save;
using Debug = UnityEngine.Debug;

namespace GlimmerOfHope.Gameplay.Dialogue
{
    public class FlagManager : IService
    {
        #region Private Fields

        private HashSet<string> _flags = new();
        private SaveManager _saveManager;
        private bool _isDirty;

        #endregion

        #region IService

        public void Initialize()
        {
            TryLoadFromSave();
        }

        public void LateInitialize()
        {
            if (_saveManager == null)
                TryLoadFromSave();
        }

        private void TryLoadFromSave()
        {
            if (ServiceLocator.TryGet<SaveManager>(out var sm))
            {
                _saveManager = sm;
                LoadFromSave();
            }
        }

        public void Shutdown()
        {
            FlushIfDirty();
        }

        #endregion

        #region Public Methods

        public void SetFlag(string flag)
        {
            if (string.IsNullOrEmpty(flag)) return;

            if (_flags.Add(flag))
            {
                _isDirty = true;
                LogDebug($"Flag set: {flag}");
            }
        }

        public void UnsetFlag(string flag)
        {
            if (string.IsNullOrEmpty(flag)) return;

            if (_flags.Remove(flag))
            {
                _isDirty = true;
                LogDebug($"Flag unset: {flag}");
            }
        }

        public bool HasFlag(string flag)
        {
            return !string.IsNullOrEmpty(flag) && _flags.Contains(flag);
        }

        public string[] GetAllFlags()
        {
            var arr = new string[_flags.Count];
            _flags.CopyTo(arr);
            return arr;
        }

        public void LoadFlags(string[] flags)
        {
            _flags.Clear();
            if (flags != null)
            {
                foreach (var f in flags)
                    _flags.Add(f);
            }
            _isDirty = false;
        }

        public void ClearAll()
        {
            _flags.Clear();
            _isDirty = true;
            FlushIfDirty();
        }

        /// <summary>
        /// Force save if changes pending. Called by DialogueRunner at end of conversation.
        /// </summary>
        public void FlushIfDirty()
        {
            if (!_isDirty) return;

            SaveToProgress();
            _isDirty = false;
        }

        #endregion

        #region Private Methods

        private void LoadFromSave()
        {
            if (_saveManager?.CurrentSave?.progression?.dialogueFlags != null)
            {
                LoadFlags(_saveManager.CurrentSave.progression.dialogueFlags.ToArray());
            }
        }

        private void SaveToProgress()
        {
            if (_saveManager?.CurrentSave?.progression != null)
            {
                _saveManager.CurrentSave.progression.dialogueFlags = new List<string>(_flags);
                _saveManager.Save();
            }
        }

        [Conditional("DEBUG")]
        private void LogDebug(string message)
        {
            Debug.Log($"[FlagManager] {message}");
        }

        #endregion
    }
}

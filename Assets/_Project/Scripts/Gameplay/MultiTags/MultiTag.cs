using System.Collections.Generic;
using UnityEngine;

namespace GlimmerOfHope.Gameplay
{
    /// <summary>
    /// A Multitag Component , when added to a GameObject it provide the possibility to add multiple Tags .
    /// </summary>
    public class MultiTag : MonoBehaviour
    {
        #region Serialized Fields

        [SerializeField]
        public List<string> _tags = new List<string>();
        #endregion

        #region  Public Methods

        public void AddTag(string newTag)
            {
                if (!_tags.Contains(newTag))
                {
                _tags.Add(newTag);
                }
            }

        public void RemoveTag(string tagToRemove)
        {
            if (_tags.Contains(tagToRemove))
            {
                _tags.Remove(tagToRemove);
            }
        }

        public bool HasTag(string tagToCheck)
        {
            return _tags.Contains(tagToCheck);
        }

        public List<string> GetTags()
        {
            return new List<string>(_tags);
        }
        #endregion
    }
}

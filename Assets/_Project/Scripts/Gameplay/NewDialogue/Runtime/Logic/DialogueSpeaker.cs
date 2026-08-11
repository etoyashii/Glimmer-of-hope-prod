using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace GlimmerOfHope.Gameplay.NewDialogue
{
    /// <summary>
    /// Drop this on any GameObject that can speak in a dialogue (NPC, player, whatever),
    /// Fill The Speaker ID and DialogueManager resolves the Transform through this ID
    /// </summary>
    public class DialogueSpeaker : MonoBehaviour
    {
        #region Serialized Fields

        [Tooltip("Must match exactly the Speaker ID used on the DialogueNode entries for this character.")]
        [FormerlySerializedAs("speakerId")]
        [SerializeField] private string _speakerId;

        #endregion

        #region Private Fields

        private static readonly Dictionary<string, Transform> Registry = new Dictionary<string, Transform>();

        #endregion

        #region Unity Lifecycle

        private void OnEnable()
        {
            if (string.IsNullOrEmpty(_speakerId)) return;
            Registry[_speakerId] = transform;
        }

        private void OnDisable()
        {
            if (string.IsNullOrEmpty(_speakerId)) return;
            if (Registry.TryGetValue(_speakerId, out var registeredTransform) && registeredTransform == transform)
                Registry.Remove(_speakerId);
        }

        #endregion

        #region Public Methods

        //Returns the Transform registered for this ID, or null if no active speaker owns it
        public static Transform GetTransform(string speakerId)
        {
            if (string.IsNullOrEmpty(speakerId)) return null;
            Registry.TryGetValue(speakerId, out var registeredTransform);
            return registeredTransform;
        }

        #endregion
    }
}

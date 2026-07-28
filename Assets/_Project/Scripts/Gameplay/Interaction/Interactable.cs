using UnityEngine;
using UnityEngine.Events;

namespace GlimmerOfHope.Gameplay.Interaction
{
    /// <summary>
    /// Component to attach to any object the player can interact with.
    /// Detection, outline and prompt display are handled by InteractionDetector.
    /// This component only stores the interaction data and reacts to focus changes.
    /// </summary>
    public class Interactable : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Prompt")]
        [Tooltip("Label shown in the interaction prompt, for example Open or Pick up.")]
        [SerializeField] private string _promptLabel = "Interact";

        [Tooltip("World offset applied to the object position to place the prompt above it.")]
        [SerializeField] private Vector3 _promptWorldOffset = new Vector3(0f, 2f, 0f);

        [Header("Behaviour")]
        [Tooltip("If true, the interactable is disabled after the first successful interaction.")]
        [SerializeField] private bool _disableAfterUse = false;

        [Header("Outline")]
        [Tooltip("Optional outline component toggled when this object becomes focused.")]
        [SerializeField] private InteractableOutline _outline;

        [Header("Events")]
        [Tooltip("Invoked every time the player successfully interacts with this object.")]
        public UnityEvent OnInteracted;

        #endregion

        #region Private Fields

        private bool _isInteractable = true;

        #endregion

        #region Public Properties

        public bool IsInteractable => _isInteractable;
        public string PromptLabel => _promptLabel;
        public Vector3 PromptAnchor => transform.position + _promptWorldOffset;

        #endregion

        #region Public Methods

        /// <summary>Called by InteractionDetector when this object gains or loses focus.</summary>
        public void SetFocused(bool focused)
        {
            if (_outline != null)
                _outline.SetOutlineActive(focused);
        }

        /// <summary>Called by InteractionDetector when the interact input is triggered while focused.</summary>
        public void Interact()
        {
            if (!_isInteractable) return;

            OnInteracted?.Invoke();

            if (_disableAfterUse)
                DisableInteraction();
        }

        /// <summary>Allows other systems to re-enable this interactable at runtime.</summary>
        public void EnableInteraction()
        {
            _isInteractable = true;
        }

        /// <summary>Allows other systems to disable this interactable at runtime, for example after a quest step.</summary>
        public void DisableInteraction()
        {
            _isInteractable = false;
            SetFocused(false);
        }

        #endregion
    }
}
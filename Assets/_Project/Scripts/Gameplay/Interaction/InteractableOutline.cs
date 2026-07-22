using UnityEngine;

namespace GlimmerOfHope.Gameplay.Interaction
{
    /// <summary>
    /// Toggles the outline effect on an interactable object.
    /// Adds the outline material as an extra material slot on each target
    /// renderer if not already present, then drives it with a
    /// MaterialPropertyBlock scoped to that slot only, so the original
    /// material and its own property block, for example FireSpell color
    /// fades, are never affected.
    /// </summary>
    public class InteractableOutline : MonoBehaviour
    {
        #region Serialized Fields

        [Tooltip("Renderers to outline. If left empty, all child renderers are used.")]
        [SerializeField] private Renderer[] _targetRenderers;

        [SerializeField] private Material _outlineMaterial;

        [SerializeField] private Color _outlineColor = Color.yellow;

        #endregion

        #region Private Fields

        private MaterialPropertyBlock _propertyBlock;
        private int[] _outlineSlotIndices;

        private static readonly int OutlineEnabledId = Shader.PropertyToID("_OutlineEnabled");
        private static readonly int OutlineColorId = Shader.PropertyToID("_OutlineColor");

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _propertyBlock = new MaterialPropertyBlock();

            if (_targetRenderers == null || _targetRenderers.Length == 0)
                _targetRenderers = GetComponentsInChildren<Renderer>();

            EnsureOutlineMaterialSlots();
        }

        #endregion

        #region Public Methods

        public void SetOutlineActive(bool active)
        {
            for (int i = 0; i < _targetRenderers.Length; i++)
            {
                Renderer renderer = _targetRenderers[i];
                if (renderer == null) continue;

                int slotIndex = _outlineSlotIndices[i];

                renderer.GetPropertyBlock(_propertyBlock, slotIndex);
                _propertyBlock.SetFloat(OutlineEnabledId, active ? 1f : 0f);
                _propertyBlock.SetColor(OutlineColorId, _outlineColor);
                renderer.SetPropertyBlock(_propertyBlock, slotIndex);
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Appends the outline material as an extra slot on every target
        /// renderer that does not already have it, and records the slot
        /// index used for each renderer so property blocks target the
        /// correct material only.
        /// </summary>
        private void EnsureOutlineMaterialSlots()
        {
            _outlineSlotIndices = new int[_targetRenderers.Length];

            for (int i = 0; i < _targetRenderers.Length; i++)
            {
                Renderer renderer = _targetRenderers[i];
                if (renderer == null) continue;

                Material[] sharedMaterials = renderer.sharedMaterials;
                int existingSlot = System.Array.IndexOf(sharedMaterials, _outlineMaterial);

                if (existingSlot != -1)
                {
                    _outlineSlotIndices[i] = existingSlot;
                    continue;
                }

                Material[] expanded = new Material[sharedMaterials.Length + 1];
                sharedMaterials.CopyTo(expanded, 0);
                expanded[sharedMaterials.Length] = _outlineMaterial;

                renderer.sharedMaterials = expanded;
                _outlineSlotIndices[i] = sharedMaterials.Length;
            }
        }

        #endregion
    }
}
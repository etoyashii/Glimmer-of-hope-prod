using UnityEngine;

namespace GlimmerOfHope.Gameplay.Interaction
{
    /// <summary>
    /// Toggles the outline effect on an interactable object, and optionally
    /// a subtle traveling ripple riding along the same outline shell.
    /// Adds the outline material as an extra material slot on each target
    /// renderer if not already present, then drives it with a
    /// MaterialPropertyBlock scoped to that slot only, so the original
    /// material and its own property block, for example FireSpell color
    /// fades, are never affected. The ripple is a pure Base Color addition
    /// in the shader, no alpha blending involved, stays fully Opaque.
    /// </summary>
    public class InteractableOutline : MonoBehaviour
    {
        #region Serialized Fields

        [Tooltip("Renderers to outline. If left empty, all child renderers are used.")]
        [SerializeField] private Renderer[] _targetRenderers;

        [SerializeField] private Material _outlineMaterial;

        [SerializeField] private Color _outlineColor = Color.yellow;

        [Header("Ripple")]
        [Tooltip("If true, a subtle traveling highlight rides along the outline while it is active.")]
        [SerializeField] private bool _useRipple = false;

        [SerializeField] private Color _rippleColor = new Color(0.6f, 1f, 0.6f);
        [SerializeField] private float _rippleSpeed = 1.5f;
        [SerializeField] private float _rippleFrequency = 3f;
        [Range(0f, 1f)]
        [SerializeField] private float _rippleIntensity = 0.6f;

        #endregion

        #region Private Fields

        private MaterialPropertyBlock _propertyBlock;
        private int[] _outlineSlotIndices;

        private static readonly int OutlineEnabledId = Shader.PropertyToID("_OutlineEnabled");
        private static readonly int OutlineColorId = Shader.PropertyToID("_OutlineColor");
        private static readonly int RippleEnabledId = Shader.PropertyToID("_RippleEnabled");
        private static readonly int RippleColorId = Shader.PropertyToID("_RippleColor");
        private static readonly int RippleSpeedId = Shader.PropertyToID("_RippleSpeed");
        private static readonly int RippleFrequencyId = Shader.PropertyToID("_RippleFrequency");
        private static readonly int RippleIntensityId = Shader.PropertyToID("_RippleIntensity");

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

                _propertyBlock.SetFloat(RippleEnabledId, active && _useRipple ? 1f : 0f);
                _propertyBlock.SetColor(RippleColorId, _rippleColor);
                _propertyBlock.SetFloat(RippleSpeedId, _rippleSpeed);
                _propertyBlock.SetFloat(RippleFrequencyId, _rippleFrequency);
                _propertyBlock.SetFloat(RippleIntensityId, _rippleIntensity);

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
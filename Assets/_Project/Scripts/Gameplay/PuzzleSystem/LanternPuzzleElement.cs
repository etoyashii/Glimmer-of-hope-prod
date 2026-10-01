using UnityEngine;
using UnityEngine.Events;
using GlimmerOfHope.Gameplay.Firefly;

namespace GlimmerOfHope.Gameplay.Puzzles
{
    [DisallowMultipleComponent]
    public class LanternPuzzleElement : PuzzleElement
    {
        #region Serialized Fields

        [Header("Broken Lantern")]
        [Tooltip("Firefly whose arrival repairs this lantern. Required.")]
        [SerializeField] private FireflyAI _firefly;

        [Header("Repair Visual")]
        [Tooltip("Healthy lantern shown once repaired. Keep it disabled in the scene.")]
        [SerializeField] private GameObject _repairedVisual;
        [Tooltip("Hides the renderers of the broken lantern once repaired.")]
        [SerializeField] private bool _hideBrokenOnRepair = true;

        [Header("Events")]
        [Tooltip("Fired the moment the lantern is repaired. Use for the visual swap, VFX and audio.")]
        public UnityEvent OnRepaired;

        #endregion

        #region Public Properties

        public bool IsRepaired => _isRepaired;

        #endregion

        #region Private Fields

        private bool _isRepaired;
        private Renderer[] _brokenRenderers;
        private bool[] _brokenStartEnabled;

        #endregion

        #region Unity Lifecycle

        protected override void Awake()
        {
            base.Awake();

            _brokenRenderers = GetComponentsInChildren<Renderer>(true);
            _brokenStartEnabled = new bool[_brokenRenderers.Length];

            for (int i = 0; i < _brokenRenderers.Length; i++)
            {
                _brokenStartEnabled[i] = _brokenRenderers[i].enabled;
            }

            ShowRepaired(false);
        }

        private void OnEnable()
        {
            if (_firefly == null)
            {
                Debug.LogWarning($"[LanternPuzzleElement] '{name}' has no firefly assigned.", this);
                return;
            }

            _firefly.EnteredLantern += HandleFireflyEntered;
        }

        private void OnDisable()
        {
            if (_firefly == null) return;

            _firefly.EnteredLantern -= HandleFireflyEntered;
        }

        #endregion

        #region Public Methods

        public void NotifyRepaired()
        {
            if (_isRepaired) return;

            _isRepaired = true;

            SetSolved(true);
            ShowRepaired(true);
            OnRepaired?.Invoke();

            Debug.Log($"[LanternPuzzleElement] '{ElementName}' repaired.");
        }

        #endregion

        #region Protected Methods

        protected override void OnReset()
        {
            _isRepaired = false;

            ShowRepaired(false);

            if (_firefly == null) return;

            _firefly.ResetFirefly();
        }

        #endregion

        #region Private Methods

        private void HandleFireflyEntered()
        {
            NotifyRepaired();
        }

        private void ShowRepaired(bool repaired)
        {
            if (_repairedVisual != null)
            {
                _repairedVisual.SetActive(repaired);
            }

            if (!_hideBrokenOnRepair) return;
            if (_brokenRenderers == null) return;

            for (int i = 0; i < _brokenRenderers.Length; i++)
            {
                if (_brokenRenderers[i] == null) continue;
                if (_repairedVisual != null && _brokenRenderers[i].transform.IsChildOf(_repairedVisual.transform)) continue;

                _brokenRenderers[i].enabled = !repaired && _brokenStartEnabled[i];
            }
        }

        #endregion

        #region Gizmos

        private void OnDrawGizmos()
        {
            Gizmos.color = _isRepaired ? Color.green : Color.cyan;
            Gizmos.DrawWireSphere(transform.position, 0.4f);
        }

        #endregion
    }
}

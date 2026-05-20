using UnityEngine;
using System.Collections;

namespace GlimmerOfHope.Gameplay.Character.SpecialActions
{
    #region Dependancies

    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(Movement))]
    [RequireComponent(typeof(SkillManager))]

    #endregion
    public class Jump : MonoBehaviour
    {
        #region SerializeFields

        [Header("Jump Arc")]

        [Tooltip("Jump Height")]
        [Range(0.5f, 20f)]
        [SerializeField] private float _jumpArcHeight = 1.8f;
        
        [Tooltip("Jump duration (s)")]
        [Range(0.2f, 5f)]
        [SerializeField] private float _jumpDuration = 0.45f;

        [Header("References")]
        [SerializeField] SkillManager _skillComp;
        [SerializeField] private CharacterController _characterControllerComp;
        [SerializeField] private Movement _movementComp;

        #endregion

        #region Private Fields

        private bool _isJumping = false;

        #endregion

        #region Public Properties

        public bool IsJumping => _isJumping;

        #endregion

        #region Public Methods

        public void TriggerJump(Vector3 landingPoint)
        {
            if (_isJumping || _skillComp.HasJump == false) return;

            StartCoroutine(JumpArc(landingPoint));
        }

        #endregion

        #region Private Methods

        private IEnumerator JumpArc(Vector3 target)
        {
            _isJumping = true;
            _movementComp.SetMovementEnabled(false);
            _characterControllerComp.enabled = false;

            Vector3 start = transform.position;
            float elapsed = 0f;

            while (elapsed < _jumpDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / _jumpDuration);

                Vector3 pos = Vector3.Lerp(start, target, t);
                pos.y += _jumpArcHeight * Mathf.Sin(t * Mathf.PI);

                transform.position = pos;
                yield return null;
            }

            transform.position = target;
            _characterControllerComp.enabled = true;
            _movementComp.SetMovementEnabled(true);
            _isJumping = false;
        }

        #endregion
    }
}
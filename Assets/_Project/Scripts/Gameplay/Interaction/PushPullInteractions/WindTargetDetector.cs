using System.Collections.Generic;
using GlimmerOfHope.Gameplay.Character.SpecialActions;
using GlimmerOfHope.Gameplay.Interaction;
using UnityEngine;

namespace GlimmerOfHope.Gameplay
{
    /// <summary>
    /// Highlights every WindPushable tagged object within reach of the
    /// caster with a green outline, by reusing InteractableOutline so both
    /// systems share the same shader and material infrastructure. Works on
    /// any object with the WindPushable tag, whether or not it also has
    /// WindReactive, since plain physics objects react to the wind skills
    /// too. Mirrors the same filters used by PushSkill and PullSkill, dot
    /// product facing check, non kinematic Rigidbody, and WindReactive
    /// angle gate, so a highlighted object is always one that will
    /// actually react to the next cast.
    /// </summary>
    public class WindTargetDetector : MonoBehaviour
    {
        #region Constants

        private const string PUSHABLE_TAG = "WindPushable";

        #endregion

        #region Serialized Fields

        [Header("References")]
        [Tooltip("Transform of the caster, usually the player.")]
        [SerializeField] private Transform _casterTransform;

        [Tooltip("Optional, only highlights targets while at least one wind skill is unlocked.")]
        [SerializeField] private SkillManager _skillManager;

        [Header("Detection Shape")]
        [Tooltip("Reach of the highlight in front of the caster. Keep in sync with PushSkill and PullSkill length and radius so the preview matches what actually reacts.")]
        [SerializeField] private float _detectionLength = 8f;
        [SerializeField] private float _detectionRadius = 3f;
        [SerializeField] private LayerMask _detectionMask = ~0;

        [Header("Check")]
        [Tooltip("Time in seconds between two detection checks, used to reduce cost.")]
        [SerializeField] private float _checkInterval = 0.15f;

        #endregion

        #region Private Fields

        private readonly Collider[] _candidatesBuffer = new Collider[32];
        private readonly HashSet<InteractableOutline> _currentlyHighlighted = new();
        private readonly HashSet<InteractableOutline> _foundThisCheck = new();

        private float _checkTimer;

        #endregion

        #region Unity Lifecycle

        private void Update()
        {
            _checkTimer -= Time.deltaTime;
            if (_checkTimer > 0f) return;

            _checkTimer = _checkInterval;
            RefreshHighlights();
        }

        #endregion

        #region Private Methods

        private void RefreshHighlights()
        {
            if (!IsAnyWindSkillUnlocked())
            {
                ClearAllHighlights();
                return;
            }

            _foundThisCheck.Clear();

            Vector3 forward = new Vector3(_casterTransform.forward.x, 0f, _casterTransform.forward.z).normalized;
            Vector3 capsuleEnd = _casterTransform.position + forward * _detectionLength;

            int count = Physics.OverlapCapsuleNonAlloc(
                _casterTransform.position,
                capsuleEnd,
                _detectionRadius,
                _candidatesBuffer,
                _detectionMask
            );

            for (int i = 0; i < count; i++)
            {
                Collider col = _candidatesBuffer[i];
                if (!col.CompareTag(PUSHABLE_TAG)) continue;
                if (!WouldReact(col, forward)) continue;

                InteractableOutline outline = col.GetComponentInParent<InteractableOutline>();
                if (outline == null) continue;

                _foundThisCheck.Add(outline);

                if (_currentlyHighlighted.Add(outline))
                    outline.SetOutlineActive(true);
            }

            // Turn off outlines for objects no longer in range or no longer valid
            _currentlyHighlighted.RemoveWhere(outline =>
            {
                if (_foundThisCheck.Contains(outline)) return false;
                if (outline != null) outline.SetOutlineActive(false);
                return true;
            });
        }

        /// <summary>
        /// Mirrors the filtering logic used in PushSkill.Push() and
        /// PullSkill.Pull(), so a highlighted object is guaranteed to be
        /// one the next cast would actually affect.
        /// </summary>
        private bool WouldReact(Collider col, Vector3 forward)
        {
            Vector3 toObject = (col.transform.position - _casterTransform.position).normalized;
            if (Vector3.Dot(forward, toObject) < 0f) return false;

            WindReactive reactive = col.GetComponent<WindReactive>();
            if (reactive != null)
            {
                Vector3 casterPos = _casterTransform.position;
                return reactive.CanReactToPush(casterPos) || reactive.CanReactToPull(casterPos);
            }

            Rigidbody rb = col.attachedRigidbody;
            return rb != null && !rb.isKinematic;
        }

        private void ClearAllHighlights()
        {
            foreach (InteractableOutline outline in _currentlyHighlighted)
                if (outline != null) outline.SetOutlineActive(false);

            _currentlyHighlighted.Clear();
        }

        private bool IsAnyWindSkillUnlocked()
        {
            if (_skillManager == null) return true;

            return _skillManager.IsSkillUnlocked((int)SkillManager.SkillType.Push)
                || _skillManager.IsSkillUnlocked((int)SkillManager.SkillType.Pull);
        }

        #endregion
    }
}
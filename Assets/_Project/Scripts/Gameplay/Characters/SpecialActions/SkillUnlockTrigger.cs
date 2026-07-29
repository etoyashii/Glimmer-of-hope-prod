using UnityEngine;

namespace GlimmerOfHope.Gameplay.Character.SpecialActions
{
    /// <summary>
    /// Adapter placed on a stele or any interactable that should unlock a
    /// skill. UnityEvent only supports invoking methods with no arguments
    /// or a single bool, int, float, string or Object argument, so a
    /// method expecting a SkillType enum cannot be wired directly into
    /// Interactable OnInteracted. This component exposes the skill to
    /// unlock as a plain serialized field, shown as a proper enum
    /// dropdown in the Inspector, and calls UnlockSkill() through a
    /// parameterless method that OnInteracted can call.
    /// </summary>
    public class SkillUnlockTrigger : MonoBehaviour
    {
        #region Serialized Fields

        [Tooltip("Manager holding the unlock state for every skill.")]
        [SerializeField] private SkillManager _skillManager;

        [Tooltip("Skill unlocked when Unlock() is called.")]
        [SerializeField] private SkillManager.SkillType _skillToUnlock;

        #endregion

        #region Public Methods

        /// <summary>Wire this to Interactable OnInteracted.</summary>
        public void Unlock()
        {
            _skillManager.UnlockSkill(_skillToUnlock);
        }

        #endregion
    }
}
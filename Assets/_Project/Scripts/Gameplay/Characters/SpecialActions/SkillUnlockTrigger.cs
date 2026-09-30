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
    /// On first interaction, unlocking the skill fires SkillManager's
    /// OnSkillTypeUnlocked event, which SkillNoteManager listens to in
    /// order to reveal the combo automatically. On any later interaction
    /// the skill is already unlocked so UnlockSkill is a no op and fires
    /// no event, so the reveal is triggered here explicitly instead.
    /// </summary>
    public class SkillUnlockTrigger : MonoBehaviour
    {
        #region Serialized Fields

        [Tooltip("Manager holding the unlock state for every skill.")]
        [SerializeField] private SkillManager _skillManager;

        [Tooltip("Manager holding the combo list and reveal animation.")]
        [SerializeField] private SkillNoteManager _skillNoteManager;

        [Tooltip("Skill unlocked, and previewed, when Unlock() is called.")]
        [SerializeField] private SkillManager.SkillType _skillToUnlock;

        #endregion

        #region Public Methods

        /// <summary>Wire this to Interactable OnInteracted.</summary>
        public void Unlock()
        {
            bool wasAlreadyUnlocked = _skillManager.IsSkillUnlocked((int)_skillToUnlock);

            _skillManager.UnlockSkill(_skillToUnlock);

            // First unlock already triggers the reveal through the
            // OnSkillTypeUnlocked event. Repeat visits need it explicitly
            // since UnlockSkill silently no ops once already unlocked.
            if (wasAlreadyUnlocked)
                _skillNoteManager.ShowCombo((int)_skillToUnlock);
        }

        #endregion
    }
}
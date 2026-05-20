using UnityEngine;

namespace GlimmerOfHope.Gameplay.Character.SpecialActions
{
    public class SkillManager : MonoBehaviour
    {
        #region SerializeFields

        [Header("Skills unlocked")]
        [SerializeField] private bool _hasPush = false;
        [SerializeField] private bool _hasJump = false;
        [SerializeField] private bool _hasClimb = false;
        // Here new boolean for new skills :

        #endregion

        #region Private Fields

        private int _skillUnlocked = -1;

        #endregion

        #region Public Properties

        public bool HasPush => _hasPush;
        public bool HasJump => _hasJump;
        public bool HasClimb => _hasClimb;

        #endregion

        #region Unity Lifecycle

        //Temporary
        private void Awake()
        {
            _hasPush = false;
            _hasJump = true;
            _hasClimb = false;
        }

        #endregion

        #region Public Methods

        public void UnlockSkill()
        {
            switch (_skillUnlocked)
            {
                case 0:
                    UnlockPushRock();
                    break;
                case 1:
                    UnlockJump();
                    break;
                case 2:
                    UnlockClimb();
                    break;

                // and more Methods called with new skills !
            }

            _skillUnlocked++;
        }

        #endregion

        #region Private Methods

        private void UnlockPushRock()
        {
            _hasPush = true;
            Debug.Log("[SkillManager] Compétence POUSSER débloquée !");
        }

        private void UnlockJump()
        {
            _hasJump = true;
            Debug.Log("[SkillManager] Compétence SAUT débloquée !");
        }

        private void UnlockClimb()
        {
            _hasClimb = true;
            Debug.Log("[SkillManager] Compétence ESCALADE débloquée !");
        }
        // New skills here :

        #endregion
    }
}
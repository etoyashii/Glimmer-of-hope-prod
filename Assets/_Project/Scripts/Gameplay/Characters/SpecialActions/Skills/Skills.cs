using System.Collections;
using UnityEngine;

namespace GlimmerOfHope.Gameplay
{
    public class Skills : MonoBehaviour
    {
        #region Serialized Fields
        [SerializeField] private float _cooldownTime;
        [SerializeField] public bool isActive = true;
        #endregion

        #region Private Fields
        private bool _isCoolingDown = false;
        #endregion

        #region Public Methods
        public void LaunchSkill()
        {
            if (!isActive) return;
            if (_isCoolingDown) return;

            PerformSkill();

            StartCoroutine(SkillCooldown(_cooldownTime));
        }

        public virtual void PerformSkill()
        {

        }
        #endregion

        IEnumerator SkillCooldown(float cooldownTime)
        {
            _isCoolingDown = true;
            yield return new WaitForSeconds(cooldownTime);
            _isCoolingDown = false;
        }
    }
}

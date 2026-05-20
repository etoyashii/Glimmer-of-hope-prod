using UnityEngine;

namespace GlimmerOfHope.Gameplay.Spells
{
    public class SpellManager : MonoBehaviour
    {
        #region Serialized Fields
        [SerializeField] private bool _EarthActive = false;
        [SerializeField] private bool _WaterActive = false;
        [SerializeField] private bool _WindActive = false;
        [SerializeField] private bool _FireActive = false;
        #endregion

        #region Private Fields
        private bool _isSpellMode1 = true;
        private ElementalSpell currentElementalSpell;
        #endregion

        #region Public Methods
        public void UnlockEarth()
        {
            _EarthActive = true;
            Debug.Log("[SkillManager] Compétence EARTH débloquée !");
            _WaterActive = false;
            _WindActive = false;
            _FireActive = false;
            currentElementalSpell = GetComponent<EarthSpell>();
        }

        public void UnlockWater()
        {
            _WaterActive = true;
            Debug.Log("[SkillManager] Compétence WATER débloquée !");
            _EarthActive = false;
            _WindActive = false;
            _FireActive = false;
            currentElementalSpell = GetComponent<WaterSpell>();
        }

        public void UnlockWind()
        {
            _WindActive = true;
            Debug.Log("[SkillManager] Compétence WIND débloquée !");
            _EarthActive = false;
            _WaterActive = false;
            _FireActive = false;
            currentElementalSpell = GetComponent<WindSpell>();
        }

        public void UnlockFire()
        {
            _FireActive = true;
            Debug.Log("[SkillManager] Compétence FIRE débloquée !");
            _EarthActive = false;
            _WaterActive = false;
            _WindActive = false;
            currentElementalSpell = GetComponent<FireSpell>();
        }
        public void UpdateSpellMode()
        {
            if (_isSpellMode1)
            {
                _isSpellMode1 = false;
            }
            else
            {
                _isSpellMode1 = true;
            }
        }
        public void CastElementalSpell()
        {
            currentElementalSpell?.CastSpell(_isSpellMode1);
        }
        #endregion
    }
}

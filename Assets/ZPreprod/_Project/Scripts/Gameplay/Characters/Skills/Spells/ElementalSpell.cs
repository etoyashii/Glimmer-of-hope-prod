using UnityEngine;

namespace GlimmerOfHope.Gameplay.Spells
{
    public class ElementalSpell : MonoBehaviour
    {
        #region Public Methods
        public virtual void CastSpell(bool spellmode)
        {
            Debug.Log("Casting an elemental spell!");
        }
        #endregion
    }
}

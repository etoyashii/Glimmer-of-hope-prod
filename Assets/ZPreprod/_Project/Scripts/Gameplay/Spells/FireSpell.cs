using UnityEngine;
using GlimmerOfHope.Gameplay.Spells;
public class FireSpell : ElementalSpell
{
    public override void CastSpell(bool spellmode)
    {
        Debug.Log("Casting a powerful fire spell!");
    }
}

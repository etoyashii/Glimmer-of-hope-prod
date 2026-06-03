using UnityEngine;
using GlimmerOfHope.Gameplay.Spells;

public class WaterSpell : ElementalSpell
{
    public override void CastSpell(bool spellmode)
    {
        Debug.Log("Casting a powerful water spell!");
    }
}

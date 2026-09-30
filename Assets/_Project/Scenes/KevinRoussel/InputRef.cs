using GlimmerOfHope.Gameplay;
using UnityEngine;

public class InputRef : MonoBehaviour
{
    public void _ActivateInput() => InputManager.FreezeInput(true);
    public void _DeactivateInput() => InputManager.FreezeInput(false);
}

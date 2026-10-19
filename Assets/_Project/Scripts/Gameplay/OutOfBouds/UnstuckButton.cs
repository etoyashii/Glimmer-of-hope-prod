using System;
using GlimmerOfHope.Gameplay.Character.SpecialActions;
using UnityEngine;
using UnityEngine.UI;

namespace GlimmerOfHope.Gameplay
{
    public class UnstuckButton : MonoBehaviour
    {
        [SerializeField] private Button button;

        private void Awake()
        {
            button.onClick.AddListener(Unstuck);
        }

        private void Unstuck()
        {
            Movement character = GameObject.FindFirstObjectByType<Movement>();

            if (character != null)
                character.transform.position = OutOfBoundsRespawn.GetNearestRespawnPosition(character.transform.position);
        }
    }
}

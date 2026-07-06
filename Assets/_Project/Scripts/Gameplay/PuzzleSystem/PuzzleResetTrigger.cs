using UnityEngine;

namespace GlimmerOfHope.Gameplay
{
    public class PuzzleResetTrigger : MonoBehaviour
    {
        [SerializeField] private PuzzleZone _puzzleZone;
        public void OnTriggerEnter(Collider other)
        {
            Debug.Log("reset !");
            _puzzleZone.ResetInitialPuzzlePositions();
        }
    }
}

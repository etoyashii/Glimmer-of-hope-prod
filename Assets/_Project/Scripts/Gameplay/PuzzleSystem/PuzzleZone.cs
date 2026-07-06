using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

namespace GlimmerOfHope.Gameplay
{
    public class PuzzleZone : MonoBehaviour
    {
        [SerializeField] private List<GameObject> _puzzleObjects;

        private List<Transform> _initialPositions = new List<Transform>();

        public void Start()
        {
            foreach (GameObject gO in _puzzleObjects)
            {
                _initialPositions.Add(gO.transform);
            }
        }

        public void ResetInitialPuzzlePositions()
        {
            int index = 0;

            foreach (Transform t in _initialPositions)
            {
                _puzzleObjects[index].transform.position = t.position;
                index++;
            }
        }
    }
}

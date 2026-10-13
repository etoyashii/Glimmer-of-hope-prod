using System.Collections.Generic;
using UnityEngine;

namespace GlimmerOfHope.Gameplay.UI
{
    public class BreathingPoints : MonoBehaviour
    {
        [SerializeField] private List<BreathingPointElement> breathingPoints;

        private int _currentValue = -1;

        private void Awake()
        {
            SetValue(0, false);
        }

        public void SetValue(int value, bool animate)
        {
            if (value == _currentValue)
                return;
            _currentValue = value;
            for (int i = 0; i < breathingPoints.Count; i++)
                breathingPoints[i].SetOn(i < value, animate);
        }
    }
}

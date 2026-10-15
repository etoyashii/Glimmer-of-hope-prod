using System;
using System.Linq;
using GlimmerOfHope.Gameplay;
using UnityEngine;
using UnityEngine.Events;

public class ButterflyUnlockRocks : MonoBehaviour
{
    [SerializeField] ButterflyColideMoss[] _butterflies;
    [SerializeField] UnityEvent _onAllValidated;

    bool _completed;

    void Awake()
    {
        _completed = false;
    }

    void Update()
    {
        if (_completed) return;
        
        if (_butterflies.All(i => i.IsValidated))
        {
            _onAllValidated?.Invoke();
            _completed = true;
        }
    }
}

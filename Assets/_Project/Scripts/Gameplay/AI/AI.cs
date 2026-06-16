using GlimmerOfHope.Core;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace GlimmerOfHope.Gameplay
{

    /// <summary>
    /// 
    /// </summary>
    public class AI : MonoBehaviour
    {
        Animator _anim;
        public Transform _player;
        State _currentState;
        NavMeshAgent _agent;


        private void Start()
        {
            _agent = GetComponent<NavMeshAgent>();
            _anim = GetComponent<Animator>();
            _currentState = new Idle(gameObject, _agent, _anim, _player);
        }

        private void Update()
        {
            _currentState = _currentState.Process();
        }
    }
}
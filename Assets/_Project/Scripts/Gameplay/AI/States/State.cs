using UnityEngine;
using UnityEngine.AI;

namespace GlimmerOfHope.Gameplay
{
    public class State
    {
        public enum STATE
        {
            IDLE,
            PATROL,
            SEEKING,
            FOLLOW,
            SPEAKING
        }

        public enum EVENT
        {
            ENTER,
            UPDATE, 
            EXIT
        }

        public STATE _currentState;
        public EVENT _stage;
        protected GameObject _npc;
        protected Animator _anim;
        protected Transform _player;
        protected State _nextState;
        protected NavMeshAgent _agent;

        float _visualDistance = 100.0f;
        float _visualAngle = 30.0f;
        float _startFollowDist = 49.0f;
        float _stopFollowDist = 10.0f;

        public State(GameObject npc, NavMeshAgent agent, Animator anim, Transform player)
        {
            _npc = npc;
            _agent = agent;
            _anim = anim;
            _player = player;
        }

        public virtual void Enter() { _stage = EVENT.UPDATE; }
        public virtual void Update() { _stage = EVENT.UPDATE; }
        public virtual void Exit() { _stage = EVENT.EXIT; }

        public State Process()
        {
            if (_stage == EVENT.ENTER) Enter();
            if (_stage == EVENT.UPDATE) Update();
            if (_stage == EVENT.EXIT)
            {
                Exit();
                return _nextState;
            }

            return this;
        }

        public bool CanSeePlayer()
        {
            Vector3 direction = _player.position - _npc.transform.position;
            float angle = Vector3.Angle(direction, _npc.transform.forward);

            if (direction.sqrMagnitude < _visualDistance && angle < _visualAngle)
            {
                return true;
            }

            return false;
        }

        public bool CanFollowPlayer()
        {
            Vector3 direction = _player.position - _npc.transform.position;
            if (direction.sqrMagnitude < _startFollowDist)
            {
                return true;
            }
            
            return false;
        }

        public bool IsNearPlayer()
        {
            Vector3 direction = _player.position - _npc.transform.position;
            if (direction.sqrMagnitude < _stopFollowDist)
            {
                return true;
            }

            return false;
        }
    }

    public class Idle : State
    {
        public Idle(GameObject npc, NavMeshAgent agent, Animator anim, Transform player) : base(npc, agent, anim, player) 
        {
            _currentState = STATE.IDLE;
        }

        public override void Enter()
        {
            //anim enter here
            base.Enter();
        }

        public override void Update()
        {
            if (CanSeePlayer())
            {
                _nextState = new Seek(_npc, _agent, _anim, _player);
                _stage = EVENT.EXIT;
            }
            if (Random.Range(0, 100) < 10)
            {
                _nextState = new Patrol(_npc, _agent, _anim, _player);
                _stage = EVENT.EXIT;
            }
        }

        public override void Exit()
        {
            //anim exit here
            base.Exit();
        }
    }

    public class Patrol : State
    {
        private int _checkPointCount;
        protected int _currentIndex = -1;

        public Patrol(GameObject npc, NavMeshAgent agent, Animator anim, Transform player) : base(npc, agent, anim, player)
        {
            _currentState = STATE.PATROL;
            agent.speed = 2;
            agent.isStopped = false;
        }

        public override void Enter()
        {
            //anim enter here
            _currentIndex = 0;
            base.Enter();
        }

        public override void Update()
        {
            if (_agent.remainingDistance < 1)
            {
                if (_currentIndex >= GameEnvironment.Singleton.Checkpoints.Count)
                {
                    _currentIndex = 0;
                }
                else
                    _currentIndex++;

                _agent.SetDestination(GameEnvironment.Singleton.Checkpoints[_currentIndex].transform.position);
            }

            if (CanSeePlayer())
            {
                _nextState = new Seek(_npc, _agent, _anim, _player);
                _stage = EVENT.EXIT;
            }
        }

        public override void Exit()
        {
            //anim exit here
            base.Exit();
        }
    }

    public class Seek : State
    {
        public Seek(GameObject npc, NavMeshAgent agent, Animator anim, Transform player) : base(npc, agent, anim, player)
        {
            _currentState = STATE.SEEKING;
            agent.speed = 0;
            agent.isStopped = true;
        }

        public override void Enter()
        {
            //Enter anim
            base.Enter();
        }

        public override void Update()
        {
            if (CanFollowPlayer() && CanSeePlayer())
            {
                _nextState = new Follow(_npc, _agent, _anim, _player);
                _stage = EVENT.EXIT;
            }
            else if (CanSeePlayer() == false)
            {
                _nextState = new Patrol(_npc, _agent, _anim, _player);
                _stage = EVENT.EXIT;
            }
        }

        public override void Exit()
        {
            //Enter exit
            base.Exit();
        }
    }

    public class Follow : State
    {
        public Follow(GameObject npc, NavMeshAgent agent, Animator anim, Transform player) : base(npc, agent, anim, player)
        {
            _currentState = STATE.FOLLOW;
            agent.speed = 5;
            agent.isStopped = false;
        }

        public override void Enter()
        {
            //enter anim
            base.Enter();
        }

        public override void Update()
        {
            _agent.SetDestination(_player.transform.position);

            if (IsNearPlayer())
            {
                _nextState = new Seek(_npc, _agent, _anim, _player);
                _stage = EVENT.EXIT;
            }
        }

        public override void Exit()
        {
            //exit anim
            base.Exit();
        }
    }
}

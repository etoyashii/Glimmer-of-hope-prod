namespace GlimmerOfHope.Gameplay.AI
{
    public interface IState
    {
        void Enter();
        void Tick();
        void Exit();
    }

    public abstract class State : IState
    {
        public virtual void Enter() { }
        public virtual void Tick() { }
        public virtual void Exit() { }
    }

    public class StateMachine : IState
    {
        private IState _current;

        public IState Current => _current;

        public void ChangeState(IState newState)
        {
            if (newState == _current) return;

            _current?.Exit();
            _current = newState;
            _current?.Enter();
        }

        public bool IsInState(IState state)
        {
            return _current == state;
        }

        public void Enter() { }

        public void Tick()
        {
            _current?.Tick();
        }

        public void Exit()
        {
            _current?.Exit();
        }
    }
}

using System.Collections;

namespace mactinite.ToolboxCommons.StateMachine
{
    public abstract class State<T> where T : StateMachine<T>
    {
        private T stateMachine;

        public T StateMachine
        {
            get => stateMachine;
            set => stateMachine = value;
        }

        public State(T stateMachine)
        {
            StateMachine = stateMachine;
        }

        public State()
        {
        }

        public virtual IEnumerator Start()
        {
            yield break;
        }

        public virtual IEnumerator Tick()
        {
            yield break;
        }

        public virtual IEnumerator End()
        {
            yield break;
        }
    }
}
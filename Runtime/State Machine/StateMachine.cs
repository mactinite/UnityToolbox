using System;
using System.Collections.Generic;
using UnityEngine;

namespace mactinite.ToolboxCommons.StateMachine
{

    public abstract class StateMachine<T> : MonoBehaviour where T : StateMachine<T>
    {
        protected State<T> State;
        public Dictionary<string, State<T>> StateMap = new Dictionary<string, State<T>>();

        public void SetState(State<T> state)
        {
            if (State != null)
                StartCoroutine(State.End());
            State = state;
            StartCoroutine(State.Start());
        }

        public void SetState<S>() where S : State<T>, new()
        {
            State<T> stateInstance = new S();
            stateInstance.StateMachine = (T) this;
            if (State != null)
                StartCoroutine(State.End());
            State = stateInstance;
            StartCoroutine(State.Start());
        }

        public void SetState(string stateName)
        {
            if (StateMap.TryGetValue(stateName, out State))
            {
                SetState(State);
            }
            else
            {
                throw new Exception(stateName +
                                    " not found in runtime state map. Did you forget to register the state or use the wrong identifier?");
            }
        }


        public void Update()
        {
            if (State != null)
                StartCoroutine(State.Tick());
        }

        private void OnDestroy()
        {
            this.StopAllCoroutines();
        }
    }
}


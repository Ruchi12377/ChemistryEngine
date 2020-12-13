using UnityEngine;

namespace Chemistry
{
    public sealed class Element : ChemistryObject
    {
        [SerializeField, NonEditableInPlay] private State state;
        [SerializeField, NonEditable] private State subState;
        [NonEditable] public State beforeState;
        public State State
        {
            get => state;
            set
            {
                beforeState = state;
                state = value;
            }
        }
        
        public State SubState
        {
            get => subState;
            set => subState = value;
        }
    }
}
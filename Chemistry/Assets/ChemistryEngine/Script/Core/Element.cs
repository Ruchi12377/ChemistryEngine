using UnityEngine;

namespace Chemistry
{
    public sealed class Element : ChemistryObject
    {
        [SerializeField, NonEditableInPlay] private State state;
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
    }
}
using UnityEngine;

namespace Chemistry
{
    public sealed class Element : ChemistryObject
    {
        [SerializeField, NonEditableInPlay] private State state;
        [SerializeField, NonEditableInPlay] private State oneMoreState;
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
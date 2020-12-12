using System;
using UnityEngine;

namespace Chemistry
{
     public sealed class Material : ChemistryObject
     { 
         [SerializeField, NonEditableInPlay] private State state;
         [NonEditableInPlay]
         public Substance substance;
         [NonSerialized]
         public Element element;
         
         public State State
         {
             get => state;
             set => state = value;
         }
     }
}

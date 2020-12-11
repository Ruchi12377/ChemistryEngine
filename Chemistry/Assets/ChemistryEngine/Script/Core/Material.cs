using System;

namespace Chemistry
{
     public sealed class Material : ChemistryObject
     { 
         [NonEditableInPlay]
         public Substance substance;
         [NonSerialized]
         public Element element;
     }
}

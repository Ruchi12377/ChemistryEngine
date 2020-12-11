using System;

namespace Chemistry
{
    [Serializable]
    public struct ParticleMagnification
    {
        [NonEditableInPlay] public SizeOffset fire;
        [NonEditableInPlay] public SizeOffset fireMini;
        [NonEditableInPlay] public SizeOffset water;
        [NonEditableInPlay] public SizeOffset ice;
        [NonEditableInPlay] public SizeOffset wind;
        [NonEditableInPlay] public SizeOffset electricity;
    }
}
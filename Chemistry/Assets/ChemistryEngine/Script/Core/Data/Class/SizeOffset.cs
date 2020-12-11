using System;
using UnityEngine;

namespace Chemistry
{
    [Serializable]
    public class SizeOffset
    {
        [NonEditableInPlay] public float offset;
        [NonEditableInPlay] public Vector2 particleMinMax;
    }
}
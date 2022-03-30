using System;
using UnityEngine;

namespace Chemistry
{
    [Serializable]
    public struct ParticleMagnification
    {
        [NonEditableInPlay, Min(0.01f)] public float fire;
        [NonEditableInPlay, Min(0.01f)] public float water;
        [NonEditableInPlay, Min(0.01f)] public float ice;
        [NonEditableInPlay, Min(0.01f)] public float wind;
    }
}
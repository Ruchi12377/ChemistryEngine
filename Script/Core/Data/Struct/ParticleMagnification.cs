using System;
using UnityEngine;

namespace ChemistryEngine.Script.Core.Data.Struct
{
	[Serializable]
	public struct ParticleMagnification
	{
		[NonEditableInPlay, Min(0.01f)] public float fire;
		[NonEditableInPlay, Min(0.01f)] public float water;
		[NonEditableInPlay, Min(0.01f)] public float ice;
		[NonEditableInPlay, Min(0.01f)] public float wind;

		public ParticleMagnification(float fire, float water, float ice, float wind)
		{
			this.fire = fire;
			this.water = water;
			this.ice = ice;
			this.wind = wind;
		}
	}
}
using UnityEngine;

namespace ChemistryEngine.Script.Core
{
    public abstract class MeshParticle : MonoBehaviour
    {
        public virtual void Destroy(GameObject parent)
        {
            UnityEngine.Object.Destroy(parent);
        }
    
        public virtual void Destroy(Transform parent)
        {
            UnityEngine.Object.Destroy(parent);
        }
    }
}

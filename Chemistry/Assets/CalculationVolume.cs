using UnityEngine;

namespace Chemistry
{
    public static class CalculationVolume
    {
        public static float GetVolume(Transform transform, Mesh mesh)
        {
            if (mesh == null) return 0;
     
            var vertices = mesh.vertices;
            var triangles = mesh.triangles;
     
            float volume = 0;
            for (var i = 0; i < triangles.Length; i += 3)
            {
                var p1 = vertices[triangles[i + 0]];
                var p2 = vertices[triangles[i + 1]];
                var p3 = vertices[triangles[i + 2]];
                volume += SignedVolumeOfTriangle(p1, p2, p3);
            }

            var s = transform.lossyScale;
            //オブジェクトのスケールをかける
            return Mathf.Abs(volume) * s.x * s.y * s.z;
        }
     
        private static float SignedVolumeOfTriangle(Vector3 p1, Vector3 p2, Vector3 p3)
        {
            var v321 = p3.x * p2.y * p1.z;
            var v231 = p2.x * p3.y * p1.z;
            var v312 = p3.x * p1.y * p2.z;
            var v132 = p1.x * p3.y * p2.z;
            var v213 = p2.x * p1.y * p3.z;
            var v123 = p1.x * p2.y * p3.z;
            return (1.0f / 6.0f) * (-v321 + v231 + v312 - v132 - v213 + v123);
        }
    }
}
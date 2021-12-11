using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random;
using Object = UnityEngine.Object;
using Mat = UnityEngine.Material;


[ExecuteAlways]
public class Electricity : MeshParticle
{
    private Transform _flash;
    private Transform _flash1;
    private Transform _flash2;
    private Transform _noise;
    private Mat _flash1Mat;
    private Mat _flash2Mat;
    private Mat _noiseMat;

    private void OnEnable()
    {
        _flash = transform;
        _flash1 = _flash.GetChild(0);
        _flash2 = _flash.GetChild(1);
        _noise = _flash.GetChild(2);

        _flash1Mat = _flash1.GetComponent<MeshRenderer>().material;
        _flash2Mat = _flash1.GetComponent<MeshRenderer>().material;
        _noiseMat = _flash1.GetComponent<MeshRenderer>().material;
    }

    private void Update()
    {
        var rot = new Vector3(Random.Range(-180, 360), Random.Range(-180, 360), Random.Range(-180, 360));
        _flash.Rotate(rot);
        _noise.Rotate(rot);
        var active = _flash1.gameObject.activeSelf == false;
        _flash1.gameObject.SetActive(active);
        _flash2.gameObject.SetActive(active == false);    
    }

    public override void Destroy(GameObject parent)
    {
        FadeOut(parent);
    }
    
    public override void Destroy(Transform parent)
    {
        FadeOut(parent.gameObject);
    }

    private void FadeOut(Object parent)
    {
        var f1 = _flash1Mat.color;
        var f2 = _flash2Mat.color;
        var n = _noiseMat.color;
        var alpha = 1f;
        StartCoroutine(FadeOutCoroutine());
        IEnumerator FadeOutCoroutine()
        {
            while (alpha <= 0)
            {
                f1.a = alpha;
                f2.a = alpha;
                n.a = alpha;
                _flash1Mat.color = f1;
                _flash2Mat.color = f2;
                _noiseMat.color = n;
                alpha -= Time.deltaTime;
                yield return null;
            }
            UnityEngine.Object.Destroy(parent);
        }   
    }

    public void Reset()
    {
        OnEnable();
    }
}

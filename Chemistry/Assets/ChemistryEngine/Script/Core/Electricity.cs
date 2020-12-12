using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random;

[ExecuteAlways]
public class Electricity : MeshParticle
{
    [SerializeField, NonEditableInPlay] private Transform flash;
    private Transform _flash1;
    private Transform _flash2;
    private Transform _noise;
    private Material _flash1Mat;
    private Material _flash2Mat;
    private Material _noiseMat;

    private void OnEnable()
    {
        _flash1 = flash.GetChild(0);
        _flash2 = flash.GetChild(1);
        _noise = flash.GetChild(2);

        _flash1Mat = _flash1.GetComponent<MeshRenderer>().material;
        _flash2Mat = _flash1.GetComponent<MeshRenderer>().material;
        _noiseMat = _flash1.GetComponent<MeshRenderer>().material;
    }

    private void Update()
    {
        var rot = new Vector3(Random.Range(-180, 360), Random.Range(-180, 360), Random.Range(-180, 360));
        flash.Rotate(rot);
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

    private void FadeOut(GameObject parent)
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
}

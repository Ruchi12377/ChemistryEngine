using System;
using System.Collections.Generic;
using System.Linq;
using UniRx;
using UniRx.Triggers;
using UnityEngine;
using Random = UnityEngine.Random;

//いろいろな計算を行う
namespace Chemistry
{
    [RequireComponent(typeof(Rigidbody))]
    public abstract class ChemistryObject : MonoBehaviour, IDisposable
    {
        [SerializeField, NonEditableInPlay] private MeshRenderer meshRenderer;
        //実体を持つかどうか
        [SerializeField, NonEditable] private ChemistryObjectType chemistryObjectChemistryObjectType;
        //パーティクルの大きさの差分
        [SerializeField, NonEditableInPlay] private ParticleMagnification particleMagnification;

        //キャッシュ用のTransform
        private Transform _transform;
        private IObservable<Collider> _triggerExit;
        private IObservable<Collision> _collisionExit;
        //現在表示されているパーティクル
        private readonly List<GameObject> _currentlyParticle = new List<GameObject>();
        //パーティクルのプレファブ
        private static ChemistryParticlePrefabs _chemistryParticle;

        private ChemistryObjectType ChemistryObjectType
        {
            get => chemistryObjectChemistryObjectType;
            set => chemistryObjectChemistryObjectType = value;
        }
        
        //ほかのChemistryObjectとあたったとき
        private void OnCollisionEnter(Collision other)
        {
            var target = other.gameObject.GetComponent<ChemistryObject>();
            if(target) ChangeState(this,target);
        }

        private void OnTriggerEnter(Collider other)
        {
            var target = other.gameObject.GetComponent<ChemistryObject>();
            if(target) ChangeState(this,target);
        }

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            var target = hit.gameObject.GetComponent<ChemistryObject>();
            if(target) ChangeState(this,target);
        }
        
        /// <summary>
        /// 炎
        /// 水
        /// 氷
        /// 風
        /// 電気
        /// 未定義
        ///
        /// Fire
        /// Water
        /// Ice
        /// Wind
        /// Electricity
        /// UnInvalidCastExceptiondefined
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        private void ChangeState(ChemistryObject a, ChemistryObject b)
        {
            if (a.GetInstanceID() < b.GetInstanceID())
            {
                //両方が処理を行ってしまわないように
                return;
            }
            try
            {
                //Elementのステートが同じなら
                //DirectCastでエラーが起きなかったら
                var sameState = ((Element) a).State == ((Element) b).State;
                //どっちもマテリアルなら
                var bothMaterial = a as Material && b as Material;
                if (sameState || bothMaterial) return;
            }
            catch  { /* ignored*/ }
            
            var em = a.ChemistryObjectType == ChemistryObjectType.Element && b.ChemistryObjectType == ChemistryObjectType.Material;
            var me = a.ChemistryObjectType == ChemistryObjectType.Material && b.ChemistryObjectType == ChemistryObjectType.Element;
            var ee = a.ChemistryObjectType == ChemistryObjectType.Element && b.ChemistryObjectType == ChemistryObjectType.Element;

            if (em)
            {
                ChangeMaterialStateFromElement((Element) a, (Material) b);
            }
            if (me)
            {
                ChangeMaterialStateFromElement((Element) b, (Material) a);
            }
            if (ee)
            {
                ChangeMutualStateFromElement((Element) a, (Element) b);
            }
            
            void ChangeMaterialStateFromElement(Element element, Material material)
            {
                //Elementが電気状態かつ、鉄Materialの時
                if (element.State == State.Electricity && material.substance == Substance.Metal)
                {
                    ChangeState(material, State.Electricity);
                    return;
                }
                var (aResult, bResult) = Match(element,material);
                ChangeState(element, aResult);
                ChangeState(material, bResult);
            }
            
            //2,Element同士は互いのステートを変える
            void ChangeMutualStateFromElement(Element aElement, Element bElement)
            {
                if (aElement == null || bElement == null)
                {
                    throw new ArgumentNullException($"{nameof(aElement)} or {nameof(bElement)}");
                }
                
                var (aResult, bResult) = Match(aElement, bElement);
                ChangeState(aElement, aResult);
                ChangeState(bElement, bResult);
            }
        }

        private void ChangeState(ChemistryObject chemistryObject, State state, bool isFirstChange = false)
        {
            //初めてじゃないのに、変化してない場合
            if (GetElement(chemistryObject).State == state && isFirstChange == false) return;
            var material = false; 
            try
            {
                material = chemistryObject is Material;
            }
            catch (Exception)
            {
                //
            }

            GetElement(chemistryObject).State = state;

            var parent = material ? ((Material)chemistryObject).element.transform : _transform;
            CreateParticle(parent, chemistryObject);
        }

        
        private static (State aState, State bState) Match(ChemistryObject a, ChemistryObject b) 
        {
            if (a.ChemistryObjectType == ChemistryObjectType.Material && b.ChemistryObjectType == ChemistryObjectType.Element)
            {
                Debug.LogError("The argument is an invalid value.");
                return (default, default);
            }
            var em = a.ChemistryObjectType == ChemistryObjectType.Element && b.ChemistryObjectType == ChemistryObjectType.Material;
            var ee = a.ChemistryObjectType == ChemistryObjectType.Element && b.ChemistryObjectType == ChemistryObjectType.Element;

            var aState = GetElement(a).State;
            var bState = GetElement(b).State;

            if (em)
            {
                // ReSharper disable once PossibleInvalidCastException
                switch (((Material)b).substance)
                {
                    case Substance.Combustible:
                        // ReSharper disable once ConvertIfStatementToSwitchStatement
                        if (aState == State.Fire && bState == State.Undefined)
                            return (State.Fire, State.Fire);
                        if (aState == State.Fire && bState == State.Water)
                            return (State.Undefined, State.Water);
                    
                        if (aState == State.Fire && bState == State.Ice)
                            return (State.Fire, State.Undefined);
                    
                        if (aState == State.Fire && bState == State.Wind)
                            return (State.Undefined, State.Wind);
                    
                        if (aState == State.Ice && bState == State.Fire)
                            return (State.Undefined, State.Fire);
                        break;
                    case Substance.NonCombustible:
                        if (aState == State.Ice && bState == State.Fire)
                            return (State.Undefined, State.Fire);
                        break;
                    case Substance.Liquid:
                        if (aState == State.Ice && bState == State.Fire)
                            return (State.Undefined, State.Fire);
                        break;
                    default:
                        Debug.LogError(new ArgumentNullException());
                        break;
                }
            }

            // ReSharper disable once InvertIf
            if (ee)
            {
                // ReSharper disable once ConvertIfStatementToSwitchStatement
                if (aState == State.Fire && bState == State.Water)
                    return (State.Undefined, State.Water);

                if (aState == State.Fire && bState == State.Ice)
                    return (State.Fire, State.Undefined);

                if (aState == State.Fire && bState == State.Wind)
                    return (State.Undefined, State.Wind);

                if (aState == State.Water && bState == State.Fire)
                    return (State.Water, State.Undefined);
            }

            return (aState, bState);
        }

        private void CreateParticle(Transform parent, ChemistryObject chemistryObject)
        {
            var state = GetElement(chemistryObject).State;
            //グリッドのようなものを作ってそれにそってパーティクルを作成するかも
            //Lengthだけなのは0以上ならあたってる判定になるから
            //var length = Physics.OverlapBoxNonAlloc(_transform.position, _transform.localScale / 2, null,_transform.rotation);

            //  ステートを変化させた時のパーティクル関係
            DestroyParticle(chemistryObject._currentlyParticle);

            chemistryObject._currentlyParticle.Clear();
            var scale = 1f;
            var minmax = Vector2.zero;

            var isMini = GetComponent<IPlant>() != null;
            GameObject go = null; 
            try
            {
                go = Instantiate(GetParticle(isMini), _transform.position, _transform.rotation);
            }
            catch (Exception)
            {
                //ignored
            }
            chemistryObject._currentlyParticle.Add(go);

            switch (state)
            {
                case State.Undefined:
                    break;
                case State.Fire:
                    if (isMini)
                    {
                        scale = particleMagnification.fireMini.offset;
                        minmax = particleMagnification.fireMini.particleMinMax;
                    }
                    else
                    {
                        scale = particleMagnification.fire.offset;
                        minmax = particleMagnification.fire.particleMinMax;
                    }

                    break;
                case State.Water:
                    scale = particleMagnification.water.offset;
                    minmax = particleMagnification.water.particleMinMax;
                    break;
                case State.Ice:
                    scale = particleMagnification.ice.offset;
                    minmax = particleMagnification.ice.particleMinMax;
                    break;
                case State.Wind:
                    scale = particleMagnification.wind.offset;
                    minmax = particleMagnification.wind.particleMinMax;
                    break;
                case State.Electricity:
                    scale = particleMagnification.electricity.offset;
                    minmax = particleMagnification.electricity.particleMinMax;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(state), state, null);
            }

            // ReSharper disable once Unity.NoNullPropagation
            var particle = go?.GetComponent<ParticleSystem>();
            if (particle != null)
            {
                ParticleSystem.MainModule main;
                main = particle.main;
                var curve = main.startLifetime;
                switch (curve.mode)
                {
                    case ParticleSystemCurveMode.Constant:
                        curve = curve.constant;
                        break;
                    case ParticleSystemCurveMode.TwoConstants:
                        curve.constantMin = Mathf.Clamp(minmax.x, 0.1f, 5);
                        curve.constantMax = Mathf.Clamp(minmax.y, 0.1f, 5);
                        break;
                    case ParticleSystemCurveMode.Curve:
                        curve = curve.Evaluate(Random.value);
                        break;
                    case ParticleSystemCurveMode.TwoCurves:
                    {
                        var t = Random.value;
                        curve = Random.Range(curve.curveMin.Evaluate(t),
                            curve.curveMax.Evaluate(t) * curve.curveMultiplier);
                    }
                        break;
                    default:
                        return;
                }

                main.startLifetime = curve;

                go.transform.SetParent(parent);
                go.transform.localScale = Vector3.one * scale;
            }

            GameObject GetParticle(bool mini)
            {
                switch (state)
                {
                    case State.Undefined:
                        break;
                    case State.Fire:
                        return mini ? _chemistryParticle.FireMini : _chemistryParticle.Fire;
                    case State.Water:
                        return _chemistryParticle.Water;
                    case State.Ice:
                        return _chemistryParticle.Ice;
                    case State.Wind:            
                        return _chemistryParticle.Wind;
                    case State.Electricity:
                        return _chemistryParticle.Electricity;
                    default:
                        return null;
                }

                return null;
            }
        }

        private static void DestroyParticle(List<GameObject> particles)
        {
            particles.ForEach(x =>
            {
                // ReSharper disable once InvertIf
                if (x != null)
                {
                    var ps = x.GetComponent<ParticleSystem>();
                    ps.Stop();
                    ps.UpdateAsObservable()
                        .Select(y => ps)
                        .Where(z => z.particleCount == 0)
                        .Subscribe(w =>
                        {
                            // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
                            // Materialの子供にElementがあってその下にElementがある場合、
                            // Particleの親ごと消す
                            Destroy(w.gameObject);
                        });
                }
            });
        }
        
        //ステートと材質が一致してるか
        private void CheckObjectType()
        {
            var material = this is Material;
            var element = this is Element;
            
            if (material) ChemistryObjectType = ChemistryObjectType.Material;
            if (element) ChemistryObjectType = ChemistryObjectType.Element;

            if (material)
            {
                var mat = this as Material;
                if (mat == null)
                {
                    Debug.LogError(new ArgumentNullException());
                    return;
                }
                
                var state = mat.element.State;
                //マテリアルかつ水or風はありえない
                if (state == State.Wind) Undefined();
                if (mat.substance != Substance.Liquid && state == State.Water) Undefined();
                
                //マテリアルかつ素材が鉄かつ炎or氷はありえない
                if(mat.substance == Substance.Metal && (state == State.Fire || state == State.Ice)) Undefined();
                
                //マテリアルかつ素材が燃えないかつ炎はおかしい
                if(mat.substance == Substance.NonCombustible && state == State.Fire) Undefined();
                
                //if(mat.substance == Substance.Liquid && state == State.Fire) Undefined();

                void Undefined()
                {
                    state = State.Undefined;
                }                    
            }
        }

        
        #region Call by UnityEngine

        private void OnEnable()
        {
            _transform = transform;
            // ReSharper disable once InvertIf
            if (this is Material material &&  material.element == null)
            {
                var go = new GameObject() {name = "Element"};
                //Element
                var element = go.AddComponent<Element>();
                element.State = State.Undefined;
                material.element = element;
                //Rigidbody
                var rb = go.GetComponent<Rigidbody>();
                rb.useGravity = false;
                rb.constraints = RigidbodyConstraints.FreezeAll;
                //transform
                var trans = go.transform;
                trans.parent = _transform;
                trans.localPosition = Vector3.zero;
            }
            
            CheckObjectType();
            ChangeState(this, GetElement(this).State, true);
            //デフォルトがElectricityだったら
            if (GetElement(this).State == State.Electricity)
            {
                _triggerExit = this.OnTriggerExitAsObservable();
                _collisionExit = this.OnCollisionExitAsObservable();

                _triggerExit
                    .Select(x => x.gameObject.GetComponent<ChemistryObject>())
                    .Where(y => y)
                    .Where(z => GetElement(z).State == State.Electricity)
                    .Subscribe(_ => GetElement(_).State = GetElement(_).beforeState);

                _collisionExit
                    .Select(x => x.gameObject.GetComponent<ChemistryObject>())
                    .Where(y => y)
                    .Where(z => GetElement(z).State == State.Electricity)
                    .Subscribe(_ => GetElement(_).State = GetElement(_).beforeState);
            }
        }

        private static Element GetElement(ChemistryObject chemistryObject)
        {
            switch (chemistryObject)
            {
                case Element element:
                    return element;
                case Material material:
                    return material.element;
                default:
                    Debug.LogError(new ArgumentNullException());
                    break;
            }
            return null;
        }

            private void OnDisable() => Dispose();
            public void Dispose()
            {
                _currentlyParticle.Clear();
            }
            
            
            private void Reset() => CheckObjectType();
            

            [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
            private static void Init()
            {
                try
                {
                    _chemistryParticle.Fire = Load("Fire");
                    _chemistryParticle.FireMini = Load("Fire_Mini");
                    _chemistryParticle.Water = Load("Water");
                    _chemistryParticle.Ice = Load("Ice");
                    _chemistryParticle.Wind = Load("Wind");
                    _chemistryParticle.Electricity = Load("Electricity");

                    GameObject Load(string path)
                    {
                        var particle = Resources.Load<GameObject>("Chemistry_" + path);
                        if (particle == null) Debug.LogError(new ArgumentNullException() + " : " + path);
                        return particle;
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                }
            }
        #endregion
    }
}
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

//いろいろな計算を行う
namespace Chemistry
{
    [RequireComponent(typeof(Rigidbody))]
    public abstract class ChemistryObject : MonoBehaviour, IDisposable
    {
        //実体を持つかどうか
        [SerializeField, NonEditable] private ChemistryObjectType chemistryObjectChemistryObjectType;
        //パーティクルの大きさの差分
        [SerializeField, NonEditableInPlay] private ParticleMagnification particleMagnification;
        [SerializeField]
        private bool verifiedElectricity;
        
        public int test;
        //キャッシュ用のTransform
        private Transform _transform;
        //現在表示されているパーティクル
        private readonly List<GameObject> _currentlyParticle = new List<GameObject>();
        //パーティクルのプレファブ
        private static ChemistryParticlePrefabs _chemistryParticle;
        private static Vector2 _defaultFireLifeTime;
        private bool _isParentChemistryObject;
        private Element _element;

        private ChemistryObjectType ChemistryObjectType
        {
            get => chemistryObjectChemistryObjectType;
            set => chemistryObjectChemistryObjectType = value;
        }

        private void OnEnter(GameObject hit)
        {
            var target = hit.GetComponent<ChemistryObject>();

            if (target)
            {
                var thisElement = _element;
                Material material = null;
                try
                {
                    material = (Material) this;
                }
                catch { /**/}

                //自分が電気ではないかつ
                var nonElectricity = thisElement.State != State.Electricity;
                //materialがnullではないかつ自分が水または属性が鉄または液体（導電）
                var conductor = material == null || thisElement.State == State.Water || material.substance == Substance.Metal || material.substance == Substance.Liquid;
                //相手が電気なら
                var targetIsElectricity = target._element.State == State.Electricity || target._element.SubState == State.Electricity;
                if (nonElectricity && conductor && targetIsElectricity)
                {
                    ChangeState(this, State.Electricity);
                }
                else
                {
                    ChangeState(this, target);
                }
            }
        }
        
        private void OnStay(GameObject hit)
        {
            var target = hit.GetComponent<ChemistryObject>();
            if (target)
            {
                /*var priorityThisState =
                    _element.State == State.Electricity && target._element.SubState != State.Electricity ||
                    _element.SubState == State.Electricity && target._element.State != State.Electricity;
                var priorityTargetState =
                    _element.State != State.Electricity && target._element.SubState == State.Electricity ||
                    _element.SubState != State.Electricity && target._element.State == State.Electricity;
                //target._element.SubState = State.Undefined;
                var compareInstanceID = gameObject.GetInstanceID() > target.gameObject.GetInstanceID();
                
                if(compareInstanceID)
                {
                    Change(target, this);
                }else if (priorityThisState)
                {
                    Change(this, target);
                }
                else if (priorityTargetState)
                {
                    Change(target, this);
                }
                else
                {
                    Change(this, target);
                }
                
                void Change(ChemistryObject a, ChemistryObject b)
                {
                    Debug.DrawLine(a._transform.position, b._transform.position, Color.yellow, 0, true);
                    
                    //Debug.Log($"{a.name}は電気。{b.name}変える");
                    //b._element.SubState = State.Electricity;
                }*/
                target.VerifiedElectricity(this);
            }
        }

        private void VerifiedElectricity(ChemistryObject co)
        {
            var e = co._element;
            //ステートが電気または認証元が有効かつ、サブステートが電気なら
            if (e.State == State.Electricity || (e.verifiedElectricity && e.SubState == State.Electricity))
            {
                Debug.Log(co.name + " is verified");
                verifiedElectricity = true;
            }
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
        /// UnInvalidCastExceptionDefined
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
                var (aResult, bResult) = MatchObjectType(element,material);
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
                
                var (aResult, bResult) = MatchObjectType(aElement, bElement);
                ChangeState(aElement, aResult);
                ChangeState(bElement, bResult);
            }
        }

        private void ChangeState(ChemistryObject chemistryObject, State state, bool isFirstChange = false)
        {
            //初めてじゃないのに、変化してない場合
            if (chemistryObject._element.State == state && isFirstChange == false) return;
            try
            {
            }
            catch (Exception)
            {
                //
            }

            //最初か、電気意外なら
            if (isFirstChange || state != State.Electricity)
            {
                chemistryObject._element.State = state;
            }
            else
            {
                //はじめ以外で電気なら
                chemistryObject._element.SubState = state;
            }
            
            var parent = _isParentChemistryObject ? _transform : _element._transform;
            CreateParticle(parent, chemistryObject, isFirstChange);
        }

        //2つのステート、素材から適切なステートを返す
        private static (State aState, State bState) MatchObjectType(ChemistryObject a, ChemistryObject b) 
        {
            if (a.ChemistryObjectType == ChemistryObjectType.Material && b.ChemistryObjectType == ChemistryObjectType.Element)
            {
                Debug.LogError("The argument is an invalid value.");
                return (default, default);
            }
            var em = a.ChemistryObjectType == ChemistryObjectType.Element && b.ChemistryObjectType == ChemistryObjectType.Material;
            var ee = a.ChemistryObjectType == ChemistryObjectType.Element && b.ChemistryObjectType == ChemistryObjectType.Element;

            var aState = a._element.State;
            var bState = b._element.State;

            if (em)
            {
                // ReSharper disable once PossibleInvalidCastException
                switch (((Material)b).substance)
                {
                    case Substance.Metal:
                        if (aState == State.Electricity || bState == State.Electricity)
                        {
                            return (State.Electricity, State.Electricity);
                        }
                        break;
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

        //パーティクル作成
        private void CreateParticle(Transform parent, ChemistryObject chemistryObject, bool isFirstChange)
        {
            if (chemistryObject._isParentChemistryObject == false) return;
            if (this is Material)
            {
                parent = parent.GetChild(0);
            }
            var element = chemistryObject._element;
            var state = element.State;
            //グリッドのようなものを作ってそれにそってパーティクルを作成するかも
            //Lengthだけなのは0以上ならあたってる判定になるから
            //var length = Physics.OverlapBoxNonAlloc(_transform.position, _transform.localScale / 2, null,_transform.rotation);

            //  ステートを変化させた時のパーティクル関係
            
            //初めてじゃないかつ、サブステートが電気じゃないなら、パーティクルを消す
            if(isFirstChange == false && element.SubState != State.Electricity) DestroyParticle(chemistryObject._currentlyParticle);

            chemistryObject._currentlyParticle.Clear();
            var scale = 1f;

            GameObject go = null;
            try
            {
                go = Instantiate(GetParticle(), _transform.position, _transform.rotation);
            }
            catch (Exception)
            {
                //ignored
            }
            chemistryObject._currentlyParticle.Add(go);
            //Set offset
            switch (state)
            {
                case State.Undefined:
                    break;
                case State.Fire:
                    scale = particleMagnification.fire.offset;
                    break;
                case State.Water:
                    scale = particleMagnification.water.offset;
                    break;
                case State.Ice:
                    scale = particleMagnification.ice.offset;
                    break;
                case State.Wind:
                    scale = particleMagnification.wind.offset;
                    break;
                case State.Electricity:
                    scale = particleMagnification.electricity.offset;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(state), state, null);
            }

            // ReSharper disable once Unity.NoNullPropagation
            if (go != null)
            {
                var particle = go.GetComponent<ParticleSystem>();
                if (particle != null)
                {
                    //lifetimeをscale似合わせる
                    var lifeTime = _defaultFireLifeTime * scale;
                    ParticleSystem.MainModule main;
                    main = particle.main;
                    var curve = main.startLifetime;
                    switch (curve.mode)
                    {
                        case ParticleSystemCurveMode.Constant:
                            curve = curve.constant;
                            break;
                        case ParticleSystemCurveMode.TwoConstants:
                            curve.constantMin = Mathf.Clamp(lifeTime.x, 0.1f, 5);
                            curve.constantMax = Mathf.Clamp(lifeTime.y, 0.1f, 5);
                            break;
                        case ParticleSystemCurveMode.Curve:
                            curve = curve.Evaluate(Random.value);
                            break;
                        case ParticleSystemCurveMode.TwoCurves:
                            var t = Random.value;
                            curve = Random.Range(curve.curveMin.Evaluate(t),
                                curve.curveMax.Evaluate(t) * curve.curveMultiplier);
                            break;
                        default:
                            return;
                    }

                    main.startLifetime = curve;
                }
                go.transform.SetParent(parent);
                go.transform.localScale = Vector3.one * scale;
            }

            GameObject GetParticle()
            {
                switch (state)
                {
                    case State.Undefined:
                        break;
                    case State.Fire:
                        return _chemistryParticle.fire;
                    case State.Water:
                        return _chemistryParticle.water;
                    case State.Ice:
                        return _chemistryParticle.ice;
                    case State.Wind:            
                        return _chemistryParticle.wind;
                    case State.Electricity:
                        return _chemistryParticle.electricity;
                    default:
                        return null;
                }

                return null;
            }
        }

        private void DestroyParticle(List<GameObject> particles)
        {
            particles.ForEach(x =>
            {
                // ReSharper disable once InvertIf
                if (x != null)
                {
                    var ps = x.GetComponent<ParticleSystem>();
                    var meshP = x.GetComponent<MeshParticle>();
                    if (ps != null)
                    {
                        ps.Stop();
                        StartCoroutine(WaitAllParticleDestroy(ps));   
                    }
                    else if (meshP != null)
                    {
                        meshP.Destroy(meshP.gameObject);
                    }
                }
            });
        }

        private IEnumerator WaitAllParticleDestroy(ParticleSystem ps)
        {
            while (ps.particleCount == 0)
            {
                yield return null;
            }

            Destroy(ps.gameObject);
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

        private void Awake()
        {
            this.OnColliderOrTriggerStayAsObservable().Subscribe(OnStay);
            
            _transform = transform;
            // ReSharper disable once InvertIf
            if (this is Material material &&  material.element == null)
            {
                var go = new GameObject {name = "Element"};
                //Element
                var element = go.AddComponent<Element>();
                element.State = material.State;
                element.test = test;
                material.element = element;
                //Rigidbody
                var rb = go.GetComponent<Rigidbody>();
                rb.useGravity = false;
                rb.constraints = RigidbodyConstraints.FreezeAll;
                //transform
                var trans = go.transform;
                trans.parent = _transform;
                trans.localPosition = Vector3.zero;
                //elementを代入
                _element = element;
            }
            else
            {
                //elementを代入
                _element = (Element)this;
            }
        }

        private void Start()
        {
            var col = GetComponent<Collider>();
            _isParentChemistryObject = col;
            CheckObjectType();
            ChangeState(this, _element.State, true);
        }

        private void FixedUpdate()
        {
            verifiedElectricity = false;
            VerifiedElectricity(this);
        }

        private void OnDisable() => Dispose();
        public void Dispose()
        {
            _currentlyParticle.Clear();
        }
        
        private void Reset() => CheckObjectType();
        
        //ほかのChemistryObjectとあたったとき
        private void OnCollisionEnter(Collision other)
        {
            OnEnter(other.gameObject);
        }

        private void OnTriggerEnter(Collider other)
        {
            OnEnter(other.gameObject);
        }

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            OnEnter(hit.gameObject);
        }

        private void OnGUI()
        {
            if (_isParentChemistryObject)
            {
                var style = new GUIStyle {fontSize = 30};
                var dash = "";
                if (/*_element.State == State.Electricity|| _element.SubState == State.Electricity*/verifiedElectricity)
                {
                    dash = "*";
                }
                //GUI.Label(new Rect(20, 30 * test, 1000, 100), $"{dash} {name} : State : {_element.State} SubState : {_element.SubState}", style);
                GUI.Label(new Rect(20, 30 * test, 1000, 100), $"{dash} {name} : {verifiedElectricity}", style);
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Init()
        {
            try
            {
                _chemistryParticle.fire = Load("Fire");
                var lifetime = _chemistryParticle.fire.GetComponent<ParticleSystem>().main.startLifetime;
                _defaultFireLifeTime = new Vector2(lifetime.constantMin, lifetime.constantMax);
                _chemistryParticle.water = Load("Water");
                _chemistryParticle.ice = Load("Ice");
                _chemistryParticle.wind = Load("Wind");
                _chemistryParticle.electricity = Load("Electricity");

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
using System;
using System.Collections;
using Chemistry;
using UnityEngine;
using Random = UnityEngine.Random;

/*
    Undefined,
    Fire,
    Water,
    Ice,
    Wind
 */

/// <summary>
/// ケミストリ演算を行うベースのクラス
/// 実態を持たないものをエレメント
/// 実態を持つものをマテリアルと呼ぶ。
/// エレメントはマテリアルのステートを変化させる
/// エレメント同士はお互いのステートを変化させる
/// マテリアル同士はお互いのステートに干渉しない
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public abstract class ChemistryObject : MonoBehaviour
{
    //横、縦の順番
    private readonly (State, State)[,] _table =
    {
        {
            (State.Undefined, State.Undefined), 
            (State.Fire, State.Fire),
            (State.Undefined, State.Water),
            (State.Ice, State.Ice),
            (State.Undefined, State.Wind),
        },
        {
            (State.Fire, State.Fire), 
            (State.Fire, State.Fire), 
            (State.Undefined, State.Water),
            (State.Fire, State.Undefined),
            (State.Undefined, State.Wind),
        },
        {
            (State.Water, State.Undefined),
            (State.Water, State.Undefined), 
            (State.Water, State.Water),
            (State.Ice, State.Ice), 
            (State.Water, State.Wind),
        },
        {
            (State.Ice, State.Ice), 
            (State.Undefined, State.Fire),
            (State.Ice, State.Ice), 
            (State.Ice, State.Ice),
            (State.Ice, State.Wind),
        },
        {
            (State.Wind, State.Undefined),
            (State.Wind, State.Undefined), 
            (State.Wind, State.Water),
            (State.Wind, State.Ice),
            (State.Wind, State.Wind),
        },
    };

    //自分自身からは変更できないが、継承してるクラスからは変更できる
    private bool IsMaterial => this is Material;

    //燃えるか
    [SerializeField, Header("燃えるか")] protected bool combustible;

    //液体か
    [SerializeField, Header("液体か")] protected bool liquid;

    [SerializeField] private State defaultState;
    [SerializeField, NonEditable] private State currentState;
    [SerializeField, NonEditableInPlay] private ParticleMagnification particleMagnification;

    private GameObject _currentParticle;

    private static ChemistryParticlePrefabs _chemistryParticle;
    private static Vector2 _defaultFireLifeTime;

    public State State { get; private set; } = State.Undefined;

    //キャッシュ用のTransform
    private Transform _transform;

    //ケミストリオブジェクトに必要なデータを読み込むための関数
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        try
        {
            _chemistryParticle.fire = Load("Fire");
            var lifetime = _chemistryParticle.fire.GetComponent<ParticleSystem>().main.startLifetime;
            _defaultFireLifeTime = new Vector2(lifetime.constantMin, lifetime.constantMax);
            _chemistryParticle.water = Load("Water");
            _chemistryParticle.ice = Load("Ice");
            _chemistryParticle.wind = Load("Wind");

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

    private void Start()
    {
        _transform = transform;
        if (defaultState != State.Undefined)
        {
            SetState(defaultState);
        }
    }

    private void Update()
    {
        currentState = State;
    }

    #region 当たり判定

    private void OnCollisionStay(Collision other)
    {
        Stay(other.gameObject);
    }

    private void OnTriggerStay(Collider other)
    {
        Stay(other.gameObject);
    }

    private void Stay(GameObject obj)
    {
        var chemistryObject = obj.GetComponent<ChemistryObject>();
        if (chemistryObject == false)
        {
            return;
        }

        //自分の方がInstanceIDが大きいなら
        if (gameObject.GetInstanceID() > chemistryObject.gameObject.GetInstanceID())
        {
            Action(this, chemistryObject);
        }
    }

    #endregion

    //c1,c2 → chemistry1, chemistry2の略
    private void Action(ChemistryObject c1, ChemistryObject c2)
    {
        var c1M = c1.IsMaterial;
        var c2M = c2.IsMaterial;

        //エレメント同士
        var ee = c1M == false && c2M == false;
        //エレメントとマテリアル(順不同)
        var em = c1M == false && c2M;
        var me = c1M && c2M == false;
        //マテリアル同士は、お互いのステートに影響を及ぼさないかつ、
        //ee,em以外の条件であるのでelseで良い
        //マテリアル同士
        //var mm = c1m && c2m;

        if (ee)
        {
            var element1 = c1 as Element;
            var element2 = c2 as Element;
            ElementElement(element1, element2);
        }
        else if (em || me)
        {
            if (em)
            {
                var element = c1 as Element;
                var material = c2 as Material;
                ElementMaterial(element, material);
            }
            else
            {
                var material = c1 as Material;
                var element = c2 as Element;
                ElementMaterial(element, material);
            }
        }

        void ElementElement(Element element1, Element element2)
        {
            var s1 = element1.State;
            var s2 = element2.State;
            //同じステートは処理する意味ない
            if (s1 == s2)
            {
                return;
            }

            //index
            var i1 = (int)s1;
            var i2 = (int)s2;

            //result
            var (r1, r2) = _table[i1, i2];
            
            Debug.Log($"{i1} {i2}");
            Debug.Log($"{r1} {r2}");

            //適応
            Debug.Log($"{element1.gameObject.name} {s1}→{r1}");
            Debug.Log($"{element2.gameObject.name} {s2}→{r2}");
            element1.SetState(r1);
            element2.SetState(r2);
        }

        void ElementMaterial(Element element, Material material)
        {
            var s1 = element.State;
            var s2 = material.State;
            //同じステートは処理する意味ない
            if (s1 == s2)
            {
                return;
            }

            //index
            var i1 = (int)s1;
            var i2 = (int)s2;

            //result
            var (_, r2) = _table[i1, i2];

            //適応
            material.SetState(r2);
        }
    }

    #region ステート関連

    private void SetState(State target)
    {
        if (State == target)
        {
            return;
        }

        var state = CheckStateFromAttribute(target);
        if (state)
        {
            State = target;
            Debug.Log($"{gameObject.name} {State}");
            CreateParticle();
        }
    }

    //ステートが適切かどうかオブジェクトの属性をもとに判断する
    private bool CheckStateFromAttribute(State target)
    {
        //燃えないのに燃やそうとしてる
        var a = combustible == false && target == State.Fire;
        //流体なのに燃やそうとしている
        var b = liquid && target == State.Fire;

        //特定の、ありえない条件に当てはまるかどうか
        return (a || b) == false;
    }

    #endregion

    #region パーティクル関連

    private void CreateParticle()
    {
        var scale = 1f;
        switch (State)
        {
            case State.Undefined:
                break;
            case State.Fire:
                scale = particleMagnification.fire;
                break;
            case State.Water:
                scale = particleMagnification.water;
                break;
            case State.Ice:
                scale = particleMagnification.ice;
                break;
            case State.Wind:
                scale = particleMagnification.wind;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(State), State, null);
        }

        //パーティクルが存在するなら
        if (_currentParticle)
        {
            //消して、上書きする
            DestroyParticle(_currentParticle);
        }

        if (State != State.Undefined)
        {
            var prefab = GetParticle(State);
            if (prefab != null)
            {
                //パーティクルを生成して、自分自身を親として設定する
                _currentParticle = Instantiate(prefab, _transform);
                var particle = _currentParticle.GetComponent<ParticleSystem>();
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
                
                _currentParticle.transform.localScale = Vector3.one * scale;
            }
        }

        void DestroyParticle(GameObject particles)
        {
            // ReSharper disable once InvertIf
            if (particles != null)
            {
                var ps = particles.GetComponent<ParticleSystem>();
                var meshP = particles.GetComponent<MeshParticle>();
                if (ps != null)
                {
                    ps.Stop();
                    StartCoroutine(WaitAllParticleDestroy(ps));
                }
                else if (meshP != null)
                {
                    meshP.Destroy(meshP.gameObject);
                }

                IEnumerator WaitAllParticleDestroy(ParticleSystem a)
                {
                    while (a.particleCount == 0)
                    {
                        yield return null;
                    }

                    Destroy(a.gameObject);
                }
            }
        }
    }

    private static GameObject GetParticle(State state)
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
            default:
                return null;
        }

        return null;
    }

    #endregion
}
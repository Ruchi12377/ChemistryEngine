using System;
using System.Collections;
using ChemistryEngine.Script.Core;
using ChemistryEngine.Script.Core.Data.Enum;
using ChemistryEngine.Script.Core.Data.Struct;
using UnityEngine;
using Random = UnityEngine.Random;

namespace ChemistryEngine.Script.NewCore
{
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
		//ログの出力を有効にするか
		private const bool EnableLog = true;

		//横、縦の順番
		private static readonly (State, State)[,] Table =
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
				(State.Fire, State.Undefined), //
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
				(State.Undefined, State.Fire), //
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

		[SerializeField, Header("燃えるか")] protected internal bool combustible;
		[SerializeField, Header("凍るか")] protected internal bool freezable;
		[SerializeField, Header("液体か")] protected internal bool liquid;

		[SerializeField, Min(0), Header("発火する時間")]
		protected float ignitionTime;

		[SerializeField, Min(0), Header("炎が風によって消える時間")]
		protected float burnOutByWindTime;

		[SerializeField, Min(0), Header("氷が溶ける時間")]
		protected float meltingTime;

		[SerializeField] private State defaultState;
		[SerializeField, NonEditable] private State currentState;
		[SerializeField, NonEditableInPlay] private ParticleMagnification particleMagnification;

		[SerializeField, Header("リセット時間は0以下にするとリセットされない")]
		private float resetIgnitionTime = 0.1f;

		[SerializeField] private ResetType ignitionResetType = ResetType.GraduallyReset;

		[SerializeField] private float resetBurnOutByWindTime = 0.1f;
		[SerializeField] private ResetType burnOutByWindResetType = ResetType.ZeroReset;

		[SerializeField] private float resetMeltingTime = 0.1f;
		[SerializeField] private ResetType meltingResetType = ResetType.None;

		private float _currentIgnitionTime;
		private float _currentBurnOutByWindTime;
		private float _currentMeltingTime;
		private float _latestChangeIgnitionTime;
		private float _latestChangeBurnOutByWindTime;
		private float _latestChangeMeltingTime;

		private GameObject _currentParticle;
		private static ChemistryParticlePrefabs _chemistryParticle;
		private static Vector2 _defaultFireLifeTime;

		public State State
		{
			get => currentState;
			private set => currentState = value;
		}

		//キャッシュ用のTransform
		private Transform _transform;

		//ケミストリオブジェクトに必要なデータを読み込むための関数
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
		private static void Initialize()
		{
			try
			{
				_chemistryParticle.Fire = Load("Fire");
				var lifetime = _chemistryParticle.Fire.GetComponent<ParticleSystem>().main.startLifetime;
				_defaultFireLifeTime = new Vector2(lifetime.constantMin, lifetime.constantMax);
				_chemistryParticle.Water = Load("Water");
				_chemistryParticle.Ice = Load("Ice");
				_chemistryParticle.Wind = Load("Wind");

				GameObject Load(string path)
				{
					var particle = Resources.Load<GameObject>("Chemistry_" + path);
					if (particle == null) LogError(new ArgumentNullException() + " : " + path);
					return particle;
				}
			}
			catch (Exception e)
			{
				LogError(e);
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

		#region 当たり判定

		//triggerだけのほうが都合がよいのでは？
		/*
		private void OnCollisionStay(Collision other)
		{
			Stay(other.gameObject);
		}
		*/

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

			//両方で同じ処理をするとおかしい動作をするので片方だけ
			//自分の方がInstanceIDが大きいなら
			if (gameObject.GetInstanceID() > chemistryObject.gameObject.GetInstanceID())
			{
				Action(this, chemistryObject);
			}
		}

		#endregion

		//OnTriggerXXX/OnCollisionXXXのあとに呼ばれる
		//https://docs.unity3d.com/ja/2021.3/Manual/ExecutionOrder.html
		private void Update()
		{
			var time = Time.time;

			//それぞれ0秒より大きいかつ、リセットが有効かつ、リセットを希望する時間を過ぎていたらリセット
			if (_currentIgnitionTime > 0 && time - _latestChangeIgnitionTime > resetIgnitionTime &&
			    resetIgnitionTime > 0)
			{
				if (ignitionResetType == ResetType.None)
				{
				}
				else if (ignitionResetType == ResetType.ZeroReset)
				{
					_currentIgnitionTime = 0;
				}
				else if (ignitionResetType == ResetType.GraduallyReset)
				{
					_currentIgnitionTime -= Time.deltaTime;
				}
			}

			if (_currentBurnOutByWindTime > 0 && time - _latestChangeBurnOutByWindTime > resetBurnOutByWindTime &&
			    resetBurnOutByWindTime > 0)
			{
				if (burnOutByWindResetType == ResetType.None)
				{
				}
				else if (burnOutByWindResetType == ResetType.ZeroReset)
				{
					_currentBurnOutByWindTime = 0;
				}
				else if (burnOutByWindResetType == ResetType.GraduallyReset)
				{
					_currentBurnOutByWindTime -= Time.deltaTime;
				}
			}

			if (_currentMeltingTime > 0 && time - _latestChangeMeltingTime > resetMeltingTime && resetMeltingTime > 0)
			{
				if (meltingResetType == ResetType.None)
				{
				}
				else if (meltingResetType == ResetType.ZeroReset)
				{
					_currentMeltingTime = 0;
				}
				else if (meltingResetType == ResetType.GraduallyReset)
				{
					_currentMeltingTime -= Time.deltaTime;
				}
			}
		}

		//c1,c2 → chemistry1, chemistry2の略
		private static void Action(ChemistryObject c1, ChemistryObject c2)
		{
			var c1M = c1 is Material;
			var c2M = c2 is Material;

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

			static void ElementElement(Element element1, Element element2)
			{
				var s1 = element1.State;
				var s2 = element2.State;
				//同じステートは処理する意味ない
				if (s1 == s2)
				{
					return;
				}

				//index
				var i1 = (int) s1;
				var i2 = (int) s2;

				//result
				var (r1, r2) = Table[i1, i2];

				//適応
				Ignition(element1, element2);
				BurnOut(element1, element2);
				Melt(element1, element2);

				//result can change
				var (rcc1, rcc2) = CanChangeState(element1, element2, r1, r2);
				Debug.Log($"{s1}→{r1}>{rcc1} {s2}→{r2}>{rcc2}");
				if (rcc1)
				{
					Log($"{element1.gameObject.name} {s1}→{r1}");
					Log($"{element2.gameObject.name} {s2}→{r2}");
					element1.SetState(r1);
				}

				if (rcc2)
				{
					Log($"{element1.gameObject.name} {s1}→{r1}");
					Log($"{element2.gameObject.name} {s2}→{r2}");
					element2.SetState(r2);
				}
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
				var i1 = (int) s1;
				var i2 = (int) s2;

				//result
				var (r1, r2) = Table[i1, i2];

				//適応
				Ignition(element, material);
				BurnOut(element, material);
				Melt(element, material);

				//result can change
				var (rcc1, _) = CanChangeState(element, material, r1, r2);
				if (rcc1)
				{
					element.SetState(r1);
				}

				//適応
				material.SetState(r2);
			}

			static (bool rcc1, bool rcc2) CanChangeState(ChemistryObject c1, ChemistryObject c2, State r1, State r2)
			{
				return (CanChangeStateInternal(c1, c2, r1, r2), CanChangeStateInternal(c2, c1, r2, r1));
			}

			static bool CanChangeStateInternal(ChemistryObject c1, ChemistryObject c2, State r1, State r2)
			{
				var s1 = c1.State;
				var s2 = c2.State;
				//炎になろうとして、発火時間を超えている
				if (s1 == State.Undefined && r1 == State.Fire && s2 == State.Fire && r2 == State.Fire)
				{
					return c1._currentIgnitionTime > c1.ignitionTime;
				}

				//炎を風が消そうとしてる
				if (s1 == State.Fire && r1 == State.Undefined && s2 == State.Wind && r2 == State.Wind)
				{
					return c1._currentBurnOutByWindTime > c1.burnOutByWindTime;
				}

				//炎を氷を溶かそうとしている
				if (s1 == State.Ice && r1 == State.Undefined && s2 == State.Fire && r2 == State.Fire)
				{
					return c1._currentMeltingTime > c1.meltingTime;
				}

				return true;
			}
		}

		#region 特定のステート同士の動作

		private static void Ignition(ChemistryObject s1, ChemistryObject s2)
		{
			var b1 = s1.State == State.Fire && s2.State == State.Undefined;
			var b2 = s1.State == State.Undefined && s2.State == State.Fire;
			var target = b1 ? s2 : s1;

			//どっちも燃えようとしていない
			if (b1 == false && b2 == false) return;

			if (s1.combustible == false) return;

			target._currentIgnitionTime += Time.deltaTime;
			target._latestChangeIgnitionTime = Time.time;
		}

		private static void BurnOut(ChemistryObject s1, ChemistryObject s2)
		{
			var b1 = s1.State == State.Fire && s2.State == State.Wind;
			var b2 = s1.State == State.Wind && s2.State == State.Fire;

			var target = b1 ? s1 : s2;
			//どっちも炎と風のペアではない
			if (b1 == false && b2 == false) return;

			target._currentBurnOutByWindTime += Time.deltaTime;
			target._latestChangeBurnOutByWindTime = Time.time;
		}

		private static void Melt(ChemistryObject s1, ChemistryObject s2)
		{
			var b1 = s1.State == State.Fire && s2.State == State.Ice;
			var b2 = s1.State == State.Ice && s2.State == State.Fire;

			var target = b1 ? s2 : s1;

			if (s1.freezable == false) return;

			//どっちも炎と氷のペアではない
			if (b1 == false && b2 == false) return;

			target._currentMeltingTime += Time.deltaTime;
			target._latestChangeMeltingTime = Time.time;
		}

		#endregion

		#region ステート関連

		private void SetState(State target)
		{
			if (State == target)
			{
				return;
			}

			//点火しようとしている
			if (currentState == State.Undefined && target == State.Fire)
			{
				_currentIgnitionTime = 0;
			}

			//炎を消そうとしている
			if (currentState == State.Fire && target == State.Undefined)
			{
				_currentBurnOutByWindTime = 0;
			}

			//炎を消そうとしている
			if (currentState == State.Ice && target == State.Undefined)
			{
				_currentMeltingTime = 0;
			}

			var state = CheckStateFromAttribute(target);
			if (state)
			{
				State = target;
				Log($"{gameObject.name} {State}");
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
			//凍らないのに凍らそうとしている
			var c = freezable == false && target == State.Ice;

			//特定の、ありえない条件に当てはまるかどうか
			//全部 && にすれば == falseいらないけど、
			//こっちのほうが可読性が高いので
			return (a || b || c) == false;
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
					while (a.particleCount > 0)
					{
						yield return null;
					}

					Destroy(a.gameObject);
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
					return _chemistryParticle.Fire;
				case State.Water:
					return _chemistryParticle.Water;
				case State.Ice:
					return _chemistryParticle.Ice;
				case State.Wind:
					return _chemistryParticle.Wind;
				default:
					return null;
			}

			return null;
		}

		#endregion

		#region ログのラッパー

		private static void Log(string s)
		{
			if (EnableLog == false) return;

			Debug.Log(s);
		}

		private static void LogError(string s)
		{
			if (EnableLog == false) return;

			Debug.LogError(s);
		}

		private static void LogError(Exception e)
		{
			if (EnableLog == false) return;

			Debug.LogError(e);
		}

		#endregion

		private enum ResetType
		{
			None,
			ZeroReset,
			GraduallyReset
		}
	}
}
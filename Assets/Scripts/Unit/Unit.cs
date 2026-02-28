
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
using DG.Tweening;
using RPGCharacterAnims;
using Unity.VisualScripting;
using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine.Assertions;
using RPGCharacterAnims.Actions;

namespace Units{
    public enum AllowedTargets
    {
        All,
        Base
    }
    
    public enum Size
    {
        Big,
        Medium,
        Small
    }
    
    public enum Type
    {
        Base,
        Swordsman,
        Ranged,
        Giant,
        MiniSkeleton
    }

    public abstract class Unit : MonoBehaviour, ISpawnable
    {

        [SerializeField]
        protected UnitData data;
        [SerializeField]
        protected BulletFactory bulletFactory;
        [SerializeField]
        protected NavMeshAgent navMeshAgent;
        [SerializeField]
        protected NavMeshObstacle navMeshObstacle;
        [SerializeField]
        private List<Renderer> renderers;
        [SerializeField]
        private Rigidbody body;
        [SerializeField]
        protected Animator animator;
        [SerializeField]
        protected Type type;
        [SerializeField]
        protected CommonUnitData commonUnitData;
        [SerializeField]
        protected GameObject forwardObstacle;

        public virtual ISpawnable Spawnable => this;
        /// <summary>
        /// Range, on which unit spots an enemy and starts walking to it. Must be equal or greater than attack range.
        /// </summary>
        public int Team
        {
            get => team;
            set
            {
                Assert.IsTrue(value == (int)Sides.Player || value == (int)Sides.Enemy, "Team must be either Player or Enemy.");
                team = value;
                SetTeamColor(UnitSpawner.Instance.TeamColors[team]);
                OnTeamSet?.Invoke(team);
            }
        }
        public bool IsDead => isDead;
        public bool HasTarget => attackTarget != null;
        public bool HasPathToTarget => navMeshAgent.hasPath;
        public bool IsAttacking => attackAllowed;
        public Unit Target => attackTarget;
        public float baseOffset => navMeshAgent.baseOffset;
        public AllowedTargets AllowedTargets => allowedTargets;
        public float Radius => navMeshAgent.radius;
        public UnitData Data => data;
        public int Health => health;
        public Type Type => type;

        public int indexTest;
        public int indexTestStop = -1;

        public UnityAction<Unit> OnDeath;
        public UnityAction<Unit> OnDeathAnimationFinish;
        public UnityAction<Unit> OnHealthChanged;
        public UnityAction<bool> OnOpponentInNoticeRange;
        public event UnityAction<int> OnTeamSet;
        public event UnityAction<Color> OnDamageColorSet;
        public event UnityAction<float> OnEmissionStrengthSet;
        

        private UnityAction<Unit, bool> StartMovementAction;
        private UnityAction<Unit, bool> OnAttackStatusChanged;
        private UnityAction<Unit> OnAttackTargetSet;
        protected Transform destination;
        protected Unit attackTarget;
        protected int team;
        protected float timePassedSinceLastAttack = 0;
        protected float timePassedSinceLastAttackTargetCheck = 0;
        protected float deathAnimationDepth = 2;
        protected Color originalColor = Color.white;
        protected DG.Tweening.Sequence damageAnimation;
        protected DG.Tweening.Sequence rotationAnimation;
        protected bool attackAllowed = false;
        protected bool attackTargetFound = false;
        protected bool mandatoryFirstAttack = false;
        protected Vector3 positionBefore;
        protected Vector3 lastAttackTargetPosition;
        private const float updateMovementTargetThreshold = 2f;
        protected int health;
        protected bool isDead = true;
        protected AllowedTargets allowedTargets = AllowedTargets.All;
        protected RPGCharacterController rPGCharacterController;

        private const string attackingLayer = "AttackingUnits";
        private const string nonAttackingLayer = "Units";

        protected virtual void Awake()
        {
            rPGCharacterController = GetComponent<RPGCharacterController>();
            if (rPGCharacterController != null)
                rPGCharacterController.enabled = true;
        }

        public virtual void Init(Transform destination, int team, UnityAction<Unit, bool> StartMovementAction, UnityAction<Unit, bool> OnAttackStatusChanged, UnityAction<Unit> OnAttackTargetSet)
        {
            this.StartMovementAction = StartMovementAction;
            this.OnAttackStatusChanged = OnAttackStatusChanged;
            this.OnAttackTargetSet = OnAttackTargetSet;
            Team = team;
            SetHealth(data.MaxHealth);
            isDead = false;

            if (this.GetType() == typeof(Base))
                return;

            this.destination = destination;
            timePassedSinceLastAttack = data.AttackRate;
            InitNavMesh();
            GetComponent<Collider>().enabled = true;

            if (data.AttackNoticeRange < data.AttackRange)
            {
                Debug.LogError("Attack notice range must be equal or greater than attack range.");
            }
        }

        public void PerformActionForEachUnit(Action<Unit> Action)
        {
            Action(this);
        }

        public void SetTeam(Sides team)
        {
            Team = (int)team;
        }

        public virtual GameObject GetGameObject()
        {
            return gameObject;
        }

        public virtual void SetAttackTarget(Unit unit, bool overrideMandatoryFirstAttack = false)
        {
            if (isDead)
                return;

            if (attackTarget != null)
            {
                attackTarget.OnDeath -= ClearAttackTarget;
            }

            if (overrideMandatoryFirstAttack)
            {
                mandatoryFirstAttack = false;
            }

            attackTarget = unit;
            attackTarget.OnDeath += ClearAttackTarget;
            attackAllowed = false;
            attackTargetFound = true;
            timePassedSinceLastAttackTargetCheck = 0;

            OnAttackTargetSet?.Invoke(unit);
        }

        public void ReceiveAttack(int damage)
        {
            SetHealth(health - damage);
            StartDamageAnimation();
            if (health <= 0)
                PerformDeath();
        }

        public void SetTeamColor(Color color)
        {
            DoActionForAllMaterials(mat =>
            {
                mat.SetColor("_TeamColor", color);
            });
        }

        public void SetCopyMode(bool enabled)
        {
            SetTransparent(enabled);
            SetShadowCastingMode(!enabled);
            if (enabled)
            {
                SetAlpha(0.4f);
                SetTeamColor(Color.white);
            }
        }

        public void SetEmissionStrength(float value)
        {
            DoActionForAllMaterials(mat =>
            {
                mat.SetFloat("_EmissionStrength", value);
            });
            OnEmissionStrengthSet?.Invoke(value);
        }

        public void Release(bool destroyChildren)
        {   
            if (destroyChildren)
                ObjectPool.Instance.ReturnObject(gameObject);
        }

        public void SetParentForUnits(Transform parent)
        {
            if (parent != transform.parent)
                transform.SetParent(parent);
        }

        protected virtual void OnEnable()
        {
            Collider collider = GetComponent<Collider>();
            if (collider != null)
                collider.enabled = false;
        }

        private void SetHealth(int value)
        {
            health = value;
            OnHealthChanged?.Invoke(this);
        }

        private void SetAlpha(float value)
        {
            DoActionForAllMaterials(mat =>
            {
                Color color = mat.color;
                color.a = value;
                mat.color = color;
            });
        }

        private void SetTransparent(bool enabled)
        {
            DoActionForAllMaterials(mat =>
            {
                if (enabled)
                {
                    mat.SetFloat("_Mode", 2f);
                    mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    mat.SetInt("_ZWrite", 0);
                }
                else
                {
                    mat.SetFloat("_Mode", 0f);
                    mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                    mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                    mat.SetInt("_ZWrite", 1);
                }
            });
        }


        private void SetShadowCastingMode(bool enabled)
        {
            for (int i = 0; i < renderers.Count; i++)
            {
                if (enabled)
                {
                    renderers[i].shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                }
                else
                {
                    renderers[i].shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                }
            }
        }

        private void InitNavMesh()
        {
            // navMeshAgent.enabled = true;
            // navMeshAgent.updateRotation = true;
            SetMovementActive(true, destination.position);
        }

        protected virtual void CheckIfAttackTargetReachable()
        {
            float sqrDistance = (transform.position - attackTarget.transform.position).sqrMagnitude;
            float adjustedAttackRange = data.AttackRange + attackTarget.Radius;
            float adjustedNoticeRange = data.AttackNoticeRange + attackTarget.Radius;

            if (attackTarget is not Base && sqrDistance > adjustedNoticeRange * adjustedNoticeRange)
            {
                attackTargetFound = false;
                attackTarget.OnDeath -= ClearAttackTarget;
                ClearAttackTarget(attackTarget);
            }
            else
            {
                bool isInAttackRange = sqrDistance <= adjustedAttackRange * adjustedAttackRange;
                if (isInAttackRange)
                {
                    if (!body.isKinematic)
                    {
                        SetMovementActive(false, Vector3.zero);
                        mandatoryFirstAttack = true;
                    }
                    RotateTowards(attackTarget.transform);
                    StartAttacking();
                }
                else
                {
                    attackAllowed = false;
                    UpdateMovementPositionIfNeeded();
                }
                if (sqrDistance <= adjustedNoticeRange * adjustedNoticeRange)
                    OnOpponentInNoticeRange?.Invoke(isInAttackRange);
            }
        }

        private void UpdateMovementPositionIfNeeded()
        {
            if (!navMeshAgent.hasPath || navMeshAgent.pathStatus == NavMeshPathStatus.PathInvalid || navMeshAgent.pathStatus == NavMeshPathStatus.PathPartial ||
                navMeshAgent.remainingDistance < 0.1f || lastAttackTargetPosition == Vector3.zero ||
                (lastAttackTargetPosition - attackTarget.transform.position).sqrMagnitude > updateMovementTargetThreshold)
            {
                lastAttackTargetPosition = attackTarget.transform.position;
                SetMovementActive(true, attackTarget.transform.position);
            }
        }

        protected virtual void ClearAttackTarget(Unit _)
        {

            attackTarget = null;
            if (attackAllowed)
                OnAttackStatusChanged?.Invoke(attackTarget, false);
            attackAllowed = false;
            attackTargetFound = false;
            mandatoryFirstAttack = false;
            lastAttackTargetPosition = Vector3.zero;
            gameObject.layer = LayerMask.NameToLayer(nonAttackingLayer);

            if (isDead)
                return;

            if (destination != null)
                SetMovementActive(true, destination.position);
                
            
            OnAttackTargetSet?.Invoke(null);
        }

        protected void SetMovementActive(bool isMoving, Vector3 destPos)
        {
            if (isDead)
                return;

            StartMovementAction?.Invoke(this, isMoving);
            body.isKinematic = !isMoving;

            // positionBefore = transform.position;
            // navMeshObstacle.enabled = !isMoving;
            // bool isTargetBase = attackTarget != null && attackTarget is Base;
            // navMeshObstacle.carving = !isMoving && isTargetBase;
            // if (forwardObstacle != null)
            //     forwardObstacle.SetActive(!isMoving && isTargetBase);

            // navMeshAgent.enabled = isMoving;
            // if (navMeshAgent.isOnNavMesh)
            // {
            //     navMeshAgent.isStopped = !isMoving;
            //     navMeshAgent.updateRotation = isMoving;
            // }
            StartMovingAnimation(isMoving);

            // if (isMoving)
            //     StartCoroutine(nameof(SetDestination), destPos);

            //navMeshAgent.ResetPath();
            // if (navMeshAgent.isOnNavMesh && destPos != Vector3.zero)
            //     navMeshAgent.SetDestination(destPos);
        }

        /* Because after switching from NavMeshObstacle to NavMeshAgent back again, the position of the unit slightly changes,
            so we need to set it to one from the frame before (on current frame it's the same for some reason).*/
        // protected IEnumerator SetDestination(Vector3 destPos)
        // {
        //     // yield return new WaitForNextFrameUnit();

        //     if (isDead)
        //         yield break;

        //     transform.position = positionBefore;

        //     navMeshAgent.ResetPath();
        //     navMeshAgent.SetDestination(destPos);
        // }

        protected virtual void StartMovingAnimation(bool isMoving)
        {
            if (animator == null)
                return; 
            animator.SetBool("Moving", isMoving);
            animator.SetFloat("Velocity Z", isMoving ? 1 : 0);
        }

        protected virtual void PerformDeath()
        {
            if (isDead)
                return;

            // navMeshAgent.enabled = false;
            // navMeshObstacle.enabled = false;
            // if (forwardObstacle != null)
            //     forwardObstacle.SetActive(false);
            ClearAttackTarget(this);
            SetMovementActive(false, Vector3.zero);

            if (rotationAnimation != null)
            {
                rotationAnimation.Kill(false);
                rotationAnimation = null;
            }
            OnDeath?.Invoke(this);
            transform.localRotation = Quaternion.identity;
            GetComponent<Collider>().enabled = false;

            if (rPGCharacterController != null) {
                rPGCharacterController.EndAction("Death");
                rPGCharacterController.StartAction("Death");
            }

            DG.Tweening.Sequence deathSeq = DOTween.Sequence();
            deathSeq.SetAutoKill(true);
            deathSeq.Insert(1f, transform.DOBlendableMoveBy(new Vector3(0, -deathAnimationDepth, 0), 2f));
            deathSeq.InsertCallback(3f, () =>
            {
                ObjectPool.Instance.ReturnObject(gameObject);
                OnDeathAnimationFinish?.Invoke(this);
                OnDeathAnimationFinish = null;
            });

            
            ClearEvents();

            isDead = true;
        }

        private void ClearEvents()
        {
            OnDeath = null;
            OnHealthChanged = null;
            OnOpponentInNoticeRange = null;
            OnTeamSet = null;
            OnDamageColorSet = null;
            OnEmissionStrengthSet = null;
            StartMovementAction = null;
            OnAttackStatusChanged = null;
        }

        public void SetDamageColor(Color color)
        {
            DoActionForAllMaterials(mat =>
            {
                mat.color = color;
            });
        }

        private void StartDamageAnimation()
        {
            if (damageAnimation != null)
            {
                damageAnimation.Kill();
            }
            damageAnimation = DOTween.Sequence();
            DoActionForAllMaterials(mat =>
            {
                damageAnimation.Append(
                    DOTween.To(
                        () => mat.color,
                        color =>
                        {
                            mat.color = color;
                            OnDamageColorSet?.Invoke(color);
                        },
                        commonUnitData.DamageColor,
                        commonUnitData.DamageColorAnimationDuration
                    ).SetEase(Ease.InCubic)
                );
                damageAnimation.Append(
                    DOTween.To(
                        () => mat.color,
                        color =>
                        {
                            mat.color = color;
                            OnDamageColorSet?.Invoke(color);
                        },
                        originalColor,
                        commonUnitData.DamageColorAnimationDuration
                    ).SetEase(Ease.OutCubic)
                );
            });
        }

        protected virtual void PerformAttack(TweenCallback OnFinish)
        {
            if (isDead)
                return;
        }

        private void Update()
        {
            if (isDead)
                return;

            if (body != null && !body.isKinematic)
            {
                RotateDuringMovement();
            }

            if (attackTarget != null && attackTargetFound)
            {
                timePassedSinceLastAttackTargetCheck += Time.deltaTime;
                if (timePassedSinceLastAttackTargetCheck >= commonUnitData.CheckForAttackTargetRate && !mandatoryFirstAttack)
                {
                    timePassedSinceLastAttackTargetCheck %= commonUnitData.CheckForAttackTargetRate;
                    CheckIfAttackTargetReachable();
                }
                if (attackAllowed)
                {
                    timePassedSinceLastAttack += Time.deltaTime;
                    if (timePassedSinceLastAttack >= data.AttackRate)
                    {
                        timePassedSinceLastAttack = 0;
                        PerformAttack(() =>
                        {
                            mandatoryFirstAttack = false;
                        });
                    }
                }
            }
        }

        protected virtual void StartAttacking()
        {
            if (attackAllowed)
                return;
            attackAllowed = true;
            gameObject.layer = LayerMask.NameToLayer(attackingLayer);
            timePassedSinceLastAttack += UnityEngine.Random.Range(0f, 0.1f * data.AttackRate);
            OnAttackStatusChanged?.Invoke(this, true);
        }

        private void RotateDuringMovement()
        {
            if (body.velocity.sqrMagnitude > 0.01f)
            {
                float turnSpeed = 180f; // degrees per second
                Vector3 moveDir = body.velocity.normalized;

                Quaternion targetRot = Quaternion.LookRotation(moveDir, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, turnSpeed * Time.deltaTime);
            }
        }

        private void RotateTowards(Transform target, TweenCallback OnComplete = null)
        {
            if (rotationAnimation != null)
            {
                rotationAnimation.Kill(false);
                rotationAnimation = null;
            }

            rotationAnimation = DOTween.Sequence();
            float angle = Mathf.Atan2(target.position.x - transform.position.x, target.position.z - transform.position.z) * Mathf.Rad2Deg;
            rotationAnimation.Append(transform.DORotate(new Vector3(0, angle, 0), Math.Abs(angle) / navMeshAgent.angularSpeed));
            if (OnComplete != null)
                rotationAnimation.OnComplete(OnComplete);
        }

        private void DoActionForAllMaterials(Action<Material> Action)
        {
            foreach (Renderer ren in renderers)
            {
                foreach (Material mat in ren.materials)
                {
                    Action(mat);
                }
            }
        }
    }
}
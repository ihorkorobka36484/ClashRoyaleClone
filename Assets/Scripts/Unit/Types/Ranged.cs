using UnityEngine;
using UnityEngine.AI;
using DG.Tweening;
using RPGCharacterAnims;
using System;
using RPGCharacterAnims.Actions;
using UnityEngine.Events;

namespace Units{
    public class Ranged : Unit
    {    
        [SerializeField]    
        public GameObject arrow;
        
        public event Action<Vector3, float> OnArrowLaunch;
        public event Action<bool> OnArrowSetActive;

        public override void Init(Transform destination, int team, UnityAction<Unit, bool> StartMovementAction, UnityAction<Unit, bool> OnAttackStatusChanged, UnityAction<Unit> OnAttackTargetSet)
        {
            base.Init(destination, team, StartMovementAction, OnAttackStatusChanged, OnAttackTargetSet);

            SwitchWeaponContext context = new SwitchWeaponContext();
            context.type = "Instant";
            context.side = "None";
            context.sheathLocation = "Back";
            context.leftWeapon = -1;
            context.rightWeapon = (int)Weapon.TwoHandBow;
            rPGCharacterController.StartAction("SwitchWeapon", context);
        }

        public void LaunchArrow(Vector3 targetPosition, float targetSize, TweenCallback OnFinish = null)
        {
                GameObject bullet = BulletFactory.Instance.Get();
                bullet.transform.localScale = arrow.transform.lossyScale;
                ArrowFlight arrowFlight = bullet.GetComponent<ArrowFlight>();

                bullet.SetActive(true);
                arrowFlight.FlyArrow(arrow.transform.position, targetPosition, targetSize, OnBulletFlyComplete);

                void OnBulletFlyComplete()
                {
                    bullet.SetActive(false);
                    OnFinish();
                }
        } 

        public void SetArrowActive(bool active)
        {
            arrow.SetActive(active);
            OnArrowSetActive?.Invoke(active);
        }

        private void TriggerBowAnimation(Action OnComplete)
        {
            animator.SetBool("Aiming", true);
            SetArrowActive(true);

            void SetBowPullValue(float value)
            {
                animator.SetFloat("BowPull", value);
            }

            DOTween.To(SetBowPullValue, 0, 1, 0.5f).SetEase(Ease.OutCubic).OnComplete(() =>
            {
                DOTween.To(SetBowPullValue, 1, 0, 0.25f).SetEase(Ease.InCubic).OnComplete(() =>
                {
                    animator.SetBool("Aiming", false);
                });

                SetArrowActive(false);
                OnComplete();
            });
        }      

        protected override void PerformAttack(TweenCallback OnFinish)
        {
            base.PerformAttack(OnFinish);

            if (attackTarget != null)
            {
                GameObject bullet = BulletFactory.Instance.Get();
                bullet.transform.localScale = arrow.transform.lossyScale;
                NavMeshAgent attackTargetNavMesh = attackTarget.GetComponent<NavMeshAgent>();
                float attackTargetSize = attackTarget.Radius;
                Vector3 targetPosition = attackTarget.transform.localPosition;

                TriggerBowAnimation(() =>
                    {
                        OnArrowLaunch?.Invoke(targetPosition, attackTargetSize);
                        LaunchArrow(targetPosition, attackTargetSize, () =>
                        {
                            if (attackTarget != null)
                                attackTarget.ReceiveAttack(data.Attack);
                            OnFinish();
                        });
                    }
                );
            }
        }
    }
}

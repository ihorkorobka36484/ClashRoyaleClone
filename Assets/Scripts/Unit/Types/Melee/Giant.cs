using UnityEngine;
using System.Collections;
using UnityEngine.AI;
using DG.Tweening;
using System.Collections.Generic;
using UnityEngine.Events;

namespace Units{
    public class Giant : Melee
    {
        private float pushbackForce = 0.8f;
        private float pushbackDelay = 0.35f;

        private bool clearTargetLock = false;
        private List<Transform> pushbackTargets = new List<Transform>();

        public override void Init(Transform destination, int team, UnityAction<Unit, bool> StartMovementAction, UnityAction<Unit, bool> OnAttackStatusChanged, UnityAction<Unit> OnAttackTargetSet)
        {
            rPGCharacterController.EndAction("Relax");
            rPGCharacterController.StartAction("Relax", true);
            allowedTargets = AllowedTargets.Base;
            deathAnimationDepth = 5f;
            StartCoroutine(BaseInit(destination, team, StartMovementAction, OnAttackStatusChanged, OnAttackTargetSet));
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            rPGCharacterController.EndAction("Relax");
            rPGCharacterController.StartAction("Relax", true);
        }

        protected override void StartMovingAnimation(bool isMoving)
        {
            animator.SetBool("Moving", isMoving);
            animator.SetFloat("Velocity Z", isMoving ? 0.2f : 0);
        }

        protected void OnTriggerEnter(Collider other)
        {
            if (isDead || other.gameObject.tag != "Unit")
                return;
            PushBack(other.gameObject.transform);
        }

        protected void OnTriggerExit(Collider other)
        {
            if (isDead || other.gameObject.tag != "Unit")
                return;
            pushbackTargets.Remove(other.gameObject.transform);
        }

        protected void PushBack(Transform encounter)
        {
            pushbackTargets.Add(encounter);
            StartCoroutine(PushBackCoroutine(encounter));
        }

        protected override void PerformDeath()
        {
            pushbackTargets.Clear();
            base.PerformDeath();
        }

        IEnumerator PushBackCoroutine(Transform encounter)
        {
            int area = NavMesh.GetAreaFromName("Walkable");
            int areaMask = 1 << area;
            NavMeshHit hit;
            Vector3 encounterPosition;
            Vector3 myPosition;
            Vector3 originalDirection;
            Vector3 direction;
            Vector3 positionToCheck;
            Vector3 hitPosition;
            Unit unit = encounter.GetComponent<Unit>();
            if (unit == null || unit.IsDead)
                yield break;

            do
            {
                encounterPosition = new Vector3(encounter.position.x, 0, encounter.position.z);
                myPosition = new Vector3(transform.position.x, 0, transform.position.z);
                float initialDistance = Vector3.Distance(myPosition, encounterPosition);
                originalDirection = (encounterPosition - myPosition).normalized;

                float y = 0;
                Vector3 myPos = transform.position + new Vector3(0, -transform.localPosition.y, 0);
                if (NavMesh.SamplePosition(myPos, out hit, 4f, areaMask))
                {
                    y = hit.position.y;
                }
                encounterPosition.y = y;
                direction = originalDirection;
                hitPosition = encounterPosition;
                float distance = initialDistance;

                do
                {
                    positionToCheck = encounterPosition + direction * pushbackForce;
                    if (NavMesh.SamplePosition(positionToCheck, out hit, 5f, areaMask))
                    {
                        float newDistance = Vector3.Distance(transform.position, hit.position);
                        if (newDistance > distance && CheckOtherPushbackTargetsDistances(encounter))
                        {
                            hitPosition = hit.position;
                            distance = newDistance;
                        }
                    }
                    direction = Quaternion.Euler(0, 45, 0) * direction;
                }
                while (direction != originalDirection);

                hitPosition.y = encounter.position.y;
                encounter.DOBlendableMoveBy(hitPosition - encounter.position, pushbackDelay - 0.01f).SetEase(Ease.OutQuad);

                yield return new WaitForSeconds(pushbackDelay);
            } while (IsTargetInPushbackArea(encounter) && !unit.IsDead);
        }

        private bool CheckOtherPushbackTargetsDistances(Transform target)
        {
            for (int i = 0; i < pushbackTargets.Count; i++)
            {
                if (target == pushbackTargets[i]) continue;
                if (Vector3.Distance(target.position, pushbackTargets[i].position) < 0.4f)
                    return false;
            }
            return true;
        }

        private bool IsTargetInPushbackArea(Transform target)
        {
            return pushbackTargets.Contains(target);
        }

        protected override void ClearAttackTarget(Unit unit)
        {
            if (clearTargetLock)
                return;
            rPGCharacterController.EndAction("Relax");
            rPGCharacterController.StartAction("Relax");
            StartCoroutine(BaseClearAttackTarget(unit));
            clearTargetLock = true;
        }

        // This delay is needed to ensure that character is in relaxed state before it starts moving again.
        IEnumerator BaseClearAttackTarget(Unit unit)
        {
            yield return new WaitForSeconds(0.1f);
            base.ClearAttackTarget(unit);
            clearTargetLock = false;
        }


        IEnumerator BaseInit(Transform destination, int team, UnityAction<Unit, bool> StartMovementAction, UnityAction<Unit, bool> OnAttackStatusChanged, UnityAction<Unit> OnAttackTargetSet)
        {
            yield return new WaitForSeconds(0.1f);
            base.Init(destination, team, StartMovementAction, OnAttackStatusChanged, OnAttackTargetSet);
        }
    }
}

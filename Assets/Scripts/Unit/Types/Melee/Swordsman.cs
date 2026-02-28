
using UnityEngine;
using DG.Tweening;
using RPGCharacterAnims;
using RPGCharacterAnims.Actions;

namespace Units{
    public class Swordsman : Melee
    {    
        [SerializeField]
        private GameObject swordInHand;
        [SerializeField]
        private GameObject swordInTheBack;

        public event System.Action<bool> OnSetSwordActive;

        private bool hadTargetBefore = false;
        private bool isSwordSheathed = true;

        protected override void Awake()
        {
            OnOpponentInNoticeRange += isInAttackRange =>
            {
                SheathWeapon(false, isInAttackRange);
            };
            OnDeathAnimationFinish += unit =>
            {
                SheathWeapon(true, true);
            };
            base.Awake();
        }

        public override void SetAttackTarget(Unit target, bool overrideMandatoryFirstAttack = false)
        {
            hadTargetBefore = HasTarget && attackTarget is not Base;
            base.SetAttackTarget(target, overrideMandatoryFirstAttack);
        }

        public void SetSwordActive(bool active)
        {
            swordInHand.SetActive(active);
            swordInTheBack.SetActive(!active);
        }
        
        protected override void ClearAttackTarget(Unit unit)
        {
            hadTargetBefore = false;
            SheathWeapon(true, false);
            base.ClearAttackTarget(unit);
        }

        private void SheathWeapon(bool active, bool instant)
        {
            if (isDead && !instant)
                return;

            if (isSwordSheathed == active)
                return;
            
            isSwordSheathed = active;

            SwitchWeaponContext context = new SwitchWeaponContext();
            float callbackDelay = active ? 0.6f : 0.1f;
            context.type = active ? "Sheath" : "Unsheath";
            if (instant)
            {
                context.type = "Instant";
            }
            context.side = active ? "None" : "Right";
            context.sheathLocation = "Back";
            context.leftWeapon = (int)Weapon.Relax;
            context.rightWeapon = active ? (int)Weapon.Relax : (int)Weapon.RightSword;
            rPGCharacterController.StartAction("SwitchWeapon", context);

            seq?.Kill();
            seq = DOTween.Sequence();
            if (instant)
                OnSwordAnimationComplete(active);
            else
                seq.InsertCallback(callbackDelay, () => OnSwordAnimationComplete(active));

            void OnSwordAnimationComplete(bool isActive)
            {
                if (isSwordSheathed == isActive)
                {
                    SetSwordActive(!isActive);
                    OnSetSwordActive?.Invoke(!isActive);
                    if (isActive)
                        animator.SetBool("Trigger", true);
                }
            }
        }
    }
}

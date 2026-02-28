using UnityEngine;

namespace Units{
    [CreateAssetMenu(fileName = "CommonUnitData", menuName = "Units/CommonUnitData", order = 3)]
    public class CommonUnitData : ScriptableObject
    {
        [SerializeField]
        private Color damageColor;
        [SerializeField]
        private float damageColorAnimationDuration;
        [SerializeField]
        private float checkForAttackTargetRate;

        public Color DamageColor => damageColor;
        public float DamageColorAnimationDuration => damageColorAnimationDuration;
        public float CheckForAttackTargetRate => checkForAttackTargetRate;
    }
}

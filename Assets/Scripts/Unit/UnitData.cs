using UnityEngine;

namespace Units{
    [CreateAssetMenu(fileName = "UnitData", menuName = "Units/UnitData", order = 2)]
    public class UnitData : ScriptableObject
    {
        [SerializeField]
        private int maxHealth;
        [SerializeField]
        private int attack;
        [SerializeField]
        private int cost;
        [SerializeField]
        [Tooltip("On this range unit starts actually attacking enemy.")]
        private float attackRange;
        [SerializeField]
        [Tooltip("Range, on which unit spots an enemy and starts walking to it. Must be equal or greater than attack range.")]
        private float attackNoticeRange;
        [SerializeField]
        private float attackRate;
        [SerializeField]
        private Size size;

        public int MaxHealth => maxHealth;
        public int Attack => attack;
        public int Cost => cost;
        public float AttackRange => attackRange;
        public float AttackNoticeRange => attackNoticeRange;
        public float AttackRate => attackRate;
        public Size Size => size;
    }
}

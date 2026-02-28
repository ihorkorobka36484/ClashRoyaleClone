using System.Collections.Generic;
using UnityEngine;

namespace Units{
    public class UnitsList : MonoBehaviour
    {
        /// <summary>
        /// Prefabs must be set in the same order as in the Units.Type enum.
        /// </summary>
        [SerializeField]
        private List<Unit> networkPrefabs = new List<Unit>();
        [SerializeField]
        private List<Unit> nonNetworkPrefabs = new List<Unit>();
        
        // Singleton instance
        public static UnitsList Instance { get; private set; }

        public Unit GetByType(Type type, bool networked = false)
        {
            List<Unit> prefabs = networked ? networkPrefabs : nonNetworkPrefabs;
            foreach (var unit in prefabs)
            {
                if (unit.Type == type)
                {
                    return unit;
                }
            }
            return null;
        }

        private void Awake()
        {
            // Ensure only one instance of UnitsList exists
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Debug.LogWarning("Multiple UnitsList instances detected. Destroying duplicate.");
                Destroy(gameObject);
            }
        }
    }
}
using System.Collections.Generic;
using UnityEngine;

namespace Units{
    public class UnitAutoSpawner : UnitSpawner
    {
        
        [SerializeField]
        private float spawnRate;
        [SerializeField]
        private List<Unit> unitPrefabs;

        private float timePassed = 0;

        protected override void Start()  {}

        private void Awake()
        {
            timePassed = spawnRate;
        }
        
        void Update() {
            if (Instance.SpawningAllowed)
            {
                timePassed += Time.deltaTime;
                if (timePassed > spawnRate)
                {
                    SpawnParams spawnParams = new SpawnParams(
                        transform.position,
                        unitPrefabs[Random.Range(0, unitPrefabs.Count)].Type,
                        true,
                        false,
                        team);
                    Instance.SendSpawnRequest(spawnParams);
                    timePassed = 0;
                }
            }
        }
    }
}

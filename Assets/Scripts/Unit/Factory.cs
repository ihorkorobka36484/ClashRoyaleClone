using TMPro;
using Unity.Netcode;
using UnityEngine;

namespace Units
{
    public class Factory : MonoBehaviour
    {
        [SerializeField]
        private Base basePrefab;
        [SerializeField]
        private Base playerBase;
        [SerializeField]
        private Base enemyBase;    
        [SerializeField] private UnitButtonPanel unitButtonPanel;
        [SerializeField] private ElixirManager elixirManagerPrefab;
        [Header("Elixir Manager values")]
        [SerializeField]
        private ProgressBar progressBar;
        [SerializeField]
        private TextMeshProUGUI currentValueText;
        [SerializeField]
        private TextMeshProUGUI maxValueText;
        

        // Singleton instance
        public static Factory Instance { get; private set; }

        public GameObject Create(Vector3 position, float yUpOffset, Type unitType, bool isCopy = false)
        {
            Unit unit = UnitsList.Instance.GetByType(unitType, !isCopy);
            GameObject go = CreateInstance(unit);
            position.y = yUpOffset;
            ISpawnable spawnable = go.GetComponent<ISpawnable>();
            

            go.transform.localPosition = position;
            // In order for units, that are a part of a SpawnGroup, to spawn in correct positions on client side,
            // we need to set their positions before calling Spawn method on them.
            SpawnGroup spawnGroup = spawnable as SpawnGroup;
            // if (spawnGroup != null)
            //     spawnGroup.SetPositionsForUnits();
            if (!isCopy)
                CheckNetwork(spawnable);
            go.transform.SetParent(UnitSpawner.Instance.UnitsParent, false);
            if (spawnGroup != null)
                spawnGroup.SetParentForUnits(spawnGroup.transform);
            if (spawnGroup != null)
                spawnGroup.SetPositionsForUnits();

            spawnable.SetCopyMode(isCopy);
            go.gameObject.SetActive(true);

            return go;
        }

        public GameObject CreateElixirManager()
        {
            ElixirManager elixirManager = Instantiate(elixirManagerPrefab);
            elixirManager.GetComponent<NetworkObject>().Spawn();
            return elixirManager.gameObject;
        }

        public void InitializeElixirManager()
        {
            ElixirManager.Instance.Initialize(progressBar, currentValueText, maxValueText, unitButtonPanel.UpdateButtonsStatus);
        }

        public GameObject CreateBase(bool isEnemy)
        {
            GameObject go = CreateInstance(basePrefab.GetComponent<Unit>());
            go.name = isEnemy ? "EnemyBase" : "PlayerBase";
            go.transform.localPosition = isEnemy ? enemyBase.transform.localPosition : playerBase.transform.localPosition;
            go.transform.localRotation = isEnemy ? enemyBase.transform.localRotation : playerBase.transform.localRotation;
            CheckNetwork(go.GetComponent<ISpawnable>());
            go.transform.SetParent(playerBase.transform.parent, false);

            ProgressBar progressBar = ProgressBarManager.Instance.CreateProgressBar(go.GetComponent<Unit>());
            progressBar.ChangeColors(progressBar.backgroundColor, UnitSpawner.Instance.TeamColors[isEnemy ? 1 : 0]);

            return go;
        }

        private GameObject CreateInstance(Unit unitType)
        {
            GameObject prefab = unitType.Spawnable.GetGameObject(); 
            GameObject go = ObjectPool.Instance.GetObject(prefab);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            return go;
        }        

        private void CheckNetwork(ISpawnable spawnable)
        {
            if (NetworkManager.Singleton.IsListening && NetworkManager.Singleton.IsHost)
            {
                SpawnGroup spawnGroup = spawnable as SpawnGroup;
                if (spawnGroup != null)
                    spawnable.GetGameObject().GetComponent<NetworkObject>().Spawn();
                spawnable.PerformActionForEachUnit(unit => unit.gameObject.GetComponent<NetworkObject>().Spawn());
            }
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

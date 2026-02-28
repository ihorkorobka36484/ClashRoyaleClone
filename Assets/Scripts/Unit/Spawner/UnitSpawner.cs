using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.AI;


namespace Units{
    public class UnitSpawner : NetworkBehaviour
    {
        [SerializeField]
        UnitButtonPanel panel;
        [SerializeField]
        ProgressBarManager progressBarManager;
        [SerializeField]
        SpawnParticles spawnParticlesPrefab;
        [SerializeField]
        TargetAcquiring targetAcquiring;
        [SerializeField]
        PathfindingUnitController pathfindingUnitController;
        [SerializeField]
        GridController gridController;
        [SerializeField]
        Transform unitsParent;
        [SerializeField]
        Transform unitCopiesParent;
        [SerializeField]
        private Color playerTeamColor;
        [SerializeField]
        private Color enemyTeamColor;
        [SerializeField, Range(0, 1)]
        protected int team;

        [Serializable]
        public readonly struct SpawnParams : INetworkSerializeByMemcpy
        {
            public readonly Vector3 Position;
            public readonly Units.Type UnitType;
            public readonly bool Spawn;
            public readonly bool PayElixir;
            public readonly int Team;
            public readonly int UnitIndex;

            public SpawnParams(Vector3 position, Units.Type unitType, bool spawn, bool payElixir, int team, int unitIndex = -1)
            {
                Position = position;
                UnitType = unitType;
                Spawn = spawn;
                PayElixir = payElixir;
                Team = team;
                UnitIndex = unitIndex;
            }
        }

        public static UnitSpawner Instance
        {
            get { return _instance; }
            protected set { _instance = value; }
        }
        public Transform UnitsParent => unitsParent;
        public IReadOnlyList<Base> Bases => bases.AsReadOnly();
        public bool SpawningAllowed => spawningAllowed;
        public List<Color> TeamColors => new List<Color> { playerTeamColor, enemyTeamColor };

        protected bool spawningAllowed = false;

        private List<Base> bases = new();
        private GameObject unitCopy;
        private const float delayBeforeSpawn = 1f;
        private static UnitSpawner _instance;
        private int clientCopyIndex = 1;
        private int testIndex = 0;
        private Dictionary<int, GameObject> unitCopies = new();

        protected virtual void Start()
        {
            unitCopiesParent.transform.position = unitsParent.transform.position;
            NetworkManager.Singleton.OnServerStarted += StartSpawning;
            if (this.GetType() != typeof(UnitSpawner))
                return;
            _instance = this;
            NetworkManager.Singleton.OnServerStarted += SpawnBases;
            panel.SetOnDragEvents(CreateUnitCopy, SendSpawnRequest, UpdateUnitCopyPosition);
        }

        public void SendSpawnRequest(SpawnParams spawnParams)
        {
            UnitData data = UnitsList.Instance.GetByType(spawnParams.UnitType).Data;

            if (IsHost)
            {                    
                UnitSpawner.Instance.TrySpawn(spawnParams);
            }
            else
            {
                if (!spawnParams.Spawn)
                {
                    RemoveCopy();
                    return;
                }
                unitCopy.transform.SetParent(unitsParent);
                spawnParams = new SpawnParams(
                    unitCopy.transform.localPosition,
                    spawnParams.UnitType,
                    spawnParams.Spawn,
                    spawnParams.PayElixir,
                    GetEnemyTeam(),
                    clientCopyIndex);
                TrySpawnUnitServerRpc(spawnParams);
                unitCopies.Add(clientCopyIndex, unitCopy);
                unitCopy = null;
                ElixirManager.Instance.UpdateSpawnLock(1);
                clientCopyIndex++;
            }
          
            if (spawnParams.PayElixir && spawnParams.Spawn)
                panel.CreateFieldElixirAnimationInMousePosition(data.Cost);
        }

        public void StartParticlesOnlySpawnAnimation(Vector3 startPosition, Transform parent, Size size, TweenCallback OnSpawnAnimationFinish = null)
        {
            Sequence spawnAnimation = DOTween.Sequence();
            GameObject spawnParticles = ObjectPool.Instance.GetObject(this.spawnParticlesPrefab.gameObject);
            spawnParticles.GetComponent<SpawnParticles>().StartParticlesAnimation(
                startPosition,
                parent,
                size,
                spawnAnimation,
                OnSpawnAnimationFinish);
        }

        public void StartSpawnAnimation(ISpawnable spawnable, bool onlyParticles = false, TweenCallback OnSpawnAnimationFinish = null)
        {
            Sequence seq = DOTween.Sequence();
            float minRandom = 0.1f;
            float maxRandom = 0.6f;
            SpawnGroup spawnGroup = spawnable as SpawnGroup;
            if (spawnGroup == null)
            {
                minRandom = 0;
                maxRandom = 0;
            }
            else
            {
                spawnGroup.SetParentForUnits(spawnGroup.transform.parent);
            }
            spawnable.PerformActionForEachUnit(unit =>
            {
                unit.gameObject.SetActive(false || spawnable is not SpawnGroup);
                seq.InsertCallback(UnityEngine.Random.Range(minRandom, maxRandom), () =>
                {

                    unit.GetComponent<NetworkTransform>().Interpolate = false;
                    unit.gameObject.SetActive(true);
                    GameObject spawnParticles = ObjectPool.Instance.GetObject(this.spawnParticlesPrefab.gameObject);
                    spawnParticles.GetComponent<SpawnParticles>().StartSpawnAnimation(
                        unit,
                        UnitSpawner.Instance.UnitsParent,
                        () =>
                        {
                            OnSpawnAnimationFinish?.Invoke();
                            spawnable.Release(false);
                            unit.GetComponent<NetworkTransform>().Interpolate = true;
                        },
                        onlyParticles);
                    unit.GetComponent<NetworkUnit>().StartSpawnAnimation();
                });
            });
        }

        public int GetEnemyTeam()
        {
            return team == 0 ? 1 : 0;
        }

        public void RemoveClientCopy(int index)
        {
            if (unitCopies.ContainsKey(index))
            {
                unitCopies[index].GetComponent<ISpawnable>().Release(true);
                unitCopies.Remove(index);
            }
        }

        private void TrySpawn(SpawnParams spawnParams)
        {
            UnitData data = UnitsList.Instance.GetByType(spawnParams.UnitType).Data;
            if (spawnParams.PayElixir)
                ElixirManager.Instance.ChangeValue(-data.Cost, (Sides)spawnParams.Team);
            StartCoroutine(TrySpawnCor(spawnParams));
        }

        private void StartSpawning()
        {
            spawningAllowed = true;
            NetworkManager.Singleton.OnServerStarted -= StartSpawning;
        }

        [ServerRpc(RequireOwnership = false)]
        private void TrySpawnUnitServerRpc(SpawnParams spawnParams)
        {
            UnitSpawner.Instance.TrySpawn(spawnParams);
        }

        private void RemoveCopy()
        {
            if (unitCopy != null)
            {
                unitCopy.GetComponent<ISpawnable>().Release(true);
                unitCopy = null;
            }
        }

        private IEnumerator TrySpawnCor(SpawnParams spawnParams)
        {
            if (!spawnParams.Spawn)
            {
                RemoveCopy();
                yield break;
            }

            GameObject oldUnitCopy = unitCopy;
            unitCopy = null;

            yield return new WaitForSeconds(delayBeforeSpawn);

            float baseOffset = UnitsList.Instance.GetByType(spawnParams.UnitType).GetComponent<NavMeshAgent>().baseOffset;
            ISpawnable spawnable = Factory.Instance.Create(spawnParams.Position, spawnParticlesPrefab.YUpOffset + baseOffset, spawnParams.UnitType).GetComponent<ISpawnable>();
            InitializeSpawnable(spawnable, spawnParams);

            if (oldUnitCopy != null)
            {
                oldUnitCopy.GetComponent<ISpawnable>().Release(true);
            }
        }

        private void InitializeSpawnable(ISpawnable spawnable, SpawnParams spawnParams)
        {
            spawnable.PerformActionForEachUnit((unit) =>
            {
                unit.GetComponent<NetworkUnit>().index.Value = spawnParams.UnitIndex;
                unit.indexTest = testIndex;
                testIndex++;
                if (spawnParams.Team == (int)Sides.Enemy)
                    unit.transform.localEulerAngles = new Vector3(0, 180, 0);
                unit.OnDeath += _unit => progressBarManager.RemoveProgressBar(_unit);
                unit.OnDeath += TryRemoveLocalField;
            });
            spawnable.SetTeam((Sides)spawnParams.Team);
            StartSpawnAnimation(spawnable, false, () =>
            {
                spawnable.PerformActionForEachUnit(unit =>
                    {
                        targetAcquiring.AddUnit(unit);
                        ProgressBar progressBar = progressBarManager.CreateProgressBar(unit);
                        if (progressBar != null)
                            progressBar.ChangeColors(progressBar.backgroundColor, UnitSpawner.Instance.TeamColors[spawnParams.Team]);
                    });
                spawnable.Init(UnitSpawner.Instance.Bases[spawnParams.Team].transform, spawnParams.Team, StartUnitMovement, CalculateFlowField, TryAddLocalField);
            });
        }

        private void TryAddLocalField(Unit unit)
        {
            if (unit is Base) return;
            gridController.AddLocalObject(unit.gameObject);
        }

        private void TryRemoveLocalField(Unit unit)
        {
            gridController.RemoveLocalObject(unit.gameObject);
        }

        private void CalculateFlowField(Unit unit, bool _)
        {
            gridController.RecalculateFieldsController.AddObjectToCheck(unit.gameObject);
        }

        private void StartUnitMovement(Unit unit, bool isMoving)
        {
            if (isMoving)
            {
                pathfindingUnitController.AddUnit(unit);
            }
            else
            {
                pathfindingUnitController.RemoveUnit(unit);
            }
        }

        private void UpdateUnitCopyPosition(Vector3 position)
        {
            if (unitCopy != null)
            {
                position.y = 0;
                unitCopy.transform.localPosition = position;
            }
        }

        private void CreateUnitCopy(Vector3 position, Type unitType)
        {
            if (unitCopy != null)
            {
                return;
            }
            unitCopy = Factory.Instance.Create(position, 0, unitType, true);
            Sides side = NetworkManager.Singleton.IsHost ? Sides.Player : Sides.Enemy;
            unitCopy.GetComponent<ISpawnable>().PerformActionForEachUnit((unit) =>
            {
                if (side == Sides.Enemy) 
                    unit.transform.localEulerAngles = new Vector3(0, 180, 0);
            });
            unitCopy.transform.SetParent(unitCopiesParent);
        }

        private void SpawnBases()
        {
            NetworkManager.Singleton.OnServerStarted -= SpawnBases;

            bases.Insert(team, Factory.Instance.CreateBase(true).GetComponent<Base>());
            bases[team].Init(null, GetEnemyTeam(), null, null, null);
            bases[team].SetTeamColor(enemyTeamColor);
            bases[team].gameObject.SetActive(true);

            bases.Insert(GetEnemyTeam(), Factory.Instance.CreateBase(false).GetComponent<Base>());
            bases[GetEnemyTeam()].Init(null, team, null, null, null);
            bases[GetEnemyTeam()].SetTeamColor(playerTeamColor);
            bases[GetEnemyTeam()].gameObject.SetActive(true);

            gridController.CreateGlobalFlowField(team, bases[team].transform.position);
            gridController.CreateGlobalFlowField(GetEnemyTeam(), bases[GetEnemyTeam()].transform.position);

            targetAcquiring.gameObject.SetActive(true);

            WinConditionChecker.Instance.Init();
            int gridSize = (int)(UnitsList.Instance.GetByType(Units.Type.Swordsman).Data.AttackNoticeRange * gridController.cellRadius);
            gridController.SetLocalGridSize(new Vector2Int(gridSize * 2, gridSize * 2));
            gridController.baseFlowFields[0].objectsToIgnore.Add(bases[team].gameObject);
            gridController.baseFlowFields[1].objectsToIgnore.Add(bases[GetEnemyTeam()].gameObject);
        }
    }
}

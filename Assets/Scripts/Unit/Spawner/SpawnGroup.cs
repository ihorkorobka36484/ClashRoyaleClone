using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

namespace Units
{
    public class SpawnGroup : MonoBehaviour, ISpawnable
    {
        [SerializeField]
        private Unit unitType;
        [SerializeField]
        int count = 1;

        public UnitData Data => unitType.Data;

        private List<Unit> units = new List<Unit>();
        private float areaWidth = 7f;
        private float areaLength = 7f;

        public void Init(Transform destination, int team, UnityAction<Unit, bool> StartMovementAction, UnityAction<Unit, bool> OnAttackStatusChanged, UnityAction<Unit> OnAttackTargetSet)
        {
            PerformActionForEachUnit((unit) =>
            {
                unit.Init(destination, team, StartMovementAction, OnAttackStatusChanged, OnAttackTargetSet);
            });
            units.Clear();
        }

        public void SetPositionsForUnits()
        {
            SpreadOutUnits(areaWidth, areaLength);
        }

        public void Release(bool destroyChildren)
        {
            if (destroyChildren) {
                foreach (Unit unit in units)
                {
                    unit.gameObject.transform.SetParent(transform.parent);
                    ObjectPool.Instance.ReturnObject(unit.gameObject);
                }
            }
            units.Clear();
            NetworkObject networkObject = GetComponent<NetworkObject>();
            if (GetComponent<NetworkObject>() != null && NetworkManager.Singleton.IsHost)
            {
                networkObject.Despawn();
                return;
            }
            Destroy(gameObject);
        }
        
        public void SetParentForUnits(Transform parent)
        {
            PerformActionForEachUnit((unit) =>
            {
                unit.transform.SetParent(parent, false);
                AdjustPositionsAfterParentingToOther(unit.transform, parent);
            });
        }

        public void SetTeam(Sides team)
        {
            PerformActionForEachUnit((unit) =>
            {
                unit.SetTeam(team);
            });
        }

        public void SetCopyMode(bool enabled)
        {
            PerformActionForEachUnit((unit) =>
            {
                unit.SetCopyMode(enabled);
            });
        }

        public void PerformActionForEachUnit(Action<Unit> Action)
        {
            foreach (Unit unit in units)
            {
                Action?.Invoke(unit);
            }
        }

        public float baseOffset => units[0].baseOffset;

        public GameObject GetGameObject()
        {
            return gameObject;
        }

        private void OnEnable()
        {
            foreach (Transform child in transform)
            {
                child.SetParent(null);
            }
            for (int i = 0; i < count; i++)
            {
                Unit unit = ObjectPool.Instance.GetObject(unitType.gameObject).GetComponent<Unit>();
                units.Add(unit);
            }
        }

        private void AdjustPositionsAfterParentingToOther(Transform unitTransform, Transform parent)
        {
            if (parent != this.transform)
            {
                unitTransform.localPosition += transform.localPosition;
            }
        }

        private void SpreadOutUnits(float areaWidth, float areaLength, float randomOffset = 0.2f)
        {
            if (units == null || units.Count == 0) return;

            int count = units.Count;
            int gridSize = 1;
            while ((gridSize * gridSize + 1) / 2 < count)
                gridSize++;

            float cellWidth = areaWidth / gridSize;
            float cellLength = areaLength / gridSize;

            int placed = 0;
            for (int row = 0; row < gridSize && placed < count; row++)
            {
                for (int col = 0; col < gridSize && placed < count; col++)
                {
                    if ((row + col) % 2 == 0)
                    {
                        float x = -areaWidth / 2f + col * cellWidth + cellWidth / 2f;
                        float z = -areaLength / 2f + row * cellLength + cellLength / 2f;

                        float randX = UnityEngine.Random.Range(-cellWidth * randomOffset, cellWidth * randomOffset);
                        float randZ = UnityEngine.Random.Range(-cellLength * randomOffset, cellLength * randomOffset);

                        units[placed].transform.localPosition = new Vector3(x + randX, 0, z + randZ);
                        //units[placed].transform.position += transform.position;
                        placed++;
                    }
                }
            }
        }
    }
}

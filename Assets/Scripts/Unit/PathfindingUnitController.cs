using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Units
{
    public class PathfindingUnitController : MonoBehaviour
    {
        public GridController gridController;
        public GameObject unitPrefab;
        public int numUnitsPerSpawn;
        public float moveSpeed;

        private List<Unit> unitsInGame;

        private void Awake()
        {
            unitsInGame = new List<Unit>();
        }

        void Update()
        {
            // if (Input.GetKeyDown(KeyCode.Alpha1))
            // {
            //     SpawnUnits();
            // }

            // if (Input.GetKeyDown(KeyCode.Alpha2))
            // {
            //     DestroyUnits();
            // }
        }

        public void AddUnit(Unit unit)
        {
            if (!unitsInGame.Contains(unit))
                unitsInGame.Add(unit);
        }

        public void RemoveUnit(Unit unit)
        {
            unitsInGame.Remove(unit);
        }

        private void FixedUpdate()
        {
            if (gridController.baseFlowFields[0] == null) { return; }
            foreach (Unit unit in unitsInGame)
            {
                if (unit.IsDead || !unit.HasTarget) continue;
                FlowField field = gridController.GetFlowFieldByLocalObject(unit.Target.gameObject);
                if (field == null) field = gridController.baseFlowFields[unit.Team];
                //Cell cellBelow = gridController.baseFlowFields[0].GetCellFromWorldPos(unit.transform.position);
                //Vector3 moveDirection = new Vector3(cellBelow.bestDirection.Vector.x, 0, cellBelow.bestDirection.Vector.y);
                Vector2 flowDir = field.GetSmoothedFlowDirection(unit.transform.position);
                Vector3 moveDirection = new Vector3(flowDir.x, 0, flowDir.y);
                Rigidbody unitRB = unit.GetComponent<Rigidbody>();
                unitRB.velocity = moveDirection * moveSpeed;
            }
        }

        // private void SpawnUnits()
        // {
        //     Vector2Int gridSize = gridController.gridSize;
        //     float nodeRadius = gridController.cellRadius;
        //     Vector2 maxSpawnPos = new Vector2(gridSize.x * nodeRadius * 2 + nodeRadius, gridSize.y * nodeRadius * 2 + nodeRadius);
        //     int colMask = LayerMask.GetMask("Impassable", "Units");
        //     Vector3 newPos;
        //     for (int i = 0; i < numUnitsPerSpawn; i++)
        //     {
        //         GameObject newUnit = Instantiate(unitPrefab);
        //         newUnit.transform.parent = transform;
        //         unitsInGame.Add(newUnit);
        //         do
        //         {
        //             newPos = new Vector3(Random.Range(0, maxSpawnPos.x), 0, Random.Range(0, maxSpawnPos.y)) + gridController.transform.position;
        //             newUnit.transform.position = newPos;
        //         }
        //         while (Physics.OverlapSphere(newPos, 0.25f, colMask).Length > 0);
        //     }
        // }

        private void DestroyUnits()
        {
            foreach (Unit unit in unitsInGame)
            {
                Destroy(unit.gameObject);
            }
            unitsInGame.Clear();
        }
    }
}
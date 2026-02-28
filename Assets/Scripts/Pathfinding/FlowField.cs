using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlowField
{
    public Cell[,] grid { get; private set; }
    public Vector2Int gridSize { get; private set; }
    public float cellRadius { get; private set; }
    public Cell destinationCell;
    public HashSet<GameObject> objectsToIgnore = new HashSet<GameObject>();

    public Vector3 OriginPos => originPos;

    private float cellDiameter;
    private Vector3 originPos;

    public FlowField(float _cellRadius, Vector2Int _gridSize)
    {
        SetBaseParams(_cellRadius, _gridSize);
    }

    public FlowField(float _cellRadius, Vector2Int _gridSize, Vector3 centerPos)
    {
        SetBaseParams(_cellRadius, _gridSize);
        originPos = centerPos - new Vector3(cellRadius * gridSize.x, 0f, cellRadius * gridSize.y);
    }

    public void SetOriginPos(Vector3 _originPos)
    {
        originPos = _originPos;
    }

    public bool HasPositionInGrid(Vector3 worldPos)
    {
        Vector3 localPos = worldPos - originPos;

        float percentX = localPos.x / (gridSize.x * cellDiameter);
        float percentY = localPos.z / (gridSize.y * cellDiameter);

        return percentX >= 0f && percentX <= 1f && percentY >= 0f && percentY <= 1f;
    }

    public void CreateGrid()
    {
        grid = new Cell[gridSize.x, gridSize.y];

        for (int x = 0; x < gridSize.x; x++)
        {
            for (int y = 0; y < gridSize.y; y++)
            {
                Vector3 worldPos = new Vector3(cellDiameter * x + cellRadius, 0, cellDiameter * y + cellRadius);
                grid[x, y] = new Cell(worldPos, new Vector2Int(x, y));
            }
        }
    }

    public void CreateCostField()
    {
        Vector3 cellHalfExtents = Vector3.one * cellRadius;
        int terrainMask = LayerMask.GetMask("Impassable", /*"RoughTerrain", */"AttackingUnits");
        foreach (Cell curCell in grid)
        {
            curCell.ResetCosts();
            Collider[] obstacles = Physics.OverlapBox(curCell.position + originPos, cellHalfExtents, Quaternion.identity, terrainMask);
            foreach (Collider col in obstacles)
            {
                if (objectsToIgnore.Contains(col.gameObject)) continue;

                if (col.gameObject.layer == LayerMask.NameToLayer("Impassable"))
                {
                    curCell.IncreaseCost(255);
                    continue;
                }
                // else if (!hasIncreasedCost && col.gameObject.layer == LayerMask.NameToLayer("RoughTerrain"))
                // {
                //     curCell.IncreaseCost(3);
                //     hasIncreasedCost = true;
                else if (col.gameObject.layer == LayerMask.NameToLayer("AttackingUnits"))
                {
                    curCell.IncreaseCost(30);
                }
            }
        }
    }

    public void CreateIntegrationField(Cell _destinationCell)
    {
        destinationCell = _destinationCell;

        destinationCell.cost = 0;
        destinationCell.bestCost = 0;

        Queue<Cell> cellsToCheck = new Queue<Cell>();

        cellsToCheck.Enqueue(destinationCell);

        while (cellsToCheck.Count > 0)
        {
            Cell curCell = cellsToCheck.Dequeue();
            List<Cell> curNeighbors = GetNeighborCells(curCell.gridIndex, GridDirection.CardinalDirections);
            foreach (Cell curNeighbor in curNeighbors)
            {
                if (curNeighbor.cost == byte.MaxValue) { continue; }
                if (curNeighbor.cost + curCell.bestCost < curNeighbor.bestCost)
                {
                    curNeighbor.bestCost = (ushort)(curNeighbor.cost + curCell.bestCost);
                    cellsToCheck.Enqueue(curNeighbor);
                }
            }
        }
    }

    public void CreateFlowField()
    {
        foreach (Cell curCell in grid)
        {
            List<Cell> curNeighbors = GetNeighborCells(curCell.gridIndex, GridDirection.AllDirections);

            int bestCost = curCell.bestCost;

            foreach (Cell curNeighbor in curNeighbors)
            {
                if (curNeighbor.bestCost < bestCost)
                {
                    bestCost = curNeighbor.bestCost;
                    curCell.bestDirection = GridDirection.GetDirectionFromV2I(curNeighbor.gridIndex - curCell.gridIndex);
                }
            }
        }
    }

    private void SetBaseParams(float _cellRadius, Vector2Int _gridSize)
    {
        cellRadius = _cellRadius;
        cellDiameter = cellRadius * 2f;
        gridSize = _gridSize;
    }

    private List<Cell> GetNeighborCells(Vector2Int nodeIndex, List<GridDirection> directions)
    {
        List<Cell> neighborCells = new List<Cell>();

        foreach (Vector2Int curDirection in directions)
        {
            Cell newNeighbor = GetCellAtRelativePos(nodeIndex, curDirection);
            if (newNeighbor != null)
            {
                neighborCells.Add(newNeighbor);
            }
        }
        return neighborCells;
    }

    private Cell GetCellAtRelativePos(Vector2Int orignPos, Vector2Int relativePos)
    {
        Vector2Int finalPos = orignPos + relativePos;

        if (finalPos.x < 0 || finalPos.x >= gridSize.x || finalPos.y < 0 || finalPos.y >= gridSize.y)
        {
            return null;
        }

        else { return grid[finalPos.x, finalPos.y]; }
    }

    public Cell GetCellFromWorldPos(Vector3 worldPos)
    {
        worldPos -= originPos;

        float percentX = worldPos.x / (gridSize.x * cellDiameter);
        float percentY = worldPos.z / (gridSize.y * cellDiameter);

        percentX = Mathf.Clamp01(percentX);
        percentY = Mathf.Clamp01(percentY);

        int x = Mathf.Clamp(Mathf.FloorToInt((gridSize.x) * percentX), 0, gridSize.x - 1);
        int y = Mathf.Clamp(Mathf.FloorToInt((gridSize.y) * percentY), 0, gridSize.y - 1);
        return grid[x, y];
    }
    
    public Vector3 GetWorldPosFromCell(Cell cell)
    {
        return cell.position + originPos;
    }

    public Vector2 GetSmoothedFlowDirection(Vector3 worldPos)
    {
        // Position relative to grid origin
        Vector3 localPos = worldPos - originPos;

        float percentX = localPos.x / (gridSize.x * cellDiameter);
        float percentY = localPos.z / (gridSize.y * cellDiameter);

        percentX = Mathf.Clamp01(percentX);
        percentY = Mathf.Clamp01(percentY);

        // Continuous coordinates in grid space
        float gridX = percentX * (gridSize.x - 1);
        float gridY = percentY * (gridSize.y - 1);

        int x0 = Mathf.FloorToInt(gridX);
        int y0 = Mathf.FloorToInt(gridY);
        int x1 = Mathf.Min(x0 + 1, gridSize.x - 1);
        int y1 = Mathf.Min(y0 + 1, gridSize.y - 1);

        float tx = gridX - x0; // fractional offset
        float ty = gridY - y0;

        // Sample flow vectors from the 4 neighbors
        Vector2 v00 = grid[x0, y0].bestDirection.Vector;
        Vector2 v10 = grid[x1, y0].bestDirection.Vector;
        Vector2 v01 = grid[x0, y1].bestDirection.Vector;
        Vector2 v11 = grid[x1, y1].bestDirection.Vector;

        // Bilinear interpolation
        Vector2 v0 = Vector2.Lerp(v00, v10, tx);
        Vector2 v1 = Vector2.Lerp(v01, v11, tx);
        Vector2 blended = Vector2.Lerp(v0, v1, ty);

        return blended.normalized;
    }
}
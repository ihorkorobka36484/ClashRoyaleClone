using UnityEngine;
 
public class Cell
{
    public Vector3 position;
    public Vector2Int gridIndex;
    public byte cost;
    public ushort bestCost;
    public GridDirection bestDirection;
 
    public Cell(Vector3 _position, Vector2Int _gridIndex)
    {
        position = _position;
        gridIndex = _gridIndex;
        cost = 1;
        bestCost = ushort.MaxValue;
        bestDirection = GridDirection.None;
    }

    public void ResetCosts()
    {
        cost = 1;
        bestCost = ushort.MaxValue;
        bestDirection = GridDirection.None;
    }
 
    public void IncreaseCost(int amount)
    {
        if (cost == byte.MaxValue) { return; }
        if (amount + cost >= 255) { cost = byte.MaxValue; }
        else { cost += (byte)amount; }
    }
}
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
 
public class GridController : MonoBehaviour
{
    public Vector2Int gridSize;
    public float cellRadius = 0.5f;
    public FlowField[] baseFlowFields = new FlowField[2];
    public Dictionary<Cell, FlowField> localFlowFields = new Dictionary<Cell, FlowField>();
    public GridDebug gridDebug;
    [SerializeField]
    private RecalculateFieldsController recalculateFieldsController;

    public RecalculateFieldsController RecalculateFieldsController => recalculateFieldsController;

    private HashSet<GameObject> localObjects = new HashSet<GameObject>();
    private Vector2Int localGridSize = new Vector2Int(40, 40);
    
    public FlowField GetFlowFieldByLocalObject(GameObject obj)
    {
        Cell candidateCell = baseFlowFields[0].GetCellFromWorldPos(obj.transform.position);
        if (localFlowFields.ContainsKey(candidateCell))
        {
            return localFlowFields[candidateCell];
        }
        return null;
    }

    public void SetLocalGridSize(Vector2Int size)
    {
        localGridSize = size;
    }

    public void AddLocalObject(GameObject obj)
    {
        localObjects.Add(obj);
    }

    public void RemoveLocalObject(GameObject obj)
    {
        localObjects.Remove(obj);
    }

    public void CheckLocalFields()
    {
        foreach (var field in localFlowFields.Values)
        {
            field.objectsToIgnore.Clear();
        }

        HashSet<Cell> fieldsToKeep = new HashSet<Cell>();
        foreach (GameObject obj in localObjects)
        {
            Cell candidateCell = baseFlowFields[0].GetCellFromWorldPos(obj.transform.position);
            if (!localFlowFields.ContainsKey(candidateCell))
            {
                InitializeLocalFlowField(obj.transform.position, true);
            }
            fieldsToKeep.Add(candidateCell);
            localFlowFields[candidateCell].objectsToIgnore.Add(obj);
        }
        List<Cell> keysToRemove = localFlowFields.Keys.Where(k => !fieldsToKeep.Contains(k)).ToList();
        foreach (var key in keysToRemove)
        {
            localFlowFields.Remove(key);
        }
        foreach (var value in localFlowFields.Values.ToList())
        {
            UpdateLocalFlowField(value);
        }
    }

    public void RecalculateFlowFields(HashSet<GameObject> objectsToCheck)
    {
        for (int i = 0; i < baseFlowFields.Length; i++)
        {
            if (baseFlowFields[i] != null)
            {
                Vector3 destination = baseFlowFields[i].GetWorldPosFromCell(baseFlowFields[i].destinationCell);
                CreateGlobalFlowField(i, destination);
            }
        }

        CheckLocalFields();
    }

    public void UpdateLocalFlowField(FlowField localFlowField)
    {
        localFlowField.CreateCostField();
        localFlowField.CreateIntegrationField(localFlowField.destinationCell);
        localFlowField.CreateFlowField();
    }

    public void InitializeLocalFlowField(Vector3 centerPos, bool setDubug = false)
    {
        Cell centerCell = baseFlowFields[0].GetCellFromWorldPos(centerPos);
        if (localFlowFields.ContainsKey(centerCell))
        {
            return;
        }
        
        FlowField localFlowField = new FlowField(cellRadius, localGridSize, centerPos);
        localFlowField.CreateGrid();
        Cell destinationCell = localFlowField.GetCellFromWorldPos(centerPos);
        localFlowField.CreateCostField();
        localFlowField.CreateIntegrationField(destinationCell);
        localFlowFields.Add(centerCell, localFlowField);

        if (setDubug)
        {
            gridDebug.SetFlowField(localFlowField);
        }
    }

    public void CreateGlobalFlowField(int side, Vector3 worldPos)
    {
        if (baseFlowFields[side] == null)
            InitializeFlowField(side);
        baseFlowFields[side].CreateCostField();
        Cell destinationCell = baseFlowFields[side].GetCellFromWorldPos(worldPos);
        baseFlowFields[side].CreateIntegrationField(destinationCell);
        baseFlowFields[side].CreateFlowField();
    }
 
    private void InitializeFlowField(int side)
    {
        baseFlowFields[side] = new FlowField(cellRadius, gridSize);
        baseFlowFields[side].CreateGrid();
        baseFlowFields[side].SetOriginPos(transform.position);
        if (side == 1)
            gridDebug.SetFlowField(baseFlowFields[side]);
    }
 
    private void Update()
    {
        if (baseFlowFields[0] != null)
        {
            baseFlowFields[0].SetOriginPos(transform.position);
            baseFlowFields[1].SetOriginPos(transform.position);
        }

        gridDebug.DrawFlowField();
        // if (Input.GetMouseButtonDown(0))
        // {
        //     InitializeFlowField();

        //     curFlowField.CreateCostField();

        //     float z = Mathf.Abs(Camera.main.transform.position.y - transform.position.y);
        //     Vector3 mousePos = new Vector3(Input.mousePosition.x, Input.mousePosition.y, z);
        //     Vector3 worldMousePos = Camera.main.ScreenToWorldPoint(mousePos);
        //     Cell destinationCell = curFlowField.GetCellFromWorldPos(worldMousePos);
        //     curFlowField.CreateIntegrationField(destinationCell);

        //     curFlowField.CreateFlowField();

        //     gridDebug.DrawFlowField();
        // }
    }
}
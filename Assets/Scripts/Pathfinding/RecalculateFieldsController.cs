using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RecalculateFieldsController : MonoBehaviour
{
    [SerializeField]
    float frequency = 0.2f;
    [SerializeField]
    GridController gridController;

    private HashSet<GameObject> objectsToCheck = new HashSet<GameObject>();
    private float timePassed = 0f;

    public void AddObjectToCheck(GameObject obj)
    {
        objectsToCheck.Add(obj);
    }

    private void Update()
    {
        timePassed += Time.deltaTime;
        if (timePassed >= frequency)
        {
            RecalculateFields();
            timePassed = 0f;
        }
    }

    private void RecalculateFields()
    {
        gridController.RecalculateFlowFields(objectsToCheck);
    }
}
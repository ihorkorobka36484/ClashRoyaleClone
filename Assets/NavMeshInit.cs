using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshSurface))]
public class NavMeshInit : MonoBehaviour
{
    [SerializeField]
    private NavMeshData myNavMeshData;
    
    void Awake()
    {
        // Assign your NavMeshData to the surface
        NavMeshSurface surface = GetComponent<NavMeshSurface>();
        if (myNavMeshData != null)
        {
            surface.navMeshData = myNavMeshData; // myNavMeshData is a NavMeshData object
        }
        else
        {
            Debug.LogError("NavMeshData is not assigned in NavMeshInit.");
        }
    }
}

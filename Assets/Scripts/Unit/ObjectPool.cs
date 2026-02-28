using System.Collections.Generic;
using UnityEngine;
using Units;
using Unity.Netcode;
using UnityEngine.Assertions;

public class ObjectPool : MonoBehaviour
{
    // Singleton instance
    public static ObjectPool Instance { get; private set; }

    // Dictionary to hold pools for different object types
    private Dictionary<string, List<GameObject>> pools = new Dictionary<string, List<GameObject>>();

    private void Awake()
    {
        // Ensure only one instance of ObjectPool exists
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogWarning("Multiple ObjectPool instances detected. Destroying duplicate.");
            Destroy(gameObject);
        }
    }

    // Retrieve an object from the pool
    public GameObject GetObject(GameObject prefab, bool setActive = true)
    {
        if (prefab.GetComponent<NetworkObject>() != null)
        {
            GameObject go = NetworkObjectPool.Singleton.GetNetworkObject(prefab, Vector3.zero, Quaternion.identity).gameObject;
            go.SetActive(setActive);
            return go;
        }

        string key = prefab.name;

        // Ensure the pool for this prefab exists
        if (!pools.ContainsKey(key))
        {
            pools[key] = new List<GameObject>();
        }

        if (pools[key].Count != 0)
        {
            GameObject obj = pools[key][0];
            obj.SetActive(setActive);
            pools[key].Remove(obj);
            return obj;
        }

        // If no inactive object is found, create a new one
        GameObject newObj = Instantiate(prefab);
        newObj.name = prefab.name; // Ensure the name matches the prefab
        newObj.SetActive(setActive);
        
        return newObj;
    }

    // Return an object to the pool
    public void ReturnObject(GameObject obj)
    {
        NetworkObject networkObject = obj.GetComponent<NetworkObject>();
        obj.SetActive(false);
        
        if (obj.GetComponent<NetworkObject>() != null && NetworkManager.Singleton.IsHost && networkObject.IsSpawned)
        {
            networkObject.Despawn();
            return;
        }
        Assert.IsNotNull(obj, "Returned object is null");
        string key = obj.name;
        Assert.IsNotNull(pools[key], "No pool for object: " + key);
        pools[key].Add(obj);
    }
}
// Purpose: This script is used to create a pool of bullets that can be used by the player to shoot at the enemies. It creates a pool of 100 bullets and reuses them when the player shoots.
using UnityEngine;

public class BulletFactory : MonoBehaviour
{
    [SerializeField]
    private GameObject bulletPrefab;
    [SerializeField]
    private Transform parent;

    public static BulletFactory Instance { get; private set; }

    private GameObject[] bullets = new GameObject[100];
    private int currentBullet = -1;
    

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogWarning("Multiple BulletFactory instances detected. Destroying duplicate.");
            Destroy(gameObject);
            return;
        }

        for (int i = 0; i < bullets.Length; i++)
        {
            bullets[i] = Instantiate(bulletPrefab);
            bullets[i].transform.position = Vector3.zero;
            bullets[i].transform.rotation = Quaternion.identity;
            bullets[i].transform.SetParent(parent, false);
            bullets[i].SetActive(false);
        }
    }

    public GameObject Get()
    {
        if (currentBullet == bullets.Length - 1)
            currentBullet = 0;
        else
            currentBullet++;
        return bullets[currentBullet];
    }
}

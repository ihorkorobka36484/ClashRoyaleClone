using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using Units;
using System.Collections.Generic;
using UnityEngine.Assertions;
using System;

public class ElixirManager : NetworkBehaviour
{
    public static ElixirManager Instance { get; private set; }

    [SerializeField]
    private ProgressBar progressBar;
    [SerializeField]
    private TextMeshProUGUI currentValueText;
    [SerializeField]
    private TextMeshProUGUI maxValueText;
    [SerializeField]
    private int maxValue = 10;
    [SerializeField]
    private float changeSpeed = 1.0f;
    [SerializeField]
    private float[] values = new float[System.Enum.GetValues(typeof(Sides)).Length];

    public bool SpawnLock => spawnLock > 0;

    private NetworkVariable<float> enemyValue = new();
    private UnityAction<float, Sides> onValueChanged;
    private bool initialized = false;
    private int spawnLock = 0;

    public void Initialize(
        ProgressBar progressBar,
        TextMeshProUGUI currentValueText,
        TextMeshProUGUI maxValueText,
        UnityAction<float, Sides> onValueChanged)
    {
        this.progressBar = progressBar;
        this.currentValueText = currentValueText;
        this.maxValueText = maxValueText;
        this.onValueChanged = onValueChanged;

        progressBar.Init(maxValue);
        maxValueText.text = "Max: " + maxValue.ToString();

        if (!IsHost)
            return;

        UpdateAllValues();
        initialized = true;
    }

    public override void OnNetworkSpawn()
    {
        Factory.Instance.InitializeElixirManager();
        
        if (!IsHost)
        {
            gameObject.SetActive(false);
            enemyValue.OnValueChanged += SetEnemyValue;
            SetEnemyValue(enemyValue.Value, enemyValue.Value);
        }
    }

    public override void OnNetworkDespawn()
    {
        if (!IsHost)
        {
            enemyValue.OnValueChanged -= SetEnemyValue;
        }
    }

    public int GetValue(Sides side)
    {
        return (int)GetFloatValue(side);
    }

    public float GetFloatValue(Sides side)
    {
        float value = values[(int)side];
        return value;
    }

    public void ChangeValue(int change, Sides side)
    {
        UpdateValue(values[(int)side] + change, side);

        if (IsHost && side == Sides.Enemy)
        {
            DecreaseSpawnLockClientRpc();
        }
    }

    public void UpdateSpawnLock(int change)
    {
        if (IsHost)
            return;

        spawnLock += change;
        print("Spawn lock updated: " + spawnLock);
        Assert.IsTrue(spawnLock >= 0, "Spawn lock cannot be negative.");
        UpdateAllValues();
    }

    [ClientRpc]
    private void DecreaseSpawnLockClientRpc()
    {
        UpdateSpawnLock(-1);
    }


    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogWarning("Multiple ElixirManager instances detected. Destroying duplicate.");
            Destroy(gameObject);
        }
    }

    private void UpdateValue(float newValue, Sides side)
    {
        Sides playerSide = NetworkManager.Singleton.IsHost ? Sides.Player : Sides.Enemy;
        values[(int)side] = Mathf.Clamp(newValue, 0, maxValue);
        
        if (side == Sides.Enemy)
        {
            enemyValue.Value = values[(int)side];
        }
        if (side == playerSide)
        {
            progressBar.SetFillAmount(GetFloatValue(side) / maxValue * 100f);
            currentValueText.text = GetValue(side).ToString();
        }
                
        onValueChanged?.Invoke(values[(int)side], side);
    }

    private void UpdateAllValues()
    {
        for (int i = 0; i < values.Length; i++)
        {
            UpdateValue(values[i], (Sides)i);
        }
    }
    
    private void SetEnemyValue(float previousValue, float newValue)
    {
        UpdateValue(newValue, Sides.Enemy);
    }

    private void Update()
    {
        if (!initialized)
            return;

        for (int i = 0; i < values.Length; i++)
        {
            UpdateValue(values[i] + changeSpeed * Time.deltaTime, (Sides)i);
        }
    }
}
using System;
using Units;
using Unity.Netcode;

public class WinConditionChecker : NetworkBehaviour
{
    public Action<Sides> OnWinConditionMet;
    public static WinConditionChecker Instance { get; private set; }

    private bool gameEnded = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void Init()
    {
        if (!IsHost)
            return;

        for (int i = 0; i < UnitSpawner.Instance.Bases.Count; i++)
        {
            UnitSpawner.Instance.Bases[i].OnDeath += CheckWinCondition;
        }
    }

    private void CheckWinCondition(Unit baseUnit)
    {
        if (gameEnded)
            return;

        OnWinConditionMet?.Invoke((Sides)baseUnit.Team);
        SendEndMatchClientRpc(baseUnit.Team);
        gameEnded = true;
    }

    [ClientRpc]
    void SendEndMatchClientRpc(int losingTeam)
    {
        if (IsHost)
            return;     

        OnWinConditionMet?.Invoke((Sides)losingTeam);
    }
}

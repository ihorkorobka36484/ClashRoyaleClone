using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using UnityEngine;

public class RelayManager : MonoBehaviour
{
    private UnityTransport unityTransport;

    public static RelayManager Instance;
    public Action<string> OnRelayCreated;
    public Action OnRelayJoined;

    void Awake()
    {
        Instance = this;
        unityTransport = GetComponentInChildren<UnityTransport>();
    }

    public async Task<string> CreateRelay()
    {
        SwitchUnityTransport();

        var allocation = await RelayService.Instance.CreateAllocationAsync(2);
        string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

        unityTransport.SetHostRelayData(
            allocation.RelayServer.IpV4,
            (ushort)allocation.RelayServer.Port,
            allocation.AllocationIdBytes,
            allocation.Key,
            allocation.ConnectionData);

        OnRelayCreated?.Invoke(joinCode);

        Debug.Log("Relay created with ID: " + allocation.AllocationId);
        Debug.Log("Join code: " + joinCode);

        return joinCode;
    }
    public async Task JoinRelay(string joinCode)
    {
        SwitchUnityTransport();

        var joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
        Debug.Log("Joined relay with ID: " + joinAllocation.AllocationId);

        unityTransport.SetClientRelayData(
            joinAllocation.RelayServer.IpV4,
            (ushort)joinAllocation.RelayServer.Port,
            joinAllocation.AllocationIdBytes,
            joinAllocation.Key,
            joinAllocation.ConnectionData,
            joinAllocation.HostConnectionData);
        
        OnRelayJoined?.Invoke();
    }

    private void SwitchUnityTransport()
    {
        NetworkManager.Singleton.GetComponent<UnityTransport>().enabled = false;
        NetworkManager.Singleton.NetworkConfig.NetworkTransport = unityTransport;
        NetworkManager.Singleton.GetComponent<UnityTransport>().enabled = true;
    }
}

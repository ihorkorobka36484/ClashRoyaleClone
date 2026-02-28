using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

namespace Units
{
    [RequireComponent(typeof(Ranged))]
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(NavMeshObstacle))]
    public class NetworkRanged : NetworkUnit
    {
        private NetworkVariable<bool> arrowActive = new();
        private Ranged ranged;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            ranged = GetComponent<Ranged>();

            if (IsOwner)
            {
                ranged.OnArrowLaunch += LaunchArrowRpcCallback;
                ranged.OnArrowSetActive += SetArrowActiveNetworkVar;
            }
            else if (IsClient)
            {
                arrowActive.OnValueChanged += SetArrowActive;
            }

        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            if (IsOwner)
            {
                ranged.OnArrowLaunch -= LaunchArrowClientRpc;
                ranged.OnArrowSetActive -= SetArrowActiveNetworkVar;
            }
            else if (IsClient)
            {
                arrowActive.OnValueChanged -= SetArrowActive;
            }
        }

        public void LaunchArrowRpcCallback(Vector3 targetPosition, float targetSize)
        {
            if (IsOwner)
                LaunchArrowClientRpc(targetPosition, targetSize);
        }

        [ClientRpc]
        private void LaunchArrowClientRpc(Vector3 targetPosition, float targetSize)
        {
            if (IsOwner)
                return;
            ranged.LaunchArrow(targetPosition, targetSize);
        }

        private void SetArrowActiveNetworkVar(bool active)
        {
            arrowActive.Value = active;
        }

        private void SetArrowActive(bool _, bool active)
        {
            ranged.SetArrowActive(active);
        }
    }
}

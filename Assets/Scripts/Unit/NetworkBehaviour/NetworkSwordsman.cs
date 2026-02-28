using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

namespace Units
{
    [RequireComponent(typeof(Swordsman))]
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(NavMeshObstacle))]
    public class NetworkSwordsman : NetworkUnit
    {
        private NetworkVariable<bool> swordActive = new();
        private Swordsman swordsman;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            swordsman = GetComponent<Swordsman>();
            if (IsOwner)
                swordsman.OnSetSwordActive += SetSwordActiveNetworkVar;
            else if (IsClient)
                swordActive.OnValueChanged += SetSwordActive;

        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            if (IsOwner)
                swordsman.OnSetSwordActive -= SetSwordActiveNetworkVar;
            else if (IsClient)
                swordActive.OnValueChanged -= SetSwordActive;
        }

        private void SetSwordActiveNetworkVar(bool active)
        {
            swordActive.Value = active;
        }

        private void SetSwordActive(bool _, bool active)
        {
            swordsman.SetSwordActive(active);
        }
    }
}

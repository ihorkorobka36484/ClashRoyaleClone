using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.AI;

namespace Units
{
    [RequireComponent(typeof(Unit))]
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(NavMeshObstacle))]
    public class NetworkUnit : NetworkBehaviour
    {
        [SerializeField]
        bool spawnAnimation = true;
        [SerializeField]
        Unit unit;

        public NetworkVariable<int> index = new();

        private NetworkVariable<float> emissionStrength = new();
        private NetworkVariable<int> hp = new();
        private NetworkVariable<Color> damageColor = new();
        private NetworkVariable<int> team = new();

        private ProgressBar progressBar;

        public override void OnNetworkSpawn()
        {
            if (unit == null)
                unit = GetComponent<Unit>();

            if (IsOwner)
            {
                unit.OnTeamSet += SetTeamNetworkVar;
                unit.OnEmissionStrengthSet += SetEmissionStrengthNetworkVar;
                unit.OnDamageColorSet += SetDamageColorNetworkVar;
                unit.OnHealthChanged += SetHPNetworkVar;
                index.OnValueChanged += Test;
            }
            else if (IsClient)
            {
                emissionStrength.OnValueChanged += SetEmissionStrength;
                damageColor.OnValueChanged += SetDamageColor;
                index.OnValueChanged += RemoveClientCopy;
                hp.OnValueChanged += SetProgressBarValue;
                team.OnValueChanged += SetTeamColor;

                unit.enabled = false;
                GetComponent<NetworkTransform>().Interpolate = false;
                GetComponent<NavMeshAgent>().enabled = false;
                GetComponent<NavMeshObstacle>().enabled = false;
                SetEmissionStrength(emissionStrength.Value, emissionStrength.Value);

                progressBar = ProgressBarManager.Instance.CreateProgressBar(unit);
                progressBar?.gameObject.SetActive(unit is Base);
                SetTeamColor(0, team.Value);
            }
        }

        public override void OnNetworkDespawn()
        {
            if (IsOwner)
            {
                unit.OnTeamSet -= SetTeamNetworkVar;
                unit.OnEmissionStrengthSet -= SetEmissionStrengthNetworkVar;
                unit.OnDamageColorSet -= SetDamageColorNetworkVar;
                unit.OnHealthChanged -= SetHPNetworkVar;
            }
            else if (IsClient)
            {
                emissionStrength.OnValueChanged -= SetEmissionStrength;
                index.OnValueChanged -= RemoveClientCopy;
                damageColor.OnValueChanged -= SetDamageColor;
                hp.OnValueChanged -= SetProgressBarValue;
                team.OnValueChanged -= SetTeamColor;
            }
        }

        public void StartSpawnAnimation()
        {
            StartSpawnAnimationClientRpc(transform.localPosition);
        }

        [ClientRpc]
        private void StartSpawnAnimationClientRpc(Vector3 position)
        {
            if (IsOwner)
                return;

            if (spawnAnimation)
            {
                UnitSpawner.Instance.StartParticlesOnlySpawnAnimation(position, transform.parent, unit.Data.Size, () =>
                {
                    GetComponent<NetworkTransform>().Interpolate = true;
                    progressBar.gameObject.SetActive(true);
                });
            }
        }

        void RemoveClientCopy(int _, int index)
        {
            UnitSpawner.Instance.RemoveClientCopy(index);
        }

        void Test(int _, int index)
        {
            unit.indexTest = index;
        }

        void SetEmissionStrengthNetworkVar(float value)
        {
            emissionStrength.Value = value;
        }

        void SetEmissionStrength(float _, float value)
        {
            unit.SetEmissionStrength(value);
        }

        void SetDamageColorNetworkVar(Color color)
        {
            damageColor.Value = color;
        }

        void SetDamageColor(Color _, Color color)
        {
            //unit.SetDamageColor(color);
        }

        void SetProgressBarValue(int _, int hp)
        {
            float fillAmount = (float)hp / (float)unit.Data.MaxHealth * 100f;
            progressBar?.SetFillAmount(fillAmount);
            if (hp <= 0)
            {
                ProgressBarManager.Instance.RemoveProgressBar(unit);
            }
        }

        void SetHPNetworkVar(Unit unit)
        {
            this.hp.Value = unit.Health;
        }

        void SetTeamNetworkVar(int team)
        {
            this.team.Value = team;
        }

        void SetTeamColor(int _, int team)
        {
            unit.Team = team;
            unit.SetTeamColor(UnitSpawner.Instance.TeamColors[team]);
            progressBar?.ChangeColors(progressBar.backgroundColor, UnitSpawner.Instance.TeamColors[team]);
            progressBar?.SetFillAmount(100f);
        }
    }
}

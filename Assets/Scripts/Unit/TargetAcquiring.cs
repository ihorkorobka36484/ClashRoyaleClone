using System.Collections.Generic;
using Units;
using UnityEngine;

public class TargetAcquiring : MonoBehaviour
{
    [SerializeField]
    private List<Unit> agents;
    [SerializeField]
    private GridController gridController;
    // How often acquiring happens in seconds
    [SerializeField]
    private float frequency = 0.2f;
    [SerializeField]
    [Tooltip("If a new possible target is closer than this value, it will be set as a target.")]
    private float distanceDifferenceToChangeTarget = 2f;
    [SerializeField]
    private ObjectPool objectPool;

    public int testIndex = -1;

    private float timePassed = 0;
    private List<Unit> toRemove = new();
    bool removeLock;

    public void AddUnit(Unit unit) {
        agents.Add(unit);
        unit.OnDeath += RemoveUnit;
    }

    public void RemoveUnit(Unit unit) {
        if (removeLock)
            toRemove.Add(unit);
        else {
            agents.Remove(unit);
        }
    }

    private void Run() {
        removeLock = true;
        for (int i = 0; i < agents.Count; i++)
        {
            Unit agent = agents[i];

            if (agent.IsDead) continue;

            Unit closestEnemy = null;
            float closestSqrDistance = float.MaxValue;
            float currentEnemySqrDistance = float.MaxValue;
            if (agent.HasTarget && agent.Target != UnitSpawner.Instance.Bases[agent.Team]) {
                currentEnemySqrDistance = ((agent.transform.position - agent.Target.transform.position) / gridController.cellRadius).sqrMagnitude;
            }

            int team = agent.Team;

            if (agent.AllowedTargets != AllowedTargets.Base)
            {
                for (int j = 0; j < agents.Count; j++)
                {
                    if (agents[j].IsDead) continue;
                    if (i != j && team != agents[j].Team)
                    {
                        float sqrDistance = ((agent.transform.position - agents[j].transform.position) / gridController.cellRadius).sqrMagnitude;
                        if (sqrDistance < closestSqrDistance)
                        {
                            closestSqrDistance = sqrDistance;
                            closestEnemy = agents[j];
                        }
                    }

                    if (closestEnemy != null && agent.Target != closestEnemy && closestSqrDistance < currentEnemySqrDistance)
                    {
                        CheckAndSetTarget(agent, closestEnemy, closestSqrDistance, currentEnemySqrDistance == float.MaxValue ? 0 : distanceDifferenceToChangeTarget);
                    }
                }
            }
            
            if (!agent.HasTarget)
            {
                CheckAndSetTarget(agent, UnitSpawner.Instance.Bases[agent.Team], 0);
            }
        }
        // Units shouldn't be removed while we are iterating through them.
        removeLock = false;
        for (int i = 0; i < toRemove.Count; i++) {
            agents.Remove(toRemove[i]);
        }
        toRemove.Clear();
    }

    private void CheckAndSetTarget(Unit attacker, Unit possibleEnemy, float sqrDistance, float additionalDistanceAdjustment = 0)
    {
        float adjustedNoticeRange = (attacker.Data.AttackNoticeRange + possibleEnemy.Radius - additionalDistanceAdjustment) / gridController.cellRadius;
        //bool ignoreNoticeRange = attacker.HasTarget && attacker.Target is Base && !attacker.HasPathToTarget;
        if (/*ignoreNoticeRange ||*/ adjustedNoticeRange * adjustedNoticeRange > sqrDistance)
        {
            attacker.SetAttackTarget(possibleEnemy, possibleEnemy is not Base);
        }
    }

    void Update()
    {
        timePassed += Time.deltaTime;
        if (timePassed > frequency) 
        {
            timePassed -= frequency;
            Run();
        }
    }
}

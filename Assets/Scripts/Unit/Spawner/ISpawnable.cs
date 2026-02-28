using System;
using UnityEngine;
using UnityEngine.Events;

namespace Units
{
    public interface ISpawnable
    {
        void Init(Transform destination, int team, UnityAction<Unit, bool> StartMovementAction, UnityAction<Unit, bool> OnAttackStatusChanged, UnityAction<Unit> OnAttackTargetSet) { }
        void SetTeam(Sides team) { }
        void SetCopyMode(bool enabled) { }
        void PerformActionForEachUnit(Action<Unit> Action) { }
        void SetParentForUnits(Transform parent) { }
        void Release(bool destroyChildren) { }
        float baseOffset { get; }
        UnitData Data { get; }
        GameObject GetGameObject();
    }
}

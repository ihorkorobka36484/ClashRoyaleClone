using UnityEngine;

namespace Units{
    public class MiniSkeleton : Melee
    {
        [SerializeField]
        SpawnGroup spawnable;
        
        public override ISpawnable Spawnable => spawnable;
    }
}

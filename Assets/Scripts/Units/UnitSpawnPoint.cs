using UnityEngine;

namespace Units
{
    public class UnitSpawnPoint : SpawnPoint
    {
        protected override bool ShouldProcessCollision(Collider other)
        {
            return other.TryGetComponent<Unit>(out _);
        }
    }
}
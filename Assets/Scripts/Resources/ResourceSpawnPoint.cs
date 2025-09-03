using UnityEngine;

public class ResourceSpawnPoint : SpawnPoint
{
   protected override bool ShouldProcessCollision(Collider other)
   {
      return other.TryGetComponent<Resource>(out _);
   }
}

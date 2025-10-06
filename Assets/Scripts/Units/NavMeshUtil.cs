using UnityEngine;

public class NavMeshUtil
{
    public static bool TrySnapToNavMesh(Vector3 position, float maxDistance, out Vector3 snapped)
    {
        if (UnityEngine.AI.NavMesh.SamplePosition(position, out var hit, maxDistance, UnityEngine.AI.NavMesh.AllAreas))
        {
            snapped = hit.position;
            
            return true;
        }

        snapped = position;
        
        return false;
    }
}

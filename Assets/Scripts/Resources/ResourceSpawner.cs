using UnityEngine;

public class ResourceSpawner : Spawner<Resource>
{
    protected override void InitializeItem(Resource resource, Vector3 position)
    {
        resource.Init(position);
        SetupItemEvents(resource);
    }

    protected override void SetupItemEvents(Resource resource)
    {
        resource.TriggeredBaseEnter += Release;
    }

    protected override void CleanupItemEvents(Resource resource)
    {
        resource.TriggeredBaseEnter -= Release;
    }

    private void Release(Resource resource)
    {
        CleanupItemEvents(resource);
        _pool.Release(resource);
    }
}
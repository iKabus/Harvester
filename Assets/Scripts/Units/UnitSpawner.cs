using UnityEngine;

public class UnitSpawner : Spawner<Unit>
{
    protected override void InitializeItem(Unit unit, Vector3 position)
    {
        unit.Init(position);
        SetupItemEvents(unit);
    }

    protected override void SetupItemEvents(Unit unit)
    {
    }

    protected override void CleanupItemEvents(Unit unit)
    {
        
    }

    private void Release(Unit unit)
    {
        CleanupItemEvents(unit);
        _pool.Release(unit);
    }
}
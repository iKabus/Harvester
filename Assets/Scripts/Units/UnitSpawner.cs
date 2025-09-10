using UnityEngine;

public class UnitSpawner : Spawner<Unit>
{
    private int _maxCount = 5;
    
    protected override void InitializeItem(Unit unit, Vector3 position)
    {
        unit.Init(position); 
        SetupItemEvents(unit);
    }

    protected override void TrySpawn()
    {
        if (_pool.CountActive >= _maxCount)
            return;

        base.TrySpawn();
    }

    protected override void SetupItemEvents(Unit unit)
    {
    }

    protected override void CleanupItemEvents(Unit unit)
    {
        
    }
}
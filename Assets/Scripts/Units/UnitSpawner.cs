using UnityEngine;

public class UnitSpawner : Spawner<Unit>
{
    [SerializeField] private int _maxCount = 5;
    
    protected override void InitializeItem(Unit unit, Vector3 position)
    {
        unit.Init(position); 
    }

    protected override bool TrySpawn()
    {
        if (ActiveCount >= _maxCount)
        {
            return false;
        }
        
        return base.TrySpawn();
    }

    protected override void SetupItemEvents(Unit unit)
    {
    }

    protected override void CleanupItemEvents(Unit unit)
    {
        
    }
}
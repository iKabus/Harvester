using System;
using UnityEngine;

public class Resource : MonoBehaviour
{
    public event Action<Resource> TriggeredBaseEnter;

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<Base>(out _))
        {
            TriggeredBaseEnter?.Invoke(this);
        }
    }
    
    public void Init(Vector3 position)
    {
        transform.position = position;
    }
}

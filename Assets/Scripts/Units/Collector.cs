using System;
using System.Collections;
using UnityEngine;

public class Collector
{
    public IEnumerator CollectRoutine(
        float duration,
        Func<bool> isResourceMissing,
        Func<bool> isOutOfRange,
        Func<IEnumerator> onReacquire)
    {
        float time = 0f;

        while (time < duration)
        {
            if (isResourceMissing())
                yield break;

            if (isOutOfRange())
            {
                if (onReacquire != null)
                    yield return onReacquire.Invoke();

                time = 0f;
                continue;
            }

            time += Time.deltaTime;
            yield return null;
        }
    }
}

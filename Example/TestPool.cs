using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestPool : MonoBehaviour
{
    public Wrj.ObjectPool objectPool;
    public Wrj.RandomizedSoundEffect soundEffect;
    private Coroutine prefabGenerationRoutine;
    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            prefabGenerationRoutine = StartCoroutine(MakePrefabs());
        }
        else if (Input.GetKeyUp(KeyCode.Space))
        {
            StopCoroutine(prefabGenerationRoutine);
        }
    }

    private IEnumerator MakePrefabs()
    {
        while (true)
        {
            objectPool.GetObject();
            soundEffect.PlayRandom(.5f, 1f);
            yield return new WaitForSeconds(.1f);
        }
    }
}

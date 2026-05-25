using System.Collections;
using UnityEngine;
using UnityEngine.VFX;

public class PooledAutoReturn : MonoBehaviour, IPoolable
{
    [SerializeField] private float lifetime = 1f;
    [SerializeField] private bool autoReturnOnSpawn;
    [SerializeField] private bool replayVisualsOnSpawn = true;

    private Coroutine returnRoutine;

    public void Configure(float _lifetime, bool _autoReturn)
    {
        lifetime = Mathf.Max(0.01f, _lifetime);
        autoReturnOnSpawn = _autoReturn;

        if (isActiveAndEnabled)
            RestartReturnRoutine();
    }

    public void OnSpawnFromPool()
    {
        if (replayVisualsOnSpawn)
            PlayVisuals();

        RestartReturnRoutine();
    }

    public void OnReturnToPool()
    {
        StopReturnRoutine();
        StopVisuals();
        autoReturnOnSpawn = false;
    }

    private void RestartReturnRoutine()
    {
        StopReturnRoutine();

        if (autoReturnOnSpawn)
            returnRoutine = StartCoroutine(ReturnAfterLifetime());
    }

    private IEnumerator ReturnAfterLifetime()
    {
        yield return new WaitForSeconds(lifetime);

        if (!PoolManager.TryReturn(gameObject))
            Destroy(gameObject);
    }

    private void StopReturnRoutine()
    {
        if (returnRoutine == null)
            return;

        StopCoroutine(returnRoutine);
        returnRoutine = null;
    }

    private void PlayVisuals()
    {
        VisualEffect[] visualEffects = GetComponentsInChildren<VisualEffect>(true);

        for (int i = 0; i < visualEffects.Length; i++)
        {
            visualEffects[i].Reinit();
            visualEffects[i].Play();
        }

        ParticleSystem[] particleSystems = GetComponentsInChildren<ParticleSystem>(true);

        for (int i = 0; i < particleSystems.Length; i++)
        {
            particleSystems[i].Clear(true);
            particleSystems[i].Play(true);
        }
    }

    private void StopVisuals()
    {
        VisualEffect[] visualEffects = GetComponentsInChildren<VisualEffect>(true);

        for (int i = 0; i < visualEffects.Length; i++)
            visualEffects[i].Stop();

        ParticleSystem[] particleSystems = GetComponentsInChildren<ParticleSystem>(true);

        for (int i = 0; i < particleSystems.Length; i++)
            particleSystems[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }
}

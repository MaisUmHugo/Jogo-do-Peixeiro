using System;
using System.Collections;
using UnityEngine;

public class FishBiteHandler : MonoBehaviour
{
    [Header("Bite Delay")]
    [SerializeField] private float _minBiteDelay = 1.5f;
    [SerializeField] private float _maxBiteDelay = 4f;

    private Coroutine _biteRoutine;

    public bool IsWaitingForBite { get; private set; }

    public void StartWaiting(Action _onBite)
    {
        StartWaiting(_onBite, 1f);
    }

    public void StartWaiting(Action _onBite, float _delayMultiplier)
    {
        StopWaiting();

        if (_onBite == null)
            return;

        _biteRoutine = StartCoroutine(WaitForBiteRoutine(_onBite, _delayMultiplier));
    }

    public void StopWaiting()
    {
        if (_biteRoutine != null)
        {
            StopCoroutine(_biteRoutine);
            _biteRoutine = null;
        }

        IsWaitingForBite = false;
    }

    private IEnumerator WaitForBiteRoutine(Action _onBite, float _delayMultiplier)
    {
        IsWaitingForBite = true;

        float delay = UnityEngine.Random.Range(_minBiteDelay, _maxBiteDelay);
        delay *= Mathf.Max(0.1f, _delayMultiplier);
        yield return new WaitForSeconds(delay);

        IsWaitingForBite = false;
        _biteRoutine = null;

        _onBite.Invoke();
    }
}

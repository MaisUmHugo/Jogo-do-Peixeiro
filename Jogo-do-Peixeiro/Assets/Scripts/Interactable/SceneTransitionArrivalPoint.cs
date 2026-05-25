using System.Collections;
using UnityEngine;

public class SceneTransitionArrivalPoint : MonoBehaviour
{
    [Header("Id")]
    [SerializeField] private string arrivalPointId = "Default";
    [SerializeField] private bool applyWhenNoPendingArrival;
    [SerializeField] private bool clearPendingArrivalAfterApply = true;

    [Header("Placement")]
    [SerializeField] private Transform boatArrivalPoint;
    [SerializeField] private Transform playerArrivalPoint;
    [SerializeField] private bool putPlayerOnBoat = true;
    [SerializeField, Min(0)] private int applyAfterFrames = 2;

    [Header("Fade")]
    [SerializeField] private bool fadeInAfterArrival = true;
    [SerializeField, Min(0f)] private float fadeInDelay = 0.05f;
    [SerializeField, Min(0f)] private float fadeInDuration = 0.55f;

    public string ArrivalPointId => arrivalPointId;

    private IEnumerator Start()
    {
        for (int i = 0; i < applyAfterFrames; i++)
            yield return null;

        bool hasPendingArrival = SceneTransitionRequest.TryGetPendingArrivalId(out string pendingArrivalId);

        if (!hasPendingArrival && !applyWhenNoPendingArrival)
            yield break;

        if (hasPendingArrival && pendingArrivalId != arrivalPointId)
            yield break;

        TryApplyArrival(true);

        if (hasPendingArrival && clearPendingArrivalAfterApply)
            SceneTransitionRequest.ClearPendingArrival();
    }

    public bool TryApplyArrival(bool _playFadeIn)
    {
        bool applied = ApplyArrival();

        if (applied && _playFadeIn && fadeInAfterArrival)
            SceneTransitionFadeController.FadeIn(fadeInDuration, fadeInDelay);

        return applied;
    }

    private bool ApplyArrival()
    {
        BoatController boat = FindFirstObjectByType<BoatController>(FindObjectsInactive.Include);
        Transform resolvedBoatPoint = boatArrivalPoint != null ? boatArrivalPoint : transform;

        if (boat != null)
        {
            boat.PlaceForSceneTransition(resolvedBoatPoint, playerArrivalPoint, putPlayerOnBoat);
            return true;
        }

        return PlacePlayerWithoutBoat();
    }

    private bool PlacePlayerWithoutBoat()
    {
        Transform target = playerArrivalPoint != null ? playerArrivalPoint : transform;
        PlayerController playerController = FindFirstObjectByType<PlayerController>(FindObjectsInactive.Include);

        if (playerController == null)
            return false;

        CharacterController characterController = playerController.GetComponent<CharacterController>();

        if (characterController != null)
            characterController.enabled = false;

        playerController.transform.SetPositionAndRotation(target.position, target.rotation);

        if (characterController != null)
            characterController.enabled = true;

        PlayerMove playerMove = playerController.GetComponent<PlayerMove>();

        if (playerMove != null)
        {
            playerMove.ResetMovementState();
            playerMove.SetSafeRespawnPosition(playerController.transform.position);
        }

        if (GameManager.instance != null)
            GameManager.instance.SetState(GameManager.GameState.OnFoot);

        Physics.SyncTransforms();
        return true;
    }
}

using UnityEngine;

public class TutorialMarkerTarget : MonoBehaviour
{
    [SerializeField] private GameObject visualRoot;
    [SerializeField] private bool hideRenderersOnPlay = true;
    [SerializeField] private bool disableCollidersOnPlay = true;

    private void Awake()
    {
        HidePreviewVisuals();
    }

    private void HidePreviewVisuals()
    {
        Transform root = visualRoot != null ? visualRoot.transform : transform;

        if (hideRenderersOnPlay)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);

            foreach (Renderer markerRenderer in renderers)
                markerRenderer.enabled = false;
        }

        if (disableCollidersOnPlay)
        {
            Collider[] colliders = root.GetComponentsInChildren<Collider>(true);

            foreach (Collider markerCollider in colliders)
                markerCollider.enabled = false;
        }
    }
}

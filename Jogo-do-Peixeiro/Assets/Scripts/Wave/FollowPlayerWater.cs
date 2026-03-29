using UnityEngine;

public class FollowPlayerWater : MonoBehaviour
{
    private Transform player;

    void Awake()
    {
        player = GameObject.FindGameObjectWithTag("Boat")?.transform;

        UpdatePosition();
    }

    void LateUpdate()
    {
        UpdatePosition();
    }

    void UpdatePosition()
    {
        if (player == null)
            return;

        transform.position = new Vector3(
            player.position.x,
            transform.position.y,
            player.position.z
        );
    }
}
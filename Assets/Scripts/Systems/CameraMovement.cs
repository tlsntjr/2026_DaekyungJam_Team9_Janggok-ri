using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    [Header("Target Player")]
    [SerializeField] private Transform playerTransform;

    [Header("Lerp speed")]
    [SerializeField] private float lerpSpeed = 0.1f;

    private void FixedUpdate()
    {
        transform.position = Vector3.Lerp(
            new Vector3(transform.position.x, transform.position.y, -10),
            new Vector3(playerTransform.position.x, playerTransform.position.y, -10),
            lerpSpeed);
    }
}

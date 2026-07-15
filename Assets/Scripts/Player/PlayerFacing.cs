using UnityEngine;

public class PlayerFacing : MonoBehaviour
{
    [Header("Sprite Renderer")]
    [SerializeField] private SpriteRenderer body;

    [SerializeField] private Camera mainCam;

    private void Reset() => mainCam = Camera.main;

    private void Update()
    {
        float mouseX = mainCam.ScreenToWorldPoint(Input.mousePosition).x;
        body.flipX = mouseX < transform.position.x;
    }
}

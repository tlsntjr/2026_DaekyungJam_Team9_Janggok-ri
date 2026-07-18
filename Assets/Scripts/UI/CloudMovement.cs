using UnityEngine;

public class CloudMovement : MonoBehaviour
{
    [Header("Vector")]
    [SerializeField] private Vector2 targetPos;

    [Header("Speed Multiplier")]
    [SerializeField] private float multiplier = 1f;

    [Header("Reverse")]
    [SerializeField] private bool isReversed = false;

    Vector2 originPosition;


    private void Start()
    {
        originPosition = transform.position;        
    }

    private void Update()
    {
        if (isReversed)
            transform.Translate(Vector3.right * Time.deltaTime * multiplier);
        else
            transform.Translate(Vector3.left * Time.deltaTime * multiplier);


        if (Vector2.Distance(transform.position, targetPos) <= 0.01f)
        {
            transform.position = originPosition;
        }
    }
}

using UnityEngine;

/// <summary>
/// ¸Ê Å»Ãâ Ãâ±¸¿¡ ³õ´Â °Í
/// </summary>
public class ExitTrigger : MonoBehaviour
{
    [SerializeField] HauntController haunt;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        Debug.Log("[Test] Exit Activated!");
        haunt.CompleteHaunt();
    }
}
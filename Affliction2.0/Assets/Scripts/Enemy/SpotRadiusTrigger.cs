using UnityEngine;

public class SpotRadiusTrigger : MonoBehaviour
{
    public EnemyComponent owner;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (owner == null) return;

        if (other.CompareTag("Player"))
        {
            owner.OnPlayerEnterSpotRadius();

        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (owner == null) return;

        if (other.CompareTag("Player") && !owner.isSpotting && !owner.spotted)
        {
            owner.OnPlayerEnterSpotRadius();
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (owner == null) return;

        if (other.CompareTag("Player"))
        {
            owner.OnPlayerExitSpotRadius();
        }
    }
}
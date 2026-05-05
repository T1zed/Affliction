using UnityEngine;

// Ce script doit être attaché sur le GameObject "spotradius"
// Le collider de cet objet doit être en mode "Is Trigger"
public class SpotRadiusTrigger : MonoBehaviour
{
    public EnemyComponent owner;

    private void OnTriggerEnter(Collider other)
    {
        if (owner == null) return;

        if (other.CompareTag("Player"))
        {
            owner.OnPlayerEnterSpotRadius();
        }
    }

   
    private void OnTriggerStay(Collider other)
    {
        if (owner == null) return;

        if (other.CompareTag("Player") && !owner.isSpotting && !owner.spotted)
        {
            owner.OnPlayerEnterSpotRadius();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (owner == null) return;

        if (other.CompareTag("Player"))
        {
            owner.OnPlayerExitSpotRadius();
        }
    }
}
using UnityEngine;

public class PlayerHitDetectorTrigger : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Ball"))
            return;

        // Get the BallHit component
        if (other.TryGetComponent<BallHit>(out BallHit ballHit))
        {
            // Only count if not already consumed
            if (!ballHit.consumed)
            {
                ballHit.Consume();
                HitManager.Instance.RegisterHit();
            }
        }
    }
}

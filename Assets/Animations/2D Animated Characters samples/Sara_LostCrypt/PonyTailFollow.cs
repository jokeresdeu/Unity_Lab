using UnityEngine;
namespace AnimationSamples2D
{
    public class PonyTailFollow : MonoBehaviour
    {
        private bool isFlipped;
        [Header("Tail")]
        [SerializeField] Transform tailAnchor = null;
        [SerializeField] Rigidbody2D tailRigidbody = null;
 
        void Start()
        {
            UpdateTailPose();
        }

        // Update is called once per frame
        void FixedUpdate()
        {
            UpdateTailPose();
        }
        private void UpdateTailPose()
        {
            // Calculate the extrapolated target position of the tail anchor.
            Vector2 targetPosition = tailAnchor.position;
            targetPosition += (Vector2)transform.position * Time.deltaTime;

            tailRigidbody.MovePosition(targetPosition);
            Quaternion flippedRotation = new Quaternion(0, 0, 1, 0);
            if (isFlipped)
                tailRigidbody.SetRotation(tailAnchor.rotation * flippedRotation);
            else
                tailRigidbody.SetRotation(tailAnchor.rotation);
        }
    }
}

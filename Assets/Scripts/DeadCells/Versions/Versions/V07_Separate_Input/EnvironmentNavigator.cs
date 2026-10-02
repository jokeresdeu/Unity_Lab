using UnityEngine;

namespace DeadCells.Versions.V07
{
    public class EnvironmentNavigator : MonoBehaviour
    {
        [Header("Ground")]
        [SerializeField] private Transform _groundChecker;
        [SerializeField] private Vector2 _groundCheckRect = new(0.7f, 0.15f);
        [SerializeField] private LayerMask _ground;

        [Header("Stand")]
        [SerializeField] private Transform _standChecker;
        [SerializeField] private Vector2 _standCheckRect = new(0.7f, 0.7f);

        [Header("Ladder")]
        [SerializeField] private Transform _ladderChecker;
        [SerializeField] private Vector2 _ladderCheckRect = new(0.6f, 1.4f);
        [SerializeField] private LayerMask _ladder;

        public bool IsGrounded =>
            Physics2D.OverlapBox(_groundChecker.position, _groundCheckRect, 0, _ground) != null;

        public bool HasLadder => CurrentLadder != null;

        public Collider2D CurrentLadder =>
            Physics2D.OverlapBox(_ladderChecker.position, _ladderCheckRect, 0, _ladder);

        public float LadderCenterX
        {
            get
            {
                var ladder = CurrentLadder;
                return ladder == null ? transform.position.x : ladder.bounds.center.x;
            }
        }

        public bool CanStand()
        {
            return Physics2D.OverlapBox(_standChecker.position, _standCheckRect, 0, _ground) == null;
        }

        public bool GroundBelow(float distance)
        {
            var position = (Vector2)_groundChecker.position + Vector2.down * distance / 2;
            var size = new Vector2(_groundCheckRect.x, _groundCheckRect.y + distance);

            return Physics2D.OverlapBox(position, size, 0, _ground) != null;
        }

        private void OnDrawGizmos()
        {
            if(_groundChecker != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireCube(_groundChecker.position, _groundCheckRect);
            }

            if(_standChecker != null)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawWireCube(_standChecker.position, _standCheckRect);
            }

            if(_ladderChecker != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireCube(_ladderChecker.position, _ladderCheckRect);
            }
        }
    }
}

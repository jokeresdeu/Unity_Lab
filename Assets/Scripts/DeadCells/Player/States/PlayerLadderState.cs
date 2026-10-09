using UnityEngine;

namespace DeadCells.Player.States
{
    [DisallowMultipleComponent]
    public sealed class PlayerLadderState : PlayerMovementState
    {
        [Header("Ladder Check")]
        [SerializeField] private Transform _ladderChecker;
        [SerializeField] private Vector2 _ladderCheckRect = new Vector2(0.6f, 1.4f);
        [SerializeField] private LayerMask _ladder;

        [Header("Ladder")]
        [SerializeField] private float _ladderSpeed = 5;
        [SerializeField] private float _ladderReattachTime = 0.2f;

        private Collider2D _currentLadder;
        private float _attachAllowedAt;

        public override PlayerState Id => PlayerState.Ladder;
        public override int ActivationPriority => 30;
        private bool IsAvailable => _currentLadder != null && Time.time >= _attachAllowedAt;

        public override void EnterState()
        {
            Rigidbody.gravityScale = 0;
            Rigidbody.linearVelocity = Vector2.zero;
            Rigidbody.position = new Vector2(_currentLadder.bounds.center.x, Rigidbody.position.y);
            Rigidbody.linearVelocityY = Input.Vertical * _ladderSpeed;
        }

        public override bool CanBeActivated(PlayerState? currentState)
        {
            FindColliders(_ladderChecker, _ladderCheckRect, _ladder, true);
            _currentLadder = Overlaps.Count > 0 ? Overlaps[0] : null;

            if(currentState == Id)
                return IsAvailable && !Input.JumpBuffered;

            var isFalling = currentState == PlayerState.Air && AnimationController.StateValue < 0;
            return IsAvailable && (Input.Vertical > 0 || isFalling) && !Input.JumpBuffered;
        }

        public override void UpdateState()
        {
            Rigidbody.linearVelocityX = 0;
            Rigidbody.linearVelocityY = Input.Vertical * _ladderSpeed;
        }

        public override void EndState()
        {
            base.EndState();
            _attachAllowedAt = Time.time + _ladderReattachTime;
        }

        private void OnDrawGizmosSelected()
        {
            if(_ladderChecker == null)
                return;

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(_ladderChecker.position, _ladderCheckRect);
        }
    }
}

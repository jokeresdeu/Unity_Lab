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
        [SerializeField] private float _centerSpeed = 6;
        [SerializeField] private float _topExitDistance = 0.12f;
        [SerializeField] private float _ladderReattachTime = 0.2f;

        private Collider2D _currentLadder;
        private float _attachAllowedAt;

        public override PlayerState Id => PlayerState.Ladder;
        public override int ActivationPriority => 30;
        private bool IsAvailable => _currentLadder != null && Time.time >= _attachAllowedAt;
        private bool IsOnTop => _currentLadder != null &&
                                Rigidbody.position.y >= _currentLadder.bounds.max.y - _topExitDistance;

        public override bool CanBeActivated(PlayerState? currentState)
        {
            FindColliders(_ladderChecker, _ladderCheckRect, _ladder, true);
            _currentLadder = Overlaps.Count > 0 ? Overlaps[0] : null;

            if(!IsAvailable || Input.JumpBuffered)
                return false;

            if(IsOnTop)
                return Input.Vertical < 0;

            if(currentState == Id)
                return true;

            var isFalling = currentState == PlayerState.Air && AnimationController.StateValue < 0;
            return Input.Vertical > 0 || isFalling;
        }

        public override void EnterState()
        {
            Rigidbody.gravityScale = 0;
            Rigidbody.linearVelocity = Vector2.zero;
            UpdateLadderMovement();
        }

        public override void UpdateState()
        {
            if(IsOnTop && Input.Vertical >= 0)
            {
                StandOnTop();
                return;
            }

            MoveToCenter();
            UpdateLadderMovement();
        }

        public override void EndState()
        {
            if(IsOnTop && Input.Vertical >= 0 && !Input.JumpBuffered)
                StandOnTop();

            base.EndState();
            _attachAllowedAt = Time.time + _ladderReattachTime;
        }

        public override void ResetState()
        {
            _currentLadder = null;
            _attachAllowedAt = 0;
        }

        private void MoveToCenter()
        {
            if(_currentLadder == null)
                return;

            var position = Rigidbody.position;
            position.x = Mathf.MoveTowards(position.x, _currentLadder.bounds.center.x,
                _centerSpeed * Time.fixedDeltaTime);
            Rigidbody.position = position;
            Rigidbody.linearVelocityX = 0;
        }

        private void UpdateLadderMovement()
        {
            Rigidbody.linearVelocityX = 0;
            Rigidbody.linearVelocityY = Input.Vertical * _ladderSpeed;
        }

        private void StandOnTop()
        {
            if(_currentLadder == null)
                return;

            var position = Rigidbody.position;
            position.y = _currentLadder.bounds.max.y;
            Rigidbody.position = position;
            Rigidbody.linearVelocity = Vector2.zero;
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

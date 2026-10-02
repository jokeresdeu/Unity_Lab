using UnityEngine;

namespace TailsOfIron.Player
{
    [RequireComponent(typeof(Animator))]
    public class PlayerAnimator : MonoBehaviour
    {
        private static int StateParam = Animator.StringToHash("State");
        [SerializeField] private Animator _animator;
        private PlayerState _playerState;

        public void SetState(PlayerState playerState)
        {
            if(_playerState == playerState)
                return;
            
            _animator.SetInteger(StateParam, (int)playerState);
            _playerState = playerState;
        }
    }
}
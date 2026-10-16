using UnityEngine;

namespace DeadCells.Enemies
{
    [DisallowMultipleComponent]
    public sealed class EnemyAnimationEvents : MonoBehaviour
    {
        private EnemyAnimationController _controller;

        public void Bind(EnemyAnimationController controller) => _controller = controller;

        public void AnimationEnd() => _controller?.AnimationEnd();
        public void AnimationAction() => _controller?.AnimationAction();
    }
}

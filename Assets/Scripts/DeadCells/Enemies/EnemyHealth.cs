using UnityEngine;

namespace DeadCells.Enemies
{
    [DisallowMultipleComponent]
    public sealed class EnemyHealth : MonoBehaviour
    {
        [SerializeField] private float _maxHealth = 100;

        private EnemyAnimationController _animation;
        private float _health;
        private bool _pendingHit;
        public bool IsDead => _health <= 0;
        
        private void Awake()
        {
            _health = _maxHealth;
            _animation = GetComponent<EnemyAnimationController>();
        }

        public void TakeDamage(float amount)
        {
            if(amount <= 0 || IsDead)
                return;

            _health = Mathf.Max(0, _health - amount);
            _pendingHit = true;
            _animation.PlayHurt();
        }

        public bool ConsumeHit()
        {
            if(!_pendingHit)
                return false;

            _pendingHit = false;
            return true;
        }
    }
}

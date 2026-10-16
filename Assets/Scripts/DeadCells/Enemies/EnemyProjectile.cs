using DeadCells.Player;
using UnityEngine;

namespace DeadCells.Enemies
{
    [DisallowMultipleComponent]
    public sealed class EnemyProjectile : MonoBehaviour
    {
        [SerializeField] private float _lifetime = 3;

        private float _speed;
        private float _damage;
        private float _direction;
        private float _remaining;

        public void Launch(float direction, float speed, float damage)
        {
            _direction = direction < 0f ? -1f : 1f;
            _speed = Mathf.Max(0f, speed);
            _damage = damage;
            _remaining = _lifetime;

            if(Mathf.Sign(transform.forward.z) != _direction)
                transform.Rotate(0, 180, 0);
        }

        private void FixedUpdate()
        {
            transform.position += Vector3.right * (_direction * _speed * Time.fixedDeltaTime);
            _remaining -= Time.fixedDeltaTime;
            if(_remaining <= 0f)
                Destroy(gameObject);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if(other == null || other.GetComponentInParent<PlayerController>() == null)
                return;

            Debug.Log($"{name}: Projectile hit {other.name} damage={_damage}");
            Destroy(gameObject);
        }
    }
}

using UnityEngine;

namespace Assets.DynamicBackgrounds.Scripts
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class ColorSpring : TweenBase
    {
        public Color From;
        public Color To;

        private SpriteRenderer _graphic;
        private Color _color;

        public void Awake()
        {
            _graphic = GetComponent<SpriteRenderer>();
            _color = _graphic.color;
        }

        protected override void OnUpdate()
        {
            _graphic.color = From + (To - From) * Sin();
        }

        public void OnDisable()
        {
            _graphic.color = _color;
        }
    }
}
using System;
using UnityEngine;

namespace TailsOfIron
{
    public class InputReader : MonoBehaviour
    {
        public event Action<float> HorizontalDirectionChanged;
        public event Action RollClicked;
        public event Action JumpClicked;

        private void Update()
        {
            var horizontalDirection = Input.GetAxisRaw("Horizontal");
            HorizontalDirectionChanged?.Invoke(horizontalDirection);
            
            if(Input.GetKeyDown(KeyCode.LeftShift))
                RollClicked?.Invoke();
            else if(Input.GetButtonDown("Jump"))
                JumpClicked?.Invoke();
        }
    }
}
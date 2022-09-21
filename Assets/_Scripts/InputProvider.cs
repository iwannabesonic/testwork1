using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Control
{
    public class InputProvider : MonoBehaviour
    {
        public InputComponent ecs_playerInputComponent;
        [SerializeField] private Joystick joystic;
        private Vector2 cash_joysticDirection;

        private void Update()
        {
            var currentDirection = joystic.Direction;
            if(ecs_playerInputComponent != null && cash_joysticDirection != currentDirection)
            {
                cash_joysticDirection = currentDirection;
                var d_input = ecs_playerInputComponent.OpenWrite(this);
                d_input.moveDirection = new Vector3(currentDirection.x, 0, currentDirection.y);
                ecs_playerInputComponent.CloseWrite(this, d_input);
            }
        }
    }
}

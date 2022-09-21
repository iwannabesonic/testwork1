using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ECS;
using Control;

namespace Anim
{
    public class AnimatorPropsSetter : MonoBehaviour
    {
        private static readonly int h_speedF = Animator.StringToHash("Speed_f");

        public InputComponent ecs_PlayerInput;
        [SerializeField] private Animator animator;

        private void Update()
        {
            var d_input = ecs_PlayerInput.Read();
            animator.SetFloat(h_speedF, d_input.moveDirection.magnitude);
        }
    }
}
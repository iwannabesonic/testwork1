using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ECS;

namespace Control
{
    public class InputSystem : UnityThreadSystem
    {
        private EntityDynamicList entityList;

        private static readonly int h_InputComponent = typeof(InputComponent).GetHashCode();
        private static readonly int h_MoveComponent = typeof(MoveComponent).GetHashCode();
        protected override void Initialize(Entity.EntityManager entityManager)
        {
            entityList = entityManager.GetDynamicList(typeof(InputComponent), typeof(MoveComponent));
        }

        protected override void Update(double deltaTime)
        {
            foreach(var entity in entityList)
            {
                var c_input = entity.GetComponent<InputComponent>(h_InputComponent);
                var c_move = entity.GetComponent<MoveComponent>(h_MoveComponent);

                if (c_input == null || c_move == null)
                    continue;

                var d_input = c_input.Read();
                var d_move = c_move.OpenWrite(this);
                d_move.direction = d_input.moveDirection;
                c_move.CloseWrite(this, d_move);
            }
        }
    }
}

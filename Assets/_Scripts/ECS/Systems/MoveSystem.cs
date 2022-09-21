using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ECS;
using Wrappers;
using Transform = ECS.Transform;

namespace Control
{
    public class MoveSystem : UnityThreadSystem
    {
        private EntityDynamicList entityList;
        private static readonly int h_MoveComponent = typeof(MoveComponent).GetHashCode();
        private static readonly int h_Transform = typeof(Transform).GetHashCode();
        private static readonly int h_UnityRigidbody = typeof(UnityRigidbody).GetHashCode();
        private static readonly Vector3 upAxis = new Vector3(0, 1, 0);

        protected override void Initialize(Entity.EntityManager entityManager)
        {
            entityList = entityManager.GetDynamicList(typeof(MoveComponent), typeof(Transform), typeof(UnityRigidbody));
        }

        protected override void Update(double deltaTime)
        {
            float deltaTimef = (float)deltaTime;
            Transform.TransformData d_transform = default;
            Vector3 dir = default;
            foreach(var entity in entityList)
            {
                var c_move = entity.GetComponent<MoveComponent>(h_MoveComponent);
                var c_transform = entity.GetComponent<Transform>(h_Transform);
                var c_rigidbody = entity.GetComponent<UnityRigidbody>(h_UnityRigidbody);

                if (c_move == null)
                    continue;

                var d_move = c_move.Read();
                if (d_move.useRigidbody && c_rigidbody == null)
                    continue;
                if(d_move.useRigidbody)
                {
                    var d_rigidbody = c_rigidbody.OpenWrite(this);
                    dir = d_move.speed * d_rigidbody.mass * deltaTimef * d_move.direction;
                    d_rigidbody.velocity = dir;
                    c_rigidbody.CloseWrite(this, d_rigidbody);
                }
                else
                {
                    d_transform = c_transform.OpenWrite(this);
                    dir = d_move.speed * deltaTimef * d_move.direction;
                    d_transform.position += dir;
                    c_transform.CloseWrite(this, d_transform);
                }

                if (dir != default)
                {
                    d_transform = c_transform.OpenWrite(this);
                    d_transform.rotation = Quaternion.Lerp(d_transform.rotation, Quaternion.LookRotation(dir, upAxis), d_move.rotationSpeed * deltaTimef);
                    c_transform.CloseWrite(this, d_transform);
                }
            }
        }
    }
}

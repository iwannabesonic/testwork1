using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ECS;
using Control;
using Wrappers;
using Transform = ECS.Transform;

public class PositionSystem : UnityThreadSystem
{
    private EntityDynamicList entityList;
    private static readonly int h_transform = typeof(Transform).GetHashCode();
    private static readonly int h_unityRigidbody = typeof(UnityRigidbody).GetHashCode();

    protected override void Initialize(Entity.EntityManager entityManager)
    {
        entityList = entityManager.GetDynamicList(typeof(Transform), typeof(UnityRigidbody));
    }

    protected override void Update(double deltaTime)
    {
        foreach(var entity in entityList)
        {
            var c_transform = entity.GetComponent<Transform>(h_transform);
            var c_rigidbody = entity.GetComponent<UnityRigidbody>(h_unityRigidbody);

            
            var d_transform = c_transform.Read();
            if(c_rigidbody == null)
            {
                c_transform.Source.position = d_transform.position;
            }
            else
            {
                var d_rigidbody = c_rigidbody.Read();
                if (!d_rigidbody.isKinematic && d_rigidbody.forceWriteTransform)
                {
                    c_rigidbody.Source.velocity = d_rigidbody.velocity;
                    d_transform = c_transform.OpenWrite(this);
                    d_transform.position = c_rigidbody.Source.position;
                    c_transform.CloseWrite(this, d_transform);
                }
                else
                {
                    c_transform.Source.position = d_transform.position;
                }
            }

            c_transform.Source.rotation = d_transform.rotation;
        }
    }
}

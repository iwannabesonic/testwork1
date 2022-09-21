using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ECS;
using Wrappers;
using System;

public class CollisionSolverSystem : UnityThreadSystem
{
    public delegate void OnCollisionEnterHandler(Entity collisionOn, Entity collisionWith);

    private EntityDynamicList entityList;
    private static readonly int h_collision = typeof(CollisionComponent).GetHashCode();
    //private static readonly int h_rigidbody = typeof(UnityRigidbody).GetHashCode();

    public event OnCollisionEnterHandler OnCollisionEnter;

    protected override void Initialize(Entity.EntityManager entityManager)
    {
        entityList = entityManager.GetDynamicList(typeof(CollisionComponent));
    }

    protected override void Update(double deltaTime)
    {
        foreach(var entity in entityList)
        {
            var c_collision = entity.GetComponent<CollisionComponent>(h_collision);

            if (c_collision == null)
                continue;

            if(c_collision.TryDequeueCollision(out var collisionData))
            {
                var otherRB = UnityRigidbody.GetFrom(collisionData.rigidbody);
                OnCollisionEnter?.Invoke(entity.Handle, Manager.FindEntityFromComponent(otherRB));
            }
        }
    }
}

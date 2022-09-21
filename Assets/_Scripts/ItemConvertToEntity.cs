using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ECS;
using Wrappers;
using Transform = UnityEngine.Transform;

namespace Items
{
    public class ItemConvertToEntity : MonoBehaviour
    {
        private Entity selfEntity;

        public Entity SelfEntity => selfEntity;

        public void Convert(uint itemType)
        {
            if (selfEntity != null)
                return;

            var world = EntityWorld.DefaultWorldInjection;

            selfEntity = Entity.Factory.Create(world,
                ComponentConvertUtils.Convert<ECS.Transform, Transform>(transform),
                new Item(new Item.ItemData() { itemType = itemType}),
                ComponentConvertUtils.Convert<UnityRigidbody, Rigidbody>(GetComponent<Rigidbody>()),
                ComponentConvertUtils.Convert<CollisionComponent, CollisionComponentProvider>(GetComponent<CollisionComponentProvider>())
                );
        }

        public void ActivateWith(uint itemType, Vector3 position, Quaternion rotation)
        {
            var c_collision = selfEntity.GetSingleComponent<CollisionComponent>();
            var d_collision = c_collision.OpenWrite(this);
            d_collision.solveCollisions = true;
            c_collision.CloseWrite(this, d_collision);

            selfEntity.GetSingleComponent<UnityRigidbody>().Source.detectCollisions = true;

            var c_item = selfEntity.GetSingleComponent<Item>();
            var d_item = c_item.OpenWrite(this);
            d_item.itemType = itemType;
            c_item.CloseWrite(this, d_item);

            var c_transform = selfEntity.GetSingleComponent<ECS.Transform>();
            var d_transform = c_transform.OpenWrite(this);
            d_transform.position = position;
            d_transform.rotation = rotation;
            c_transform.CloseWrite(this, d_transform);

            transform.position = position;
            transform.rotation = rotation;
            gameObject.SetActive(true);

            EntityWorld.DefaultWorldInjection.EntityManager.TransferIn(selfEntity);
        }
    }
}

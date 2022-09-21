using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ECS;
using Items;
using Transform = ECS.Transform;

public class ItemStackPositionSystem : UnityThreadSystem
{
    public Vector3 posOffsetOnIndex = new Vector3(0, 0.2f, 0);
    public Vector3 localRotEulers = new Vector3(0, 90, 0);
    private EntityDynamicList entityList;
    private static int h_itemStack = typeof(ItemStack).GetHashCode();

    protected override void Initialize(Entity.EntityManager entityManager)
    {
        entityList = entityManager.GetDynamicList(typeof(ItemStack));
    }

    protected override void Update(double deltaTime)
    {
        var localRot = Quaternion.Euler(localRotEulers);
        foreach(var entity in entityList)
        {
            var c_itemStack = entity.GetComponent<ItemStack>(h_itemStack);
            var d_transform = entity.Handle.GetSingleComponent<Transform>().Read();
            var localOffset = c_itemStack.Read().startHandleLocalPosition;

            int iterator = 0;
            foreach(var itemEntity in c_itemStack.GetEnumerable())
            {
                var item_transform = itemEntity.GetSingleComponent<Transform>();
                var d_item_transform = item_transform.OpenWrite(this);
                d_item_transform.position = Vector3.Lerp(d_item_transform.position, (d_transform.position + d_transform.rotation * localOffset) + d_transform.rotation * (posOffsetOnIndex * iterator++), 32 * (float)deltaTime);
                d_item_transform.rotation = d_transform.rotation * localRot;
                item_transform.CloseWrite(this, d_item_transform);
            }
        }
    }
}

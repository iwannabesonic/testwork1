using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ECS;

namespace Items
{
    public class ItemStack : Component<ItemStack.ItemStackData>
    {
        private readonly object _sync = new object();
        private ItemStackData _data;
        protected override ref ItemStackData Data => ref _data;

        public bool AddItemInStack(Entity entity)
        {
            if (_data.handledItems.Count >= _data.maxCapacity)
                return false;

            var item = entity.GetSingleComponent<Item>();
            if (item == null)
                return false;

            lock(_sync)
                _data.handledItems.Push(entity);
            return true;
        }

        public bool RemoveItemFromStack(out Entity entity)
        {
            lock(_sync)
                return _data.handledItems.TryPop(out entity);
        }

        public IEnumerable<Entity> GetEnumerable()
        {
            lock(_sync)
                foreach(var item in _data.handledItems)
                    yield return item;
        }

        public ItemStack(int maxCapacity, Vector3 startHandleLocalPosition) : base(ComponentTags.Single_Type)
        {
            _data = new ItemStackData(maxCapacity)
            {
                startHandleLocalPosition = startHandleLocalPosition
            };
        }
        public struct ItemStackData
        {
            public Vector3 startHandleLocalPosition;
            public int maxCapacity;
            public readonly Stack<Entity> handledItems;

            public ItemStackData(int maxCapacity)
            {
                this.maxCapacity = maxCapacity;
                handledItems = new Stack<Entity>(maxCapacity);
                startHandleLocalPosition = default;
            }
        }
    }
}

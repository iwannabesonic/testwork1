using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ECS;

namespace Items
{
    public class Item : Component<Item.ItemData>
    {
        private ItemData _data;
        protected override ref ItemData Data => ref _data;

        public Item(ItemData data) : base(ComponentTags.Single_Type)
        {
            _data = data;
        }
        public struct ItemData
        {
            public uint itemType;
        }
    }
}

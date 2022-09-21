using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Items
{
    public class ItemPool : MonoBehaviour
    {
        [SerializeField] private ItemConvertToEntity prefab;
        [SerializeField] private Material[] randomMaterias; 
        private Stack<ItemConvertToEntity> pool = new Stack<ItemConvertToEntity>();

        public void AddInPool(ItemConvertToEntity itemConverter)
        {
            pool.Push(itemConverter);
        }

        public ItemConvertToEntity GetFromPool()
        {
            if(pool.TryPop(out var result))
            {
                return result;
            }
            else
            {
                result = Instantiate(prefab, transform, true);
                result.GetComponentInChildren<Renderer>().material = SelectRandom();
                return result;
            }
        }

        private Material SelectRandom()
        {
            return randomMaterias[Random.Range(0, randomMaterias.Length)];
        }
    }
}

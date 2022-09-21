using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;

namespace Items
{
    public class ItemGenerator : MonoBehaviour
    {
        public float spawnRange = 5f;
        [SerializeField] private ItemPool pool;
        [SerializeField] private float spawnDelay = 3f;
        [SerializeField] private uint maxItemType = 3;

        private void OnEnable()
        {
            Observable.Timer(System.TimeSpan.FromSeconds(spawnDelay)).Repeat().TakeUntilDisable(this).Subscribe(Generate);
        }

        private void Generate(long _)
        {
            var position = Random.insideUnitSphere * spawnRange;
            var rotation = Quaternion.Euler(0, position.y * 360, 0);
            position.y = 0;
            position = transform.position + position;

            var item = pool.GetFromPool();
            item.transform.SetPositionAndRotation(position, rotation);
            uint itemType = (uint)Random.Range(0, maxItemType + 1);
            if (item.SelfEntity == null)
                item.Convert(itemType);
            else
                item.ActivateWith(itemType, position, rotation);
        }
    }
}

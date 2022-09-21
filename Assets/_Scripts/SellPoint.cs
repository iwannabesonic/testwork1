using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Items;
using Wrappers;
using ECS;
using UniRx;
using System;

public class SellPoint : MonoBehaviour
{
    [SerializeField] private GameLogic gameLogic;
    [SerializeField] private CashProvider cash;
    [SerializeField] private ItemPool pool;
    [SerializeField] private List<SellData> sellData = new List<SellData>();

    private IDisposable updater;

    private void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("Player"))
        {
            updater = Observable.Timer(TimeSpan.FromSeconds(0.25)).Repeat().TakeUntilDestroy(gameObject).Subscribe(Tick);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            updater?.Dispose();
        }
    }

    private void Tick(long _)
    {
        if (gameLogic.RemoveItemFromStack(out var entity))
        {
            var item = entity.GetSingleComponent<Item>();
            var sellPrice = GetPriceForType(item.Read().itemType);
            var item_go = entity.GetSingleComponent<UnityRigidbody>().Source.gameObject;

            EntityWorld.Main.EntityManager.TransferIn(entity);
            item_go.SetActive(false);
            pool.AddInPool(item_go.GetComponent<ItemConvertToEntity>());
            cash.AddCash(sellPrice);
        }
    }

    private int GetPriceForType(uint type)
    {
        foreach(var dt in sellData)
        {
            if (dt.itemType == type)
                return dt.sellPrice;
        }
        return 1;
    }

    [Serializable]
    public struct SellData
    {
        public uint itemType;
        public int sellPrice;
    }
}

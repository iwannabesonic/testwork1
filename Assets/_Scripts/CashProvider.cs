using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class CashProvider : MonoBehaviour
{
    private int cashAmount;

    public int CashAmount => cashAmount;

    public event UnityAction<int> OnCashChanged;

    public void AddCash(int amount)
    {
        cashAmount += amount;
        OnCashChanged?.Invoke(cashAmount);
    }

    public bool TryGetCash(int amount)
    {
        if (cashAmount >= amount)
        {
            cashAmount -= amount;
            OnCashChanged?.Invoke(cashAmount);
            return true;
        }
        else return false;
    }
}

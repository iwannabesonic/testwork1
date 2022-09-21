using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UniRx;
using IDisposable = System.IDisposable;

public class CashAmountRenderer : MonoBehaviour
{
    public string outputFormat = "${0}";
    [SerializeField] private TextMeshProUGUI printer;
    [SerializeField] private CashProvider cash;
    private int cash_value = 0, targetValue =0;
    private IDisposable updater;

    private void Start()
    {
        cash.OnCashChanged += Cash_OnCashChanged;
        Cash_OnCashChanged(0);
    }

    private void Cash_OnCashChanged(int amount)
    {
        targetValue = amount;
        SmoothChange();
    }

    private void SmoothChange()
    {
        updater?.Dispose();
        float t = 0;
        var cashValue = cash_value;
        updater = Observable.EveryUpdate().TakeWhile(_ => t <= 1).TakeUntilDestroy(gameObject).Subscribe(_ => 
        {
            var val = Mathf.Lerp(cashValue, targetValue, t);
            cash_value = (int)val;
            printer.text = string.Format(outputFormat, cash_value);
            t += Time.deltaTime;
            if(t>=1)
            {
                cash_value = targetValue;
                printer.text = string.Format(outputFormat, cash_value);
            }
        });
    }
}

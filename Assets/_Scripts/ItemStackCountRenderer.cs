using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ItemStackCountRenderer : MonoBehaviour
{
    public string outputFormat = "Stack: {0}/{1}";
    [SerializeField] private GameLogic gameLogic;
    [SerializeField] private TextMeshProUGUI printer;

    private void Start()
    {
        gameLogic.OnChangeItemsInStack += GameLogic_OnChangeItemsInStack;
    }

    private void GameLogic_OnChangeItemsInStack(int current, int max)
    {
        printer.text = string.Format(outputFormat, current, max);
    }
}

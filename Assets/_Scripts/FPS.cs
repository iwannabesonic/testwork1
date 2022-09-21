using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UniRx;

public class FPS : MonoBehaviour
{
    public string outputFormat = "FPS: {0}";
    [SerializeField] private TextMeshProUGUI printer;
    [SerializeField] private float displayFPSEveryPeriod = 0.5f;

    private int framesCount = 0;
    private float summaryTime = 0;

    private void OnEnable()
    {
        Observable.Timer(System.TimeSpan.FromSeconds(displayFPSEveryPeriod)).Repeat().TakeUntilDisable(this).Subscribe(_ => 
        {
            var fps = 1f / (summaryTime / framesCount);
            printer.text = string.Format(outputFormat, fps.ToString("0"));
            framesCount = 0;
            summaryTime = 0;
        });
    }

    private void Update()
    {
        framesCount += 1;
        summaryTime += Time.deltaTime;
    }
}

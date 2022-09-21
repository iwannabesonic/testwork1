using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using TMPro;
using System.Text;
using Core.Async;
using Core.Async.Tasks;
using Core.Async.LowLevel;

namespace Core.DebugInfo
{
    public class ThreadInfoMonitor : MonoBehaviour
    {
        StringBuilder builder = new StringBuilder();
        [SerializeField] TextMeshProUGUI infoOut;
        float curTime;

        // Update is called once per frame
        void Update()
        {
            if (Time.unscaledTime - curTime > 0.1f)
            {
                builder.Clear();
                foreach (var thread in UnityAsyncCoreHandle.Core)
                {
                    var time = thread.CurrentTime - thread.LasLoopCallTime;
                    if (time.TotalSeconds >= 3)
                    {
                        builder.Append($"<color=red>{thread.Name} is not responding. Silence: {time.TotalSeconds:f0} seconds</color>\n");
                    }
                    else
                    {
                        builder.Append($"{thread.Name}: {(float)thread.TrueDelay.Ticks / NativeThread.ticksPerMillisecond:f2} ms Load: {(1 - thread.IdleTime):p}\n");
                    }
                }

                ISyncDelegateThread udt = UnityDelegateThread.Self;
                builder.Append($"UDT jobs {udt.ImportantActionQueneCount} / {udt.ActionQueneCount}\n");
                udt = AsyncCalculationThread.Self;
                builder.Append($"ACT jobs {udt.ImportantActionQueneCount} / {udt.ActionQueneCount}\n");
                builder.Append($"UD tasks {UnityTaskDispatcher.ImportantQueneCount} / {UnityTaskDispatcher.QueneCount}\n");
                builder.Append($"SC: pivot = {AsyncUnityContextSheduler.Self.CurrentPivot}, {AsyncUnityContextSheduler.Self.WasUpdated} / {AsyncUnityContextSheduler.Self.CurrentUpdate}");

                infoOut.SetText(builder.ToString());
                curTime = Time.unscaledTime;
            }
        }
    }
}

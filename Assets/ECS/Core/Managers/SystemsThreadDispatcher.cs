using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Core;
using Core.Async;
using Core.Async.LowLevel;

namespace ECS
{
    public sealed class SystemsThreadDispatcher : UnitySingleton<SystemsThreadDispatcher>
    {
        public const uint defaultUpdateTimeForThreads_MS = 50; 
        public delegate void SystemEntryPoint(double deltaTime);
        public const string unity_thread = "unity";
        private class Scheduler
        {
            private readonly List<SystemEntryPoint> systemsEntryPoints = new List<SystemEntryPoint>(16);
            private readonly object _sync = new object();

            public void Add(SystemEntryPoint entryPoint)
            {
                if (entryPoint == null)
                    return;

                if (!systemsEntryPoints.Contains(entryPoint))
                {
                    systemsEntryPoints.Add(entryPoint);
                }

            }

            public void Remove(SystemEntryPoint entryPoint)
            {
                if (entryPoint == null)
                    return;

                lock (_sync)
                    systemsEntryPoints.Remove(entryPoint);
            }

            public void Schedule(double deltaTime)
            {
                lock (_sync)
                {
                    foreach (var entryPoint in systemsEntryPoints)
                    {
                        try
                        {
                            entryPoint(deltaTime);
                        }
                        catch (Exception e)
                        {
                            Debug.LogException(e);
                        }
                    }
                }
            }
        }

        private class AsyncScheduler : Scheduler, ISupportAsyncCall
        {
            public bool enabledUpdate => true;

            public bool enabledLateUpdate => false;

            public void AsyncLateUpdate(TimeSpan updateDelay)
            {
                throw new NotSupportedException();
            }

            public void AsyncUpdate(TimeSpan updateDelay)
            {
                var deltaTime = updateDelay.TotalSeconds;
                base.Schedule(deltaTime);
            }
        }

        private readonly Dictionary<string, Scheduler> schedulers = new Dictionary<string, Scheduler>(16)
        {
            {unity_thread, new Scheduler() }
        };
        private Scheduler unity_scheduler;

        protected override void SingletonAwake()
        {
            unity_scheduler = schedulers[unity_thread];
        }

        public static void RegisterEntryPoint(string thread, SystemEntryPoint entryPoint, uint delayUpdate_MS = defaultUpdateTimeForThreads_MS)
        {
            if (delayUpdate_MS == 0)
                delayUpdate_MS = 1;
            if(thread != unity_thread)
                thread = thread + delayUpdate_MS.ToString();
            if(Self.schedulers.TryGetValue(thread, out Scheduler scheduler))
            {
                scheduler.Add(entryPoint);
            }
            else
            {
                var newScheduler = new AsyncScheduler();
                newScheduler.Add(entryPoint);
                _self.schedulers.Add(thread, newScheduler);
                newScheduler.RegisterSelf(thread);
                UnityAsyncCoreHandle.Core.GetThread(thread).Delay = new TimeSpan(NativeThread.ticksPerMillisecond * delayUpdate_MS);
            }
        }

        public static void RemoveEntryPoint(string thread, SystemEntryPoint entryPoint, uint delayUpdate_MS = defaultUpdateTimeForThreads_MS)
        {
            if (delayUpdate_MS == 0)
                delayUpdate_MS = 1;
            thread = thread + delayUpdate_MS.ToString();
            if (Self.schedulers.TryGetValue(thread, out var scheduler))
            {
                scheduler.Remove(entryPoint);
            }
        }

        private void Update()
        {
            unity_scheduler.Schedule(Time.deltaTime);
        }
    }
}

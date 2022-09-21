using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Core.LowLevel;
using System;
using Core.Async.LowLevel;

namespace Core.Async
{
    public interface ISupportSyncCall
    {
        public bool RequireUpdate { get; }
        public void SyncUpdate();
    }
    public class AsyncUnityContextSheduler : UnitySingleton<AsyncUnityContextSheduler>, ISupportAsyncCall
    {
        [SerializeField] int maxSheduleHandlersPerTick = 100;

        private List<ISupportSyncCall> handlers = new List<ISupportSyncCall>(1024);
        private List<ISupportSyncCall> cashedHandlersList = new List<ISupportSyncCall>(1024);

        private readonly object _sync = new object();
        private int pivot = 0;
        private int wasUpdated = 0;
        private int currentUpdate = 0;
        private bool requireSyncUpdate = false;

        public int CurrentPivot => pivot;
        public int MaxUpdated => maxSheduleHandlersPerTick;
        public int WasUpdated => wasUpdated;
        public int CurrentUpdate => currentUpdate;

        bool ISupportAsyncCall.enabledUpdate => false;

        bool ISupportAsyncCall.enabledLateUpdate => true;

        private void Start()
        {
            UnityAsyncCoreHandle.Self.RegisterCall(ThreadDefinition.systemsTread, this);
        }

        private void AsyncUpdate()
        {
            if (requireSyncUpdate)
                return;

            cashedHandlersList.Clear();
            
            int totalCount = handlers.Count;
            int wasUpdated = 0;
            currentUpdate = 0;
            lock(_sync)
            {
                for (int i = pivot; (wasUpdated < maxSheduleHandlersPerTick) && (currentUpdate < totalCount); currentUpdate++, i++)
                {
                    if (i >= totalCount)
                        i = 0;
                    pivot = i;
                    var handle = handlers[i];
                    if (handle.RequireUpdate)
                    {
                        //handle.SyncUpdate();
                        cashedHandlersList.Add(handle);
                        wasUpdated++;
                    }
                }

                this.wasUpdated = wasUpdated;
                if(cashedHandlersList.Count>0)
                    requireSyncUpdate = true;
            }
        }

        void Update()
        {
            if(requireSyncUpdate)
            {
                foreach (var handle in cashedHandlersList)
                    handle.SyncUpdate();
                requireSyncUpdate = false;
            }
        }

        public void RegisterCall(ISupportSyncCall caller)
        {
            lock (_sync)
            {
                if (handlers.Contains(caller))
                    return;

                handlers.Add(caller);
            }
        }

        public void RemoveCall(ISupportSyncCall caller)
        {
            handlers.Remove(caller);
        }

        void ISupportAsyncCall.AsyncUpdate(TimeSpan updateDelay)
        {
            throw new NotImplementedException();
        }

        void ISupportAsyncCall.AsyncLateUpdate(TimeSpan updateDelay)
        {
            AsyncUpdate();
        }
    }
}

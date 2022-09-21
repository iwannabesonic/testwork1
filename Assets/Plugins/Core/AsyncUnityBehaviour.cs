using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Core.Async.Wrappers
{
    public abstract class AsyncUnityBehaviour : MonoBehaviour, ISupportSyncCall
    {
        protected abstract bool RequireUpdate { get; }

        bool ISupportSyncCall.RequireUpdate => RequireUpdate;

        protected virtual void OnEnable()
        {
            AsyncUnityContextSheduler.Self.RegisterCall(this);
        }

        protected virtual void OnDisable()
        {
            AsyncUnityContextSheduler.Self.RemoveCall(this);
        }

        protected abstract void SyncUpdate();

        void ISupportSyncCall.SyncUpdate() => SyncUpdate();
    }
}

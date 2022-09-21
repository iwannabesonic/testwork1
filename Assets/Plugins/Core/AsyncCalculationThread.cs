using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Core.Async
{
    public class AsyncCalculationThread : MonoBehaviour, ISupportAsyncCall, ISyncDelegateThread
    {
        private static AsyncCalculationThread _self;
        public static AsyncCalculationThread Self
        {
            get
            {
                if (_self == null)
                {
                    _self = FindObjectOfType<AsyncCalculationThread>();
                    _self.Initialize();
                }

                return _self;
            }
        }
        void Awake()
        {
            if (_self == null)
            {
                _self = this;
                //Initialize();
            }
            else if (_self != this)
                Destroy(this);
        }
        void Start()
        {
            Initialize();
        }

        private int managedThreadID = -1;
        public const string threadName = ThreadDefinition.contextThread;
        bool ISupportAsyncCall.enabledUpdate => true;

        bool ISupportAsyncCall.enabledLateUpdate => true;

        int ISyncDelegateThread.ActionQueneCount => fromThreadUpdateQuene.Count + fromThreadLateUpdateQuene.Count;

        int ISyncDelegateThread.ImportantActionQueneCount => hight_fromThreadUpdateQuene.Count + hight_fromThreadLateUpdateQuene.Count;

        void ISupportAsyncCall.AsyncLateUpdate(TimeSpan updateDelay)
        {
            while (hight_fromThreadLateUpdateQuene.Count > 0) //hight priority
            {
                try
                {
                    hight_fromThreadLateUpdateQuene.Dequeue().Invoke();
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }

            if (fromThreadLateUpdateQuene.Count > 0) //low priority
            {
                try
                {
                    fromThreadLateUpdateQuene.Dequeue().Invoke();
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }

        void ISupportAsyncCall.AsyncUpdate(TimeSpan updateDelay)
        {
            if(managedThreadID is -1)
                managedThreadID = Thread.CurrentThread.ManagedThreadId;
            while (hight_fromThreadUpdateQuene.Count > 0) //hight priority
            {
                try
                {
                    hight_fromThreadUpdateQuene.Dequeue().Invoke();
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }

            if (fromThreadUpdateQuene.Count > 0) //low priority
            {
                try
                {
                    fromThreadUpdateQuene.Dequeue().Invoke();
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }

        private Queue<Action> fromThreadUpdateQuene = new Queue<Action>();
        private Queue<Action> fromThreadLateUpdateQuene = new Queue<Action>();
        private Queue<Action> hight_fromThreadUpdateQuene = new Queue<Action>();
        private Queue<Action> hight_fromThreadLateUpdateQuene = new Queue<Action>();

        void Initialize()
        {
            this.RegisterSelf(threadName);
        }

        private void OnDestroy()
        {
            this.RemoveSelf(threadName);
        }

        public int ManagedThreadID => managedThreadID;

        public void Execute(Action action, bool hightPriority, CallMethod method)
        {
            switch (method)
            {
                case CallMethod.Update:
                    if (hightPriority)
                        hight_fromThreadUpdateQuene.Enqueue(action);
                    else
                        fromThreadUpdateQuene.Enqueue(action);
                    break;
                case CallMethod.LateUpdate:
                    if (hightPriority)
                        hight_fromThreadLateUpdateQuene.Enqueue(action);
                    else
                        fromThreadLateUpdateQuene.Enqueue(action);
                    break;
            }
        }
        public void Execute(Action action, CallMethod method) => Execute(action, false, method);
        public void Execute(Action action, bool hightPriority) => Execute(action, hightPriority, CallMethod.Update);
        public void Execute(Action action) => Execute(action, false, CallMethod.Update);

        void ISyncDelegateThread.ImportantExecute(Action action)
        {
            Execute(action, true, CallMethod.Update);
        }
    }

    public enum CallMethod
    {
        Update,
        LateUpdate,
    }
}

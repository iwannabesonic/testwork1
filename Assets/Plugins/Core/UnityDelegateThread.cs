using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace Core.Async
{
    /// <summary>
    /// Позволяет выполнять код в основном потоке юнити, вызывая его.
    /// </summary>
    public class UnityDelegateThread : MonoBehaviour, ISyncDelegateThread
    {
        public enum ExecuteMode
        {
            Update,
            FixedUpdate
        }

        private static UnityDelegateThread _self;
        public static UnityDelegateThread Self => _self;

        private SyncDelegateThread update_DelegateThread = new SyncDelegateThread();
        private SyncDelegateThread fixedUpdate_DelegateThread = new SyncDelegateThread();

        #region interface
        void ISyncDelegateThread.Execute(Action action) => update_DelegateThread.Execute(action);
        void ISyncDelegateThread.ImportantExecute(Action action) => update_DelegateThread.ImportantExecute(action);
        int ISyncDelegateThread.ActionQueneCount => update_DelegateThread.ActionQueneCount + fixedUpdate_DelegateThread.ActionQueneCount;
        int ISyncDelegateThread.ImportantActionQueneCount => update_DelegateThread.ImportantActionQueneCount + fixedUpdate_DelegateThread.ImportantActionQueneCount;
        #endregion

        private void Awake()
        {
            if (_self == null)
            {
                _self = this;
            }
            else if (_self != this)
                Destroy(gameObject);
        }

        //private Queue<Action> quene = new Queue<Action>();
        //private Queue<Action> importatnQuene = new Queue<Action>();

        private void Update()
        {
            update_DelegateThread.Update();
        }
        private void FixedUpdate()
        {
            fixedUpdate_DelegateThread.Update();
        }

        /// <summary>
        /// Выполняет код в основном потоке юнити. Выполнение кода не всегда произойдет в ближайшем кадре
        /// </summary>
        /// <param name="action"></param>
        public static void Execute(Action action, ExecuteMode mode)
        {
            switch (mode)
            {
                case ExecuteMode.Update:
                    _self.update_DelegateThread.Execute(action);
                    break;
                case ExecuteMode.FixedUpdate:
                    _self.fixedUpdate_DelegateThread.Execute(action);
                    break;
            }
        }
        /// <summary>
        /// Гарантированно выполнит код в ближайший апдейт
        /// </summary>
        /// <param name="action"></param>
        public static void ImportantExecute(Action action, ExecuteMode mode)
        {
            switch (mode)
            {
                case ExecuteMode.Update:
                    _self.update_DelegateThread.ImportantExecute(action);
                    break;
                case ExecuteMode.FixedUpdate:
                    _self.fixedUpdate_DelegateThread.ImportantExecute(action);
                    break;
            }
        }

        public int ActionQueneCount => update_DelegateThread.ActionQueneCount;
        public int ImportantActionQueneCount => update_DelegateThread.ImportantActionQueneCount;

        public int ManagedThreadID => update_DelegateThread.ManagedThreadID;
    }
    public enum ExecuteType
    {
        Default,
        Important,
    }
    public interface ISyncDelegateThread
    {
        int ActionQueneCount { get; }
        int ImportantActionQueneCount { get; }

        void Execute(Action action);
        void ImportantExecute(Action action);

        int ManagedThreadID { get; }
    }
    public class SyncDelegateThread : ISyncDelegateThread
    {
        private Queue<Action> quene = new Queue<Action>();
        private Queue<Action> importatnQuene = new Queue<Action>();
        public int saveImportantLimit = 100;
        private int managedThreadID = -1;
        public void Update()
        {
            if(managedThreadID is -1)
                managedThreadID = Thread.CurrentThread.ManagedThreadId;
            if (quene.Count > 0)
                quene.Dequeue()?.Invoke();
            int executes = 0;
            while (importatnQuene.Count > 0 && executes < saveImportantLimit)
            {
                importatnQuene.Dequeue()?.Invoke();
                executes++;
            }
        }

        /// <summary>
        /// Выполняет код в основном потоке юнити. Выполнение кода не всегда произойдет в ближайшем кадре
        /// </summary>
        /// <param name="action"></param>
        public void Execute(Action action)
        {
            quene.Enqueue(action);
        }
        /// <summary>
        /// Гарантированно выполнит код в ближайший апдейт
        /// </summary>
        /// <param name="action"></param>
        public void ImportantExecute(Action action)
        {
            importatnQuene.Enqueue(action);
        }

        public void ClearQuene() => quene.Clear();
        public void ClearImportantQuene() => importatnQuene.Clear();
        public void Clear()
        {
            ClearQuene();
            ClearImportantQuene();
        }

        public int ActionQueneCount => quene.Count;
        public int ImportantActionQueneCount => importatnQuene.Count;
        public int ManagedThreadID => managedThreadID;
    }



}

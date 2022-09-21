using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Core.Async;
using Core.Async.Tasks;
using Core.LowLevel;
using Core.Async.LowLevel;

namespace Core.AI
{
    public class AICore : MonoBehaviour, ISupportAsyncCall
    {
        #region mem and mdl
        Memory mem;
        IAIModule[] modules;
        Vault _attributes;
        public Vault Attributes => _attributes;
        public Memory LocalMemory => mem;
        public IAIModule GetMDL(string mdlName)
        {
            foreach(var mdl in modules)
            {
                if (mdlName.Equals(mdl.MDL_name))
                    return mdl;
            }
            return null;
        }
        public T GetMDL<T>(string mdlName) where T : class, IAIModule
        {
            foreach(var mdl in modules)
            {
                if (mdlName.Equals(mdl.MDL_name))
                    if (mdl is T tmdl)
                        return tmdl;
            }
            return null;
        }
        public T GetMDL<T>() where T : IAIModule
        {
            foreach (var mdl in modules)
            {
                if (mdl is T tmdl)
                    return tmdl;
            }
            return default;
        }
        #endregion
        public void SetAttributes(Vault newatr) => _attributes = newatr;
        public void InitializeModules(IEnumerable<IAIModule> loadList)
        {
            List<IAIModule> list = new List<IAIModule>(loadList);
            list.Sort((x, y) => 
            {
                if (x.Priority > y.Priority)
                    return -1;
                else if (x.Priority < y.Priority)
                    return 1;
                else return 0;
            });

            mem = new Memory(64, 64);
            if (delegateThread != null)
                delegateThread.Clear();
            else delegateThread = new SyncDelegateThread();

            if (coroutineExecuter != null)
                coroutineExecuter.StopAllCoroutines();
            else coroutineExecuter = new CoroutineExecuter();

            modules = list.ToArray();
            foreach(var mdl in modules)
            {
                mdl.SetCore(this);
            }
            
        }

        #region unity

        private NativeTransform nt;
        private AsyncGameObject ago;
        //private Fraction selfFr;
        public AsyncTransform asyncTransform => nt.transform;
        public NativeTransform nativeTransform => nt;
        public AsyncGameObject nativeGameObject => ago;
        //public Fraction SelfFraction => selfFr;
        private void OnEnable()
        {
            if (modules != null)
            {
                foreach (var mdl in modules)
                    mdl.OnCoreEnabled();
            }

            asyncEnabled = true;
            
        }
        private void OnDisable()
        {
            if (modules != null)
            {
                foreach (var mdl in modules)
                    mdl.OnCoreDisabled();
            }

            asyncEnabled = false;
        }
        private void Awake()
        {
            //selfFr = GetComponentInChildren<Fraction>();
            UnityAsyncCoreHandle.Self.RegisterCall(ThreadDefinition.aiThread, this);
            delegateThread = new SyncDelegateThread();
            coroutineExecuter = new CoroutineExecuter();
            asyncEnabled = gameObject.activeInHierarchy;
            nt = GetComponent<NativeTransform>();
            if (nt == null)
                nt = gameObject.AddComponent<NativeTransform>();
            ago = GetComponent<AsyncGameObject>();
            if (ago == null)
                ago = gameObject.AddComponent<AsyncGameObject>();
            
        }
        private void OnDestroy()
        {
            if (modules != null)
            {
                foreach (var mdl in modules)
                    mdl.OnCoreDestroy();
            }

            UnityAsyncCoreHandle.Self?.RemoveCall(ThreadDefinition.aiThread, this);
        }
        void Update()
        {
            delegateThread.Update();

            coroutineExecuter.Update(Time.deltaTime);
        }
        private void OnApplicationQuit()
        {
            UnityAsyncCoreHandle.Self.RemoveCall("AI thread", this);
        }
        #endregion

        #region coroutines
        private CoroutineExecuter coroutineExecuter;

        public bool IsCoroutineActive(int reference) => coroutineExecuter.IsActiveCoroutine(reference);
        public int StartCoroutine(IEnumerator<float> routine) => coroutineExecuter.StartCoroutine(routine);
        public bool StopCoroutine(int reference) => coroutineExecuter.StopCoroutine(reference);
        public new void StopAllCoroutines() => coroutineExecuter.StopAllCoroutines();
        #endregion

        #region iasync
        private bool asyncEnabled = true;
        private double _deltaTime;
        public double DeltaTime => _deltaTime;
        bool ISupportAsyncCall.enabledUpdate => asyncEnabled;
        bool ISupportAsyncCall.enabledLateUpdate => asyncEnabled;
        void ISupportAsyncCall.AsyncLateUpdate(TimeSpan updateDelay)
        {
            _deltaTime = updateDelay.TotalSeconds;

            if (modules is null) return;

            foreach(var mdl in modules)
            {
                try
                {
                    if(mdl.Enabled)
                        mdl.LateUpdate();
                }
                catch(Exception e)
                {
                    Debug.LogError($"AI mdl \"{mdl.MDL_name}\" in {mdl.GetType().Name}.LateUpdate() was aborted on exception {e.Message}");
                    Debug.LogException(e);
                }
            }
        }

        void ISupportAsyncCall.AsyncUpdate(TimeSpan updateDelay)
        {
            _deltaTime = updateDelay.TotalSeconds;

            if (modules is null) return;

            foreach (var mdl in modules)
            {
                try
                {
                    if (mdl.Enabled)
                        mdl.Update();
                }
                catch (Exception e)
                {
                    Debug.LogError($"AI mdl \"{mdl.MDL_name}\" in {mdl.GetType().Name}.Update() was aborted on exception {e.Message}");
                    Debug.LogException(e);
                }
            }
        }
        #endregion
        #region sync
        SyncDelegateThread delegateThread;
        /// <summary>
        /// Выполняет действие в основном потоке юнити
        /// </summary>
        /// <param name="action"></param>
        /// <param name="executeType"></param>
        public void SyncExecute(Action action, ExecuteType executeType)
        {
            switch(executeType)
            {
                case ExecuteType.Default:
                    delegateThread.Execute(action);
                    return;
                case ExecuteType.Important:
                    delegateThread.ImportantExecute(action);
                    return;
            }
        }
        /// <summary>
        /// Выполняет операцию в асинхронном потоке, поддерживая возвращение значения
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="task"></param>
        /// <returns></returns>
        public UnityTask<T> AsyncRun<T>(Func<T> task)
        {
            var utask = UnityTaskDispatcher.Async(task, ExecuteType.Default);
            return utask;
        }
        public UnityTask<T> AsyncRunFast<T>(Func<T> task)
        {
            var utask = UnityTaskDispatcher.Async(task, ExecuteType.Important);
            return utask;
        }
        public T SyncRun<T>(Func<T> task)
        {
            var utask = UnityTaskDispatcher.Async(task, ExecuteType.Important);
            return utask.AsyncEnd();
        }
        #endregion



    }

    

    public interface IAIModule
    {
        /// <summary>
        /// Имя модуля
        /// </summary>
        string MDL_name { get; }
        /// <summary>
        /// Активен ли модуль
        /// </summary>
        bool Enabled { get; set; }
        /// <summary>
        /// Ядро модуля
        /// </summary>
        AICore Core { get; }
        void SetCore(AICore core);
        int Priority { get; }
        void Update();
        void LateUpdate();

        void OnCoreEnabled();
        void OnCoreDisabled();
        void OnCoreDestroy();
    }
    public abstract class AIModule : IAIModule
    {
        private AICore _core;
        public AICore Core => _core;

        public void SetCore(AICore newCore)
        {
            if (_core != newCore)
            {
                var oldCore = _core;
                _core = newCore;
                OnCoreChanged(oldCore);
            }
        }

        public abstract string MDL_name { get; }
        public abstract int Priority { get; }
        public abstract void Update();
        public virtual void LateUpdate() { }

        //props
        private bool _enabled = true;
        public bool Enabled
        {
            get => _enabled;
            set
            {
                if(_enabled!=value)
                {
                    if (value)
                        OnEnabled();
                    else
                        OnDisabled();
                    _enabled = value;
                }
            }
        }

        //callbacks
        protected virtual void OnCoreChanged(AICore oldCore) { }
        protected virtual void OnEnabled() { }
        protected virtual void OnDisabled() { }
        public virtual void OnCoreEnabled() { }
        public virtual void OnCoreDisabled() { }
        public virtual void OnCoreDestroy() { }
    }


    
}

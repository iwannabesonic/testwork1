using Core.Async.LowLevel;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Core.Async
{
    public class UnityAsyncCoreHandle : MonoBehaviour
    {
        private static UnityAsyncCoreHandle _self;

        public static UnityAsyncCoreHandle Self
        {
            get
            {
                if (_self == null)
                    _self = GameObject.FindObjectOfType<UnityAsyncCoreHandle>();
                return _self;
            }
        }
        public static AsyncCore Core => Self.core;
        public static AsyncTime Time => Self.asyncTime;

        private AsyncCore core;

        private void Awake()
        {
            if (_self == null)
            {
                _self = this;
                Init();
            }
            else if (_self != this)
                Destroy(gameObject);
        }
        
        
        void Init()
        {
            core = new AsyncCore();
            core.Execute();
        }

        private void OnApplicationQuit()
        {
            Dispose();
        }

        private void OnDestroy()
        {
            Dispose();
        }

        private void Dispose()
        {
            if (core !=null && core.IsExecuted)
                core.Dispose();
            core = null;
        }

        #region unity
        private AsyncTime asyncTime;

        private void Update()
        {
            asyncTime = AsyncTime.GetTime();
        }
        #endregion

        public void RegisterCall(string threadName, ISupportAsyncCall call)
        {
            var thread = core.GetThread(threadName);
            if (thread is null)
            {
                thread = new NativeThread(threadName);
                core.RegisterNewThread(threadName, thread);
                Debug.Log($"New thread {threadName} was created");
            }

            thread.Add(call);
        }
        public bool RemoveCall(string threadName, ISupportAsyncCall call)
        {
            if (core == null)
                return false;

            var thread = core.GetThread(threadName);
            if (thread != null)
            {
                return thread.Remove(call);
            }
            else return false;
        }
    }

    /// <summary>
    /// Time like a unity time. Update every unity frame. Can be used in other threads
    /// </summary>
    public readonly struct AsyncTime
    {
        private static AsyncTime lastTime;
        public static AsyncTime Async() => lastTime;
        /// <summary>
        /// Call only in unity thread
        /// </summary>
        /// <returns></returns>
        public static AsyncTime GetTime()
        {
            lastTime = new AsyncTime(Time.deltaTime, Time.unscaledDeltaTime, Time.time, Time.timeScale, Time.unscaledTime);
            return lastTime;
        }
        public AsyncTime(float deltaTime, float unscaledDeltaTime, float time, float timeScale, float unscaledTime)
        {
            this.deltaTime = deltaTime;
            this.unscaledDeltaTime = unscaledDeltaTime;
            this.time = time;
            this.timeScale = timeScale;
            this.unscaledTime = Time.unscaledTime;
        }

        public float deltaTime { get; }
        public float unscaledDeltaTime { get; }
        public float time { get; }
        public float unscaledTime { get; }
        public float timeScale { get; }
    }
}

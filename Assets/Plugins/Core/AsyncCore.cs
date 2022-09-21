using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using UnityEngine;

namespace Core.Async.LowLevel
{
    public interface IExecutable : IDisposable
    {
        void Execute();
        void Terminate();

        bool IsExecuted { get; }
    }
    
    public class AsyncCore : IExecutable, IEnumerable<NativeThread>
    {
        private Dictionary<string, NativeThread> list = new Dictionary<string, NativeThread>();

        public void RegisterNewThread(string key, NativeThread thread)
        {
            list.Add(key, thread);
            if (isRunning)
                if (!thread.IsExecuted)
                    thread.Execute();
        }
        public NativeThread GetThread(string key)
        {
            if (list.TryGetValue(key, out var thread))
                return thread;
            else return null;
        }
        public void TerminateThread(string key)
        {
            if (list.TryGetValue(key, out var thread))
            {
                if (thread.IsExecuted)
                    thread.Terminate();
                list.Remove(key);
            }
        }

        bool isRunning = false;
        private bool disposedValue;

        public void Execute()
        {
            if (isRunning) throw new MethodAccessException("Core is already executed");
            isRunning = true;
            foreach(var pair in list)
            {
                var thread = pair.Value;
                if (!thread.IsExecuted)
                    thread.Execute();
            }
        }
        public void Terminate()
        {
            if (!isRunning) throw new MethodAccessException("Core is not executed");
            isRunning = false;

            foreach (var pair in list)
            {
                var thread = pair.Value;
                if (thread.IsExecuted)
                    thread.Terminate();
            }
        }
        public void Abort()
        {
            isRunning = false;
            foreach(var pair in list)
            {
                try
                {
                    pair.Value.Abort();
                }
                catch (Exception) { }
            }
        }
        public bool IsExecuted => isRunning;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: освободить управляемое состояние (управляемые объекты)
                }

                // TODO: освободить неуправляемые ресурсы (неуправляемые объекты) и переопределить метод завершения
                if(isRunning)
                {
                    Terminate();
                }
                // TODO: установить значение NULL для больших полей
                disposedValue = true;
            }
        }

        // // TODO: переопределить метод завершения, только если "Dispose(bool disposing)" содержит код для освобождения неуправляемых ресурсов
        ~AsyncCore()
        {
            // Не изменяйте этот код. Разместите код очистки в методе "Dispose(bool disposing)".
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            // Не изменяйте этот код. Разместите код очистки в методе "Dispose(bool disposing)".
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        IEnumerator<NativeThread> IEnumerable<NativeThread>.GetEnumerator()
        {
            foreach (var pair in list)
                yield return pair.Value;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return (this as IEnumerable<NativeThread>).GetEnumerator();
        }
    }

    public class NativeThread : IExecutable
    {
        public const long ticksPerMillisecond = 10_000;

        private Thread thread;
        private bool isRunning = false;
        private string threadName = null;
        private List<ISupportAsyncCall> asyncCalls;
        private TimeSpan delayUpdate = new TimeSpan(ticksPerMillisecond * 50);//20 updates per second
        private TimeSpan lastUpdateTakeTime, lastLoopCallTime;
        public TimeSpan TrueDelay => lastUpdateTakeTime;
        public TimeSpan LasLoopCallTime => lastLoopCallTime;
        public TimeSpan CurrentTime => createThreadWatch.Elapsed;
        Stopwatch createThreadWatch = new Stopwatch();
        public double IdleTime { get; private set; }
        public string Name
        {
            get
            {
                if (isRunning)
                {
                    return thread.Name;
                }
                else throw new AccessViolationException("Thread is not executed");
            }
            set
            {
                if(isRunning)
                {
                    thread.Name = value;
                    threadName = value;
                }
                else throw new AccessViolationException("Thread is not executed");
            }
        }
        public TimeSpan Delay
        {
            get => delayUpdate;
            set
            {
                if (value < TimeSpan.Zero)
                    throw new ArgumentOutOfRangeException("Delay must be zero or highter");
                delayUpdate = value;
            }
        }

        public void Join()
        {
            if(isRunning)
            {
                Terminate();
                thread.Join();
            }
        }

        public void Execute()
        {
            if (isRunning) throw new MethodAccessException("Thread is already executed");

            isRunning = true;

            thread = new Thread(Loop);
            if(threadName!=null)
            thread.Name = threadName;
            thread.Start();
            loopWatch.Start();
            createThreadWatch.Restart();
        }
        public void Terminate()
        {
            if(!isRunning) throw new MethodAccessException("Thread is not executed");
            isRunning = false;
            loopWatch.Stop();
            createThreadWatch.Stop();
        }
        public void Abort()
        {
            Terminate();
            thread.Abort();
        }
        public bool IsExecuted => isRunning;

        Stopwatch loopWatch = new Stopwatch();
        private bool disposedValue;

        private void Loop()
        {
            ISupportAsyncCall call;
            while(isRunning)
            {
                lastLoopCallTime = createThreadWatch.Elapsed;
                //update
                int length = asyncCalls.Count;
                for(int i =0;i<length;i++)
                {
                    try
                    {
                        call = asyncCalls[i];
                        if (call.enabledUpdate)
                            call.AsyncUpdate(lastUpdateTakeTime);

                        if (!isRunning)
                            goto EXIT;
                    }
                    catch(ArgumentOutOfRangeException)
                    { continue; }
                    catch(Exception e)
                    {
                        UnityEngine.Debug.LogException(e);
                    }
                }

                //lateupdate
                for (int i = 0; i < length; i++)
                {
                    try
                    {
                        call = asyncCalls[i];
                        if (call.enabledLateUpdate)
                            call.AsyncLateUpdate(lastUpdateTakeTime);

                        if (!isRunning)
                            goto EXIT;
                    }
                    catch (ArgumentOutOfRangeException)
                    { continue; }
                    catch (Exception e)
                    {
                        UnityEngine.Debug.LogException(e);
                    }
                }

                var lUTTime = loopWatch.Elapsed;
                var delta = delayUpdate - lUTTime;

                if (delta < TimeSpan.Zero)
                {
                    Thread.Sleep(0);
                }
                else
                {
                   
                    Thread.Sleep(delta);
                }

                lastUpdateTakeTime = loopWatch.Elapsed;
                IdleTime = (1d - lUTTime.TotalMilliseconds / delayUpdate.TotalMilliseconds);
                loopWatch.Restart();
            }

            EXIT:
            {

            }
        }

        public void Add(ISupportAsyncCall call)
        {
            if (!asyncCalls.Contains(call))
                asyncCalls.Add(call);
        }
        public bool Remove(ISupportAsyncCall call)
        {
            return asyncCalls.Remove(call);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: освободить управляемое состояние (управляемые объекты)
                }

                // TODO: освободить неуправляемые ресурсы (неуправляемые объекты) и переопределить метод завершения
                if (isRunning)
                {
                    Terminate();
                    thread.Join();
                }
                // TODO: установить значение NULL для больших полей
                disposedValue = true;
            }
        }

        // // TODO: переопределить метод завершения, только если "Dispose(bool disposing)" содержит код для освобождения неуправляемых ресурсов
        ~NativeThread()
        {
            // Не изменяйте этот код. Разместите код очистки в методе "Dispose(bool disposing)".
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            // Не изменяйте этот код. Разместите код очистки в методе "Dispose(bool disposing)".
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public NativeThread()
        {
            asyncCalls = new List<ISupportAsyncCall>();
        }
        public NativeThread(string threadName) : this()
        {
            this.threadName = threadName;
        }
        public NativeThread(ICollection<ISupportAsyncCall> calls)
        {
            asyncCalls = new List<ISupportAsyncCall>(calls);
        }
        public NativeThread(string threadName, ICollection<ISupportAsyncCall> calls)
        {
            this.threadName = threadName;
            asyncCalls = new List<ISupportAsyncCall>(calls);
        }
    }
    public interface ITransform
    {
         Vector3 position { get; }
         Vector3 localPosition { get; }
         Quaternion rotation { get; }
         Quaternion localRotation { get; }
         Vector3 eulerAngles { get; }
         Vector3 localEulerAngles { get; }
         Vector3 localScale { get; }
         Vector3 forward { get; }
         Vector3 up { get; }
         Vector3 right { get; }
    }
    public readonly struct AsyncTransform : ITransform
    {
        public AsyncTransform(Vector3 position,
            Vector3 localPosition,
            Quaternion rotation,
            Quaternion localRotation,
            Vector3 eulerAngles,
            Vector3 localEulerAngles,
            Vector3 localScale,
            Vector3 forward,
            Vector3 up,
            Vector3 right)
        {
            this.position = position;
            this.localPosition = localPosition;
            this.rotation = rotation;
            this.localRotation = localRotation;
            this.eulerAngles = eulerAngles;
            this.localEulerAngles = localEulerAngles;
            this.forward = forward;
            this.up = up;
            this.right = right;
            this.localScale = localScale;
        }

        public AsyncTransform(Transform transform)
        {
            position = transform.position;
            localPosition = transform.localPosition;
            rotation = transform.rotation;
            localRotation = transform.localRotation;
            eulerAngles = transform.eulerAngles;
            localEulerAngles = transform.localEulerAngles;
            forward = transform.forward;
            right = transform.right;
            up = transform.up;
            localScale = transform.localScale;
        }

        public readonly Vector3 position;
        public readonly Vector3 localPosition;
        public readonly Quaternion rotation;
        public readonly Quaternion localRotation;
        public readonly Vector3 eulerAngles;
        public readonly Vector3 localEulerAngles;
        public readonly Vector3 localScale;
        public readonly Vector3 forward;
        public readonly Vector3 up;
        public readonly Vector3 right;

        Vector3 ITransform.position => position;
        Vector3 ITransform.localPosition => localPosition;
        Quaternion ITransform.rotation => rotation;
        Quaternion ITransform.localRotation => localRotation;
        Vector3 ITransform.eulerAngles => eulerAngles;
        Vector3 ITransform.localEulerAngles => localEulerAngles;
        Vector3 ITransform.localScale => localScale;
        Vector3 ITransform.forward => forward;
        Vector3 ITransform.up => up;
        Vector3 ITransform.right => right;
    }
}

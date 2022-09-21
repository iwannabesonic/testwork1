using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Core.Async.Tasks
{
    /// <summary>
    /// Диспетчер вызова синхронных задач в основном потоке
    /// </summary>
    [DefaultExecutionOrder(100)]
    public class UnityTaskDispatcher : UnitySingleton<UnityTaskDispatcher>, ISyncDelegateThread
    {
        
        #region syncThread
        private SyncDelegateThread delegateThread = new SyncDelegateThread();

        public int ActionQueneCount => ((ISyncDelegateThread)delegateThread).ActionQueneCount;

        public int ImportantActionQueneCount => ((ISyncDelegateThread)delegateThread).ImportantActionQueneCount;

        public void Execute(Action action)
        {
            delegateThread.Execute(action);
        }

        public void ImportantExecute(Action action)
        {
            delegateThread.ImportantExecute(action);
        }
        #endregion

        public static int QueneCount => _self.ActionQueneCount;
        public static int ImportantQueneCount => _self.ImportantActionQueneCount;

        public int ManagedThreadID { get; private set; }

        void Update()
        {
            delegateThread.Update();
        }

        private List<object> schedulers = new List<object>();

        protected override void SingletonAwake()
        {
            ManagedThreadID = Thread.CurrentThread.ManagedThreadId;
        }

        /// <summary>
        /// Выполняет синхронную операцию, приостанавливая текущий поток до возвращения результатов
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="func"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        private T internal_Sync<T>(in Func<T> func, ExecuteType type)
        {
            return GetScheduler<T>().Schedule(func).SyncStart(delegateThread,type);
        }

        /// <summary>
        /// Выполняет задачу синхронно
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="func"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static T Sync<T>(in Func<T> func, ExecuteType type) => Self.internal_Sync(func, type);
        /// <summary>
        /// Выполняет задачу синхронно. <see cref="ExecuteType"/> = <see cref="ExecuteType.Important"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="func"></param>
        /// <returns></returns>
        public static T Sync<T>(in Func<T> func) => Self.internal_Sync(func, ExecuteType.Important);

        private UnityTask<T> internal_Async<T>(in Func<T> func, ExecuteType type)
        {
            return GetScheduler<T>().Schedule(func).AsyncStart(delegateThread, type);
        }

        /// <summary>
        /// Выполняет задачу асинхронно. Значение необходимо явно вернуть через <see cref="UnityTask{T}.AsyncEnd"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="func"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static UnityTask<T> Async<T>(in Func<T> func, ExecuteType type) => Self.internal_Async(func, type);
        /// <summary>
        /// Выполняет задачу асинхронно. Значение необходимо явно вернуть через <see cref="UnityTask{T}.AsyncEnd"/>. <see cref="ExecuteType"/> = <see cref="ExecuteType.Important"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="func"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static UnityTask<T> Async<T>(in Func<T> func) => Self.internal_Async(func, ExecuteType.Important);

        public UnityTaskScheduler<T> GetScheduler<T>()
        {
            foreach(var itm in schedulers)
            {
                if(itm is UnityTaskScheduler<T> shT)
                {
                    return shT;
                }
            }

            UnityTaskScheduler<T> sh = new UnityTaskScheduler<T>();
            schedulers.Add(sh);
            return sh;
        }

       
    }

    
    public sealed class UnityTaskScheduler<T>
    {
        private Queue<UnityTask<T>> freeTaskQuene = new Queue<UnityTask<T>>();
        //private Queue<UnityTask<T>> busyTaskQuene = new Queue<UnityTask<T>>();

        public UnityTask<T> Schedule(in Func<T> func)
        {
            return DenqueneFree(func);
        }

        private UnityTask<T> DenqueneFree(in Func<T> func)
        {
            UnityTask<T> task = null;
            if (freeTaskQuene.Count > 0)
            {
                task = freeTaskQuene.Dequeue();
                UnityTask<T>.Reallocate(task, func);
            }
            else
            {
                task = new UnityTask<T>(func);
            }

            task.OnTaskEnd += EnqueneFree;

            return task;
        }

        private void EnqueneFree(UnityTask<T> task)
        {
            freeTaskQuene.Enqueue(task);
        }
    }

    public interface IUnityTask
    {
        void Abort();
        object SyncStart(ISyncDelegateThread th, ExecuteType tp);
        IUnityTask AsyncStart(ISyncDelegateThread th, ExecuteType tp);
        object AsyncEnd();
        object Result { get; }
        //bool IsCompleted { get; }
        bool IsRunning { get; }
        //event Action OnTaskComplete;
        //event Action<Exception> OnTaskAbort;
        //event Action<IUnityTask> OnTaskEnd;
    }
    /// <summary>
    /// Представляет собой асинхронную операцию, выполняемую в UnityPlayerLoop потоке, которая возвращает значение
    /// </summary>
    /// <typeparam name="T">Тип возвращаемого значения</typeparam>
    public sealed partial class UnityTask<T> : IUnityTask, IAsyncResult
    {
        private static readonly TimeSpan STATIC_waitTime = new TimeSpan(0, 0, 0, 0, 1);

        private T returnedValue;
        private Func<T> executedAction;
        private bool isCompleted,
            isRunning,
            isAborted;
        //private bool autoThrowException;
        private Exception throwedException;
        private object asyncState;
        private bool? runSync;

        public event Action<T> OnTaskComplete;
        public event Action<Exception> OnTaskAbort;
        public event Action<UnityTask<T>> OnTaskEnd;

        #region itask
        object IUnityTask.SyncStart(ISyncDelegateThread th, ExecuteType tp) => SyncStart(th, tp);
        object IUnityTask.AsyncEnd() => AsyncEnd();
        IUnityTask IUnityTask.AsyncStart(ISyncDelegateThread th, ExecuteType tp) => AsyncStart(th, tp);
        object IUnityTask.Result => Result;
        #endregion

        public UnityTask(Func<T> func)
        {
            //this.autoThrowException = autoThrowException;
            if (func == null)
            {
                isCompleted = true;
                isRunning = false;
                isAborted = false;
                return;
            }
           
            executedAction = func;
            isCompleted = false;
            isRunning = false;
            isAborted = false;
            asyncState = null;
            runSync = null;
        }

        public static void Reallocate(UnityTask<T> task, Func<T> func)
        {
            if (func == null)
            {
                task.isCompleted = true;
                task.isRunning = false;
                task.isAborted = false;
                return;
            }

            task.executedAction = func;
            task.isCompleted = false;
            task.isRunning = false;
            task.isAborted = false;

            task.OnTaskAbort = null;
            task.OnTaskComplete = null;
            task.OnTaskEnd = null;

            task.asyncState = null;
            task.runSync = null;
        }

        /// <summary>
        /// Завершает операцию преждевременно
        /// </summary>
        public void Abort()
        {
            throwedException = new OperationCanceledException();
            isAborted = true;
            isRunning = false;
        }

        /// <summary>
        /// Запускает операцию в другом потоке с возвращением значения в исходных поток
        /// </summary>
        /// <param name="thread">Асинхронный поток, в котором будут производиться вычисления</param>
        /// <param name="executeType">Тип вызова</param>
        /// <returns></returns>
        public T SyncStart(ISyncDelegateThread thread, ExecuteType executeType, object asyncState = null)
        {
            if (isRunning) throw new InvalidOperationException("Task is started");

            if (isCompleted && !isAborted) return returnedValue;
            if (isCompleted && isAborted) throw throwedException;

            this.asyncState = asyncState;
            runSync = true;

            isRunning = true;
            Action del = delegate {
                try
                {
                    returnedValue = this.executedAction.Invoke();
                    isCompleted = true;
                    isRunning = false;
                }
                catch (Exception e)
                {
                    isCompleted = true;
                    isRunning = false;
                    isAborted = true;
                    throwedException = e;
                }
            };

            switch (executeType)
            {
                case ExecuteType.Default:
                    thread.Execute(del);
                    break;
                case ExecuteType.Important:
                    thread.ImportantExecute(del);
                    break;
            }

            while (isRunning)
            {
                Thread.Sleep(STATIC_waitTime);
                continue;
            } //wait

            if (isAborted)
            {
                OnTaskAbort?.Invoke(throwedException);
                throw throwedException;
            }
            else
            {
                OnTaskComplete?.Invoke(returnedValue);
                OnTaskEnd?.Invoke(this);
                return returnedValue;
            }
        }

        /// <summary>
        /// Запускает операцию в другом потоке. Возвращаемое значение необходимо взять явно
        /// </summary>
        /// <param name="thread">Асинхронный поток, в котором будут производиться вычисления</param>
        /// <param name="executeType">Тип вызова</param>
        /// <returns></returns>
        public UnityTask<T> AsyncStart(ISyncDelegateThread thread, ExecuteType executeType, object asyncState = null)
        {
            if (isRunning) throw new InvalidOperationException("Task is started");

            if (isCompleted) return this;

            this.asyncState = asyncState;
            runSync = false;

            isRunning = true;
            Action del = delegate {
                try
                {
                    returnedValue = this.executedAction.Invoke();
                    isCompleted = true;
                    isRunning = false;
                }
                catch (Exception e)
                {
                    isCompleted = true;
                    isRunning = false;
                    isAborted = true;
                    throwedException = e;
                }
            };

            switch (executeType)
            {
                case ExecuteType.Default:
                    thread.Execute(del);
                    break;
                case ExecuteType.Important:
                    thread.ImportantExecute(del);
                    break;
            }

            return this;
        }

        private T InlineSyncRun()
        {
            try
            {
                isRunning = false;
                returnedValue = this.executedAction();
                OnTaskComplete?.Invoke(returnedValue);
                return returnedValue;
            }
            catch(Exception e)
            {
                throwedException = e;
                isAborted = true;
                OnTaskAbort?.Invoke(throwedException);
                throw e;
            }
            finally
            {
                isCompleted = true;
                OnTaskEnd?.Invoke(this);
            }
        }

        /// <summary>
        /// Возвращает значение, после вызова AsyncStart или Start. Если операция незавершена, будет произведено ожидание
        /// </summary>
        /// <returns></returns>
        public T AsyncEnd()
        {
            if (isCompleted)
            {
                if (isAborted) throw throwedException;
                return returnedValue;
            }

            if (IsMainThread())
                throw new InvalidOperationException("Can't get result in main thread while operation not completed");
            while (isRunning)
            {
                Thread.Sleep(STATIC_waitTime);
                continue;
            } //wait

            if (isAborted)
            {
                OnTaskAbort?.Invoke(throwedException);
                OnTaskEnd?.Invoke(this);
                throw throwedException;
            }
            else
            {
                OnTaskComplete?.Invoke(returnedValue);
                OnTaskEnd?.Invoke(this);
                return returnedValue;
            }
        }

        public bool IsCompleted => isCompleted;
        public bool IsRunning => isRunning;
        /// <summary>
        /// Возвращает значение. Если задача не завершена, будет произведено ожидание
        /// </summary>
        public T Result
        {
            get
            {          
                return AsyncEnd();
            }
        }

        public Awaiter GetAwaiter()
        {
            return new Awaiter(this);
        }

        public object AsyncState => asyncState;

        public WaitHandle AsyncWaitHandle => throw new NotSupportedException();

        public bool CompletedSynchronously
        {
            get
            {
                if (runSync.HasValue)
                    return runSync.Value;
                else throw new InvalidOperationException("Task not runned");
            }
        }

        public static bool IsMainThread()
        {
            return Thread.CurrentThread.ManagedThreadId == UnityTaskDispatcher.Self.ManagedThreadID;
        }

        public readonly struct Awaiter : ICriticalNotifyCompletion
        {
            private readonly UnityTask<T> source;

            public Awaiter(UnityTask<T> source)
            {
                this.source = source;
            }

            public void OnCompleted(Action continuation)
            {
                //source.AsyncEnd();
                continuation();
            }

            public void UnsafeOnCompleted(Action continuation)
            {
                //source.AsyncEnd();
                continuation();
            }

            public T GetResult()
            {
                return source.Result;
            }

            public bool IsCompleted => source.IsCompleted;
        }
    }

    public sealed partial class UnityTask<T>
    {
        public static UnityTask<T> Run(Func<T> func, ExecuteType executeType = ExecuteType.Important)
        {
            if (IsMainThread())
            {
                var task = UnityTaskDispatcher.Self.GetScheduler<T>().Schedule(func);
                task.InlineSyncRun();
                return task;
            }
            else
            {
                return UnityTaskDispatcher.Async(func, executeType);
            }
        }
    }
}

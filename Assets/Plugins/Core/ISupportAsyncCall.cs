using System;

namespace Core.Async
{
    public interface ISupportAsyncCall
    {
        bool enabledUpdate { get; }
        void AsyncUpdate(TimeSpan updateDelay);
        bool enabledLateUpdate { get; }
        void AsyncLateUpdate(TimeSpan updateDelay);
    }

    public static class ISupportAsyncCallExtentions
    {
        public static void RegisterSelf(this ISupportAsyncCall obj, string threadName)
        {
            UnityAsyncCoreHandle.Self.RegisterCall(threadName, obj);
        }
        public static void RemoveSelf(this ISupportAsyncCall obj, string threadName)
        {
            UnityAsyncCoreHandle.Self?.RemoveCall(threadName, obj);
        }
    }
}

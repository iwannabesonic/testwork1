using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace Core.Async
{
    public class NativeTransformDispatcher : UnitySingleton<NativeTransformDispatcher>
    {
        private List<NativeTransform> all = new List<NativeTransform>();
        private List<NativeTransform> importantAll = new List<NativeTransform>();
        private Queue<(NativeTransform, bool)> registerQueue = new Queue<(NativeTransform, bool)>();
        private Queue<NativeTransform> removeQueue = new Queue<NativeTransform>();

        private void RegisterNew(NativeTransform tr, bool important)
        {
            if (all.Contains(tr) || importantAll.Contains(tr))
                return;

            var list = important ? importantAll : all;
            list.Add(tr);

        }

        private void Remove(NativeTransform tr)
        {
            if (!all.Remove(tr))
            {
                importantAll.Remove(tr);
            }
        }

        private const int maxEnumeratePerFrame = 1000;
        private const int minEnumeratePerFrame = 300;
        private bool isEnumerating = false;
        private int pivot = 0;

        int impCount = 0,
            reqEnumCount;
        private void Update()
        {
            if (!isEnumerating)
            {
                DoRegisterQueue();
                DoRemoveQueue();

                isEnumerating = true;
                pivot = 0;
                impCount = importantAll.Count;
            }

            reqEnumCount = maxEnumeratePerFrame - impCount;
            if (reqEnumCount < minEnumeratePerFrame)
                reqEnumCount = minEnumeratePerFrame;
            if (reqEnumCount > all.Count - pivot)
                reqEnumCount = all.Count - pivot;

            for (int i = 0; i < impCount; i++)
                importantAll[i].UpdateNative();

            var prePivot = pivot;
            for(int i = prePivot; i<prePivot+reqEnumCount;i++)
            {
                pivot = i;
                all[i].UpdateNative();
            }

            if(pivot == all.Count-1)
            {
                isEnumerating = false;
            }
        }

        private void DoRegisterQueue()
        {
            while(registerQueue.TryDequeue(out var val))
            {
                RegisterNew(val.Item1, val.Item2);
            }
        }
        private void DoRemoveQueue()
        {
            while (removeQueue.TryDequeue(out var val))
            {
                Remove(val);
            }
        }

        public void EnqueueRegisterNew(NativeTransform tr, bool important)
        {
            registerQueue.Enqueue((tr, important));
        }
        public void EnqueueRemove(NativeTransform tr)
        {
            removeQueue.Enqueue(tr);
        }
    }
}

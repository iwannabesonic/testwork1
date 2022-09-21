using Core.Async;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Core
{
    /// <summary>
    /// Синхронный таймер для работы в потоке Unity
    /// </summary>
    public struct UnitySyncTimer
    {
        private readonly float initialTime;
        private float timer;
        private bool isRunned;
        public float StartTime => initialTime;

        public UnitySyncTimer(float time)
        {
            this.timer = initialTime = time;
            isRunned = true;
            IsLooped = false;
        }

        public void Start() => isRunned = true;
        public void Stop() => isRunned = false;
        public void Reset()
        {
            isRunned = false;
            timer = initialTime;
        }
        public void Reset(float newTime)
        {
            isRunned = false;
            timer = newTime;
        }
        public void Restart()
        {
            isRunned = true;
            timer = initialTime;
        }
        public void Restart(float newTime)
        {
            isRunned = true;
            timer = newTime;
        }
        public bool IsRunned => isRunned;
        public bool IsStoped => !isRunned;
        public bool IsLooped { get; set; }
        public float TimeLeft => timer;
        public bool Sync(float deltaTime)
        {
            if (!isRunned) return false;
            timer -= deltaTime;
            if (timer < 0)
            {
                if (IsLooped)
                {
                    Restart();
                    return true;
                }
                else
                {
                    Stop();
                    return true;
                }
            }
            else return false;
        }
    }

    /// <summary>
    /// Асинхронный таймер на тиках Unity
    /// </summary>
    public struct UnityAsyncTimer
    {
        private readonly float initialTime;
        private float startTime;
        private float timer;
        private readonly bool useTimeScale;

        public bool Async()
        {
            if (useTimeScale)
            {
                var delta = AsyncTime.Async().time - startTime;
                if (delta >= timer)
                {
                    return true;
                }
                else return false;
            }
            else
            {
                var delta = AsyncTime.Async().unscaledTime - startTime;
                if (delta >= timer)
                {
                    return true;
                }
                else return false;
            }
        }

        public float TimeLeft
        {
            get
            {
                float delta;
                if (useTimeScale)
                {
                    delta = AsyncTime.Async().time - startTime;
                }
                else
                {
                    delta = AsyncTime.Async().unscaledTime - startTime;
                }

                var output = timer - delta;
                if (output > 0)
                    return output;
                else return 0;
            }
        }

        public void Restart()
        {
            timer = initialTime;
            if (useTimeScale)
                startTime = AsyncTime.Async().time;
            else
                startTime = AsyncTime.Async().unscaledTime;
        }

        public UnityAsyncTimer(float time, bool useTimeScale)
        {
            this.useTimeScale = useTimeScale;
            timer = initialTime = time;
            if (useTimeScale)
                startTime = AsyncTime.Async().time;
            else
                startTime = AsyncTime.Async().unscaledTime;
        }
    }

}

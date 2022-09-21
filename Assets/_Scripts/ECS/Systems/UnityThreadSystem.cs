using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ECS
{
    public abstract class UnityThreadSystem : BaseSystem
    {
        protected sealed override string Thread => SystemsThreadDispatcher.unity_thread;
    }
}

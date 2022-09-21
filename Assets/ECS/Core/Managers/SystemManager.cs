using System;
using System.Collections.Generic;
using Debug = UnityEngine.Debug;
namespace ECS
{
    public abstract partial class BaseSystem
    {
        public sealed class SystemManager
        {
            private uint statecash = 0, validstatecash =0;
            private EntityWorld world;
            private Dictionary<int, (int, BaseSystem)> systems = new Dictionary<int, (int, BaseSystem)>();

            private BaseSystem[] executionList;

            public TSystem AddSystem<TSystem>() where TSystem : BaseSystem, new()
            {
                int currentTypeHash = GetTypeHash<TSystem>();
                if (systems.ContainsKey(currentTypeHash))
                    throw new InvalidOperationException($"System with type {typeof(TSystem).Name} already contains in world");

                var newSystem = new TSystem();
                newSystem.InternalInitialize(world.EntityManager, world);
                int priority = GetAttribute<TSystem>()?.Priority ?? 0;
                systems.Add(currentTypeHash, (priority, newSystem));
                statecash++;
                return newSystem;
            }

            public bool RemoveSystem<TSystem>() where TSystem:BaseSystem
            {
                var result = systems.Remove(GetTypeHash<TSystem>());
                if (result)
                    statecash++;
                return result;
            }

            public TSystem GetSystem<TSystem>() where TSystem : BaseSystem
            {
                var hash = GetTypeHash<TSystem>();
                if (systems.TryGetValue(hash, out var newSystem))
                    return newSystem.Item2 as TSystem;
                else return null;
            }

            [System.Obsolete]
            private void RegenerateExecutionList()//TODO Optimize sort algorithm
            {
                executionList = new BaseSystem[systems.Count];
                var values = systems.Values;

                List<(int, BaseSystem)> list = new List<(int, BaseSystem)>(values);
                list.Sort((x, y) =>
                {
                    if (x.Item1 > y.Item1) return 1;
                    else if (x.Item1 < y.Item1) return -1;
                    else return 0;
                });
                for (int i = 0; i < executionList.Length; i++)
                {
                    executionList[i] = list[i].Item2;
                }
            }

            private SystemPriorityAttribute GetAttribute<TSystem>() where TSystem:BaseSystem
            {
                foreach(object attr in typeof(TSystem).GetCustomAttributes(false))
                {
                    if (attr is SystemPriorityAttribute output)
                        return output;
                }
                return null;
            }

            private int GetTypeHash<TSystem>() where TSystem : BaseSystem
            {
                return typeof(TSystem).GetHashCode();
            }

            public SystemManager(EntityWorld world)
            {
                this.world = world;
            }
        }
    }

    /// <summary>
    /// Temporary not supported
    /// </summary>
    [System.AttributeUsage(System.AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    sealed class SystemPriorityAttribute : System.Attribute
    {
        // See the attribute guidelines at 
        //  http://go.microsoft.com/fwlink/?LinkId=85236
        readonly int priority;

        // This is a positional argument
        public SystemPriorityAttribute(int priority)
        {
            this.priority = priority;
        }

        public int Priority => priority; 
    }
}

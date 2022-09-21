using System;
using System.Collections;
using System.Collections.Generic;
namespace ECS
{
    public class EntityDynamicList : IEnumerable<EntityDynamicList.Handler>
    {
        public class Handler : IDisposable
        {
            private readonly Entity handle;
            private Dictionary<int, IList<IComponent>> handledComponents;
            private int _componentsCount = 0;
            private bool isDisposed = false; 
            public Entity Handle => handle;
            public bool IsDisposed => isDisposed;
            public bool IsEmpty => _componentsCount is 0;

            private class ComponentComparer : IEqualityComparer<int>
            {
                public bool Equals(int x, int y) => x == y;

                public int GetHashCode(int obj) => obj;
            }

            public T GetComponent<T>(int hashCode) where T : IComponent
            {
                if (handledComponents.TryGetValue(hashCode, out var component))
                    return component.Count > 0 ? (T)component[0] : default(T);
                else return default(T);
            }

            public bool TryGetComponent<T>(int hashCode, out T component) where T :IComponent
            {
                var result = handledComponents.TryGetValue(hashCode, out var output);
                if(result && output.Count > 0 && output[0] is T raw)
                {
                    component = raw;
                    return true;
                }
                else
                {
                    component = default(T);
                    return false;
                }
            }

            public void GetComponents<T>(int hashCode, IList<T> list) where T :IComponent
            {
                if (handledComponents.TryGetValue(hashCode, out var component) && component.Count > 0 && component[0] is T)
                {
                    foreach(T cp in component)
                    {
                        if (cp != null)
                            list.Add(cp);
                    }
                }
            }

            public bool TryGetComponents<T>(int hashCode, IList<T> list) where T : IComponent
            {
                if (handledComponents.TryGetValue(hashCode, out var component) && component.Count > 0 && component[0] is T)
                {
                    foreach (T cp in component)
                    {
                        if (cp != null)
                            list.Add(cp);
                    }
                    return true;
                }
                else return false;
            }

            public void Dispose()
            {
                if (isDisposed)
                    return;
                isDisposed = true;
                handle.OnAddNewComponentHandler -= Handle_OnAddNewComponentHandler;
                handle.OnRemoveNewComponentHandler -= Handle_OnRemoveNewComponentHandler;
                handledComponents = null;
                _componentsCount = 0;
            }

            public Handler(Entity handle, in Type[] handledTypes)
            {
                if (handledTypes == null || handledTypes.Length == 0)
                    throw new ArgumentException("Require minimum 1 component type");

                this.handle = handle;
                List<IComponent> tempList = new List<IComponent>(16);
                handledComponents = new Dictionary<int, IList<IComponent>>(8, new ComponentComparer());
                foreach(var type in handledTypes)
                {
                    handle.GetComponents(type, tempList, true);
                    handledComponents.Add(type.GetHashCode(), new List<IComponent>(tempList));
                    _componentsCount += tempList.Count;
                }

                handle.OnAddNewComponentHandler += Handle_OnAddNewComponentHandler;
                handle.OnRemoveNewComponentHandler += Handle_OnRemoveNewComponentHandler;
            }

            private void Handle_OnRemoveNewComponentHandler(Entity _, IComponent component) => RemoveComponent(component);
            private void Handle_OnAddNewComponentHandler(Entity _, IComponent component) => AddComponent(component);


            private void AddComponent(IComponent component)
            {
                var hashCode = component.GetType().GetHashCode();
                if(handledComponents.TryGetValue(hashCode, out var list))
                {
                    list.Add(component);
                    _componentsCount++;
                }
            }

            private void RemoveComponent(IComponent component)
            {
                var hashCode = component.GetType().GetHashCode();
                if (handledComponents.TryGetValue(hashCode, out var list))
                {
                    list.Remove(component);
                    _componentsCount--;
                }
            }
        }

        private Type[] typeMask;
        private List<Handler> handledEntities;

        private void AddEntity(Entity entity)
        {
            var handler = new Handler(entity, typeMask);
            handledEntities.Add(handler);
        }

        private void RemoveEntity(Entity entity)
        {
            Handler handler = null;
            foreach(var h in handledEntities)
                if(h.Handle == entity)
                {
                    handler = h;
                    break;
                }
            if (handler == null)
                return;

            handledEntities.Remove(handler);
            handler.Dispose();
        }

        public EntityDynamicList(Entity.EntityManager manager, params Type[] typeMask)
        {
            handledEntities = new List<Handler>(manager.Count);
            this.typeMask = typeMask;

            foreach(var entity in manager)
            {
                AddEntity(entity);
            }

            manager.OnTransferNewEntityHandler += Manager_OnTransferNewEntityHandler;
            manager.OnRemoveEntityHandler += Manager_OnRemoveEntityHandler;
        }

        private void Manager_OnRemoveEntityHandler(Entity.EntityManager _, Entity arg2) => RemoveEntity(arg2);
        private void Manager_OnTransferNewEntityHandler(Entity.EntityManager _, Entity arg2) => AddEntity(arg2);

        public IEnumerator<Handler> GetEnumerator()
        {
            for(int i =0; i< handledEntities.Count; i++)
            {
                var selected = handledEntities[i];
                if(!selected.IsDisposed && !selected.IsEmpty)
                    yield return selected;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        public IEnumerable<Handler> GetActiveHandlers()
        {
            for (int i = 0; i < handledEntities.Count; i++)
            {
                var selected = handledEntities[i];
                if (!selected.IsDisposed && selected.Handle.Enabled)
                    yield return selected;
            }
        }
    }
}

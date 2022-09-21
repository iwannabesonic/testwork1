using System;
using System.Collections;
using System.Collections.Generic;
namespace ECS
{
    public sealed partial class Entity
    {
        public sealed class EntityManager : IEnumerable<Entity>
        {
            private readonly object _sync = new object();
            private Dictionary<Guid, Entity> handledEntities = new Dictionary<Guid, Entity>(128);
            private EntityWorld world;

            public event Action<EntityManager, Entity> OnTransferNewEntityHandler;
            public event Action<EntityManager, Entity> OnRemoveEntityHandler;
            public int Count => handledEntities.Count;

            public void TransferIn(Entity entity)
            {
                if (handledEntities.ContainsKey(entity.Reference))
                    return;

                if (entity.InWorld() && EntityWorld.TryGetWorld(entity.InWorldID, out var otherWorld))
                    otherWorld.EntityManager.RemoveEntity(entity);

                lock (_sync)
                {
                    handledEntities.Add(entity.Reference, entity);
                }
                entity._worldSettings.worldID = world.WorldID;
                OnTransferNewEntityHandler?.Invoke(this, entity);
            }

            private void RemoveEntity(Entity entity)
            {
                lock (_sync)
                {
                    handledEntities.Remove(entity.Reference);
                }
                OnRemoveEntityHandler?.Invoke(this, entity);
            }

            public Entity GetEntity(Guid reference)
            {
                if (handledEntities.TryGetValue(reference, out var entity))
                    return entity;
                else return null;
            }

            public void FillList(IList<Entity> list)
            {
                list.Clear();
                foreach (var entity in this)
                    list.Add(entity);
            }

            public void GetComponentsList<TComponent>(in IList<TComponent> list) where TComponent : IComponent
            {
                foreach(var entity in this)
                {
                    entity.GetComponents(in list, false);
                }
            }

            public EntityDynamicList GetDynamicList(params Type[] requireTypes)
            {
                return new EntityDynamicList(this, requireTypes);
            }

            public Entity FindEntityFromComponent<T>(T component) where T : IComponent
            {
                if(component == null)
                    return null;

                var id = component.Reference;
                foreach(var entity in this)
                {
                    if (entity.GetComponent<T>(id) != null)
                        return entity;
                }
                return null;
            }

            public IEnumerator<Entity> GetEnumerator()
            {
                lock (_sync)
                {
                    foreach (var entity in handledEntities.Values)
                        yield return entity;
                }
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            public EntityManager(EntityWorld world)
            {
                this.world = world;
            }
        }
    }
}

using System.Collections;
using System;

namespace ECS
{
    public abstract partial class BaseSystem : IDisposable
    {
        public const string defaultThread = "DefaultSystemsThread";
        private Entity.EntityManager _manager;
        private EntityWorld _world;
        private bool _isInternalEnabled = true, _isWorldEnabled = true;

        public bool IsActiveAndEnabled => _isInternalEnabled && _isWorldEnabled;
        public bool Enabled
        {
            get => _isInternalEnabled;
            set
            {
                _isInternalEnabled = value;
            }
        }
        protected Entity.EntityManager Manager => _manager;
        protected abstract string Thread { get; }
        protected virtual uint Delay => SystemsThreadDispatcher.defaultUpdateTimeForThreads_MS;
        protected abstract void Initialize(Entity.EntityManager entityManager);
        protected abstract void Update(double deltaTime);
        private void Main_EntryPoint(double deltaTime)
        {
            if (!IsActiveAndEnabled)
                return;
            Update(deltaTime);
        }

        private void InternalInitialize(Entity.EntityManager entityManager, EntityWorld world)
        {
            _manager = entityManager;
            entityManager.OnTransferNewEntityHandler += EntityManager_OnTransferNewEntityHandler;
            entityManager.OnRemoveEntityHandler += EntityManager_OnRemoveEntityHandler;
            world.OnEnabledWorldHandler += World_OnEnabledWorldHandler;
            world.OnDisableWorldHandler += World_OnDisableWorldHandler;
            _world = world;
            _isWorldEnabled = world.IsEnabled;
            Initialize(entityManager);

            SystemsThreadDispatcher.RegisterEntryPoint(Thread, Main_EntryPoint, Delay);
        }

        private void World_OnDisableWorldHandler(EntityWorld obj)
        {
            _isWorldEnabled = false;
        }

        private void World_OnEnabledWorldHandler(EntityWorld obj)
        {
            _isWorldEnabled = true;
        }

        private void EntityManager_OnRemoveEntityHandler(Entity.EntityManager arg1, Entity arg2) => OnEntityLeftWorld(arg2);
        private void EntityManager_OnTransferNewEntityHandler(Entity.EntityManager arg1, Entity arg2) => OnEntityEnterWorld(arg2);

        protected virtual void OnEntityEnterWorld(Entity entity) { }
        protected virtual void OnEntityLeftWorld(Entity entity) { }
        protected virtual void OnDisposed() { }

        public void Dispose()
        {
            _manager.OnTransferNewEntityHandler -= EntityManager_OnTransferNewEntityHandler;
            _manager.OnRemoveEntityHandler -= EntityManager_OnRemoveEntityHandler;
            _world.OnEnabledWorldHandler -= World_OnEnabledWorldHandler;
            _world.OnDisableWorldHandler -= World_OnDisableWorldHandler;
            SystemsThreadDispatcher.RemoveEntryPoint(Thread, Main_EntryPoint, Delay);
            OnDisposed();
        }
    }
}

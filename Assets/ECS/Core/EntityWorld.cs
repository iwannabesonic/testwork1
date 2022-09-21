using System;
using System.Collections.Generic;
using EntityManager = ECS.Entity.EntityManager;
using SystemManager = ECS.BaseSystem.SystemManager;
namespace ECS
{
    public abstract class EntityWorld
    {
        #region static
        public const int INVALID_WORLD = -1;
        public static EntityWorld Main
        {
            get
            {
                if (Worlds.TryGetValue(0, out var world))
                    return world;
                else return new MainEntityWorld();
            }
        }
        public static EntityWorld DefaultWorldInjection { get; set; }
        private static Dictionary<int, EntityWorld> Worlds = new Dictionary<int, EntityWorld>();

        public static bool TryGetWorld(int worldID, out EntityWorld world)
        {
            if (worldID == INVALID_WORLD)
            {
                world = null;
                return false;
            }
            else return Worlds.TryGetValue(worldID, out world);
        }
        #endregion

        private EntityManager entityManager;
        private SystemManager systemManager;
        private int worldID;
        private bool _isEnabled = true;

        public bool IsEnabled => _isEnabled;
        public int WorldID => worldID;
        public EntityManager EntityManager => entityManager;
        public SystemManager SystemManager => systemManager;
        public event Action<EntityWorld> OnEnabledWorldHandler;
        public event Action<EntityWorld> OnDisableWorldHandler;
        public void SetEnabled(bool val)
        {
            if (_isEnabled == val)
                return;
            _isEnabled = val;
            if (val)
                OnEnabledWorldHandler?.Invoke(this);
            else
                OnDisableWorldHandler?.Invoke(this);
        }

        public EntityWorld(int id)
        {
            if (id == INVALID_WORLD || Worlds.ContainsKey(id))
                throw new ArgumentException($"Invalid id {id}. Use other");
            worldID = id;
            entityManager = new EntityManager(this);
            systemManager = new SystemManager(this);
            Worlds.Add(id, this);
        }
    }
}

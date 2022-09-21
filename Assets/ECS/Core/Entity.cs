using System.Collections;
using System.Collections.Generic;
using System;

namespace ECS
{
    public enum EntityInstance
    {
        Original,
        CopyReference
    }

    public sealed partial class Entity : IEquatable<Entity>
    {
        public class EntityWorldSettings
        {
            public int worldID = EntityWorld.INVALID_WORLD;
            private bool _enabled = true;

            public bool Enabled
            {
                get => _enabled;
                set => _enabled = value;
            }
            public EntityWorldSettings(EntityWorld world)
            {
                if (world == null)
                    return;

                worldID = world.WorldID;
            }
        }

        public static partial class Factory
        {
            public static Entity Create(EntityWorld inWorld)
            {
                return Create(inWorld, null);
            }

            public static Entity Create(EntityWorld inWorld, params IComponent[] toAttachComponents)
            {
                var settings = new EntityWorldSettings(inWorld);
                var entity = new Entity(settings, toAttachComponents);
                if (inWorld != null)
                    inWorld.EntityManager.TransferIn(entity);
                return entity;
            }
        }


        #region fields
        private readonly EntityInstance _instance;
        private readonly Guid _reference;
        private readonly Dictionary<uint, IComponent> _components;
        private readonly Dictionary<Type, IComponent> _singleComponents;
        private readonly EntityWorldSettings _worldSettings;
        private Action<Entity, IComponent> _OnAddNewComponent;
        private Action<Entity, IComponent> _OnRemoveNewComponent;
#endregion

#region properties
        public Guid Reference => _reference;
        public EntityInstance Instance => _instance;
        public int InWorldID => _worldSettings.worldID;
        public event Action<Entity, IComponent> OnAddNewComponentHandler
        {
            add => _OnAddNewComponent += value;
            remove => _OnRemoveNewComponent -= value;
        }
        public event Action<Entity, IComponent> OnRemoveNewComponentHandler
        {
            add => _OnAddNewComponent += value;
            remove => _OnRemoveNewComponent -= value;
        }
        public bool Enabled
        {
            get => _worldSettings.Enabled;
            set => _worldSettings.Enabled = value;
        }
        #endregion

        #region constructor
        private Entity(EntityWorldSettings worldSettings)//create as original
        {
            _components = new Dictionary<uint, IComponent>(8);
            _singleComponents = new Dictionary<Type, IComponent>(8);
            _reference = Guid.NewGuid();
            _instance = EntityInstance.Original;
            _worldSettings = worldSettings;
        }

        private Entity(EntityWorldSettings worldSettings, params IComponent[] toAttachComponents) : this(worldSettings)//create as original
        {
            foreach(var component in toAttachComponents)
                AddComponent(component);
        }

        public Entity(Entity source) //create as copyreference
        {
            _instance = EntityInstance.CopyReference;
            _reference = source._reference;
            _components = source._components;
            _worldSettings = source._worldSettings;
            _OnAddNewComponent = source._OnAddNewComponent;
            _OnRemoveNewComponent = source._OnRemoveNewComponent;
        }
#endregion

#region Components operation
        public void AddComponent<T>(T component) where T: IComponent
        {
            CheckArgument();

            bool hasSingleTag = component.HasTag(ComponentTags.Single_Type);
            var type = component.GetType();
            if (hasSingleTag && GetComponent(type) != null)
                throw new InvalidOperationException($"Component of type {component.GetType().Name} can be in one instance attached. Has flag {nameof(ComponentTags.Single_Type)}");
            _components.Add(component.Reference, component);
            if(hasSingleTag)
                _singleComponents.Add(type, component);
            _OnAddNewComponent?.Invoke(this, component);
            void CheckArgument()
            {
                if (component.Reference is 0)
                    throw new ArgumentNullException("Component is not initialized");
                if (component == null)
                    throw new ArgumentNullException("Component cannot be null");
                if (_components.ContainsKey(component.Reference))
                    throw new ArgumentException($"Component already attached to this entity {this}");
            }
        }

        public bool RemoveComponent<T>(T component) where T : IComponent
        {
            CheckArgument();

            var type = component.GetType();
            var result = _components.Remove(component.Reference);
            _singleComponents.Remove(type);
            if(result)
                _OnRemoveNewComponent?.Invoke(this, component);
            return result;

            void CheckArgument()
            {
                if (component.Reference is 0)
                    throw new ArgumentNullException("Component is not initialized");
                if (component == null)
                    throw new ArgumentNullException("Component cannot be null");
                //if (!_components.ContainsKey(component.Reference))
                //    throw new ArgumentException($"Component not attached to this entity {this}");
            }
        }

        public T GetComponent<T>() where T : IComponent
        {
            foreach (var cmp in _components.Values)
                if (cmp is T output)
                    return output;
            return default(T);
        }

        public IComponent GetComponent(Type type)
        {
            foreach (var cmp in _components.Values)
                if (cmp.GetType().IsEquivalentTo(type))
                    return cmp;
            return default(IComponent);
        }

        public IComponent GetSingleComponent(Type type)
        {
            if (_singleComponents.TryGetValue(type, out var output))
                return output;
            else return null;
        }

        public T GetSingleComponent<T>() where T :IComponent
        {
            if (_singleComponents.TryGetValue(typeof(T), out var output))
                return (T)output;
            else return default(T);
        }

        public bool TryGetSingleComponent<T>(out T output) where T : IComponent
        {     
            bool result =  _singleComponents.TryGetValue(typeof(T), out var raw);
            if (result && raw is T pred)
            {
                output = pred;
                return true;
            }
            else
            {
                output = default(T);
                return false;
            }
        }

        public T GetComponent<T>(uint reference) where T : IComponent
        {
            if (_components.TryGetValue(reference, out var output))
                return (T)output;
            else return default(T);
        }

        public bool TryGetComponent<T>(out T component) where T : IComponent
        {
            component = GetComponent<T>();
            return component != null;
        }

        public void GetComponents<T>(in IList<T> list, bool clear = false) where T : IComponent
        {
            if(clear)
                list.Clear();
            foreach (var pair in _components)
                if (pair.Value is T output)
                    list.Add(output);
        }

        public void GetComponents(Type type, in IList<IComponent> list, bool clear = false)
        {
            if (clear)
                list.Clear();

            foreach(var component in _components.Values)
            {
                if (component.GetType() == type)
                    list.Add(component);
            }
        }
#endregion

#region object overrides
        public override string ToString() => _reference.ToString("N");
        public override bool Equals(object obj)
        {
            if (obj is Entity entity && this._reference == entity._reference)
                return true;
            else return false;
        }
        public override int GetHashCode()
        {
            return this._reference.GetHashCode();
        }

        public bool Equals(Entity other)
        {
            if (other != null)
                return false;
            return this._reference == other._reference;
        }

        public static bool operator ==(Entity left, Entity right)
        {
            if ((object)left == null && (object)right == null)
                return true;
            if ((object)left == null || (object)right == null)
                return false;
            return left._reference == right._reference;
        }
        public static bool operator !=(Entity left, Entity right)
        {
            if ((object)left == null && (object)right == null)
                return false;
            if ((object)left == null || (object)right == null)
                return true;
            return left._reference != right._reference;
        }
        #endregion
    }
}

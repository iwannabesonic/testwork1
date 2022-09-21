using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ECS;

namespace Wrappers
{
    public class CollisionComponent : Component<CollisionComponent.CollisionComponentData>, IConvertableComponent<CollisionComponentProvider>
    {
        private CollisionComponentData _data;
        private CollisionComponentProvider _source;
        private readonly object _sync = new object();
        protected override ref CollisionComponentData Data => ref _data;
        public CollisionComponentProvider Source => _source;

        public void EnqueueCollision(Collision collision)
        {
            if (!_data.solveCollisions)
                return;

            lock (_sync)
            _data.CollisionDataQueue.Enqueue(new CollisionData(collision));
        }

        public bool TryDequeueCollision(out CollisionData output)
        {
            if(_data.CollisionDataQueue.Count is 0)
            {
                output = default;
                return false;
            }
            else
            {
                lock(_sync)
                    output = _data.CollisionDataQueue.Dequeue();
                return true;
            }
        }

        public CollisionComponent() : base(ComponentTags.Single_Type)
        {
            _data = new CollisionComponentData(16);
        }
        public struct CollisionComponentData
        {
            public bool solveCollisions;
            public readonly Queue<CollisionData> CollisionDataQueue;

            public CollisionComponentData(int startCapacity)
            {
                CollisionDataQueue = new Queue<CollisionData>(startCapacity);
                solveCollisions = true;
            }
        }

        public struct CollisionData
        {
            public readonly Rigidbody rigidbody;
            public readonly Collider collider;

            public CollisionData(Collision collision)
            {
                rigidbody = collision.rigidbody;
                collider = collision.collider;
            }
        }

        void IConvertableComponent<CollisionComponentProvider>.ApplyConversion(CollisionComponentProvider from)
        {
            _source = from;
            _source.RegisterComponent(this);
        }
    }
}

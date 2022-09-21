using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ECS;

namespace Wrappers
{
    public sealed class UnityRigidbody : Component<UnityRigidbody.RigidbodyData>, IConvertableComponent<Rigidbody>
    {
        #region static
        private static Dictionary<int, UnityRigidbody> allRigidbodies = new Dictionary<int, UnityRigidbody>(128);
        public static UnityRigidbody GetFrom(Rigidbody source)
        {
            if(source == null)
                return null;

            var hash = source.GetHashCode();
            if (allRigidbodies.TryGetValue(hash, out UnityRigidbody result))
                return result;
            else return null;
        }
        #endregion

        private RigidbodyData _data;
        private Rigidbody unityRb;

        public Rigidbody Source => unityRb;
        protected override ref RigidbodyData Data => ref _data;

        public UnityRigidbody() : base(ComponentTags.Single_Type) { }
        void IConvertableComponent<Rigidbody>.ApplyConversion(Rigidbody from)
        {
            unityRb = from;
            _data = new RigidbodyData()
            {
                velocity = from.velocity,
                mass = from.mass,
                isKinematic = from.isKinematic
            };

            allRigidbodies.Add(from.GetHashCode(), this);
        }
        public struct RigidbodyData
        {
            public bool isKinematic;
            public bool forceWriteTransform;
            public Vector3 velocity;
            public float mass;
        }
    }
}

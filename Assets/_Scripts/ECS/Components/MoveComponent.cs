using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ECS;

namespace Control
{
    public sealed class MoveComponent : Component<MoveComponent.MoveData>
    {
        private MoveData _data;
        protected override ref MoveData Data => ref _data;
        public MoveComponent(MoveData data) : base(ComponentTags.Single_Type)
        {
            _data = data;
        }
        public struct MoveData
        {
            public Vector3 direction;
            public float speed;
            public float rotationSpeed;
            public bool useRigidbody;
        }
    }
}

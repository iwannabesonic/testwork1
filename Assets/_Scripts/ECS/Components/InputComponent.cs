using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ECS;

namespace Control
{
    public sealed class InputComponent : Component<InputComponent.InputData>
    {
        private InputData data;
        protected override ref InputData Data => ref data;

        public InputComponent() : base(ComponentTags.Single_Type) { }
        public struct InputData
        {
            public Vector3 moveDirection;
        }
    }
}

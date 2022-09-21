
using Vector3 = UnityEngine.Vector3;
using Quaternion = UnityEngine.Quaternion;

namespace ECS
{
    public class Transform : Component<Transform.TransformData>, IConvertableComponent<UnityEngine.Transform>
    {
        private TransformData _data = default;
        private UnityEngine.Transform _source;
        protected override ref TransformData Data => ref _data;

        public UnityEngine.Transform Source => _source;
        public struct TransformData
        {
            public Vector3 position;
            public Quaternion rotation;
            public Vector3 scale;
        }

        public Transform() : base(ComponentTags.Single_Type)
        {
            
        }

        void IConvertableComponent<UnityEngine.Transform>.ApplyConversion(UnityEngine.Transform from)
        {
            _data.position = from.position;
            _data.rotation = from.rotation;
            _data.scale = from.localScale;
            _source = from;
        }
    }
}

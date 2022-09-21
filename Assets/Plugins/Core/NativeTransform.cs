using Core;
using Core.Async;
using Core.Async.LowLevel;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Core.Async
{
    /// <summary>
    /// Подкомпонент обычного Transform. Позволяет делать снимки
    /// </summary>
    [RequireComponent(typeof(Transform), m_Type0 = typeof(AsyncGameObject))]
    public class NativeTransform : MonoBehaviour, ITransform
    {
        [SerializeField] private bool important = false;
        private AsyncTransform asyncTransform;
        private Transform _selfTransform;
        private bool _isStatic = false;
        private bool _isDisposed = false;
        public bool IsDisposed => _isDisposed;
        public NativeTransform parent => __parent;

        public int childCount => __childern.Count;
        public NativeTransform GetChild(int index) => __childern[index];

        public new AsyncGameObject gameObject => __gameObject;

        private List<NativeTransform> __childern = new List<NativeTransform>();
        private NativeTransform __parent;
        private AsyncGameObject __gameObject;

        private void internal_SetAsChildren(NativeTransform newChildren)
        {
            if(!__childern.Contains(newChildren))
            {
                __childern.Add(newChildren);
            }
        }

        private void internal_RemoveAsChildren(NativeTransform children)
        {
            __childern.Remove(children);
        }


        private void Awake()
        {
            _selfTransform = base.transform;
            asyncTransform = new AsyncTransform(_selfTransform);

            

            var bsParent = base.transform.parent;
            if(bsParent!=null)
            {
                if(bsParent.TryGetComponent<NativeTransform>(out var tr))
                {
                    __parent = tr;
                    tr.internal_SetAsChildren(this);
                }
                else
                {
                    __parent = bsParent.gameObject.AddComponent<NativeTransform>();
                    __parent.internal_SetAsChildren(this);
                }
            }

            __gameObject = GetComponent<AsyncGameObject>();
            _isStatic = __gameObject.isStatic;
            NativeTransformDispatcher.Self.EnqueueRegisterNew(this, important);
        }

        public void SetParent(NativeTransform newParent)
        {
            if (newParent == __parent) return;
            
            __parent?.internal_RemoveAsChildren(this);
            __parent = newParent;
            __parent?.internal_SetAsChildren(this);

            UnityDelegateThread.ImportantExecute(delegate { base.transform.SetParent(newParent); }, UnityDelegateThread.ExecuteMode.Update);
        }
        public void SetParent(NativeTransform newParent, bool worldPositionStays)
        {
            if (newParent == __parent) return;

            __parent?.internal_RemoveAsChildren(this);
            __parent = newParent;
            __parent?.internal_SetAsChildren(this);

            UnityDelegateThread.ImportantExecute(delegate { base.transform.SetParent(newParent, worldPositionStays); }, UnityDelegateThread.ExecuteMode.Update);
        }

        public void UpdateNative()
        {
            if (_isDisposed) return;
            if (_isStatic) return;

            asyncTransform = new AsyncTransform(_selfTransform);
        }
        private void OnDestroy()
        {
            _isDisposed = true;

            __parent?.internal_RemoveAsChildren(this);

            NativeTransformDispatcher.Self?.EnqueueRemove(this);
        }
        /// <summary>
        /// Снимок состояния трансформа на момент последнего обновления
        /// </summary>
        public new AsyncTransform transform => asyncTransform;

        public Vector3 position => asyncTransform.position;

        public Vector3 localPosition => asyncTransform.localPosition;

        public Quaternion rotation => asyncTransform.rotation;

        public Quaternion localRotation => asyncTransform.localRotation;

        public Vector3 eulerAngles => asyncTransform.eulerAngles;

        public Vector3 localEulerAngles => asyncTransform.localEulerAngles;

        public Vector3 localScale => asyncTransform.localScale;

        public Vector3 forward => asyncTransform.forward;

        public Vector3 up => asyncTransform.up;

        public Vector3 right => asyncTransform.right;

        public static implicit operator Transform (NativeTransform tr)
        {
            return tr._selfTransform;
        }

        
    }
}

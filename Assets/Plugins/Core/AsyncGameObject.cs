using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Core.LowLevel;
using Core.Async;
using Core.Async.Tasks;
using UnityEngine;
using Core.Async.LowLevel;

namespace Core.Async
{
    /// <summary>
    /// Позволяет выполнять безопасно операции вне основного потока unity
    /// </summary>
    [RequireComponent(typeof(NativeTransform))]
    public class AsyncGameObject : MonoBehaviour
    {
        public event Action<AsyncGameObject> OnStateChangedHandler;  

        private List<object> __components;
        private readonly object _sync = new object();
        private NativeTransform __nt;
        private GameObject __unityGameObject;
        public new GameObject gameObject => __unityGameObject;

        #region const
        public bool isStatic => __isStatic;
        private bool __isStatic;

        public bool activeSelf => __activeSelf;
        private bool __activeSelf;

        public bool activeInHierarchy => __activeInHierarchy;
        private bool __activeInHierarchy;

        public new string tag { get => __tag; set
            {
                if (value.Equals(__tag)) return;
                
                UnityDelegateThread.ImportantExecute(delegate { gameObject.tag = value; __tag = gameObject.tag; }, UnityDelegateThread.ExecuteMode.Update);
            }
        }
        private string __tag;
        
        public void SetActive(bool value)
        {
            UnityDelegateThread.ImportantExecute(delegate {
                gameObject.SetActive(value);
                __activeSelf = gameObject.activeSelf;
                __activeInHierarchy = gameObject.activeInHierarchy;
            }, UnityDelegateThread.ExecuteMode.Update);
        }

        public new string name
        {
            get => __name;
            set
            {
                if (value.Equals(__name)) return;

                UnityDelegateThread.ImportantExecute(delegate { gameObject.name = value; __name = gameObject.name; }, UnityDelegateThread.ExecuteMode.Update);
            }
        }
        private string __name;

        public new bool CompareTag(string tag)
        {
            return __tag.Equals(tag);
        }
        #endregion

        public new NativeTransform transform => __nt;
        public AsyncTransform asyncTransform => __nt.transform;

        private void Awake()
        {
            __components = new List<object>(base.GetComponents<Component>());
            __nt = base.GetComponent<NativeTransform>();
            __unityGameObject = base.gameObject;

            __isStatic = gameObject.isStatic;
            __activeSelf = gameObject.activeSelf;
            __activeInHierarchy = gameObject.activeInHierarchy;
            __tag = gameObject.tag;
            __name = gameObject.name;
        }

        //overrides

        public void AddComponent<T>(Action<T> callbackInMainThread) where T : Component
        {
            UnityDelegateThread.ImportantExecute(delegate {
                var t = gameObject.AddComponent<T>();
                lock (_sync)
                    __components.Add(t);
                callbackInMainThread?.Invoke(t);
                OnStateChangedHandler?.Invoke(this);
            }, UnityDelegateThread.ExecuteMode.Update);
        }

        public T AddComponent<T>() where T : Component
        {
            return UnityTaskDispatcher.Sync<T>(delegate
            {
                var t = gameObject.AddComponent<T>();
                lock (_sync)
                    __components.Add(t);
                return t;
            });
        }

        public new T GetComponent<T>()
        {
            lock(_sync)
            {
                foreach(var comp in __components)
                {
                    if (comp is T tcomp)
                        return tcomp;
                }

                return UnityTaskDispatcher.Sync<T>(() => 
                {
                    var t = gameObject.GetComponent<T>();
                    lock (_sync)
                        __components.Add(t);
                    return t;
                });
            }
        }

        public new T[] GetComponents<T>()
        {
            lock (_sync)
            {
                List<T> allComps = new List<T>();
                foreach (var comp in __components)
                {
                    if (comp is T tcomp)
                        allComps.Add(tcomp);
                }
                return allComps.ToArray();
            }
        }

        public new void GetComponents<T>(List<T> list)
        {
            lock (_sync)
            {
                
                foreach (var comp in __components)
                {
                    if (comp is T tcomp)
                        list.Add(tcomp);
                }
                
            }
        }

        public new bool TryGetComponent<T>(out T component)
        {
            lock(_sync)
            {
                component = GetComponent<T>();
                
                if ((object)component != null)
                {
                    return true;
                }
                else return false;
            }
        }

        public new T GetComponentInChildren<T>()
        {
            var t = GetComponent<T>();
            if(t is null)
            {
                int childCount = __nt.childCount;
                for(int i =0;i<childCount;i++)
                {
                    t = __nt.GetChild(i).gameObject.GetComponent<T>();
                    if (t is null) continue;
                    else
                    {
                        return t;
                    }
                }
            }

            return default(T);
        }

        public new T[] GetComponentsInChildren<T>()
        {
            List<T> all = new List<T>();
            var t = GetComponent<T>();
            Write(t);

            int childCount = __nt.childCount;
            for (int i = 0; i < childCount; i++)
            {
                t = __nt.GetChild(i).gameObject.GetComponent<T>();
                if (t is null) continue;
                else
                {
                    Write(t);
                }
            }


            return all.ToArray();

            void Write(T component)
            {
                if ((object)component != null)
                    all.Add(component);
            }
        }

        public new void GetComponentsInChildren<T>(List<T> array)
        {
            List<T> all = array;
            var t = GetComponent<T>();
            Write(t);

            int childCount = __nt.childCount;
            for (int i = 0; i < childCount; i++)
            {
                t = __nt.GetChild(i).gameObject.GetComponent<T>();
                if (t is null) continue;
                else
                {
                    Write(t);
                }
            }


            return;

            void Write(T component)
            {
                if ((object)component != null)
                    all.Add(component);
            }
        }

        public static implicit operator GameObject(AsyncGameObject go)
        {
            return go.gameObject;
        }

        private void OnEnable()
        {
            __activeSelf = true;
            __activeInHierarchy = gameObject.activeInHierarchy;
            OnStateChangedHandler?.Invoke(this);
        }

        private void OnDisable()
        {
            __activeSelf = false;
            __activeInHierarchy = gameObject.activeInHierarchy;
            OnStateChangedHandler?.Invoke(this);
        }

        private bool __disposed = false;
        public bool IsDisposed => __disposed;
        private void OnDestroy()
        {
            __disposed = true;
        }
    }
}

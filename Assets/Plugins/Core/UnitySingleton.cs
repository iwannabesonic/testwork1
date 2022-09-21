using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Core
{
    /// <summary>
    /// Used for Unity based singletons, inherited from <see cref="MonoBehaviour"/>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class UnitySingleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        protected static T _self;
        public static T Self
        {
            get
            {
                if (_self == null)
                    _self = FindObjectOfType<T>();
                return _self;
            }
        }

        private void Awake()
        {
            if (_self == null && gameObject != null)
            {
                _self = this as T;
            }
            else if ((object)_self != this as T)
            {
                Destroy(gameObject);
                return;
            }

            SingletonAwake();
        }

        /// <summary>
        /// Use this instead Awake
        /// </summary>
        protected virtual void SingletonAwake() { }
    }
}

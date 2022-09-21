using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ECS
{
    public interface IConvertableComponent<T> where T : class
    {
        void ApplyConversion(T from);
        T Source { get; }
    }

    public static class ComponentConvertUtils
    {
        public static TECSComponent Convert<TECSComponent, TSource>(in TSource source) where TECSComponent : IComponent, IConvertableComponent<TSource>, new() where TSource : class
        {
            var newComponent = new TECSComponent();
            newComponent.ApplyConversion(source);
            return newComponent;
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Wrappers
{
    public class CollisionComponentProvider : MonoBehaviour
    {
        private CollisionComponent ecs_component;

        public void RegisterComponent(CollisionComponent component)
        {
            if (ecs_component != null)
                return;

            ecs_component = component;
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (ecs_component == null)
                return;
            ecs_component.EnqueueCollision(collision);
        }
    }
}

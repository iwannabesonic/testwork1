using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ECS;
using Control;

public class DefaultUnityWorld : EntityWorld
{
    public DefaultUnityWorld(int id) : base(id)
    {
        EntityWorld.DefaultWorldInjection = this;

        SystemManager.AddSystem<InputSystem>();
        SystemManager.AddSystem<CollisionSolverSystem>();
        SystemManager.AddSystem<MoveSystem>();
        SystemManager.AddSystem<ItemStackPositionSystem>();
        SystemManager.AddSystem<PositionSystem>();

    }
}

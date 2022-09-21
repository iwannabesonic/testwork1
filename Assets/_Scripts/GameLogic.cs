using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Control;
using ECS;
using Wrappers;
using Items;
using Transform = UnityEngine.Transform;

public class GameLogic : MonoBehaviour
{
    [SerializeField] private InputProvider inputProvider;
    [SerializeField] private GameObject playerToEntity;
    [SerializeField] private float playerSpeed = 200, playerRotationSpeed =45;
    [SerializeField] private bool useRigidbodyForMove = true;

    private UnityRigidbody ecs_playerRb;
    private ItemStack ecs_playerItemStack;
    private Entity playerEntity;
    private DefaultUnityWorld world;

    public event UnityAction<int, int> OnChangeItemsInStack;

    public bool RemoveItemFromStack(out Entity entity)
    {
        var result = ecs_playerItemStack.RemoveItemFromStack(out entity);
        if (result)
        {
            InvokeEvent();
        }
        return result;
    }

    private void InvokeEvent()
    {
        var d_itemStack = ecs_playerItemStack.Read();
        OnChangeItemsInStack?.Invoke(d_itemStack.handledItems.Count, d_itemStack.maxCapacity);
    }

    private void Awake()
    {
        Application.targetFrameRate = 60;
        world = new DefaultUnityWorld(1);
        world.SystemManager.GetSystem<CollisionSolverSystem>().OnCollisionEnter += ECSBootstrapper_OnCollisionEnter;
    }

    private void Start()
    {
        var rb = playerToEntity.GetComponent<Rigidbody>();
        var tr = playerToEntity.transform;    
        playerEntity = Entity.Factory.Create(world,
            ComponentConvertUtils.Convert<ECS.Transform, Transform>(tr),
            ecs_playerRb = ComponentConvertUtils.Convert<UnityRigidbody, Rigidbody>(rb),
            ecs_playerItemStack = new ItemStack(8, new Vector3(0,0.5f,-0.5f)),
            inputProvider.ecs_playerInputComponent = new InputComponent(),
            ComponentConvertUtils.Convert<CollisionComponent, CollisionComponentProvider>(playerToEntity.GetComponent<CollisionComponentProvider>()),
            new MoveComponent(new MoveComponent.MoveData()
            {
                speed = playerSpeed,
                rotationSpeed = playerRotationSpeed,
                useRigidbody = useRigidbodyForMove
            }));
        var d_rb = ecs_playerRb.OpenWrite(this);
        d_rb.forceWriteTransform = useRigidbodyForMove;
        ecs_playerRb.CloseWrite(this, d_rb);

        playerToEntity.GetComponent<Anim.AnimatorPropsSetter>().ecs_PlayerInput = inputProvider.ecs_playerInputComponent;
        InvokeEvent();
    }

    private void ECSBootstrapper_OnCollisionEnter(Entity collisionOn, Entity collisionWith)
    {
        if (collisionOn != playerEntity || collisionWith == null)
            return;

        if(ecs_playerItemStack.AddItemInStack(collisionWith))
        {
            var c_collision = collisionWith.GetSingleComponent<CollisionComponent>();
            var d_col = c_collision.OpenWrite(this);
            d_col.solveCollisions = false;
            c_collision.CloseWrite(this, d_col);
            var ecs_rb = collisionWith.GetSingleComponent<UnityRigidbody>();
            ecs_rb.Source.detectCollisions = false;
            InvokeEvent();
        }
    }

   
}

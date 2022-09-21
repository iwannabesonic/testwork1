namespace ECS
{
    public static class EntityExtentions
    {
        public static bool InWorld(this Entity entity) => entity.InWorldID != -1;
    }

    public sealed partial class Entity
    {
        public static partial class Factory
        {
            public static Entity CreatePrimitive(EntityWorld inWorld) => Create(inWorld, new Transform());
        }
    }
}


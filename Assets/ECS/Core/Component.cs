using System;
using System.Threading.Tasks;
using Unity.Jobs;
using System.Collections.Generic;

namespace ECS
{
    [Flags]
    public enum ComponentTags : ulong
    {
        None        = 0x0,

        Single_Type = 0x1,
        RESERVED_02 = 0x2,
        RESERVED_03 = 0x4,
        RESERVED_04 = 0x8,

        RESERVED_05 = 0x10,
        RESERVED_06 = 0x20,
        RESERVED_07 = 0x40,
        RESERVED_08 = 0x80,

        RESERVED_09 = 0x100,
        RESERVED_10 = 0x200,
        RESERVED_11 = 0x400,
        RESERVED_12 = 0x800,

        RESERVED_13 = 0x1_000,
        RESERVED_14 = 0x2_000,
        RESERVED_15 = 0x4_000,
        RESERVED_16 = 0x8_000,

        RESERVED_17 = 0x10_000,
        RESERVED_18 = 0x20_000,
        RESERVED_19 = 0x40_000,
        RESERVED_20 = 0x80_000,

        RESERVED_21 = 0x100_000,
        RESERVED_22 = 0x200_000,
        RESERVED_23 = 0x400_000,
        RESERVED_24 = 0x800_000,

        RESERVED_25 = 0x1_000_000,
        RESERVED_26 = 0x2_000_000,
        RESERVED_27 = 0x4_000_000,
        RESERVED_28 = 0x8_000_000,

        RESERVED_29 = 0x10_000_000,
        RESERVED_30 = 0x20_000_000,
        RESERVED_31 = 0x40_000_000,
        RESERVED_32 = 0x80_000_000,

        RESERVED_33 = 0x100_000_000,
        RESERVED_34 = 0x200_000_000,
        RESERVED_35 = 0x400_000_000,
        RESERVED_36 = 0x800_000_000,

        RESERVED_37 = 0x1_000_000_000,
        RESERVED_38 = 0x2_000_000_000,
        RESERVED_39 = 0x4_000_000_000,
        RESERVED_40 = 0x8_000_000_000,

        RESERVED_41 = 0x10_000_000_000,
        RESERVED_42 = 0x20_000_000_000,
        RESERVED_43 = 0x40_000_000_000,
        RESERVED_44 = 0x80_000_000_000,

        RESERVED_45 = 0x100_000_000_000,
        RESERVED_46 = 0x200_000_000_000,
        RESERVED_47 = 0x400_000_000_000,
        RESERVED_48 = 0x800_000_000_000,

        RESERVED_49 = 0x1_000_000_000_000,
        RESERVED_50 = 0x2_000_000_000_000,
        RESERVED_51 = 0x4_000_000_000_000,
        RESERVED_52 = 0x8_000_000_000_000,

        RESERVED_53 = 0x10_000_000_000_000,
        RESERVED_54 = 0x20_000_000_000_000,
        RESERVED_55 = 0x40_000_000_000_000,
        RESERVED_56 = 0x80_000_000_000_000,

        RESERVED_57 = 0x100_000_000_000_000,
        RESERVED_58 = 0x200_000_000_000_000,
        RESERVED_59 = 0x400_000_000_000_000,
        RESERVED_60 = 0x800_000_000_000_000,

        RESERVED_61 = 0x1_000_000_000_000_000,
        RESERVED_62 = 0x2_000_000_000_000_000,
        RESERVED_63 = 0x4_000_000_000_000_000,
        RESERVED_64 = 0x8_000_000_000_000_000,
    }

    public interface IComponent
    {
        bool HasTag(ComponentTags tag);
        uint Reference { get; }

        object OpenWrite(object openPermission);
        void CloseWrite(object openPermission, in object data);
        object Read();
    }

    public abstract class BaseComponent
    {
        private uint _reference;
        public uint Reference => _reference;
        private static uint globalReference = 0;
        protected BaseComponent()
        {
            _reference = ++globalReference;
        }
    }

    public abstract class Component<TData> : BaseComponent, IComponent where TData : struct
    {
        private ComponentTags _tags;
        private bool locked = false;
        private object owner;
        private readonly object _sync = new object();

        public bool HasTag(ComponentTags requireTag) => (_tags & requireTag) == requireTag;

        protected void AddTag(ComponentTags tag) => _tags |= tag;
        protected void RemoveTag(ComponentTags tag) => _tags &= ~tag;
        protected abstract ref TData Data { get; }

        public TData OpenWrite(object openPermission)
        {
            if (owner == openPermission)
                return Data;

            WaitRelease();
            locked = true;
            owner = openPermission;
            return Data;
        }

        public void CloseWrite(object openPermission, in TData data)
        {
            if (owner != openPermission)
                return;
            if (!locked)
                throw new InvalidOperationException("Component not in write mode");
            
            Data = data;
            locked = false;
            owner = null;
        }

        public TData Read(object openPermission = null)
        {
            if (openPermission == owner)
                return Data;

            WaitRelease();
            return Data;
        }

        public Task<TData> ReadAsync()
        {
            return Task.Run(() => Read());
        }

        private void WaitRelease()
        {
            lock (_sync)
            {
                while (locked)
                    continue;
            }
        }

        object IComponent.OpenWrite(object openPermission) => OpenWrite(openPermission);
        void IComponent.CloseWrite(object openPermission, in object data) => CloseWrite(openPermission, (TData)data);
        object IComponent.Read() => Read();

        protected Component(ComponentTags initialTags) : base()
        {
            _tags = initialTags;    
        }
    }
}

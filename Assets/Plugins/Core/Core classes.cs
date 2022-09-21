using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using UnityEngine;
using Core.Json;
using Core.LowLevel;

namespace Core
{
    public interface IProgram<in T>
    {
        void Start(Memory memory);
        void Dispose();
        void Update(T argument);
        bool IsRunned { get; }
    }

    public enum ProgramState
    {
        Not_started,
        Wait,
        Runned,
        Started,
        Disposed
    }

    public class ProgramDriverShell
    {
        Memory _localMemory;

        IProgram<float> program;

        public ProgramDriverShell(Memory selectMemory)
        {
            _localMemory = selectMemory;
        }

        public void Drive(float deltaTime)
        {
            if (program is null) return;
            lock (sync)
            {
                program.Update(deltaTime);
            }
        }

        private readonly object sync = new object();
        public void SetProgram(IProgram<float> newProgram)
        {
            if (newProgram == program) return;

            lock (sync)
            {
                program.Dispose();
                program = newProgram;

                program.Start(_localMemory);
            }
        }
    }

    public interface ISupportCoroutines
    {
        int StartCoroutine(IEnumerator<float> routine);
        bool StopCoroutine(int reference);
        void StopAllCoroutines();
        bool IsActiveCoroutine(int reference);
    }

    public class CoroutineExecuter : ISupportCoroutines
    {
        private class CoroutineInstance
        {
            private UnitySyncTimer timer;
            private IEnumerator<float> coroutine;
            private bool executeEnd = false;
            public bool ExecuteEnd => executeEnd;
            private void Enumerate()
            {
                try
                {
                    if (coroutine.MoveNext())
                        timer.Restart(coroutine.Current);
                    else
                    {
                        executeEnd = true;
                    }
                }
                catch(Exception e)
                {
                    executeEnd = true;
                    Debug.LogException(e);
                }
            }
            public void DoStep(float deltaTime)
            {
                if (timer.Sync(deltaTime))
                    Enumerate();
            }

            public CoroutineInstance(IEnumerator<float> coroutine)
            {
                ReSetUp(coroutine);
            }
            public CoroutineInstance()
            {
                executeEnd = true;
                timer = new UnitySyncTimer(0) { IsLooped = false };
            }
            public void ReSetUp(IEnumerator<float> coroutine)
            {
                executeEnd = false;
                timer = new UnitySyncTimer(0) { IsLooped = false };
                timer.Start();

                this.coroutine = coroutine;
            }
        }

        private Dictionary<int, CoroutineInstance> list = new Dictionary<int, CoroutineInstance>();
        private readonly object sync = new object();

        public void Update(float deltaTime)
        {
            lock (sync)
            {
                foreach (var pair in list)
                {
                    var coroutine = pair.Value;
                    if (coroutine.ExecuteEnd) continue;
                    else coroutine.DoStep(deltaTime);
                }
            }
        }

        private KeyValuePair<int, CoroutineInstance> GetNextInstance()
        {
            KeyValuePair<int, CoroutineInstance>? requestPair = null;
            
            foreach(var pair in list)
            {
                if(pair.Value.ExecuteEnd)
                {
                    requestPair = pair;
                    break;
                }    
            }

            if(requestPair is null)
            {
                var rnd = new System.Random(Environment.TickCount);
                int opCount = 0;
                int index;
                do
                {
                    if (opCount++ > 128)
                    {
                        index = rnd.Next(int.MinValue, 0);
                    }
                    else
                    {
                        index = rnd.Next(1, int.MaxValue);
                    }
                }
                while (list.ContainsKey(index));

                requestPair = new KeyValuePair<int, CoroutineInstance>(index, new CoroutineInstance());
                list.Add(index, requestPair.Value.Value);
            }

            return requestPair.Value;
        }

        public int StartCoroutine(IEnumerator<float> routine)
        {
            lock (sync)
            {
                var pair = GetNextInstance();
                pair.Value.ReSetUp(routine);
                return pair.Key;
            }
        }

        public bool StopCoroutine(int reference)
        {
            lock (sync)
            {
                return list.Remove(reference);
            }
        }

        public void StopAllCoroutines()
        {
            lock (sync)
            {
                list.Clear();
            }
        }

        public bool IsActiveCoroutine(int reference)
        {
            if (list.TryGetValue(reference, out var routine))
            {
                return !routine.ExecuteEnd;
            }
            else return false;
        }
    }

    public interface ITagged : IEnumerable<string>
    {
        void Add(string tag);
        bool Remove(string tag);

        int Count { get; }

        bool TagExist(string tag);

        IReadOnlyCollection<string> ToList();
        string ToString();

        //enums

        
    }

    public static class ITaggedExtentions
    {
        public static ITagged GetAsTagged(this ITagged tagged) => tagged;
    }

    [System.Serializable]
    public sealed class NameRef :IEnumerable<string>, ITagged
    {
        public static bool ShowSystemNames { get; set; } = false;
        private Guid _guid;
        private string system_name;
        private string description_name;
        private string spriteID;
        private uint localID;
        private List<string> tagsList = new List<string>(4);

        #region copy
        public static NameRef DeepCopy(NameRef source)
        {
            NameRef copy = new NameRef(source.system_name, source.description_name, source.spriteID, source.localID);
            copy._guid = source._guid;
            copy.tagsList = new List<string>(source.tagsList);
            copy.tagsValue_cash = source.tagsValue_cash;

            return copy;
        }
        #endregion

        #region itagged support
        void ITagged.Add(string tag) => Add(tag);
        bool ITagged.Remove(string tag) => RemoveTag(tag);
        int ITagged.Count => tagsList.Count;
        bool ITagged.TagExist(string tag) => TagExist(tag);
        IReadOnlyCollection<string> ITagged.ToList() => tagsList.AsReadOnly();
        string ITagged.ToString() => TagsValue();

        //IEnumerator<string> IEnumerable<string>.GetEnumerator() => tagsList.GetEnumerator();
        //IEnumerator IEnumerable.GetEnumerator() => tagsList.GetEnumerator();
        #endregion

        /// <summary>
        /// Определяет неявные копии объектов в виде относительных ссылок.
        /// </summary>
        public Guid UID => _guid;
        public string UIDFormat => _guid.ToString("N");
        public static string GetUIDFormat(Guid guid) => guid.ToString("N");
        public static Guid GetUID(string uidFormat) => Guid.ParseExact(uidFormat, "N");
        public void SetRef(Guid newUid) => _guid = newUid;
        public void SetRef(string newUidFormat) => _guid = Guid.ParseExact(newUidFormat, "N");
        public bool Equals(NameRef other)
        {
            return other._guid == _guid;
        }
        /// <summary>
        /// Ломает id, лишая связи с другим nameref
        /// </summary>
        public void BreakRef() => _guid = Guid.NewGuid();
        public string Value
        {
            get
            {
                if (ShowSystemNames)
                    return system_name;
                else return description_name;
            }
        }
        public string SystemVal => system_name;
        public string DescVal { get => description_name; set => ChangeDescriptionName(value); }
        public void ChangeDescriptionName(string newName) => description_name = newName;
        public string SpriteID { get => spriteID; set => spriteID = value; }
        public uint LocalID { get => localID; set => localID = value; }
        public bool TagExist(string tag) => tagsList.Contains(tag);

        [NonSerialized] private string tagsValue_cash;
        public string TagsValue()
        {
            if (tagsList.Count > 0)
            {
                if (tagsValue_cash is null)
                {
                    StringBuilder builder = new StringBuilder();
                    foreach (var tg in tagsList)
                    {
                        builder.Append($"{tg} | ");
                    }
                    builder.Remove(builder.Length - 3, 2);
                    tagsValue_cash = builder.ToString();
                }
                return tagsValue_cash;
            }
            else return string.Empty;
        }
        public IReadOnlyCollection<string> Tags => tagsList.AsReadOnly();
        public bool AddTag(string tag)
        {
            if (TagExist(tag))
                return false;
            tagsList.Add(tag);
            tagsValue_cash = null;
            return true;
        }
        /// <summary>
        /// Использовать только для инициализатора
        /// </summary>
        /// <param name="tag"></param>
        public void Add(string tag) => AddTag(tag);
        public bool RemoveTag(string tag)
        {
            bool res = tagsList.Remove(tag);
            if (res)
                tagsValue_cash = null;
            return res;
        }

        IEnumerator<string> IEnumerable<string>.GetEnumerator()
        {
            return tagsList.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return (this as IEnumerable<string>).GetEnumerator();
        }

        public NameRef()
        {
            system_name = description_name = "undefine";
            spriteID = string.Empty;
            localID = 0;
            _guid = Guid.NewGuid();
        }
        public NameRef(string systemName) : this()
        {
            system_name = description_name = systemName;
        }
        public NameRef(string systemName, string descName) : this()
        {
            system_name = systemName;
            description_name = descName;
        }
        public NameRef(string systemName, string descName, string spriteID) : this()
        {
            system_name = systemName;
            description_name = descName;
            this.spriteID = spriteID;
        }
        public NameRef(string systemName, string descName, string spriteID, uint localID) : this()
        {
            system_name = systemName;
            description_name = descName;
            this.spriteID = spriteID;
            this.localID = localID;
        }

        /* json */
        public static class JsonUtils
        {
            public static string ToJson(NameRef name)
            {
                using(StringWriter writer = new StringWriter())
                {
                    writer.Write("{ ");
                    writer.Write($"\"uid\": \"{name._guid :N}\", ");
                    writer.Write($"\"sys\": \"{name.system_name}\", ");
                    writer.Write($"\"des\": \"{name.description_name}\", ");
                    writer.Write($"\"sid\": \"{name.spriteID}\", ");
                    writer.Write($"\"lid\": {name.localID}, ");
                    writer.Write($"\"tag\": [ ");
                    int count = 0;
                    foreach(var tg in name.tagsList)
                    {
                        if(count++ != name.tagsList.Count-1)
                            writer.Write($"\"{tg}\", ");
                        else
                            writer.Write($"\"{tg}\" ");

                    }
                    writer.Write("] } ");
                    return writer.ToString();
                }
            }

            public static NameRef FromJson(string json)
            {
                StringBuilder builder = new StringBuilder();
                using(StringReader reader = new StringReader(json))
                {
                    while (Pass(reader.Read())) continue;
                    while (Write(reader.Read())) continue;
                    ////request read UID naming
                    //for (int i = 0; i < 3; i++)
                    //{
                    //    builder.Append((char)reader.Read());
                    //}
                    for (int i = 0; i < 3; i++) reader.Read();

                    string uid = null, readValue = builder.ToString();
                    builder.Clear();
                    //Debug.Log($"Readval = {readValue}");
                    if (readValue.Equals("uid"))
                    {
                        while (Write(reader.Read())) continue;
                        uid = builder.ToString();
                        builder.Clear();
                    }
                    else
                    {
                        //for (int i = 0; i < 3; i++) reader.Read(); //skip to sys_name
                        goto READ_SYS_NAME;
                    }

                    while (Pass(reader.Read())) continue;
                    while (Pass(reader.Read())) continue;
                    for (int i = 0; i < 3; i++) reader.Read(); //skip to sys_name

                    READ_SYS_NAME:
                    while (Write(reader.Read())) continue;
                    string sys_name = builder.ToString();
                    builder.Clear(); //Debug.Log($"Sysname = {sys_name}");

                    while (Pass(reader.Read())) continue;
                    while (Pass(reader.Read())) continue;
                    for (int i = 0; i < 3; i++) reader.Read(); //skip to des_name

                    while (Write(reader.Read())) continue;
                    string des_name = builder.ToString();
                    builder.Clear();

                    while (Pass(reader.Read())) continue;
                    while (Pass(reader.Read())) continue;
                    for (int i = 0; i < 3; i++) reader.Read(); //skip to spriteID

                    while (Write(reader.Read())) continue;
                    string spriteID = builder.ToString();
                    builder.Clear();

                    while (Pass(reader.Read())) continue;
                    while (Pass(reader.Read())) continue;
                    for (int i = 0; i < 2; i++) reader.Read(); //skip to localID

                    uint localID = (uint)CoreJsonUtils.ReadDouble(reader, builder);

                    while (Pass(reader.Read())) continue;
                    //while (Pass(reader.Read())) continue; //skip to tags
                    List<string> tags = new List<string>();

                    CoreJsonUtils.ReadTagsBlock(CoreJsonUtils.ReadNextBlock(reader, '['), tags);

                    Guid guid = default;
                    if(uid!=null)
                    {
                        guid = Guid.ParseExact(uid, "N");
                    }
                    else
                    {
                        guid = Guid.NewGuid();
                    }

                    NameRef name = new NameRef(sys_name, des_name) { tagsList = tags, spriteID = spriteID, localID = localID, _guid = guid };

                    return name;
                }

                bool Pass(int value)
                {
                    switch (value)
                    {
                        case '"':

                            return false;

                        case -1: throw new ReadEndException();
                        default: return true;
                    }
                }
                bool Write(int value)
                {
                    switch (value)
                    {
                        case '"':

                            return false;

                        case -1: throw new ReadEndException();
                        default:
                            {
                                builder.Append((char)value);
                            }
                            return true;
                    }
                }
            }
        }
    }
    #region Vaults

    #region string vault
    [System.Serializable]
    public sealed class Vault : IEnumerable, IEnumerable<VaultItem>
    {
        #region copy
        public static Vault DeepCopy(Vault source)
        {
            Vault copy = new Vault();
            copy.dict = new Dictionary<string, VaultItem>(source.dict.Count);
            foreach(var key in source.dict)
            {
                var itmcopy = VaultItem.DeepCopy(key.Value);
                copy.dict.Add(itmcopy.key, itmcopy);
            }

            return copy;
        }
        #endregion

        private Dictionary<string, VaultItem> dict;

        public VaultItem this[string key] => Read(key);
        public VaultItem Read(string key)
        {
            if (dict.TryGetValue(key, out var val))
                return val;
            else
            {
                val = new VaultItem(key, 0);
                dict.Add(key, val);
                return val;
            }
        }
        public bool Remove(string key) => dict.Remove(key);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="baseValue"></param>
        /// <exception cref="ArgumentException"></exception>
        /// <returns></returns>
        public VaultItem Add(string key, double baseValue)
        {
            var newItm = new VaultItem(key, baseValue);
            dict.Add(key, newItm);
            return newItm;
        }
        public VaultItem Add(string key, double baseValue, ICollection<string> tags)
        {
            var newItm = new VaultItem(key, baseValue, tags);
            dict.Add(key, newItm);
            return newItm;
        }
        public VaultItem Add(string key, double baseValue, double addValue = 0, double rawValue = 0, double multValue = 1)
        {
            var newItm = new VaultItem(key, baseValue) { AdditionalValue = addValue, RawValue = rawValue, MultipileValue = multValue };
            dict.Add(key, newItm);
            return newItm;
        }
        public void Add(VaultItem item)
        {
            dict.Add(item.key, item);
        }
        public void Write(VaultItem itm)
        {
            dict.Add(itm.key, itm);
        }

        public int Count => dict.Count;



        #region ienumerable
        IEnumerator<VaultItem> IEnumerable<VaultItem>.GetEnumerator()
        {
            foreach (var pair in dict)
            {
                yield return pair.Value;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return (this as IEnumerable<VaultItem>).GetEnumerator();
        }
        #endregion

        public Vault()
        {
            dict = new Dictionary<string, VaultItem>();
        }
        public Vault(int size)
        {
            dict = new Dictionary<string, VaultItem>();
        }
        public Vault(ICollection<VaultItem> items)
        {
            dict = new Dictionary<string, VaultItem>(items.Count);
            foreach (var itm in items)
            {
                dict.Add(itm.key, itm);
            }
        }
        public Vault(params VaultItem[] items)
        {
            dict = new Dictionary<string, VaultItem>(items.Length);
            foreach (var itm in items)
            {
                dict.Add(itm.key, itm);
            }
        }
    }

    public interface IVaultItem
    {
        string key { get; }
        double value { get; }
        double baseValue { get; }
        double addValue { get; set; }
        double multValue { get; set; }
        double rawValue { get; set; }

    }
    [System.Serializable]
    public sealed class VaultItem : ITagged, IVaultItem
    {
        public readonly string key;

        private double base_value, add_value, mult_value, raw_value;

        #region itagged support
        private readonly List<string> tagsList;
        void ITagged.Add(string tag)
        {
            if (!tagsList.Contains(tag))
            {
                tagsList.Add(tag);
                tagsCash = null;
            }
        }

        bool ITagged.Remove(string tag)
        {
            var res = tagsList.Remove(tag);
            if (res)
                tagsCash = null;
            return res;
        }

        public bool TagExist(string tag) => tagsList.Contains(tag);

        IReadOnlyCollection<string> ITagged.ToList() => tagsList.AsReadOnly();

        [NonSerialized] private string tagsCash;
        string ITagged.ToString()
        {
            if (tagsList.Count > 0)
            {
                if (tagsCash is null)
                {
                    StringBuilder builder = new StringBuilder();
                    foreach (var tg in tagsList)
                    {
                        builder.Append($"{tg} | ");
                    }
                    builder.Remove(builder.Length - 3, 2);
                    tagsCash = builder.ToString();
                }
                return tagsCash;
            }
            else return string.Empty;
        }

        int ITagged.Count => tagsList.Count;

        IEnumerator<string> IEnumerable<string>.GetEnumerator() => tagsList.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => tagsList.GetEnumerator();
        #endregion
        #region ivaultitem support
        string IVaultItem.key => key;
        double IVaultItem.value => Value;
        double IVaultItem.baseValue => base_value;
        double IVaultItem.addValue { get => add_value; set => AdditionalValue = value; }
        double IVaultItem.multValue { get => mult_value; set => MultipileValue = value; }
        double IVaultItem.rawValue { get => raw_value; set => RawValue = value; }
        #endregion

        /*chages*/
        /// <summary>
        /// Добавочное значение к базовому. Изменяемо
        /// </summary>
        public double AdditionalValue { get => add_value; set => add_value = value; }
        /// <summary>
        /// Добавочное значение, не подвержено скейлингу. Изменяемо
        /// </summary>
        public double RawValue { get => raw_value; set => raw_value = value; }
        /// <summary>
        /// Множитель базового значения. Изменяемо
        /// </summary>
        public double MultipileValue { get => mult_value; set => mult_value = value; }

        /*readonly*/
        /// <summary>
        /// Базовое значение. Не изменяемо
        /// </summary>
        public double BaseValue => base_value;
        /// <summary>
        /// Само значение. Вычисляемо
        /// </summary>
        public double Value => (base_value + add_value) * mult_value + raw_value;
        public float AsFloat => (float)Value;
        public int AsInt => (int)Value;
        public bool AsBool => (Value > 0.01) || (Value < -0.01);
        public bool NotNull => (Value > 0.0000001) || (Value < -0.0000001);

        #region methods
        public void Reset(double newBaseValue)
        {
            base_value = newBaseValue;
            Reset();
        }
        public void Reset()
        {
            add_value = raw_value = 0;
            mult_value = 1;
        }

        public static VaultItem DeepCopy(VaultItem source)
        {
            VaultItem copy = new VaultItem(source.key, source.base_value, source.tagsList);
            copy.add_value = source.add_value;
            copy.mult_value = source.mult_value;
            copy.raw_value = source.raw_value;
            return copy;
        }
        #endregion

        public override string ToString()
        {
            return $"{key}: {Value:f2}";
        }
        public VaultItem(string key, double baseValue)
        {
            this.key = key;

            base_value = baseValue;
            add_value = raw_value = 0;
            mult_value = 1;

            tagsList = new List<string>();
        }

        public VaultItem(string key, double baseValue, ICollection<string> tags)
        {
            this.key = key;

            base_value = baseValue;
            add_value = raw_value = 0;
            mult_value = 1;

            tagsList = new List<string>(tags);
        }
    }
    #endregion

    #region generic vault
    [System.Serializable]
    public sealed class Vault<T> : IEnumerable, IEnumerable<VaultItem<T>> where T:Enum
    {

        #region copy
        public static Vault<T> DeepCopy(Vault<T> source)
        {
            Vault<T> copy = new Vault<T>();
            copy.dict = new Dictionary<T, VaultItem<T>>(source.dict.Count);
            foreach (var key in source.dict)
            {
                var itmcopy = VaultItem<T>.DeepCopy(key.Value);
                copy.dict.Add(itmcopy.key, itmcopy);
            }

            return copy;
        }
        #endregion

        private Dictionary<T, VaultItem<T>> dict;

        public VaultItem<T> this[T index] => Read(index);
        public VaultItem<T> Read(T key)
        {
            if (dict.TryGetValue(key, out var val))
                return val;
            else
            {
                val = new VaultItem<T>(key, 0);
                dict.Add(key, val);
                return val;
            }
        }
        public bool Remove(T key) => dict.Remove(key);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="baseValue"></param>
        /// <exception cref="ArgumentException"></exception>
        /// <returns></returns>
        public VaultItem<T> Add(T key, double baseValue)
        {
            var newItm = new VaultItem<T>(key, baseValue);
            dict.Add(key, newItm);
            return newItm;
        }
        public VaultItem<T> Add(T key, double baseValue, ICollection<string> tags)
        {
            var newItm = new VaultItem<T>(key, baseValue, tags);
            dict.Add(key, newItm);
            return newItm;
        }
        public void Add(VaultItem<T> item)
        {
            dict.Add(item.key, item);
        }
        public void Write(VaultItem<T> itm)
        {
            dict.Add(itm.key, itm);
        }

        public int Count => dict.Count;



        #region ienumerable
        IEnumerator<VaultItem<T>> IEnumerable<VaultItem<T>>.GetEnumerator()
        {
            foreach (var pair in dict)
            {
                yield return pair.Value;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return (this as IEnumerable<VaultItem<T>>).GetEnumerator();
        }
        #endregion

        public Vault()
        {
            dict = new Dictionary<T, VaultItem<T>>();
        }
        public Vault(int size)
        {
            dict = new Dictionary<T, VaultItem<T>>();
        }
        public Vault(ICollection<VaultItem<T>> items)
        {
            dict = new Dictionary<T, VaultItem<T>>(items.Count);
            foreach (var itm in items)
            {
                dict.Add(itm.key, itm);
            }
        }
        public Vault(params VaultItem<T>[] items)
        {
            dict = new Dictionary<T, VaultItem<T>>(items.Length);
            foreach (var itm in items)
            {
                dict.Add(itm.key, itm);
            }
        }
    }

    [System.Serializable]
    public sealed class VaultItem<T> : ITagged where T: Enum
    {
        public readonly T key;

        private double base_value, add_value, mult_value, raw_value;

        #region itagged support
        private readonly List<string> tagsList;
        void ITagged.Add(string tag)
        {
            if (!tagsList.Contains(tag))
            {
                tagsList.Add(tag);
                tagsCash = null;
            }
        }

        bool ITagged.Remove(string tag)
        {
            var res = tagsList.Remove(tag);
            if (res)
                tagsCash = null;
            return res;
        }

        public bool TagExist(string tag) => tagsList.Contains(tag);

        IReadOnlyCollection<string> ITagged.ToList() => tagsList.AsReadOnly();

        [NonSerialized] private string tagsCash;
        string ITagged.ToString()
        {
            if (tagsList.Count > 0)
            {
                if (tagsCash is null)
                {
                    StringBuilder builder = new StringBuilder();
                    foreach (var tg in tagsList)
                    {
                        builder.Append($"{tg} | ");
                    }
                    builder.Remove(builder.Length - 3, 2);
                    tagsCash = builder.ToString();
                }
                return tagsCash;
            }
            else return string.Empty;
        }

        int ITagged.Count => tagsList.Count;

        IEnumerator<string> IEnumerable<string>.GetEnumerator() => tagsList.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => tagsList.GetEnumerator();
        #endregion

        /*chages*/
        /// <summary>
        /// Добавочное значение к базовому. Изменяемо
        /// </summary>
        public double AdditionalValue { get => add_value; set => add_value = value; }
        /// <summary>
        /// Добавочное значение, не подвержено скейлингу. Изменяемо
        /// </summary>
        public double RawValue { get => raw_value; set => raw_value = value; }
        /// <summary>
        /// Множитель базового значения. Изменяемо
        /// </summary>
        public double MultipileValue { get => mult_value; set => mult_value = value; }

        /*readonly*/
        /// <summary>
        /// Базовое значение. Не изменяемо
        /// </summary>
        public double BaseValue => base_value;
        /// <summary>
        /// Само значение. Вычисляемо
        /// </summary>
        public double Value => (base_value + add_value) * mult_value + raw_value;
        public float AsFloat => (float)Value;
        public bool AsBool => (Value > 0.01) || (Value < -0.01);
        public bool NotNull => (Value > 0.0000001) || (Value < -0.0000001);

        #region methods
        public void Reset(double newBaseValue)
        {
            base_value = newBaseValue;
            Reset();
        }
        public void Reset()
        {
            add_value = raw_value = 0;
            mult_value = 1;
        }

        public static VaultItem<T> DeepCopy(VaultItem<T> source)
        {
            VaultItem<T> copy = new VaultItem<T>(source.key, source.base_value, source.tagsList);
            copy.add_value = source.add_value;
            copy.mult_value = source.mult_value;
            copy.raw_value = source.raw_value;
            return copy;
        }
        #endregion

        public override string ToString()
        {
            return $"{key}: {Value:f2}";
        }
        public VaultItem(T key, double baseValue)
        {
            this.key = key;

            base_value = baseValue;
            add_value = raw_value = 0;
            mult_value = 1;

            tagsList = new List<string>();
        }

        public VaultItem(T key, double baseValue, ICollection<string> tags)
        {
            this.key = key;

            base_value = baseValue;
            add_value = raw_value = 0;
            mult_value = 1;

            tagsList = new List<string>(tags);
        }
    }

    public static class VaultJsonUtils
    {
        public static string ToJson(Vault vault)
        {
            if (vault is null) return "{}";

            var culture = System.Globalization.CultureInfo.InvariantCulture.NumberFormat;
            using (StringWriter writer = new StringWriter())
            {
                writer.Write("{ ");//init
                writer.Write($"\"typeof\": \"{typeof(string)}\", ");
                
                #region code
                int counter = 0;
                foreach(var itm in vault)
                {
                    writer.Write($"\"{itm.key}\": {{ ");

                    #region tags
                    writer.Write($"\"tg\": [ ");
                    int count = 0; int length = (itm as ITagged).Count;
                    foreach (var tg in itm)
                    {
                        if(count++ != length-1)
                        writer.Write($"\"{tg}\", ");
                        else
                            writer.Write($"\"{tg}\" ");
                    }
                    writer.Write("], ");
                   #endregion

                    writer.Write($"\"bv\": {itm.BaseValue.ToString(culture)}, ");
                    writer.Write($"\"av\": {itm.AdditionalValue.ToString(culture)}, ");
                    writer.Write($"\"rv\": {itm.RawValue.ToString(culture)}, ");
                    writer.Write($"\"mv\": {itm.MultipileValue.ToString(culture)} ");

                    counter++;
                    if(vault.Count == counter)
                        writer.Write("} ");
                    else
                        writer.Write("}, ");
                }
                #endregion

                writer.Write("} ");//end
                return writer.ToString();
            }
        }

        public static string ToJson<T>(Vault<T> vault) where T:Enum
        {
            if (vault is null) return "{}";

            var culture = System.Globalization.CultureInfo.InvariantCulture.NumberFormat;
            using (StringWriter writer = new StringWriter())
            {
                writer.Write("{ ");//init
                writer.Write($"\"typeof\": \"{typeof(T)}\", ");
                #region code
                int counter = 0;
                foreach (var itm in vault)
                {
                    writer.Write($"\"{itm.key}\": {{ ");

                    #region tags
                    writer.Write($"\"tg\": [ ");
                    int count = 0; int length = (itm as ITagged).Count;
                    foreach (var tg in itm)
                    {
                        if (count++ != length - 1)
                            writer.Write($"\"{tg}\", ");
                        else
                            writer.Write($"\"{tg}\" ");
                    }
                    writer.Write("], ");
                    #endregion

                    writer.Write($"\"bv\": {itm.BaseValue.ToString(culture)}, ");
                    writer.Write($"\"av\": {itm.AdditionalValue.ToString(culture)}, ");
                    writer.Write($"\"rv\": {itm.RawValue.ToString(culture)}, ");
                    writer.Write($"\"mv\": {itm.MultipileValue.ToString(culture)} ");

                    counter++;
                    if (vault.Count == counter)
                        writer.Write("} ");
                    else
                        writer.Write("}, ");
                }
                #endregion

                writer.Write(" }");//end
                return writer.ToString();
            }
        }

        public static Vault FromJson(string json)
        {
            if (json is "{}") return null;

            StringBuilder builder = new StringBuilder(32);
            Vault vault = new Vault();

            using (StringReader reader = new StringReader(json))
            {
                try
                {
                    while (Pass(reader.Read())) continue; //skip syntax
                    while (Pass(reader.Read())) continue; //skip "typeof"
                    while (Pass(reader.Read())) continue; //skip ": "
                    while (Read(reader.Read())) continue; //read typeof value
                    string typeofValue = builder.ToString();

                    if (!typeofValue.Equals(typeof(string).ToString()))
                    {
                        Debug.LogWarning("Неверный упакованный тип. Была выполнена попытка преобразования");
                    }

                    while (true)
                    {
                        /*Read property*/
                        while (Pass(reader.Read())) continue;

                        builder.Clear();
                        while (Read(reader.Read())) continue; //read key
                        var key = builder.ToString();

                        //read tags
                        List<string> tagsList = new List<string>();
                        CoreJsonUtils.ReadTagsBlock(CoreJsonUtils.ReadNextBlock(reader, '['), tagsList);
                        

                        /*BASE_VALUE*/
                        var item = new VaultItem(key, ReadDouble(reader), tagsList);

                        /*ADD_VALUE*/
                        item.AdditionalValue = ReadDouble(reader);

                        /*RAW_VALUE*/
                        item.RawValue = ReadDouble(reader);

                        /*MULT_VALUE*/
                        item.MultipileValue = ReadDouble(reader);

                        vault.Write(item);
                    }
                }
                catch(ReadEndException)
                {
                    return vault;
                }
            }

            bool Pass(int value)
            {
                switch(value)
                {
                    case '"':

                        return false;

                    case -1: throw new ReadEndException();
                    default: return true;
                }
            }
            bool Read(int code)
            {
                switch (code)
                {
                    case '"':
                    case ':':
                    case '{':
                    case '}':
                    case ',':
                        return false;

                    case -1: throw new ReadEndException();

                    default:
                        {
                            builder.Append((char)code);
                        }
                        return true;
                }
            }
            double ReadDouble(StringReader reader)
            {
                try
                {
                    builder.Clear();
                    while (Pass(reader.Read())) continue; //pass syntaxis
                    while (Pass(reader.Read())) continue; //pass name_value
                    reader.Read(); //pass :
                    while (Read(reader.Read())) continue; //read number

                    //while (InversePass(reader.Read())) continue;

                    return double.Parse(builder.ToString(), System.Globalization.CultureInfo.InvariantCulture.NumberFormat);
                }
                catch(FormatException e)
                {
                    Debug.Log($"Builder value = {builder.ToString()}");
                    throw e;
                }
            }
        }
        public static Vault<T> FromJson<T>(string json) where T : Enum
        {
            if (json is "{}") return null;

            StringBuilder builder = new StringBuilder(32);
            Vault<T> vault = new Vault<T>();

            using (StringReader reader = new StringReader(json))
            {
                try
                {
                    //read type
                    while (Pass(reader.Read())) continue; //skip syntax
                    while (Pass(reader.Read())) continue; //skip "typeof"
                    while (Pass(reader.Read())) continue; //skip ": "
                    while (Read(reader.Read())) continue; //read typeof value
                    string typeofValue = builder.ToString();

                    if(!typeofValue.Equals(typeof(T).ToString()))
                    {
                        throw new ArgumentException("Несовместимый тип");
                    }

                    while (true)
                    {
                        /*Read property*/
                        while (Pass(reader.Read())) continue;

                        builder.Clear();
                        while (Read(reader.Read())) continue; //read key
                        T key = (T)Enum.Parse(typeof(T), builder.ToString());

                        //read tags
                        List<string> tagsList = new List<string>();
                        CoreJsonUtils.ReadTagsBlock(CoreJsonUtils.ReadNextBlock(reader, '['), tagsList);

                        /*BASE_VALUE*/
                        var item = new VaultItem<T>(key, ReadDouble(reader), tagsList);

                        /*ADD_VALUE*/
                        item.AdditionalValue = ReadDouble(reader);

                        /*RAW_VALUE*/
                        item.RawValue = ReadDouble(reader);

                        /*MULT_VALUE*/
                        item.MultipileValue = ReadDouble(reader);

                        vault.Write(item);
                    }
                }
                catch (ReadEndException)
                {
                    return vault;
                }
            }

            bool Pass(int value)
            {
                switch (value)
                {
                    case '"':

                        return false;

                    case -1: throw new ReadEndException();
                    default: return true;
                }
            }
            bool InversePass(int value)
            {
                switch (value)
                {
                    case ':':

                        return true;

                    case -1: throw new ReadEndException();
                    default: return false;
                }
            }
            bool Read(int code)
            {
                switch (code)
                {
                    case '"':
                    case ':':
                    case '{':
                    case '}':
                    case ',':
                        return false;

                    case -1: throw new ReadEndException();

                    default:
                        {
                            builder.Append((char)code);
                        }
                        return true;
                }
            }
            double ReadDouble(StringReader reader)
            {
                try
                {
                    builder.Clear();
                    while (Pass(reader.Read())) continue; //pass syntaxis
                    while (Pass(reader.Read())) continue; //pass name_value
                    reader.Read(); //pass :
                    while (Read(reader.Read())) continue; //read number

                    //while (InversePass(reader.Read())) continue;

                    return double.Parse(builder.ToString(), System.Globalization.CultureInfo.InvariantCulture.NumberFormat);
                }
                catch (FormatException e)
                {
                    Debug.Log($"Builder value = {builder.ToString()}");
                    throw e;
                }
            }
        }

             
    }
    #endregion

    #endregion

    public class RefVal<T>
    {
        private T _val;

        public T Value
        {
            get => _val;
            set
            {
                if (_val.Equals(value))
                    return;
                _val = value;
                OnValueChanged?.Invoke(_val);
            }
        }

        public event Action<T> OnValueChanged;

        public RefVal(T incomeValue)
        {
            OnValueChanged = null;
            _val = incomeValue;
        }

        public override bool Equals(object obj)
        {
            return _val.Equals(obj);
        }
        public override string ToString()
        {
            return _val.ToString();
        }
        public override int GetHashCode()
        {
            return _val.GetHashCode();
        }

        public static explicit operator T(RefVal<T> @ref)
        {
            return @ref.Value;
        }
        public static implicit operator RefVal<T>(T val)
        {
            return new RefVal<T>(val);
        }
        
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.LowLevel
{
    public sealed class Memory
    {
        #region core
        public struct Field
        {
            public readonly string name;
            public object value;

            public Field(string name, object value)
            {
                this.name = name;
                this.value = value;
            }
        }

        public class Heap
        {
            public float MemoryUsedPercent => memoryIndex / (float)(size);

            private int memoryIndex = 0;
            private Field[] heap;
            private Dictionary<string, int> refList;
            private int size;
            public Heap(int size)
            {
                heap = new Field[size];
                refList = new Dictionary<string, int>(size);
                this.size = size;
            }
            public void Flush()
            {
                refList.Clear();
                memoryIndex = 0;
            }
            public object ReadRaw(string fieldName)
            {
                try
                {
                    return heap[refList[fieldName]].value;
                }
                catch (KeyNotFoundException)
                {
                    throw new ViolationReadMemoryException(fieldName, "Can't read; Field does not exist");
                }
            }
            /// <summary>
            /// Производит чтение по названию поля
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <param name="fieldName"></param>
            /// <exception cref="ViolationReadMemoryException"></exception>
            /// <returns></returns>
            public T Read<T>(string fieldName) where T : class
            {
                try
                {

                    return heap[refList[fieldName]].value as T;
                }
                catch (KeyNotFoundException)
                {
                    throw new ViolationReadMemoryException(fieldName, "Can't read; Field does not exist");
                }
            }
            public void Read<T>(string fieldName, out T value) where T : class
            {
                value = Read<T>(fieldName);
            }
            /// <summary>
            /// Производит чтение с поля по указателю
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <param name="reference"></param>
            /// <exception cref="ViolationReadMemoryException"></exception>
            /// <returns></returns>
            public T Read<T>(int reference) where T : class
            {
                try
                {
                    if (reference >= memoryIndex) throw new ViolationReadMemoryException(reference.ToString("X"), "Field is null");
                    return heap[reference].value as T;
                }
                catch (IndexOutOfRangeException)
                {
                    throw new ViolationReadMemoryException(reference.ToString("X"), "Access denied");
                }

            }
            public void Read<T>(int reference, out T value) where T : class
            {
                value = Read<T>(reference);
            }
            public void WriteRaw(string fieldName, object value)
            {
                try
                {
                    heap[refList[fieldName]].value = value;
                }
                catch (KeyNotFoundException)
                {
                    if (memoryIndex < size)
                    {
                        refList.Add(fieldName, memoryIndex);
                        heap[memoryIndex] = new Field(fieldName, value);
                        memoryIndex++;
                    }
                    else
                    {
                        throw new ViolationWriteMemoryException(fieldName, "Out of memory");
                    }
                }
            }
            /// <summary>
            /// Производит запись в поле. Если поле не существует, то оно создается
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <param name="fieldName"></param>
            /// <param name="value"></param>
            public void Write<T>(string fieldName, T value) where T : class
            {
                try
                {
                    heap[refList[fieldName]].value = value;
                }
                catch (KeyNotFoundException)
                {
                    if (memoryIndex < size)
                    {
                        refList.Add(fieldName, memoryIndex);
                        heap[memoryIndex] = new Field(fieldName, value);
                        memoryIndex++;
                    }
                    else
                    {
                        throw new ViolationWriteMemoryException(fieldName, "Out of memory");
                    }
                }
            }
            /// <summary>
            /// Производит запись в поле через указатель
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <param name="reference"></param>
            /// <exception cref="ViolationWriteMemoryException"></exception>
            /// <param name="value"></param>
            public void Write<T>(int reference, T value) where T : class
            {
                try
                {
                    if (reference >= memoryIndex) throw new ViolationWriteMemoryException(reference.ToString("X"), "Field is null");

                    heap[reference].value = value;
                }
                catch (IndexOutOfRangeException)
                {
                    throw new ViolationWriteMemoryException(reference.ToString("X"), "Access denied");
                }
            }
            /// <summary>
            /// Возвращает указатель на поле в памяти
            /// </summary>
            /// <param name="fieldName"></param>
            /// <returns></returns>
            public int GetNativeReference(string fieldName)
            {
                try
                {
                    return refList[fieldName];
                }
                catch (KeyNotFoundException)
                {
                    return -1;
                }
            }

            /// <summary>
            /// Возвращает поле для низкоуровневой записи переменных.
            /// </summary>
            /// <param name="fieldName"></param>
            /// <exception cref="ViolationWriteMemoryException"></exception>
            /// <returns></returns>
            public ref Field GetNative(string fieldName)
            {
                try
                {
                    return ref heap[refList[fieldName]];
                }
                catch (KeyNotFoundException)
                {
                    throw new ViolationReadMemoryException(fieldName, "Can't read; Field does not exist");
                }
            }

            /// <summary>
            /// Возвращает поле для низкоуровневой записи переменных.
            /// </summary>
            /// <param name="reference"></param>
            /// <exception cref="ViolationWriteMemoryException"></exception>
            /// <returns></returns>
            public ref Field GetNative(int reference)
            {
                try
                {
                    if (reference >= memoryIndex) throw new ViolationWriteMemoryException(reference.ToString("X"), "Field is null");

                    return ref heap[reference];
                }
                catch (IndexOutOfRangeException)
                {
                    throw new ViolationWriteMemoryException(reference.ToString("X"), "Access denied");
                }
            }
        }
        public class Stack
        {
            public float MemoryUsedPercent => memoryIndex / (float)(size);

            private int memoryIndex = 0;
            private Field[] heap;
            private Dictionary<string, int> refList;
            private int size;
            public Stack(int size)
            {
                heap = new Field[size];
                refList = new Dictionary<string, int>(size);
                this.size = size;
            }
            public void Flush()
            {
                refList.Clear();
                memoryIndex = 0;
            }
            public object ReadRaw(string fieldName)
            {
                try
                {
                    return heap[refList[fieldName]].value;
                }
                catch (KeyNotFoundException)
                {
                    throw new ViolationReadMemoryException(fieldName, $"\'{fieldName}\' Can't read; Field does not exist");
                }
            }
            /// <summary>
            /// Производит чтение по названию поля
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <param name="fieldName"></param>
            /// <exception cref="ViolationReadMemoryException"></exception>
            /// <returns></returns>
            public T Read<T>(string fieldName) where T : struct
            {
                try
                {

                    return (T)heap[refList[fieldName]].value;
                }
                catch (KeyNotFoundException)
                {
                    throw new ViolationReadMemoryException(fieldName, $"\'{fieldName}\' Can't read; Field does not exist");
                }
            }
            public void Read<T>(string fieldName, out T value) where T : struct
            {
                value = Read<T>(fieldName);
            }
            /// <summary>
            /// Производит чтение с поля по указателю
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <param name="reference"></param>
            /// <exception cref="ViolationReadMemoryException"></exception>
            /// <returns></returns>
            public T Read<T>(int reference) where T : struct
            {
                try
                {
                    if (reference >= memoryIndex) throw new ViolationWriteMemoryException(reference.ToString("X"), $"{reference.ToString("X")} Field is null");
                    return (T)heap[reference].value;
                }
                catch (IndexOutOfRangeException)
                {
                    throw new ViolationWriteMemoryException(reference.ToString("X"), $"{reference.ToString("X")} Access denied");
                }

            }
            public void Read<T>(int reference, out T value) where T : struct
            {
                value = Read<T>(reference);
            }
            public void WriteRaw(string fieldName, object value)
            {
                try
                {
                    heap[refList[fieldName]].value = value;
                }
                catch (KeyNotFoundException)
                {
                    if (memoryIndex < size)
                    {
                        refList.Add(fieldName, memoryIndex);
                        heap[memoryIndex] = new Field(fieldName, value);
                        memoryIndex++;
                    }
                    else
                    {
                        throw new ViolationWriteMemoryException(fieldName, $"\'{fieldName}\' Out of memory");
                    }
                }
            }
            /// <summary>
            /// Производит запись в поле. Если поле не существует, то оно создается
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <param name="fieldName"></param>
            /// <param name="value"></param>
            public void Write<T>(string fieldName, T value) where T : struct
            {
                try
                {
                    heap[refList[fieldName]].value = value;
                }
                catch (KeyNotFoundException)
                {
                    if (memoryIndex < size)
                    {
                        refList.Add(fieldName, memoryIndex);
                        heap[memoryIndex] = new Field(fieldName, value);
                        memoryIndex++;
                    }
                    else
                    {
                        throw new ViolationWriteMemoryException(fieldName, $"\'{fieldName}\' Out of memory");
                    }
                }
            }
            /// <summary>
            /// Производит запись в поле через указатель
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <param name="reference"></param>
            /// <exception cref="ViolationWriteMemoryException"></exception>
            /// <param name="value"></param>
            public void Write<T>(int reference, T value) where T : struct
            {
                try
                {
                    if (reference >= memoryIndex) throw new ViolationWriteMemoryException(reference.ToString("X"), $"{reference.ToString("X")} Field is null");

                    heap[reference].value = value;
                }
                catch (IndexOutOfRangeException)
                {
                    throw new ViolationWriteMemoryException(reference.ToString("X"), $"{reference.ToString("X")} Access denied");
                }
            }
            /// <summary>
            /// Возвращает указатель на поле в памяти
            /// </summary>
            /// <param name="fieldName"></param>
            /// <returns></returns>
            public int GetNativeReference(string fieldName)
            {
                try
                {
                    return refList[fieldName];
                }
                catch (KeyNotFoundException)
                {
                    return -1;
                }
            }

            /// <summary>
            /// Возвращает поле для низкоуровневой записи переменных.
            /// </summary>
            /// <param name="fieldName"></param>
            /// <exception cref="ViolationWriteMemoryException"></exception>
            /// <returns></returns>
            public ref Field GetNative(string fieldName)
            {
                try
                {
                    return ref heap[refList[fieldName]];
                }
                catch (KeyNotFoundException)
                {
                    throw new ViolationReadMemoryException(fieldName, $"\'{fieldName}\' Can't read; Field does not exist");
                }
            }

            /// <summary>
            /// Возвращает поле для низкоуровневой записи переменных.
            /// </summary>
            /// <param name="reference"></param>
            /// <exception cref="ViolationWriteMemoryException"></exception>
            /// <returns></returns>
            public ref Field GetNative(int reference)
            {
                try
                {
                    if (reference >= memoryIndex) throw new ViolationWriteMemoryException(reference.ToString("X"), $"{reference.ToString("X")} Field is null");

                    return ref heap[reference];
                }
                catch (IndexOutOfRangeException)
                {
                    throw new ViolationWriteMemoryException(reference.ToString("X"), $"{reference.ToString("X")} Access denied");
                }
            }
        }
        #endregion

        private Heap _heap;
        private Stack _stack;
        public Heap mHeap => _heap;
        public Stack mStack => _stack;

        public Memory(int heapSize, int stackSize)
        {
            _heap = new Heap(heapSize);
            _stack = new Stack(stackSize);
        }

        public T Read<T>(string fieldName)
        {
            if (typeof(T).IsValueType)
            {
                return (T)_stack.ReadRaw(fieldName);
            }
            else
            {
                return (T)_heap.ReadRaw(fieldName);
            }
        }

        public void Read<T>(string fieldName, out T value)
        {
            if (typeof(T).IsValueType)
            {
                value = (T)_stack.ReadRaw(fieldName);
            }
            else
            {
                value = (T)_heap.ReadRaw(fieldName);
            }
        }

        public void Write<T>(string fieldName, T value)
        {
            if (typeof(T).IsValueType)
            {
                _stack.WriteRaw(fieldName, value);
            }
            else
            {
                _heap.WriteRaw(fieldName, value);
            }
        }

        /// <summary>
        /// Полностью очищает память
        /// </summary>
        public void Flush()
        {
            _heap.Flush();
            _stack.Flush();
        }
    }


    [Serializable]
    public class ViolationReadMemoryException : Exception
    {
        public string Name { get; }
        public ViolationReadMemoryException(string fieldName) { Name = fieldName; }
        public ViolationReadMemoryException(string fieldName, string message) : base(message) { Name = fieldName; }
        public ViolationReadMemoryException(string fieldName, string message, Exception inner) : base(message, inner) { Name = fieldName; }
        protected ViolationReadMemoryException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    [Serializable]
    public class ViolationWriteMemoryException : Exception
    {
        public string Name { get; }
        public ViolationWriteMemoryException(string fieldName) { Name = fieldName; }
        public ViolationWriteMemoryException(string fieldName, string message) : base(message) { Name = fieldName; }
        public ViolationWriteMemoryException(string fieldName, string message, Exception inner) : base(message, inner) { Name = fieldName; }
        protected ViolationWriteMemoryException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

}

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using UnityEngine;

namespace Core.Json
{
    public interface IJsonSerializble
    {
        string ToJson();
        //object FromJson(string json);
    }

    //Not use
    public interface IJsonSerializble<T> : IJsonSerializble where T : new()
    {
        //string ToJson();
        //new T FromJson(string json);
    }


    public static class CoreJsonUtils
    {
        #region json interface
        [System.Obsolete]
        public static string ToJson(IJsonSerializble graph) => graph.ToJson();
        [System.Obsolete]
        public static object FromJson(string json)
        {
            StringBuilder builder = new StringBuilder();
            using (StringReader reader = new StringReader(json))
            {
                while (Pass(reader, ':')) continue; //to typeof
                while (WriteUntil(reader, builder, ',')) continue; //read type
                var strType = builder.ToString().Replace('"', ' ').Trim();

                Type type = Type.GetType(strType);
                //Debug.Log(strType);

                //if (IsBasedType(type, typeof(Layer)))
                //{
                //    return Layer.JsonUtils.FromJson(json);
                //}
                //else if (IsBasedType(type, typeof(Item)))
                //{
                //    return Item.JsonUtils.FromJson(json);
                //}
                 return null;
            }

            bool IsBasedType(Type type, Type reqType)
            {
                while(!type.FullName.Equals(reqType.FullName))
                {
                    type = type.BaseType;
                    if (type.FullName.Equals(typeof(object).FullName))
                        return false;
                }
                return true;
            }
        }
        #endregion

        #region vectors
        public static string ToJson(Vector2 vector)
        {
            using (StringWriter writer = new StringWriter())
            {
                writer.Write($"[ {vector.x.ToString(NumberFormat)}, {vector.y.ToString(NumberFormat)} ] ");
                return writer.ToString();
            }
        }
        public static string ToJson(Vector3 vector)
        {
            using (StringWriter writer = new StringWriter())
            {
                writer.Write($"[ {vector.x.ToString(NumberFormat)}, {vector.y.ToString(NumberFormat)}, {vector.z.ToString(NumberFormat)} ] ");
                return writer.ToString();
            }
        }

        public static object FromJson_Vector(StringReader reader)
        {
            return FromJson_Vector(ReadNextBlock(reader, '['));
        }
        public static object FromJson_Vector(string json)
        {
            StringBuilder builder = new StringBuilder();
            using (StringReader reader = new StringReader(json))
            {
                int size = json.Split(',').Length;

                switch (size)
                {
                    case 2:
                        {
                            Vector2 value;
                            while (Pass(reader, skipValueTo)) continue;//skip to number
                            value.x = ReadFloat(reader, builder);
                            value.y = ReadFloat(reader, builder);
                            return value;
                        }
                    case 3:
                        {
                            Vector3 value;
                            while (Pass(reader, skipValueTo)) continue;//skip to number
                            value.x = ReadFloat(reader, builder);
                            value.y = ReadFloat(reader, builder);
                            value.z = ReadFloat(reader, builder);
                            return value;
                        }
                    default: throw new FormatException("Блок не является вектором");
                }
            }
        }

        #endregion

        public static IFormatProvider NumberFormat => System.Globalization.CultureInfo.InvariantCulture.NumberFormat;

        public static void ReadTagsBlock(string block, List<string> buffer)
        {
            using (StringReader reader = new StringReader(block))
            {
                ReadTagsBlock(reader, buffer);
            }
        }
        public static void ReadTagsBlock(TextReader reader, List<string> buffer)
        {
            StringBuilder builder = new StringBuilder();
            //List<string> tags = new List<string>();

            try
            {
                while (true)
                {
                    builder.Clear();
                    while (Pass(reader, '"')) continue;//skip to tag
                    while (Write(reader, builder)) continue; //write tag
                    var tg = builder.ToString().Trim();
                    if (!string.IsNullOrWhiteSpace(tg))
                        buffer.Add(tg);
                }
            }
            catch (ReadEndException)
            {
                return;
            }
        }

        private static readonly char[] skipValueTo = { '[', ':' };
        private static readonly char[] readValueTo = { ',', '}', ']', '"' };
        public static double ReadDouble(TextReader reader, StringBuilder builder)
        {
            builder.Clear();
            try
            {
                while (WriteUntil(reader, builder, readValueTo)) continue;

                if (string.IsNullOrWhiteSpace(builder.ToString()))
                    throw new ReadEndException();
                return double.Parse(builder.ToString(), NumberFormat);
            }
            catch (FormatException e)
            {
                Debug.Log($"ERROR Builder value = {builder.ToString()}");
                throw e;
            }
        }
        public static float ReadFloat(TextReader reader, StringBuilder builder)
        {
            builder.Clear();
            try
            {
                while (WriteUntil(reader, builder, readValueTo)) continue;

                return float.Parse(builder.ToString(), NumberFormat);
            }
            catch (FormatException e)
            {
                Debug.Log($"ERROR Builder value = {builder.ToString()}");
                throw e;
            }
        }

        public static bool Pass(int value)
        {
            switch (value)
            {
                case '"':

                    return false;

                case -1: throw new ReadEndException();
                default: return true;
            }
        }
        public static bool Pass(TextReader reader, params char[] passToAny)
        {
            var value = reader.Read();
            switch (value)
            {
                case -1: throw new ReadEndException();
                default:
                    {
                        foreach (var ch in passToAny)
                            if (value == ch)
                                return false;
                    }
                    return true;
            }
        }
        public static bool Pass(TextReader reader, char passToAny)
        {
            var value = reader.Read();
            switch (value)
            {
                case -1: throw new ReadEndException();
                default:
                    {
                        //foreach (var ch in passToAny)
                        if (value == passToAny)
                            return false;
                    }
                    return true;
            }
        }
        /// <summary>
        /// Останавливает запись на символе "
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static bool Write(TextReader reader, StringBuilder builder)
        {
            var value = reader.Read();
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
        public static bool WriteUntil(TextReader reader, StringBuilder builder, params char[] writeToAny)
        {
            var value = reader.Read();
            switch (value)
            {
                case -1: throw new ReadEndException();

                default:
                    {
                        foreach (var ch in writeToAny)
                        {
                            if (ch == value)
                            {
                                return false;
                            }
                        }

                        builder.Append((char)value);
                    }
                    return true;
            }
        }

        public static string ReadNextBlock(TextReader reader, char separator = '{')
        {
            StringBuilder blockBuilder = new StringBuilder();
            int beginWrite = 0;
            while (true)
            {
                int value = reader.Read();

                switch (separator)
                {
                    case '<':
                    case '>':
                        {
                            switch (value)
                            {
                                case '<':
                                    {
                                        beginWrite++;
                                        goto default;
                                    }
                                case '>':
                                    {
                                        blockBuilder.Append((char)value);
                                        beginWrite--;
                                        if (beginWrite is 0)
                                            return blockBuilder.ToString(); //exit
                                    }
                                    break;
                                case -1:
                                    throw new ReadEndException("Данные неожиданно оборвались");
                                default:
                                    {
                                        if (beginWrite > 0)
                                            blockBuilder.Append((char)value);
                                    }
                                    break;
                            }
                        }
                        break;
                    case '{':
                    case '}':
                        {
                            switch (value)
                            {
                                case '{':
                                    {
                                        beginWrite++;
                                        goto default;
                                    }
                                case '}':
                                    {
                                        blockBuilder.Append((char)value);
                                        beginWrite--;
                                        if (beginWrite is 0)
                                            return blockBuilder.ToString(); //exit
                                    }
                                    break;
                                case -1:
                                    throw new ReadEndException("Данные неожиданно оборвались");
                                default:
                                    {
                                        if (beginWrite > 0)
                                            blockBuilder.Append((char)value);
                                    }
                                    break;
                            }
                        }
                        break;
                    case '[':
                    case ']':
                        {
                            switch (value)
                            {
                                case '[':
                                    {
                                        beginWrite++;
                                        goto default;
                                    }
                                case ']':
                                    {
                                        blockBuilder.Append((char)value);
                                        beginWrite--;
                                        if (beginWrite is 0)
                                            return blockBuilder.ToString(); //exit
                                    }
                                    break;
                                case -1:
                                    throw new ReadEndException("Данные неожиданно оборвались");
                                default:
                                    {
                                        if (beginWrite > 0)
                                            blockBuilder.Append((char)value);
                                    }
                                    break;
                            }
                        }
                        break;
                    default: throw new ArgumentException("Неизвестный сепаратор. Доступные сепараторы '{' / '[' / '<'");
                }

            }
        }
    }

    [Serializable]
    public class ReadEndException : Exception
    {
        public ReadEndException() { }
        public ReadEndException(string message) : base(message) { }
        public ReadEndException(string message, Exception inner) : base(message, inner) { }
        protected ReadEndException(
          SerializationInfo info,
          StreamingContext context) : base(info, context) { }
    }
}

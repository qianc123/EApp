using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Xml;
using System.Xml.Serialization;
using EApp.Core.Exceptions;
using System.Runtime.Serialization.Formatters.Binary;

namespace EApp.Common.Serialization
{
    /// <summary>
    /// 用于对象序列化，反序列化 成XML, 文件 或者 二进制字节流
    /// </summary>
    public class SerializationManager
    {
        public delegate string TypeSerializeHandler(object obj);

        public delegate object TypeDeserializeHandler(string data);
        
        private static Dictionary<Type, KeyValuePair<TypeSerializeHandler, TypeDeserializeHandler>> handlers = 
            new Dictionary<Type, KeyValuePair<TypeSerializeHandler, TypeDeserializeHandler>>();
        
        static SerializationManager()
        {
            InitDefaultSerializeHandlers();
        }

        public static byte[] SerializeToBinary(object obj) 
        {
            BinaryFormatter binaryFormatter = new BinaryFormatter();

            using (MemoryStream memoryStream = new MemoryStream())
            {
                binaryFormatter.Serialize(memoryStream, obj);

                byte[] bytes = memoryStream.ToArray();

                memoryStream.Close();

                return bytes;
            }
        }

        /// <summary>
        /// Serialize object to xml string
        /// </summary>
        /// <param name="obj">Serialized object</param>
        /// <returns>string</returns>
        public static string Serialize(object obj)
        {
            if (obj == null) return string.Empty; 
            
            if (handlers.ContainsKey(obj.GetType())) 
            {
                return handlers[obj.GetType()].Key.Invoke(obj);
            }
            else 
            {
                StringBuilder sb = new StringBuilder();
                StringWriter sw = new StringWriter(sb);
                XmlSerializer ser = new XmlSerializer(obj.GetType());
                ser.Serialize(sw, obj);
                sw.Close();
                return sb.ToString();
            }
        }
        
        /// <summary>
        /// Deserialize string to object
        /// </summary>
        /// <param name="type">The type of object</param>
        /// <param name="data">Deserialized string.</param>
        /// <returns>Object</returns>
        public static object Deserialize(Type type, string data)
        {
            if (data == null) return null; 
            
            if (handlers.ContainsKey(type)) 
            {
                return handlers[type].Value.Invoke(data);
            }
            else 
            {
                StringReader sr = new StringReader(data);
                XmlSerializer ser = new XmlSerializer(type);
                object obj = ser.Deserialize(sr);
                sr.Close();
                return obj;
            }
        }
        
        /// <summary>
        /// Serialize object to file.
        /// </summary>
        /// <param name="obj">Serialized object</param>
        /// <param name="fileName">he name of file used to stored object.</param>
        /// <returns>bool</returns>
        public static bool Serialize(object obj, string fileName)
        {
            if (obj == null) return false; 
            FileStream fs = null;
            try {
                fs = new FileStream(fileName, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
                XmlSerializer ser = new XmlSerializer(obj.GetType());
                ser.Serialize(fs, obj);
                return true;
            }
            catch {
                return false;
            }
            finally {
                if (fs != null) fs.Close(); 
            }
        }
        
        private static bool DoSerialize(object obj, string fileName)
        {
            if (obj == null) return false; 
            FileStream fs = null;
            try {
                fs = new FileStream(fileName, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
                XmlSerializer ser = new XmlSerializer(obj.GetType());
                ser.Serialize(fs, obj);
                return true;
            }
            catch  {
                return false;
            }
            finally {
                if (fs != null) fs.Close(); 
            }
        }
        
        /// <summary>
        /// Serialize object to file.
        /// </summary>
        /// <typeparam name="TObjType"></typeparam>
        /// <param name="obj">Serialized object</param>
        /// <param name="fileName">The name of file used to stored object.</param>
        /// <returns>bool</returns>
        public static bool Serialize<TObjType>(TObjType obj, string fileName)
        {
            return DoSerialize(obj, fileName);
        }
        /// <summary>
        /// Serialize object to file.
        /// </summary>
        /// <param name="obj">The object implements IXmlSerializable.</param>
        /// <param name="fileName">The name of file used to stored object.</param>
        public static void Serialize(IXmlSerializable obj, string fileName)
        {
            FileStream fs = null;
            XmlWriter write = null;
            try {
                fs = new FileStream(fileName, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
                write = new XmlTextWriter(fs, Encoding.Default);
                obj.WriteXml(write);
            }
            catch {
                throw;
            }
            finally {
                if (fs != null) fs.Close(); 
            }
        }


        public static object DeserializeFromBinary(byte[] bytes)
        {
            BinaryFormatter binaryFormatter = new BinaryFormatter();

            using (MemoryStream memoryStream = new MemoryStream(bytes))
            {
                object obj = binaryFormatter.Deserialize(memoryStream);

                memoryStream.Close();

                return obj;
            }
        }

        /// <summary>
        /// Deserialize xml file to object.
        /// </summary>
        /// <param name="fileName">The file used to Deserialize object.</param>
        /// <param name="type">The type of object</param>
        /// <returns>Object</returns>
        /// <remarks></remarks>
        public static object Deserialize(string fileName, Type type)
        {
            if (!File.Exists(fileName)) return null; 
            FileStream fs = null;
            try {
                fs = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read);
                XmlSerializer ser = new XmlSerializer(type);
                object obj = ser.Deserialize(fs);
                return obj;
            }
            catch  {
                return null;
            }
            finally {
                if (fs != null) fs.Close(); 
            }
        }
        
        /// <summary>
        /// Deserialize xml file to object.
        /// </summary>
        /// <param name="obj">The object implements IXmlSerializable.</param>
        /// <param name="fileName">The name of file used to stored object.</param>
        public static void Deserialize(IXmlSerializable obj, string fileName)
        {
            if (!File.Exists(fileName)) return; 
            FileStream fs = null;
            XmlReader reader = null;
            try {
                fs = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read);
                reader = new XmlTextReader(fs);
                obj.ReadXml(reader);
            }
            catch  {
            }
            finally {
                //Throw
                if (fs != null) fs.Close(); 
            }
        }
        
        /// <summary>
        /// Deserialize xml string to object.
        /// </summary>
        /// <param name="data">Deserialized string</param>
        /// <param name="obj">The object implements IXmlSerializable.</param>
        public static void Deserialize(string data, IXmlSerializable obj)
        {
            if (data.Equals(string.Empty)) return; 
            XmlReader reader = null;
            StringReader sr = null;
            try {
                sr = new StringReader(data);
                reader = new XmlTextReader(sr);
                obj.ReadXml(reader);
            }
            catch {
                
            }
            finally {
                if (sr != null) sr.Close(); 
            }
        }
        
        /// <summary>
        /// Deserialize xml file to object
        /// </summary>
        /// <typeparam name="objType">Deserialized object</typeparam>
        /// <param name="fileName">The file used to Deserialize object.</param>
        /// <returns>Object</returns>
        public static TObjType Deserialize<TObjType>(string data)
        {
            return (TObjType)Deserialize(typeof(TObjType), data);
        }
        
        public static TObjType DeserializeFileToObject<TObjType>(string fileName)
        {
            return (TObjType)Deserialize(fileName, typeof(TObjType));
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <param name="serializeHandler"></param>
        /// <param name="deserializeHandler"></param>
        /// <remarks></remarks>
        public static void RegisterSerializeHandler(Type type, TypeSerializeHandler serializeHandler, TypeDeserializeHandler deserializeHandler)
        {
            Monitor.Enter(handlers);
            
            if (handlers.ContainsKey(type)) {
                handlers[type] = new KeyValuePair<TypeSerializeHandler, TypeDeserializeHandler>(serializeHandler, deserializeHandler);
            }
            else {
                handlers.Add(type, new KeyValuePair<TypeSerializeHandler, TypeDeserializeHandler>(serializeHandler, deserializeHandler));
            }
            
            Monitor.Exit(handlers);
        }
        
        
        #region InitDefaultSerializeHandlers

        private static void InitDefaultSerializeHandlers()
        {   
            RegisterSerializeHandler(typeof(string), new TypeSerializeHandler(ToString), new TypeDeserializeHandler(LoadString));
            RegisterSerializeHandler(typeof(int), new TypeSerializeHandler(ToString), new TypeDeserializeHandler(LoadInt));
            RegisterSerializeHandler(typeof(long), new TypeSerializeHandler(ToString), new TypeDeserializeHandler(LoadLong));
            RegisterSerializeHandler(typeof(short), new TypeSerializeHandler(ToString), new TypeDeserializeHandler(LoadShort));
            RegisterSerializeHandler(typeof(byte), new TypeSerializeHandler(ToString), new TypeDeserializeHandler(LoadByte));
            RegisterSerializeHandler(typeof(bool), new TypeSerializeHandler(ToString), new TypeDeserializeHandler(LoadBool));
            RegisterSerializeHandler(typeof(decimal), new TypeSerializeHandler(ToString), new TypeDeserializeHandler(LoadDecimal));
            RegisterSerializeHandler(typeof(char), new TypeSerializeHandler(ToString), new TypeDeserializeHandler(LoadChar));
            RegisterSerializeHandler(typeof(sbyte), new TypeSerializeHandler(ToString), new TypeDeserializeHandler(LoadSbyte));
            RegisterSerializeHandler(typeof(float), new TypeSerializeHandler(ToString), new TypeDeserializeHandler(LoadFloat));
            RegisterSerializeHandler(typeof(double), new TypeSerializeHandler(ToString), new TypeDeserializeHandler(LoadDouble));
            RegisterSerializeHandler(typeof(byte[]), new TypeSerializeHandler(ByteArrayToString), new TypeDeserializeHandler(LoadByteArray));
            RegisterSerializeHandler(typeof(Guid), new TypeSerializeHandler(ToString), new TypeDeserializeHandler(LoadGuid));
                
            RegisterSerializeHandler(typeof(DateTime), new TypeSerializeHandler(ToString), new TypeDeserializeHandler(LoadDateTime));
        }
        
        
        private static string ToString(object obj)
        {
            return obj.ToString();
        }
        
        private static object LoadString(string data)
        {
            return data;
        }
        
        private static object LoadInt(string data)
        {
            return int.Parse(data);
        }
        
        private static object LoadLong(string data)
        {
            return long.Parse(data);
        }
        
        private static object LoadShort(string data)
        {
            return short.Parse(data);
        }
        
        private static object LoadByte(string data)
        {
            return byte.Parse(data);
        }
        
        private static object LoadBool(string data)
        {
            return bool.Parse(data);
        }
        
        private static object LoadDecimal(string data)
        {
            return decimal.Parse(data);
        }
        
        private static object LoadChar(string data)
        {
            return char.Parse(data);
        }
        
        private static object LoadSbyte(string data)
        {
            return sbyte.Parse(data);
        }
        
        private static object LoadFloat(string data)
        {
            return float.Parse(data);
        }
        
        private static object LoadDouble(string data)
        {
            return double.Parse(data);
        }
        
        private static string ByteArrayToString(object data)
        {
            return Convert.ToBase64String((byte[])data);
        }
        
        private static object LoadByteArray(string data)
        {
            return Convert.FromBase64String(data);
        }
        
        private static object LoadGuid(string data)
        {
            return new Guid(data);
        }
        
        private static object LoadDateTime(string data)
        {
            return DateTime.Parse(data);
        }
        
        #endregion
        
    }
}



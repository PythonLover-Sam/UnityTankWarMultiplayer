using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

namespace UnityMultiplayerServer
{
    public static class NetworkUtils
    {
        /// <summary>
        /// 序列化工具
        /// </summary>
        /// <param name="obj">待序列化对象</param>
        /// <returns>字节数组</returns>
        public static byte[] Serialize(object obj)
        {
            if (obj == null || !obj.GetType().IsSerializable)
            {
                return null;
            }
            BinaryFormatter formatter = new BinaryFormatter();
            using (MemoryStream stream = new MemoryStream())
            {
                formatter.Serialize(stream, obj);
                byte[] data = stream.ToArray();
                return data;
            }
        }
        /// <summary>
        /// 反序列化
        /// </summary>
        /// <typeparam name="T">反序列化类型</typeparam>
        /// <param name="data">字节数组</param>
        /// <returns>反序列化结果</returns>
        public static T Deserialize<T>(byte[] data) where T : class
        {
            if (data == null || !typeof(T).IsSerializable) return null;
            Console.WriteLine("deserialized");
            BinaryFormatter formatter = new BinaryFormatter();
            using (MemoryStream stream = new MemoryStream(data))
            {
                object obj = formatter.Deserialize(stream);
                return obj as T;
            }
        }
    }
}


using EasyTCP.Packets;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;

namespace EasyTCP.Serialize
{
    public class StandardSerialize : ISerialization
    {
        public T FromRaw<T>(byte[] data)
        {
            if (typeof(T).IsValueType)
            {
				IntPtr ptr = Marshal.AllocHGlobal(data.Length);
				Marshal.Copy(data, 0, ptr, data.Length);
				T _struct = (T)Marshal.PtrToStructure(ptr, typeof(T));
				Marshal.FreeHGlobal(ptr);
                return _struct;
			}
            using (MemoryStream memoryStream = new MemoryStream(data))
            {
                BinaryFormatter formatter = new BinaryFormatter();
                return (T)formatter.Deserialize(memoryStream);
            }
        }

		public object FromRaw(byte[] data, Type type)
		{
			if (type.IsValueType)
			{
				IntPtr ptr = Marshal.AllocHGlobal(data.Length);
				Marshal.Copy(data, 0, ptr, data.Length);
				object _struct = Marshal.PtrToStructure(ptr, type);
				Marshal.FreeHGlobal(ptr);
				return _struct;
			}
			using (MemoryStream memoryStream = new MemoryStream(data))
			{
				BinaryFormatter formatter = new BinaryFormatter();
				return formatter.Deserialize(memoryStream);
			}
		}
		/// <summary>
		/// работает только с классами!!!
		/// </summary>
		/// <param name="data"></param>
		/// <returns></returns>
		public object FromRaw(byte[] data)
		{
			using (MemoryStream memoryStream = new MemoryStream(data))
			{
				BinaryFormatter formatter = new BinaryFormatter();
				return formatter.Deserialize(memoryStream);
			}
		}

		public byte[] Raw(object obj)
		{
			if (obj == null)
				return new byte[0];
            if (obj.GetType().IsValueType)
            {
				int structSize = Marshal.SizeOf(obj);
				byte[] struct_bytes = new byte[structSize];

				IntPtr ptr = Marshal.AllocHGlobal(structSize);
				Marshal.StructureToPtr(obj, ptr, false);
				Marshal.Copy(ptr, struct_bytes, 0, structSize);
				Marshal.FreeHGlobal(ptr);
                return struct_bytes;
			}
            using (MemoryStream memoryStream = new MemoryStream())
            {
                BinaryFormatter formatter = new BinaryFormatter();
                formatter.Serialize(memoryStream, obj);
                return memoryStream.ToArray();
            }
        }

		public void InitConnection(Connection connection)
		{

		}
	}
}

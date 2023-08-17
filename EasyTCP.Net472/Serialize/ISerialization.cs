using System;

namespace EasyTCP.Serialize
{
	public interface ISerialization
	{
		byte[] Raw(object obj);
		T FromRaw<T>(byte[] data);
		object FromRaw(byte[] data);
		object FromRaw(byte[] data, Type type);

		void InitConnection(Connection connection);
	}
}

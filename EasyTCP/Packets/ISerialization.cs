
namespace EasyTCP.Packets
{
	public interface ISerialization
	{
		public byte[] Raw(object obj);
		public T FromRaw<T>(byte[] data);
	}
}

namespace EasyTCP.Serialize
{
    public interface ISerialization
    {
        public byte[] Raw(object obj);
        public T FromRaw<T>(byte[] data);

		public void InitConnection(Connection connection);
    }
}

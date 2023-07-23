namespace EasyTCP.Serialize
{
    public interface ISerialization
    {
        public byte[] Raw(object obj);
        public T FromRaw<T>(byte[] data);
        public object FromRaw(byte[] data, Type type);

		public void InitConnection(Connection connection);
    }
}

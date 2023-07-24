using System.Runtime.InteropServices;

namespace EasyTCP.Packets
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct PacketFirewall
    {
		/// <summary>
		/// максимальная длина строки 255 символов
		/// </summary>
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 255)]
		public string Answer;
        public short Code;
    }

}

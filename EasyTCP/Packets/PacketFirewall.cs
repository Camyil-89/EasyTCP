using System.Runtime.InteropServices;

namespace EasyTCP.Packets
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct PacketFirewall
    {
        public string Answer;
        public short Code;
    }

}

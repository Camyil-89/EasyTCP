using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace EasyTCP.Packets
{
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct ReceiveInfo
	{
		public long Receive;
		public long TotalNeedReceive;
	}
}

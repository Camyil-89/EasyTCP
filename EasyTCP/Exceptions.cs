using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyTCP
{
	public class ExceptionEasyTCPSslNotAuthenticated : Exception
	{
		public ExceptionEasyTCPSslNotAuthenticated(string message)
		: base(message)
		{
		}

		public ExceptionEasyTCPSslNotAuthenticated(string message, Exception inner)
			: base(message, inner)
		{
		}
	}
	public class ExceptionEasyTCPFirewall : Exception
	{
		public ExceptionEasyTCPFirewall(string message)
		: base(message)
		{
		}

		public ExceptionEasyTCPFirewall(string message, Exception inner)
			: base(message, inner)
		{
		}
	}
	public class ExceptionEasyTCPAbortConnect : Exception
	{
		public ExceptionEasyTCPAbortConnect(string message)
		: base(message)
		{
		}

		public ExceptionEasyTCPAbortConnect(string message, Exception inner)
			: base(message, inner)
		{
		}
	}
	public class ExceptionEasyTCPTimeout : Exception
	{
		public ExceptionEasyTCPTimeout(string message)
		: base(message)
		{
		}

		public ExceptionEasyTCPTimeout(string message, Exception inner)
			: base(message, inner)
		{
		}
	}
}

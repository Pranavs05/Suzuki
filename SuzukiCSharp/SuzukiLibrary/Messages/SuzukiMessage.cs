using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SuzukiLibrary
{
	public class SuzukiMessage
	{
		public string      Msg;
		public UInt16      Port;
		public string      Address;


		public SuzukiMessage( string msg )
		{
			Msg = msg;
		}

		public SuzukiMessage( string msg, UInt16 port, string address )
		{
			Msg = msg;
			Port = port;
			Address = address;
		}
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SuzukiLibrary.Messages
{
	public struct ElectNode
	{
		public UInt32      electNodeId;
	}

	public class ElectBroadcast : MessageBase
	{
		public ElectNode       value;

		//
		public ElectBroadcast( UInt32 sendId )
		{
			senderId = sendId;
			type = "electBroadcast";
			value.electNodeId = senderId;
		}
	}
}

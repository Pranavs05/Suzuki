using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SuzukiLibrary.Messages
{
	public struct NodeId
	{
		public UInt32      nodeId;
	}

	public class ElectionBroadcast : MessageBase
	{
		NodeId      value;


		//
		public ElectionBroadcast( UInt32 sendId )
		{
			senderId = sendId;
			type = "electionBroadcast";
			value.nodeId = senderId;
		}
	}
}

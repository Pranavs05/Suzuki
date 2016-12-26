using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SuzukiLibrary.Messages
{
	public struct RequestNumber
	{
		public UInt64      requestNumber;
	}


	public class Request : MessageBase
	{
		public RequestNumber       value;



		//
		public Request( UInt32 sendId, UInt64 RequestNumber )
		{
			senderId = sendId;
			type = "request";
			value.requestNumber = RequestNumber;
		}
	}
}

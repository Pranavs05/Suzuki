using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SuzukiLibrary.Messages
{
	public class ElectionOk : MessageBase
	{

		public ElectionOk( UInt32 sendId )
		{
			senderId = sendId;
			type = "electionOK";
		}
	}
}

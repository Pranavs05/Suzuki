using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SuzukiLibrary.Messages
{
	public class TokenMessage : MessageBase
	{
		public Token       value;


		//
		public TokenMessage( UInt32 sendId, Token token )
		{
			senderId = sendId;
			type = "token";
			value = token;
		}
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;



namespace SuzukiLibrary
{
	//public struct LastRequest
	//{
	//	UInt32		nodeId;
	//	UInt64      number;
	//}


	public class Token
	{
		Dictionary< UInt32, UInt64 >	lastRequests;
		Queue< UInt32 >					queue;


		public Token()
		{
			queue = new Queue< UInt32 >();
			lastRequests = new Dictionary< UInt32, UInt64 >();
		}


		#region Properties

		public Dictionary<UInt32, UInt64> LastRequests
		{
			get
			{
				return lastRequests;
			}

			set
			{
				lastRequests = value;
			}
		}

		public Queue<uint> Queue
		{
			get
			{
				return queue;
			}

			set
			{
				queue = value;
			}
		} 
		#endregion
	}
}

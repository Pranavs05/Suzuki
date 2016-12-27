using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;



namespace SuzukiLibrary
{
	public struct LastRequest
	{
		public UInt32      nodeId;
		public UInt64      number;
	}


	public class Token
	{
		Collection< LastRequest >		lastRequests;
		Queue< UInt32 >					queue;


		public Token()
		{
			queue = new Queue< UInt32 >();
			lastRequests = new Collection< LastRequest >();
		}


		#region HelperFunctions

		public UInt64	GetSeqNumber( UInt32 nodeId )
		{
			foreach( var item in lastRequests )
			{
				if( item.nodeId == nodeId )
					return item.number;
			}

			// Fixme:
			return 0;
		}

		public void		SetSeqNumber( UInt32 nodeId, UInt64 seq )
		{
			for( int i = 0; i < lastRequests.Count; ++i )
			{
				if( lastRequests[ i ].nodeId == nodeId )
				{
					var newDesc = new LastRequest();
					newDesc.number = seq;
					newDesc.nodeId = nodeId;
					lastRequests[ i ] = newDesc;
				}
			}
		}

		#endregion


		#region Properties

		public Collection< LastRequest > LastRequests
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

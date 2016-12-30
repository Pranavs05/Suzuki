using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Collections;
using System.Collections.Concurrent;
using Newtonsoft.Json;
using System.IO;
using Newtonsoft.Json.Linq;



namespace SuzukiLibrary
{
	public delegate void SendBroadcastDelegate( Messages.MessageBase msg );
	public delegate void SendDelegate( Messages.MessageBase msg, UInt16 port, string address );

	public class SuzukiCore
	{
		event MessageForLogger			LogMessage;
		public SendBroadcastDelegate    SendBroadcast;
		public SendDelegate             Send;

		Semaphore       m_semaphore;

		// Suzuki algorithm
		Token           m_token;
		bool            m_possesedCriticalSection;
		object          m_tokenLock = new object();

		Dictionary< UInt32, UInt64 >    m_requestNumbers;

		Config.Configuration    m_configuration;


		public SuzukiCore()
		{
			m_semaphore = new Semaphore( 0, 1 );

			m_token = null;
			m_possesedCriticalSection = false;
			m_requestNumbers = new Dictionary<UInt32, UInt64>();

			m_configuration = null;
		}

		public void Init( Config.Configuration config )
		{
			m_configuration = config;

			foreach( var node in m_configuration.Nodes )
			{
				m_requestNumbers[ node.NodeID ] = 0;
			}

			//CreateToken();  // Temporary
		}


		public void AccessResource()
		{
			bool isToken = false;
			lock ( m_tokenLock )
			{
				if( m_token != null )
				{
					isToken = true;
					m_possesedCriticalSection = true;
				}
			}

			if( !isToken )
			{
				UInt64 seqNumber = IncrementSeqNumber();
				Messages.Request request = new Messages.Request( m_configuration.NodeID, seqNumber );

				SendBroadcast( request );

				m_semaphore.WaitOne();
			}
			LogMessage( this, "Accessed critical section" );
		}

		public void FreeResource()
		{
			lock ( m_tokenLock )
			{
				m_possesedCriticalSection = false;

				LogMessage( this, "Released critical section" );

				var thisNodeId = m_configuration.NodeID;
				m_token.SetSeqNumber( thisNodeId, m_requestNumbers[ thisNodeId ] );

				// Enqueue other requests.
				foreach( var node in m_configuration.Nodes )
				{
					if( m_requestNumbers[ node.NodeID ] == m_token.GetSeqNumber( node.NodeID ) + 1 &&
						!m_token.Queue.Contains( node.NodeID ) )
					{
						m_token.Queue.Enqueue( node.NodeID );
					}
				}

				// Send to first node from queue.
				if( m_token.Queue.Count != 0 )
				{
					var nextNode = m_token.Queue.Dequeue();
					SendToken( nextNode );
				}
			}
		}

		public void RequestMessage( Messages.Request request )
		{
			var nodeId = request.senderId;
			var lastReqId = m_requestNumbers[ nodeId ];

			m_requestNumbers[ nodeId ] = Math.Max( lastReqId, request.value.requestNumber );

			lock ( m_tokenLock )
			{
				// We can immediatly send token.
				if( m_token != null &&
					m_possesedCriticalSection == false &&
					m_requestNumbers[ nodeId ] == m_token.GetSeqNumber( nodeId ) + 1 )
				{
					SendToken( nodeId );
				}
				else
				{
					// Note: we don't enqueue node here. This happens while releasing critical section.
					//m_token.Queue.Enqueue( nodeId );
				}
			}
		}


		public void TokenMessage( Messages.TokenMessage msg )
		{
			lock ( m_tokenLock )
			{
				var nodeDesc = m_configuration.FindNode( msg.senderId );
				LogMessage( this, "Tokend received from node [" + nodeDesc.NodeID + "] " + nodeDesc.NodeIP + " Port: " + nodeDesc.Port );

				m_token = msg.value;
				m_possesedCriticalSection = true;

				m_semaphore.Release( 1 );
			}
		}

		public void CreateToken()
		{
			//// Note: This function creates token. In future use election instead.
			//bool lower = true;
			//foreach( var node in m_configuration.Nodes )
			//{
			//	if( node.NodeID > m_configuration.NodeID )
			//		lower = false;
			//}

			//if( lower )
			//{
				Token token = new Token();
				foreach( var item in m_requestNumbers )
				{
					var lastRequest =  new LastRequest();
					lastRequest.nodeId = item.Key;
					lastRequest.number = item.Value;

					token.LastRequests.Add( lastRequest );
				}

				m_token = token;
				LogMessage( this, "Created token" );
			//}
		}

		#region Sequence number
		private UInt64 IncrementSeqNumber()
		{
			return ++m_requestNumbers[ m_configuration.NodeID ];
		}

		#endregion

		private void SendToken( UInt32 nodeId )
		{
			Messages.TokenMessage token = new Messages.TokenMessage( m_configuration.NodeID, m_token );
			m_token = null;

			var nodeDesc = m_configuration.FindNode( nodeId );
			//var jsonString = JsonConvert.SerializeObject( token );

			Send( token, nodeDesc.Port, nodeDesc.NodeIP );

			LogMessage( this, "Token sended to node: [" + nodeDesc.NodeID + "] " + nodeDesc.NodeIP + " Port: " + nodeDesc.Port );
		}


		public void SetLoggerHandler( MessageForLogger handler )
		{
			LogMessage += handler;
		}
	}
}

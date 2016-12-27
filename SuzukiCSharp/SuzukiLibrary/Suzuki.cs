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

	public delegate void MessageForLogger( object sender, string message );

	enum State
	{
		Election,

	}



    public class Suzuki
    {
		event MessageForLogger		LogMessage;

		Protocol        m_protocol;
		Thread          m_receiver;
		Semaphore       m_semaphore;

		// Suzuki algorithm
		Token           m_token;
		UInt64          m_seqNumber;
		bool            m_possesedCriticalSection;
		object          m_seqLock = new object();
		object          m_tokenLock = new object();

		Dictionary< UInt32, UInt64 >    m_requestNumbers;

		Config.Configuration    m_configuration;

		// Suzuki Helpers
		string			ConfigPath { get; set; }


		public Suzuki()
		{
			m_protocol = new Protocol();
			m_receiver = null;
			m_semaphore = new Semaphore( 0, 1 );

			m_token = null;
			m_seqNumber = 0;
			m_possesedCriticalSection = false;
			m_requestNumbers = new Dictionary< UInt32, UInt64 >();

			m_configuration = null;
			ConfigPath = "SuzukiConfig.json";
		}


		public void		Init()
		{
			m_configuration = JsonConvert.DeserializeObject< Config.Configuration >( ReadConfig( ConfigPath ) );

			foreach( var node in m_configuration.Nodes )
			{
				m_requestNumbers[ node.NodeID ] = 0;
			}

			m_protocol.Init( m_configuration );

			m_receiver = new Thread( QueryMessage );
			m_receiver.Start();

			LogMessage( this, "Suzuki started" );

			CreateToken();  // Temporary
		}


		public void		ShutDown()
		{
			m_protocol.ShutDown();
		}

		public void		SetLoggerHandler( MessageForLogger handler )
		{
			LogMessage += handler;
			m_protocol.SetLoggerHandler( handler );
		}


		public void		AccessResource()
		{
			bool isToken = false;
			lock( m_tokenLock )
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
		}

		public void		FreeResource()
		{
			lock ( m_tokenLock )
			{
				m_possesedCriticalSection = false;

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


		string			ReadConfig( string filePath )
		{
			if( File.Exists( filePath ) )
			{
				return File.ReadAllText( filePath );
			}
			return "";
		}

		private void	QueryMessage()
		{
			foreach( var item in m_protocol.MessageQueue.GetConsumingEnumerable() )
			{
				var json = (JObject)JsonConvert.DeserializeObject( item.Msg );
				var type = json[ "type"].ToString();
				if( type == "request" )
				{
					Messages.Request request = JsonConvert.DeserializeObject< Messages.Request >( item.Msg );
					RequestMessage( request );
				}
				else if( type == "token" )
				{
					Messages.TokenMessage token = JsonConvert.DeserializeObject< Messages.TokenMessage >( item.Msg );
					TokenMessage( token );
				}
				else if( type == "electionOK" )
				{
					Messages.ElectionOk electionOk = JsonConvert.DeserializeObject< Messages.ElectionOk >( item.Msg );
					ElectionOk( electionOk );
				}
				else if( type == "elctionBroadcast" )
				{
					Messages.ElectionBroadcast electionBroadcast = JsonConvert.DeserializeObject< Messages.ElectionBroadcast >( item.Msg );
					ElectionBroadcast( electionBroadcast );
				}
			}
		}

		private void	RequestMessage( Messages.Request request )
		{
			var nodeId = request.senderId;
			var lastReqId = m_requestNumbers[ nodeId ];

			m_requestNumbers[ nodeId ] = Math.Max( lastReqId, request.value.requestNumber );

			lock( m_tokenLock )
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

		private void	TokenMessage( Messages.TokenMessage msg )
		{
			lock( m_tokenLock )
			{
				m_token = msg.value;
				m_possesedCriticalSection = true;

				m_semaphore.Release( 1 );

				var nodeDesc = m_configuration.FindNode( msg.senderId );

				LogMessage( this, "Tokend received from node [" + nodeDesc.NodeID + "] " + nodeDesc.NodeIP + " Port: " + nodeDesc.Port );
			}
		}

		private void	ElectionBroadcast( Messages.ElectionBroadcast election )
		{

		}

		private void	ElectionOk( Messages.ElectionOk ok )
		{

		}

		private void	CreateToken()
		{
			// Note: This function creates token. In future use election instead.
			bool lower = true;
			foreach( var node in m_configuration.Nodes )
			{
				if( node.NodeID < m_configuration.NodeID )
					lower = false;
			}

			if( lower )
			{
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
			}
		}

		#region Sequence number
		private UInt64	IncrementSeqNumber()
		{
			UInt64 result;
			lock ( m_seqLock )
			{
				result = ++m_seqNumber;
			}
			return result;
		}

		#endregion

		private void	SendBroadcast( Messages.MessageBase msg )
		{
			var jsonString = JsonConvert.SerializeObject( msg );

			foreach( var node in m_configuration.Nodes )
			{
				// Skip this application node.
				if( node.NodeID == m_configuration.NodeID )
					continue;

				m_protocol.Send( jsonString, node.Port, node.NodeIP );
			}
		}

		private void	SendToken( UInt32 nodeId )
		{
			Messages.TokenMessage token = new Messages.TokenMessage( m_configuration.NodeID, m_token );
			m_token = null;

			var nodeDesc = m_configuration.FindNode( nodeId );
			var jsonString = JsonConvert.SerializeObject( token );

			m_protocol.Send( jsonString, nodeDesc.Port, nodeDesc.NodeIP );

			LogMessage( this, "Token sended to node: [ " + nodeDesc.NodeID + " ] " + nodeDesc.NodeIP + " Port: " + nodeDesc.Port );
		}


	}
}

﻿using System;
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
using System.Timers;


namespace SuzukiLibrary
{
	public delegate void SendBroadcastDelegate( Messages.MessageBase msg );
	public delegate void SendDelegate( Messages.MessageBase msg, UInt16 port, string address );
	public delegate void RestartElectionDelegate();

	public class SuzukiCore
	{
		event MessageForLogger			LogMessage;
		public SendBroadcastDelegate    SendBroadcast;
		public SendDelegate             Send;
		public RestartElectionDelegate	RestartElection;

		Semaphore       m_semaphore;

		// Suzuki algorithm
		Token           m_token;
		bool            m_possesedCriticalSection;
		bool            m_waitingForAccess;
		object          m_tokenLock = new object();

		Dictionary< UInt32, UInt64 >    m_requestNumbers;

		Config.Configuration    m_configuration;
		System.Timers.Timer		m_tokenReceiveTimeout;


		public SuzukiCore()
		{
			m_semaphore = new Semaphore( 0, 1 );

			m_token = null;
			m_possesedCriticalSection = false;
			m_waitingForAccess = false;
			m_requestNumbers = new Dictionary<UInt32, UInt64>();

			m_configuration = null;
			m_tokenReceiveTimeout = null;
		}

		public void Init( Config.Configuration config )
		{
			m_configuration = config;

			foreach( var node in m_configuration.Nodes )
			{
				m_requestNumbers[ node.NodeID ] = 0;
			}

			m_tokenReceiveTimeout = new System.Timers.Timer( config.TokenReceiveTimeout );
			m_tokenReceiveTimeout.Elapsed += ReceiveTimeout;
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
				RequestCriticalSection();

				m_waitingForAccess = true;		// Hopefully no synchronization is needed.
				m_semaphore.WaitOne();
				m_waitingForAccess = false;

				m_tokenReceiveTimeout.Stop();
			}
			LogMessage( this, "Accessed critical section" );
		}

		private void RequestCriticalSection()
		{
			UInt64 seqNumber = IncrementSeqNumber();
			Messages.Request request = new Messages.Request( m_configuration.NodeID, seqNumber );

			SendBroadcast( request );
			m_tokenReceiveTimeout.Start();
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

				if( m_token != null )
				{
					LogMessage( this, "Duplicate tokend received from node [" + nodeDesc.NodeID + "] " + nodeDesc.NodeIP + " Port: " + nodeDesc.Port );
					
					// @todo: Hmmm, consider merging tokens.
					return;
				}

				LogMessage( this, "Tokend received from node [" + nodeDesc.NodeID + "] " + nodeDesc.NodeIP + " Port: " + nodeDesc.Port );

				m_token = msg.value;
				ReleaseSemaphore();
			}
		}

		private void ReleaseSemaphore()
		{
			m_possesedCriticalSection = true;
			m_semaphore.Release( 1 );
		}

		public void CreateToken()
		{
			Token token = new Token();
			foreach( var item in m_requestNumbers )
			{
				var lastRequest =  new LastRequest();
				lastRequest.nodeId = item.Key;
				lastRequest.number = item.Value;

				token.LastRequests.Add( lastRequest );
			}

			lock ( m_tokenLock )
			{
				m_token = token;
			}

			LogMessage( this, "Created token" );
		}

		private void ReceiveTimeout( Object source, ElapsedEventArgs e )
		{
			LogMessage( this, "Waiting for token timeout." );

			// Do something. Start election for example.
			m_tokenReceiveTimeout.Stop();
			RestartElection();
		}

		#region Sequence number
		private UInt64 IncrementSeqNumber()
		{
			return ++m_requestNumbers[ m_configuration.NodeID ];
		}

		#endregion

		private void SendToken( UInt32 nodeId )
		{
			Messages.TokenMessage token = null;

			lock ( m_tokenLock )
			{
				token = new Messages.TokenMessage( m_configuration.NodeID, m_token );
				m_token = null;
			}

			var nodeDesc = m_configuration.FindNode( nodeId );
			//var jsonString = JsonConvert.SerializeObject( token );

			Send( token, nodeDesc.Port, nodeDesc.NodeIP );

			LogMessage( this, "Token sended to node: [" + nodeDesc.NodeID + "] " + nodeDesc.NodeIP + " Port: " + nodeDesc.Port );
		}


		public void SetLoggerHandler( MessageForLogger handler )
		{
			LogMessage += handler;
		}

		public void KillToken()
		{
			lock( m_tokenLock )
			{
				if( m_token != null )
					LogMessage( this, "Token killed" );
				m_token = null;

				if( m_possesedCriticalSection )
				{
					// Problems. What todo? Wait for release ???
				}
			}
		}

		public void ElectionEnded()
		{
			// Resend request if user tried to access critical section before election.
			// Else do nothing.
			if( m_waitingForAccess && m_token != null )
				ReleaseSemaphore();
			else if( m_waitingForAccess )
				RequestCriticalSection();
		}
	}
}

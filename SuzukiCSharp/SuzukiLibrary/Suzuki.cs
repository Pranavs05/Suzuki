using System;
using System.Collections.Generic;
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
		object          m_seqLock = new object();

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

			m_configuration = null;
			ConfigPath = "SuzukiConfig.json";
		}


		public void		Init()
		{
			m_configuration = JsonConvert.DeserializeObject< Config.Configuration >( ReadConfig( ConfigPath ) );

			m_protocol.Init();

			m_receiver = new Thread( QueryMessage );
			m_receiver.Start();
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
			if( m_token == null )
			{
				UInt64 seqNumber = IncrementSeqNumber();
				Messages.Request request = new Messages.Request( m_configuration.NodeID, seqNumber );

				SendBroadcast( request );

				m_semaphore.WaitOne();
			}
		}

		public void		FreeResource()
		{
			// Send token to other nodes
			m_semaphore.Release( 1 );
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

		}

		private void	TokenMessage( Messages.TokenMessage msg )
		{

		}

		private void	ElectionBroadcast( Messages.ElectionBroadcast election )
		{

		}

		private void	ElectionOk( Messages.ElectionOk ok )
		{

		}

		private Token	CreateToken()
		{
			Token token = new Token();
			return token;
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
				m_protocol.Send( jsonString, node.Port, node.NodeIP );
			}
		}

	}
}

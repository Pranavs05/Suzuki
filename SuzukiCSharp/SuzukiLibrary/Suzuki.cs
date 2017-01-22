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


    public class Suzuki
    {
		event MessageForLogger		LogMessage;

		Protocol        m_protocol;
		Thread          m_receiver;


		Config.Configuration    m_configuration;
		SuzukiCore              m_suzuki;
		Election                m_election;
		bool                    m_elected;

		// Suzuki Helpers
		string			ConfigPath { get; set; }




		public Suzuki()
		{
			m_protocol = new Protocol();
			m_suzuki = new SuzukiCore();
			m_election = new Election();
			m_receiver = null;
			m_elected = false;

			m_configuration = null;
			ConfigPath = "SuzukiConfig.json";
		}


		public void		Init( MessageForLogger handler )
		{
			m_configuration = JsonConvert.DeserializeObject< Config.Configuration >( ReadConfig( ConfigPath ) );

			m_protocol.Init( m_configuration );

			m_suzuki.Init( m_configuration );
			m_suzuki.Send = Send;
			m_suzuki.SendBroadcast = SendBroadcast;
			m_suzuki.RestartElection = StartElection;

			m_election.Init( m_configuration );
			m_election.Send = Send;
			m_election.SendBroadcast = SendBroadcast;
			m_election.WonElection = ElectionEnded;

			m_receiver = new Thread( QueryMessage );
			m_receiver.Start();

			SetLoggerHandler( handler );

			LogMessage( this, "Suzuki started. Node info: [" + m_configuration.NodeID + "] Port:" + m_configuration.Port );
		}

		//debug
		public void KillToken( object param )
		{
			m_suzuki.KillToken();
		}


		public void		ShutDown()
		{
			m_protocol.ShutDown();
		}

		public void		StartElection()
		{
			LogMessage( this, "Election started" );
			m_elected = false;
			m_election.StartElection();
		}

		public void		AccessResource()
		{
			m_suzuki.AccessResource();
		}

		public void		FreeResource()
		{
			m_suzuki.FreeResource();
		}


		private void	QueryMessage()
		{
			foreach( var item in m_protocol.MessageQueue.GetConsumingEnumerable() )
			{
				var json = (JObject)JsonConvert.DeserializeObject( item.Msg );
				var type = json[ "type"].ToString();

				if( type == "electionOK" )
				{
					Messages.ElectionOk electionOk = JsonConvert.DeserializeObject< Messages.ElectionOk >( item.Msg );
					m_election.ElectionOk( electionOk );
				}
				else if( type == "electionBroadcast" )
				{
					Messages.ElectionBroadcast electionBroadcast = JsonConvert.DeserializeObject< Messages.ElectionBroadcast >( item.Msg );
					m_election.ElectionBroadcast( electionBroadcast );
					m_elected = false;
				}
				else if( type == "electBroadcast" )
				{
					Messages.ElectBroadcast electBroadcast = JsonConvert.DeserializeObject< Messages.ElectBroadcast >( item.Msg );
					m_election.ElectBroadcast( electBroadcast );
				}
				else if( m_elected )
				{
					if( m_elected && type == "request" )
					{
						Messages.Request request = JsonConvert.DeserializeObject< Messages.Request >( item.Msg );
						m_suzuki.RequestMessage( request );
					}
					else if( m_elected && type == "token" )
					{
						Messages.TokenMessage token = JsonConvert.DeserializeObject< Messages.TokenMessage >( item.Msg );
						m_suzuki.TokenMessage( token );
					}
				}
				else
				{
					LogMessage( this, "Ignored " + type + " Waiting for election." );
				}
			}
		}

		private void	ElectionEnded( bool result )
		{
			m_suzuki.Init( m_configuration );

			if( result )
				m_suzuki.CreateToken();

			m_election.Clear();
			m_elected = true;
			m_suzuki.ElectionEnded();
		}

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

		private void Send( Messages.MessageBase msg, UInt16 port, string address )
		{
			var jsonString = JsonConvert.SerializeObject( msg );
			m_protocol.Send( jsonString, port, address );
		}

		public void SetLoggerHandler( MessageForLogger handler )
		{
			LogMessage += handler;
			m_protocol.SetLoggerHandler( handler );
			m_suzuki.SetLoggerHandler( handler );
			m_election.SetLoggerHandler( handler );
		}

		string ReadConfig( string filePath )
		{
			if( File.Exists( filePath ) )
			{
				return File.ReadAllText( filePath );
			}
			return "";
		}




		public bool NoElectionResponse
		{
			get
			{
				return m_election.NoElectionResponse;
			}

			set
			{
				m_election.NoElectionResponse = value;
			}
		}


	}
}

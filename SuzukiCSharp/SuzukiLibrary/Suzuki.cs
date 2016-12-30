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

		// Suzuki Helpers
		string			ConfigPath { get; set; }


		public Suzuki()
		{
			m_protocol = new Protocol();
			m_suzuki = new SuzukiCore();
			m_receiver = null;

			m_configuration = null;
			ConfigPath = "SuzukiConfig.json";
		}


		public void		Init()
		{
			m_configuration = JsonConvert.DeserializeObject< Config.Configuration >( ReadConfig( ConfigPath ) );

			m_protocol.Init( m_configuration );

			m_suzuki.Init( m_configuration );
			m_suzuki.Send = m_protocol.Send;
			m_suzuki.SendBroadcast = SendBroadcast;

			m_receiver = new Thread( QueryMessage );
			m_receiver.Start();

			LogMessage( this, "Suzuki started. Node info: [" + m_configuration.NodeID + "] Port:" + m_configuration.Port );
		}


		public void		ShutDown()
		{
			m_protocol.ShutDown();
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
				if( type == "request" )
				{
					Messages.Request request = JsonConvert.DeserializeObject< Messages.Request >( item.Msg );
					m_suzuki.RequestMessage( request );
				}
				else if( type == "token" )
				{
					Messages.TokenMessage token = JsonConvert.DeserializeObject< Messages.TokenMessage >( item.Msg );
					m_suzuki.TokenMessage( token );
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


		private void	ElectionBroadcast( Messages.ElectionBroadcast election )
		{

		}

		private void	ElectionOk( Messages.ElectionOk ok )
		{

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


		public void SetLoggerHandler( MessageForLogger handler )
		{
			LogMessage += handler;
			m_protocol.SetLoggerHandler( handler );
			m_suzuki.SetLoggerHandler( handler );
		}

		string ReadConfig( string filePath )
		{
			if( File.Exists( filePath ) )
			{
				return File.ReadAllText( filePath );
			}
			return "";
		}

	}
}

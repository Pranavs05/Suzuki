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



namespace SuzukiLibrary
{

	public delegate void MessageForLogger( object sender, string message );


    public class Suzuki
    {
		event MessageForLogger		LogMessage;

		Protocol        m_protocol;
		Thread          m_receiver;

		// Suzuki algorithm
		Token           m_token;
		UInt64          m_seqNumber;

		Config.Configuration    m_configuration;

		// Suzuki Helpers
		string			ConfigPath { get; set; }


		public Suzuki()
		{
			m_protocol = new Protocol();
			m_receiver = null;

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

			}
		}


    }
}

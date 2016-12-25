using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Collections;
using System.Net;
using System.Net.Sockets;
using System.Collections.Concurrent;
using Newtonsoft.Json;
using System.IO;

namespace SuzukiLibrary
{

	public delegate void MessageForLogger( object sender, string message );


    public class Suzuki
    {
		public event MessageForLogger		LogMessage;

		BlockingCollection< SuzukiMessage > m_messageQueue;
		
		bool                                End;


		// Suzuki algorithm
		Token           m_token;
		UInt32          m_id;
		UInt64          m_seqNumber;

		Config.Configuration    m_configuration;

		// Suzuki Helpers
		string			ConfigPath { get; set; }


		public Suzuki()
		{
			End = false;
			m_messageQueue = new BlockingCollection< SuzukiMessage >();


			m_token = null;
			m_seqNumber = 0;

			m_configuration = null;
			ConfigPath = "SuzukiConfig.json";
		}


		public void		Init()
		{
			m_configuration = JsonConvert.DeserializeObject< Config.Configuration >( ReadConfig( ConfigPath ) );
		}



		string		ReadConfig( string filePath )
		{
			if( File.Exists( filePath ) )
			{
				return File.ReadAllText( filePath );
			}
			return "";
		}


		void		ShutDown()
		{
			End = true;
			m_messageQueue.CompleteAdding();
		}


		bool		WaitForConnections		( ConnectionInfo info )
		{
			TcpListener server = null;

			try
			{
				server = new TcpListener( info.Address, info.Port );
				server.Start();

				while( End )
				{
					TcpClient client = server.AcceptTcpClient();
					LogMessage( this, "Client accepted: " + client.Client.RemoteEndPoint.ToString() );

					Receiver receiver = new Receiver( client, m_messageQueue );
					Task receiverTask = new Task( ( obj ) =>
					{
						receiver = obj as Receiver;
						receiver.ProcessClient();
					}, receiver );

					receiverTask.Start();
				}

				return true;
			}
			catch( SocketException e )
			{
				LogMessage( this, e.ToString() );

				return false;
			}
			finally
			{
				server.Stop();
			}
		}

    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;


namespace SuzukiLibrary
{
	public class Protocol
	{
		BlockingCollection< SuzukiMessage > m_messageQueue;
		BlockingCollection< SuzukiMessage > m_sendQueue;

		bool            End;
		Thread          SenderThread;
		Thread          WaitingThread;
		ConnectionInfo  Info;

		TcpListener		server = null;

		// Events
		public event MessageForLogger       LogMessage;





		public Protocol()
		{
			End = false;
			MessageQueue = new BlockingCollection< SuzukiMessage >();
			SendQueue = new BlockingCollection< SuzukiMessage >();
			SenderThread = null;
			WaitingThread = null;

			Info = new ConnectionInfo();
			Info.Address = IPAddress.Parse( "127.0.0.1" );
			Info.Port = 3331;
		}


		public void Init( Config.Configuration config )
		{
			SenderThread = new Thread( Sender );
			WaitingThread = new Thread( WaitForConnections );

			Info.Port = config.Port;
			Info.Address = IPAddress.Parse( config.Address );

			SenderThread.Start();
			WaitingThread.Start();
		}


		public void ShutDown()
		{
			End = true;
			MessageQueue.CompleteAdding();
			SendQueue.CompleteAdding();

			SenderThread?.Join();
			server?.Stop();
			//WaitingThread?.Abort();
		}


		public void Send( string content, UInt16 port, string address )
		{
			SuzukiMessage msg = new SuzukiMessage( content, port, address );
			SendQueue.Add( msg );
		}


		void Sender()
		{
			while( !End )
			{
				try
				{
					SuzukiMessage msg = SendQueue.Take();

					Byte[] data = Encoding.ASCII.GetBytes( msg.Msg );

					TcpClient client = new TcpClient( msg.Address, msg.Port );

					NetworkStream stream = client.GetStream();
					stream.Write( data, 0, data.Length );

					stream.Close();
					client.Close();
				}
				catch( SocketException e )
				{
					LogMessage( this, e.ToString() );
				}
				catch( InvalidOperationException )
				{
					// SendQueue has ended.
				}
			}
		}

		void WaitForConnections()
		{		
			while( !End )
			{
				try
				{
					server = new TcpListener( Info.Address, Info.Port );
					server.Start();

					while( !End )
					{
						TcpClient client = server.AcceptTcpClient();
						//LogMessage( this, "Client accepted: " + client.Client.RemoteEndPoint.ToString() );

						Receiver receiver = new Receiver( client, MessageQueue );
						Task receiverTask = new Task( ( obj ) =>
					{
						receiver = obj as Receiver;
						receiver.ProcessClient();
					}, receiver );

						receiverTask.Start();
					}

				}
				catch( SocketException e )
				{
					LogMessage( this, e.ToString() );
				}
				finally
				{
					server.Stop();
				}
			}
		}


		public void SetLoggerHandler( MessageForLogger handler )
		{
			LogMessage += handler;
		}



		#region Properties

		public BlockingCollection<SuzukiMessage> MessageQueue
		{
			get
			{
				return m_messageQueue;
			}

			set
			{
				m_messageQueue = value;
			}
		}

		public BlockingCollection<SuzukiMessage> SendQueue
		{
			get
			{
				return m_sendQueue;
			}

			set
			{
				m_sendQueue = value;
			}
		} 
		#endregion
	}
}

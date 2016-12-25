using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Collections.Concurrent;


namespace SuzukiLibrary
{
	public class Receiver
	{
		TcpClient								Client;
		BlockingCollection< SuzukiMessage >     Queue;



		public Receiver( TcpClient client, BlockingCollection< SuzukiMessage > queue )
		{
			Client = client;
			Queue = queue;
		}


		public void		ProcessClient()
		{
			///https://msdn.microsoft.com/pl-pl/library/system.net.sockets.tcplistener(v=vs.110).aspx
			NetworkStream stream = Client.GetStream();

			int i;
			String data = "";
			Byte[] bytes = new Byte[ 2048 ];

			try
			{
				// Loop to receive all the data sent by the client.
				while( ( i = stream.Read( bytes, 0, bytes.Length ) ) != 0 )
				{
					// Translate data bytes to a ASCII string.
					data += Encoding.ASCII.GetString( bytes, 0, i );
				}

				Queue.Add( new SuzukiMessage( data ) );
			}
			catch( SocketException e )
			{
				Console.WriteLine( "SocketException: {0}", e );
			}
			finally
			{
				Client.Close();
			}
		}

	}
}

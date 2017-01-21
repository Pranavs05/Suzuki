using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Timers;


namespace SuzukiLibrary
{
	public delegate void WonElection();

	public class Election
	{
		event MessageForLogger          LogMessage;

		public SendBroadcastDelegate    SendBroadcast;
		public SendDelegate             Send;
		public WonElection              WonElection;


		Config.Configuration			m_configuration;
		Dictionary< UInt32, bool >		m_oks;

		Timer                           m_electionOkTimeout;

		// Debug
		public bool			NoElectionResponse { get; set;	}



		public Election()
		{
			m_electionOkTimeout = null;
			m_configuration = null;
		}

		public void Init( Config.Configuration config )
		{
			m_configuration = config;
			m_electionOkTimeout = new Timer( m_configuration.ElectionOkTimeout );
			m_electionOkTimeout.Elapsed += ElectionTimeoutElapsed;
		}


		public void		ElectionBroadcast		( Messages.ElectionBroadcast election )
		{
			if( election.senderId < m_configuration.NodeID )
			{
				if( m_oks == null )
				{
					StartElection();
				}
				else
				{
					LogMessage( this, "Two nodes started election at the same time" );
				}
			}
			else if( election.senderId > m_configuration.NodeID )
			{
				if( NoElectionResponse )
				{
					Messages.ElectionOk ok = new Messages.ElectionOk( m_configuration.NodeID );

					var senderDesc = m_configuration.FindNode( election.senderId );
					Send( ok, senderDesc.Port, senderDesc.NodeIP );
				}
				else
				{
					LogMessage( this, "ElectionOk intentionally not send. Check Menu -> Debug -> No election response" );
				}

				// Don't take part in election anymore.
				m_electionOkTimeout.Stop();
			}
			else
			{
				LogMessage( this, "Duplicate node with the same id in network" );
			}
		}

		public void		ElectionOk				( Messages.ElectionOk ok )
		{
			if( m_oks != null )
			{
				m_oks[ ok.senderId ] = true;
				CheckIfWon();
			}
			else
			{
				LogMessage( this, "Unexpected electionOK message from: [" + ok.senderId + "]" );
			}
		}


		public void		StartElection()
		{
			m_oks = new Dictionary< uint, bool >();
			foreach( var node in m_configuration.Nodes )
			{
				m_oks[ node.NodeID ] = false;
			}
			m_oks[ m_configuration.NodeID ] = true;

			Messages.ElectionBroadcast msg = new Messages.ElectionBroadcast( m_configuration.NodeID );
			SendBroadcast( msg );

			m_electionOkTimeout.Start();
		}

		public void		Clear()
		{
			m_oks = null;
		}

		private void CheckIfWon()
		{
			bool allOk = true;
			foreach( var ok in m_oks )
			{
				if( !ok.Value )
					allOk = false;
			}

			// Notify outside world
			if( allOk )
			{
				m_electionOkTimeout.Stop();
				WonElection();
			}
		}

		public void SetLoggerHandler( MessageForLogger handler )
		{
			LogMessage += handler;
		}

		public void		ElectionTimeoutElapsed( Object source, ElapsedEventArgs e )
		{
			LogMessage( this, "Election timeout elapsed." );

			WonElection();

			m_electionOkTimeout.Stop();
		}
	}
}

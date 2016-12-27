using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Net.Http;


namespace Client.Worker
{
	public class ResourceAccessor : UpdatableViewBase
	{
		Logging.Logger		mLoggerRef;
		ResourceConfig      mConfig;

		string              mContent;

		SuzukiLibrary.Suzuki    m_Suzuki;


		// Commands
		Commands.RelayCommand   SaveResourceConfigCommand;
		//Commands.RelayCommand   GetResourceCommand;
		//Commands.RelayCommand   SetResourceCommand;

		// Constants
		string cConfigPath = "ResourceConfig.xml";



		public ResourceAccessor( Logging.Logger logger )
		{
			mLoggerRef = logger;
			mContent = "";

			mConfig = Helpers.Serialization.Deserialize< ResourceConfig >( cConfigPath );
			if( mConfig == null )
				mConfig = new ResourceConfig();

			m_Suzuki = new SuzukiLibrary.Suzuki();

			SaveResourceConfigCommand = new Commands.RelayCommand( SaveConfig );
			GetResourceCommand = new Commands.RelayCommand( GetResource );
			SetResourceCommand = new Commands.RelayCommand( SetResource );
			StartSuzukiCommand = new Commands.RelayCommand( StartSuzuki );
		}

		private void GetResource( object param )
		{
			//m_Suzuki.AccessResource();
			HttpGet();
			//m_Suzuki.FreeResource();
		}

		private void SetResource( object param )
		{
			//m_Suzuki.AccessResource();
			HttpPost();
			//m_Suzuki.FreeResource();
		}

		private void StartSuzuki( object param )
		{
			m_Suzuki.SetLoggerHandler( LogMessage );
			m_Suzuki.Init();
		}

		public void ShutDown()
		{
			m_Suzuki.ShutDown();
		}

		private async void HttpPost()
		{
			m_Suzuki.AccessResource();
			// ... Use HttpClient.
			using( HttpClient client = new HttpClient() )
			{
				client.DefaultRequestHeaders.Accept.Add( new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue( "application/json" ) );

				StringContent content = new StringContent( Content, Encoding.UTF8, "application/json" );
				HttpResponseMessage response = await client.PostAsync( mConfig.ServerAddress, content );

				try
				{
					response.EnsureSuccessStatusCode();
					mLoggerRef.LogMessage( "Set resource [ " + mConfig.ServerAddress + " ]" );
				}
				catch( Exception )
				{
					mLoggerRef.LogMessage( "Error while setting resource [ " + mConfig.ServerAddress + " ]. Code: " + response.StatusCode );
				}
				finally
				{
					m_Suzuki.FreeResource();
				}
			}
		}

		private async void HttpGet()
		{
			m_Suzuki.AccessResource();
			// ... Target page.
			string page = mConfig.ServerAddress;

			// ... Use HttpClient.
			using( HttpClient client = new HttpClient() )
			using( HttpResponseMessage response = await client.GetAsync( page ) )
			using( HttpContent content = response.Content )
			{
				try
				{
					response.EnsureSuccessStatusCode();

					// ... Read the string.
					Content = await content.ReadAsStringAsync();
					mLoggerRef.LogMessage( "Get resource from [ " + mConfig.ServerAddress + " ]" );
				}
				catch( Exception )
				{
					mLoggerRef.LogMessage( "Error while getting resource [ " + mConfig.ServerAddress + " ]. Code: " + response.StatusCode );
				}
				finally
				{
					m_Suzuki.FreeResource();
				}

			}
		}

		private void SaveConfig( object parameter )
		{
			Helpers.Serialization.Serialize( cConfigPath, mConfig );
		}

		private void LogMessage( object sender, string msg )
		{
			mLoggerRef.LogMessage( msg );
		}

		#region MyRegion
		public ICommand SaveResourceConfig
		{
			get
			{
				return SaveResourceConfigCommand;
			}
		}

		public ICommand GetResourceCommand { get; set;  }

		public ICommand SetResourceCommand { get; set; }

		public ICommand StartSuzukiCommand { get; set; }

		public string Content
		{
			get
			{
				return mContent;
			}

			set
			{
				mContent = value;
				OnPropertyChanged( "Content" );
			}
		}
		#endregion
	}
}

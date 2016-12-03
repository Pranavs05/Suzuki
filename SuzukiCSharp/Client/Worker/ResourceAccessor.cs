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

			SaveResourceConfigCommand = new Commands.RelayCommand( SaveConfig );
			GetResourceCommand = new Commands.RelayCommand( GetResource );
			SetResourceCommand = new Commands.RelayCommand( SetResource );
		}

		private void GetResource( object param )
		{
			HttpGet();
		}

		private void SetResource( object param )
		{
			HttpPost();
		}

		private async void HttpPost()
		{
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
			}
		}

		private async void HttpGet()
		{
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

			}
		}

		private void SaveConfig( object parameter )
		{
			Helpers.Serialization.Serialize( cConfigPath, mConfig );
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

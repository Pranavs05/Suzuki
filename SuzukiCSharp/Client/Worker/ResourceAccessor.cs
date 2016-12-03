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
			
			mConfig = Helpers.Serialization.Deserialize< ResourceConfig >( cConfigPath );
			if( mConfig == null )
				mConfig = new ResourceConfig();

			SaveResourceConfigCommand = new Commands.RelayCommand( SaveConfig );
			GetResourceCommand = new Commands.RelayCommand( GetResource );
			SetResourceCommand = new Commands.RelayCommand( SetResource );
		}

		private void GetResource( object param )
		{
			DoWork();
		}

		private void SetResource( object param )
		{
			DoWork();
		}

		private async void DoWork()
		{
			// ... Target page.
			string page = mConfig.ServerAddress;

			// ... Use HttpClient.
			using( HttpClient client = new HttpClient() )
			using( HttpResponseMessage response = await client.GetAsync( page ) )
			using( HttpContent content = response.Content )
			{
				// ... Read the string.
				Content = await content.ReadAsStringAsync();

				mLoggerRef.LogMessage( "Resource [ " + mConfig.ServerAddress + " ] accessed." );
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

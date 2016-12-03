using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client.Logging
{
	public struct LoggerMessage
	{
		string		timeStamp;
		string		message;


		public LoggerMessage( DateTime time, string log )
		{
			timeStamp = time.ToString();
			message = log;
		}

		// Properties
		public string TimeStamp
		{
			get
			{
				return timeStamp;
			}
		}

		public string Message
		{
			get
			{
				return message;
			}
		}
	}



	public class Logger
	{
		ObservableCollection< LoggerMessage >      mLogs;

		public Logger()
		{
			mLogs = new ObservableCollection< LoggerMessage >();
		}



		public void LogMessage( string msg )
		{
			mLogs.Add( new LoggerMessage( DateTime.Now, msg ) );
		}



		#region Properties


		public ObservableCollection< LoggerMessage > Logs
		{
			get
			{
				return mLogs;
			}

			set
			{
				mLogs = value;
			}
		} 
		#endregion
	}
}

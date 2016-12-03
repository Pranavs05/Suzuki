using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client.Logging
{
	class LoggerView : UpdatableViewBase
	{
		Logger      mLoggerRef;
		

		LoggerView( Logger log )
		{
			mLoggerRef = log;
			DisplayName = "Log";
		}

		#region Properties


		public ObservableCollection<LoggerMessage> Logs
		{
			get
			{
				return mLoggerRef.Logs;
			}
		}
		#endregion
	}
}

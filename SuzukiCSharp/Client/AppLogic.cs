using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Client.Logging;

namespace Client
{
	public class AppLogic
	{
		Logger      mLogger;


		public AppLogic()
		{
			mLogger = new Logger();

		}


		#region Properties

		public Logger Logger
		{
			get
			{
				return mLogger;
			}

			set
			{
				mLogger = value;
			}
		} 
		#endregion
	}
}

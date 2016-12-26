using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Client.Logging;
using Client.Worker;
using System.Windows.Input;

namespace Client
{
	public class AppLogic
	{
		Logger				mLogger;
		ResourceAccessor	mAccessor;

		public AppLogic()
		{
			mLogger = new Logger();
			mAccessor = new ResourceAccessor( mLogger );
		}

		public void ShutDown()
		{
			mAccessor.ShutDown();
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

		public ResourceAccessor Accessor
		{
			get
			{
				return mAccessor;
			}

			set
			{
				mAccessor = value;
			}
		}

		
		#endregion
	}
}

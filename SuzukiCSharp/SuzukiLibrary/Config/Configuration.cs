using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;



namespace SuzukiLibrary.Config
{
	public class Configuration
	{
		public UInt32       NodeID;
		public UInt16       Port;

		public Collection< NodeDescriptor >    Nodes;

		// Resources


		#region Functions

		public Configuration()
		{
			Nodes = new Collection<NodeDescriptor>();
		}


		#endregion
	}
}

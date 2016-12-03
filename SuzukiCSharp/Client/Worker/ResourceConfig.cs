using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.Threading.Tasks;


namespace Client.Worker
{
	public class ResourceConfig
	{
		public string		ServerAddress { get; set; }
		public UInt16		ServerPort { get; set; }


		public ResourceConfig()
		{
			// Default values
			ServerAddress = "https://evening-peak-26255.herokuapp.com/";
			ServerPort = 80;
		}
	}
}

using System;
using System.Xml.Serialization;
using System.IO;


namespace Client.Helpers
{
	public class Serialization
	{

		public static bool Serialize	( string filePath, object obj )
		{
			try
			{
				XmlSerializer ser = new XmlSerializer( obj.GetType() );

				using( FileStream writer = new FileStream( filePath, FileMode.OpenOrCreate ) )
				{
					ser.Serialize( writer, obj );
				}
			} catch( Exception e )
			{
				Console.WriteLine( e.ToString() );
				return false;
			}

			return true;
		}

		public static ObjectType Deserialize< ObjectType > ( string filePath )
		{
			try
			{
				XmlSerializer deser = new XmlSerializer( typeof( ObjectType ) );

				using( FileStream reader = new FileStream( filePath, FileMode.Open ) )
				{
					object obj = deser.Deserialize( reader );
					return (ObjectType)obj;
				}

			} catch( Exception e )
			{
				Console.WriteLine( e.ToString() );
				return default( ObjectType );
			}
		}
	}
}

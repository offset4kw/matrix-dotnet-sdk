using System;
using MatrixSDK.AppService;
namespace EchoAppservice
{
	class MainClass
	{
		public static void Main (string[] args)
		{
			MatrixAppservice appservice = new MatrixAppservice ();
			appservice.Run ();
		}
	}
}

using System;
using MatrixSDK;
namespace ExampleClient
{
	class MainClass
	{
		public static void Main (string[] args)
		{
			MatrixClient client = new MatrixClient ("https://half-shot.uk");
			client.LoginWithPassword ("", "");
			MatrixUser user = client.GetUser ("@Half-Shot:half-shot.uk");
		}
	}
}

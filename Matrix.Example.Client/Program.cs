using System;
using System.IO;
using System.Linq;
using Matrix.Client;
using Matrix.Structures;

namespace Matrix.Example.Client
{
    class Program
    {
        static void Main(string[] args)
        {
            string homeserverUrl = "http://localhost:8008";
            MatrixClient client;
            if (File.Exists("/tmp/mx_access"))
            {
                string[] tokens = File.ReadAllText("/tmp/mx_access").Split("$");
                client = new MatrixClient(homeserverUrl);
                client.UseExistingToken(tokens[1], tokens[0]);
            }
            else
            {
                string username = "will";
                string password = "password";
                client = new MatrixClient(homeserverUrl);
                MatrixLoginResponse login = client.LoginWithPassword(username, password);
                File.WriteAllText("/tmp/mx_access", $"{login.access_token}${login.user_id}");
            }
            Console.WriteLine("Starting sync");
            client.StartSync();
            Console.WriteLine("Finished initial sync");
            foreach (var room in client.GetAllRooms())
            {
                Console.WriteLine($"Found room: {room.ID}");
            }
        }
    }
}

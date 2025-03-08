using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Data.SqlClient;
using Dapper;

namespace ServerExample2
{
    public class Server
    {
        private TcpListener _listener;
        private Database database;
        public Dictionary<TcpClient, string> LoggedInUsers = new Dictionary<TcpClient, string>();

        public void Start()
        {
            try
            {
                database = new Database(this);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to initialize database: {ex.Message}");
                Console.WriteLine("Press enter to exit.");
                Console.ReadLine();
                Environment.Exit(0);
            }

            // Listen on any IP address on port 5000
            _listener = new TcpListener(IPAddress.Any, 5000);
            _listener.Start();
            Console.WriteLine("Server started on port 5000...");

            AcceptClientsAsync();
        }

        private async void AcceptClientsAsync()
        {
            while (true)
            {
                // Accept a new client connection
                TcpClient client = await _listener.AcceptTcpClientAsync();
                Console.WriteLine($"New client connected: {client.GetHashCode()}");
                ProcessClientAsync(client);
            }
        }

        private async void ProcessClientAsync(TcpClient client)
        {
            NetworkStream stream = client.GetStream();
            byte[] buffer = new byte[1024];

            // Continue to process messages while the client is connected
            while (client.Connected)
            {
                int bytesRead = 0;
                try
                {
                    bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error reading from client: " + ex.Message);
                    break;
                }

                // If zero bytes are read, the client disconnected.
                if (bytesRead == 0)
                {
                    break;
                }

                string message = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();
                string args;
                string response = "";

                // Handle create account command
                if (message.StartsWith("create"))
                {
                    args = message.Substring(6);
                    response = await database.CreateAccount(client, args);
                }
                // Add more commands here as needed
                else
                {
                    response = "Unknown Command";
                }

                if (response != "")
                {
                    await Client.SendMessage(client, response);
                }
            }

            // Clean up when client disconnects
            try
            {
                LoggedInUsers.Remove(client);
                Console.WriteLine($"Client disconnected: {client.GetHashCode()}");
            }
            catch { }
            client.Close();
        }
    }

    class Program
    {
        static void Main()
        {
            Server server = new Server();
            server.Start();

            Console.WriteLine("Press ENTER to exit.");
            Console.ReadLine();
        }
    }
}
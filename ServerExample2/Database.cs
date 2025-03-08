using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Dapper;

namespace ServerExample2
{
    public class Database
    {
        private string connectionString = $"Server=localhost\\MSSQLSERVER01;Database=YourDatabase;Trusted_Connection=True;MultipleActiveResultSets=True";
        private IDbConnection DB;
        private Server Server;

        public Database(Server server)
        {
            DB = new SqlConnection(connectionString);
            const string checkExistsQuery = @"
            SELECT COUNT(*) 
            FROM INFORMATION_SCHEMA.TABLES 
            WHERE TABLE_NAME = 'Users'";

            const string createUserTableQuery = @"
            CREATE TABLE Users (
                Username NVARCHAR(50) NOT NULL PRIMARY KEY,
                Hash NVARCHAR(MAX) NOT NULL,
                Salt NVARCHAR(MAX) NOT NULL
            );";

            int tableCount = DB.QuerySingleOrDefault<int>(checkExistsQuery);
            if (tableCount == 0)
            {
                DB.Execute(createUserTableQuery);
            }
            Server = server;
        }

        public async Task<string> CreateAccount(TcpClient client, string message)
        {
            string[] args = message.Split(':');
            if (args.Length < 3)
            {
                return "Invalid message format.";
            }

            string username = args[0];
            string hash = args[1];
            string salt = args[2];

            string insertQuery = "INSERT INTO Users (Username, Hash, Salt) VALUES (@Username, @Hash, @Salt)";
            string checkQuery = "SELECT Username FROM Users WHERE Username = @Username";

            try
            {
                var existingUser = await DB.QuerySingleOrDefaultAsync<string>(checkQuery, new { Username = username });
                if (existingUser != null)
                {
                    return "User Exists";
                }

                int rowsAffected = await DB.ExecuteAsync(insertQuery, new { Username = username, Hash = hash, Salt = salt });
                Console.WriteLine($"{rowsAffected} row(s) inserted.");
                Server.LoggedInUsers.Add(client, username);
                return "Success";
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error: {e.Message}");
                return $"Error: {e.Message}";
            }
        }
    }
}

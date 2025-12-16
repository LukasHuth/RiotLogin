using System.Data.SQLite;

namespace LeagueLogin;

public class AccountManager
{
    public class AccountData
    {
        public string username;
        public string password;
    }
    private static readonly string dbPath = "Data Source=database.db;Version=3;";

    public AccountManager()
    {
        Console.WriteLine("Creating Table...");
        using (var connection = new SQLiteConnection(dbPath))
        {
            connection.Open();
            Console.WriteLine("Creating Table...");
            string createQuery = @"CREATE TABLE IF NOT EXISTS accounts (alias TEXT NOT NULL PRIMARY KEY, username TEXT NOT NULL, password TEXT);";
            using (var command = new SQLiteCommand(createQuery, connection))
            {
                int updated = command.ExecuteNonQuery();
                Console.WriteLine("Table created " + updated);
            }
        }
    }

    public List<String> getEntries()
    {
        List<String> entries = new List<string>();
        Console.WriteLine("Retrieving accounts...");
        using var  connection = new SQLiteConnection(dbPath);
        connection.Open();
        string query = @"SELECT alias FROM accounts;";
        using var command = new SQLiteCommand(query, connection);
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            entries.Add(reader.GetString(0));
        }
        return entries;
    }
    
    public void removeAccount(String alias)
    {
        using (var connection = new SQLiteConnection(dbPath))
        {
            connection.Open();
            SQLiteTransaction transaction = connection.BeginTransaction();
            string deleteQuery = @"DELETE FROM accounts WHERE alias = @alias;";
            using (var command = new SQLiteCommand(deleteQuery, connection, transaction))
            {
                command.Parameters.AddWithValue("@alias", alias);
                command.ExecuteNonQuery();
            }
            transaction.Commit();
        }
    }

    public void addAccount(String alias, String username, String password)
    {
        using (var connection = new SQLiteConnection(dbPath))
        {
            connection.Open();
            SQLiteTransaction transaction = connection.BeginTransaction();
            string insert_query = @"INSERT INTO accounts (alias, username, password) VALUES (@alias, @username, @password);";
            using (var command = new SQLiteCommand(insert_query, connection, transaction))
            {
                command.Parameters.AddWithValue("@alias", alias);
                command.Parameters.AddWithValue("@username", username);
                command.Parameters.AddWithValue("@password", password);
                command.ExecuteNonQuery();
            }
            transaction.Commit();
        }
    }
    public AccountData? getAccount(String alias)
    {
        using (var connection = new SQLiteConnection(dbPath))
        {
            connection.Open();
            string query = @"SELECT username, password FROM accounts WHERE alias = @alias LIMIT 1;";
            using (var command = new SQLiteCommand(query, connection))
            {
                command.Parameters.AddWithValue("@alias", alias);
                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        string username = reader["username"].ToString() ?? string.Empty;
                        string password = reader["password"].ToString() ?? string.Empty;
                        return new AccountData { username = username, password = password };
                    }
                }
            }
        }

        return null;
    }
}
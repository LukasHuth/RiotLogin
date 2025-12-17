using System.Data.SQLite;
using System.Security.Cryptography;
using System.Text;
using LeagueLogin.AccountManager.Conversion;

namespace LeagueLogin.AccountManager;

public class AccountManager
{
    public class AccountData
    {
        public string username;
        public string password;
    }
    private static readonly string dbPath = "Data Source=database.db;Version=3;";
    private static readonly long versionNumber = 1;

    public AccountManager()
    {
        Console.WriteLine("Creating Table...");
        initialize();
    }

    private static void initialize()
    {
        using var connection = new SQLiteConnection(dbPath);
        connection.Open();
        if (hasAccountsTable(connection))
        {
            alterTable(connection);
        }
        else
        {
            createTable(connection);
        }
    }

    private static long getVersionNumber(SQLiteConnection connection)
    {
        string versionQuery = @"PRAGMA user_version;";
        using var versionQueryCommand = new SQLiteCommand(versionQuery, connection);
        return (long)(versionQueryCommand.ExecuteScalar() ?? 0);
    }
    private static void alterTable(SQLiteConnection connection)
    {
        long currentVersion = getVersionNumber(connection);
        while (currentVersion < versionNumber)
        {
            switch (currentVersion)
            {
                case 0:
                    V0toV1.convert(connection);
                    break;
                default:
                    throw new Exception("Unexpected version number: " + currentVersion);
            }
            currentVersion = getVersionNumber(connection);
        }
    }
    private static void createTable(SQLiteConnection connection)
    {
        string createQuery = @"CREATE TABLE accounts (alias TEXT NOT NULL PRIMARY KEY, username TEXT NOT NULL, password BLOB NOT NULL, nonce BLOB NOT NULL, salt BLOB NOT NULL);";
        using (var command = new SQLiteCommand(createQuery, connection))
        {
            command.ExecuteNonQuery();
            Console.WriteLine("Table created ");
        }
        setVersion(connection, versionNumber);
    }

    internal static void setVersion(SQLiteConnection connection, long versionNumber)
    {
        var transaction = connection.BeginTransaction();
        setVersion(connection, transaction, versionNumber);
        transaction.Commit();
    }

    internal static void setVersion(SQLiteConnection connection, SQLiteTransaction transaction, long versionNumber)
    {
        string versionQuery = $"PRAGMA user_version = {versionNumber};";
        using var command = new SQLiteCommand(versionQuery, connection, transaction);
        command.ExecuteNonQuery();
    }

    private static bool hasAccountsTable(SQLiteConnection connection)
    {
        using var cmd = new SQLiteCommand("SELECT name FROM sqlite_master WHERE type='table' AND name='accounts';", connection);
        return cmd.ExecuteScalar() != null;
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
            string insert_query = @"INSERT INTO accounts (alias, username, password, nonce, salt) VALUES (@alias, @username, @password, @nonce, @salt);";
            using (var command = new SQLiteCommand(insert_query, connection, transaction))
            {
                (byte[] ciphertext, byte[] nonce, byte[] salt) = encrypt(App.master_password??"", password);
                command.Parameters.AddWithValue("@alias", alias);
                command.Parameters.AddWithValue("@username", username);
                command.Parameters.AddWithValue("@password", ciphertext);
                command.Parameters.AddWithValue("@nonce", nonce);
                command.Parameters.AddWithValue("@salt", salt);
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
            string query = @"SELECT username, password, nonce, salt FROM accounts WHERE alias = @alias LIMIT 1;";
            using (var command = new SQLiteCommand(query, connection))
            {
                command.Parameters.AddWithValue("@alias", alias);
                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        string username = reader["username"].ToString() ?? string.Empty;
                        byte[] passwordCipher = (byte[])reader["password"];
                        byte[] nonceCipher = (byte[])reader["nonce"];
                        byte[] saltCipher = (byte[])reader["salt"];
                        string password = decrypt(App.master_password??"", passwordCipher, nonceCipher, saltCipher);
                        return new AccountData { username = username, password = password };
                    }
                }
            }
        }

        return null;
    }

    private static byte[] DeriveKey(string masterPassword, byte[] salt)
    {
        using var pbkdf2 = new Rfc2898DeriveBytes(
            masterPassword, salt, 100_000, HashAlgorithmName.SHA256);
        return pbkdf2.GetBytes(32);
    }
    internal static (byte[] Ciphertext, byte[] Nonce, byte[] Salt) encrypt(string masterPassword, string password)
    {
        byte[] salt = RandomNumberGenerator.GetBytes(16);
        byte[] key = DeriveKey(masterPassword, salt);

        const int TagSize = 16;
        using var aes = new AesGcm(key, TagSize);
        byte[] nonce = RandomNumberGenerator.GetBytes(12);
        byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
        byte[] ciphertext = new byte[passwordBytes.Length];
        byte[] tag = new byte[TagSize];
        
        aes.Encrypt(nonce, passwordBytes, ciphertext, tag);
        
        byte[] combined = new byte[ciphertext.Length + tag.Length];
        Buffer.BlockCopy(ciphertext, 0, combined, 0, ciphertext.Length);
        Buffer.BlockCopy(tag, 0, combined, ciphertext.Length, tag.Length);
        

        return (combined, nonce, salt);
    }

    internal static string decrypt(string masterPassword, byte[] ciphertextWithTag, byte[] nonce, byte[] salt)
    {
        Console.WriteLine(ciphertextWithTag.Length);
        byte[] key = DeriveKey(masterPassword, salt);

        using var aes = new AesGcm(key, 16);
        byte[] ciphertext = new byte[ciphertextWithTag.Length - 16];
        byte[] tag = new byte[16];
        
        Buffer.BlockCopy(ciphertextWithTag, 0, ciphertext, 0, ciphertext.Length);
        Buffer.BlockCopy(ciphertextWithTag, ciphertext.Length, tag, 0, tag.Length);

        byte[] plaintextBytes = new byte[ciphertext.Length];
        aes.Decrypt(nonce, ciphertext, tag, plaintextBytes);
        
        return Encoding.UTF8.GetString(plaintextBytes);
    }

    public void changeMasterPassword(string newMasterPassword)
    {
        using var connection = new SQLiteConnection(dbPath);
        connection.Open();
        ChangeMasterPassword.changeMasterPassword(newMasterPassword, connection);
    }
}
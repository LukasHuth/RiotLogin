using System.Data.SQLite;

namespace LeagueLogin.AccountManager.Conversion;

public class V0toV1
{
    public static void convert(SQLiteConnection connection)
    {
        var transaction = connection.BeginTransaction();
        string createMigrationQuery =
            "CREATE TABLE accounts_migration (alias TEXT NOT NULL PRIMARY KEY, username TEXT NOT NULL, password BLOB NOT NULL, nonce BLOB NOT NULL, salt BLOB NOT NULL);";
        using (var createMigrationCommand =
               new SQLiteCommand(createMigrationQuery, connection, transaction))
        {
            createMigrationCommand.ExecuteNonQuery();
        }
        string readMigrationQuery = "SELECT alias, username, password FROM accounts;";
        using (var readMigrationCommand = new SQLiteCommand(readMigrationQuery, connection, transaction))
        {
            var migrationReader = readMigrationCommand.ExecuteReader();
            while (migrationReader.Read())
            {
                string alias = migrationReader["alias"].ToString() ?? string.Empty;
                string username = migrationReader["username"].ToString() ?? string.Empty;
                string old_password = migrationReader["password"].ToString() ?? string.Empty;
                (byte[] password, byte[] nonce, byte[] salt) = AccountManager.encrypt(App.master_password ?? "", old_password);
                string insertUpdatedVersion =
                    "INSERT INTO accounts_migration (alias, username, password, nonce, salt) VALUES (@alias, @username, @password, @nonce, @salt);";
                using (var insertUpdatedVersionCommand =
                       new SQLiteCommand(insertUpdatedVersion, connection, transaction))
                {
                    insertUpdatedVersionCommand.Parameters.AddWithValue("@alias", alias);
                    insertUpdatedVersionCommand.Parameters.AddWithValue("@username", username);
                    insertUpdatedVersionCommand.Parameters.AddWithValue("@password", password);
                    insertUpdatedVersionCommand.Parameters.AddWithValue("@nonce", nonce);
                    insertUpdatedVersionCommand.Parameters.AddWithValue("@salt", salt);
                    insertUpdatedVersionCommand.ExecuteNonQuery();
                }
            }
        }
        string dropOldDatabaseQuery = "DROP TABLE IF EXISTS accounts;";
        using (var dropOldDatabaseCommand =
               new SQLiteCommand(dropOldDatabaseQuery, connection, transaction))
        {
            dropOldDatabaseCommand.ExecuteNonQuery();
        }
        string renameMigrationQuery = "ALTER TABLE accounts_migration RENAME TO accounts;";
        using (var renameMigrationCommand =
               new SQLiteCommand(renameMigrationQuery, connection, transaction))
        {
            renameMigrationCommand.ExecuteNonQuery();
        }
        AccountManager.setVersion(connection, transaction, 1);
        transaction.Commit();
    }
}
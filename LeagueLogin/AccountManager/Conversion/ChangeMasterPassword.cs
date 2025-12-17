using System.Data.SQLite;

namespace LeagueLogin.AccountManager.Conversion;

public class ChangeMasterPassword
{
    public static void changeMasterPassword(string newMasterPassword, SQLiteConnection connection)
    {
        var transaction = connection.BeginTransaction();
        string selectQuery = "SELECT alias, password, nonce, salt FROM accounts";
        using var selectQueryCommand = new SQLiteCommand(selectQuery, connection);
        using var reader = selectQueryCommand.ExecuteReader();
        while (reader.Read())
        {
            string alias = reader["alias"].ToString() ?? String.Empty;
            byte[] old_password = (byte[])reader["password"];
            byte[] old_nonce = (byte[])reader["nonce"];
            byte[] old_salt = (byte[])reader["salt"];
            string decrypted_password = AccountManager.decrypt(App.master_password ?? "", old_password, old_nonce, old_salt);
            (byte[] password, byte[] nonce, byte[] salt) =
                AccountManager.encrypt(newMasterPassword, decrypted_password);
            string updateQuery = "UPDATE accounts SET password = @password, nonce = @nonce, salt = @salt WHERE alias = @alias";
            using var updateQueryCommand = new SQLiteCommand(updateQuery, connection);
            updateQueryCommand.Parameters.AddWithValue("@alias", alias);
            updateQueryCommand.Parameters.AddWithValue("@password", password);
            updateQueryCommand.Parameters.AddWithValue("@nonce", nonce);
            updateQueryCommand.Parameters.AddWithValue("@salt", salt);
            updateQueryCommand.ExecuteNonQuery();
        }
        transaction.Commit();
    }
}
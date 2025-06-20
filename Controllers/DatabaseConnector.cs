
using System.Data.SQLite;

public static class DatabaseConnector
{
    private static SQLiteConnection conn = new();
    public static SQLiteConnection Db()
    {
        if (conn == null || conn.State == System.Data.ConnectionState.Closed)
        {
            conn = new SQLiteConnection("Data Source=C:/Users/Mark/Documents/adatbazis_alkalmazasok_keszitese/Share/database/UserDB.sqlite3");
            conn.Open();
        }
        return conn;
    }

    // Method to create and return a new SQLiteConnection instance
    public static SQLiteConnection CreateNewConnection()
    {
        var conn = new SQLiteConnection("Data Source=C:/Users/Mark/Documents/adatbazis_alkalmazasok_keszitese/Share/database/UserDB.sqlite3");
        conn.Open();
        return conn;
    }
}
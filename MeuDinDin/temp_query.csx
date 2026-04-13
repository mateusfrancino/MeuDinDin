using Microsoft.Data.Sqlite;
var dbPath = @"C:\Users\mateus.francino\source\repos\MeuDinDin\MeuDinDin\App_Data\meudindin.db";
using var conn = new SqliteConnection($"Data Source={dbPath}");
conn.Open();
using var cmd = conn.CreateCommand();
cmd.CommandText = "select name from sqlite_master where type='table' order by name;";
using var reader = cmd.ExecuteReader();
while (reader.Read()) Console.WriteLine(reader.GetString(0));

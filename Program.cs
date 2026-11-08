using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
var app = builder.Build();

Baza.Inicijalizuj();
app.MapControllers();
app.Run("http://localhost:5000");

public static class Baza
{
    public const string ConnString = "Data Source=vvs.db";

    public static void Inicijalizuj()
    {
        if (File.Exists("vvs.db")) File.Delete("vvs.db");
        using var conn = new SqliteConnection(ConnString);
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE Users (Id INTEGER PRIMARY KEY, Username TEXT, Password TEXT);
            CREATE TABLE Proizvodi (Id INTEGER PRIMARY KEY, Naziv TEXT, Kategorija TEXT, Cijena REAL);
            INSERT INTO Users (Username, Password) VALUES ('admin', 'admin123'), ('amra', 'sifra2024');
            INSERT INTO Proizvodi (Naziv, Kategorija, Cijena) VALUES
                ('Stolica', 'Namjestaj', 49.9), ('Sto', 'Namjestaj', 120), ('Lampa', 'Rasvjeta', 25);";
        cmd.ExecuteNonQuery();
    }
}

[ApiController]
[Route("api")]
public class ProdavnicaController : ControllerBase
{

    [HttpPost("login")]
    public IActionResult Login([FromForm] string username, [FromForm] string password)
    {
        using var conn = new SqliteConnection(Baza.ConnString);
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Username FROM Users WHERE Username = '" + username +
                          "' AND Password = '" + password + "'";
        try
        {
            var user = cmd.ExecuteScalar();
            return Ok(user != null ? $"Prijava uspješna. Dobrodošli, {user}" : "Pogrešni podaci");
        }
        catch (SqliteException ex)
        {
            return StatusCode(500, ex.Message);  
        }
    }

    [HttpGet("proizvodi")]
    public IActionResult Proizvodi([FromQuery] string naziv)
    {
        using var conn = new SqliteConnection(Baza.ConnString);
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = $"SELECT * FROM Proizvodi WHERE Naziv = '{naziv}'";
        try
        {
            return Ok(Citaj(cmd));
        }
        catch (SqliteException ex)
        {
            return StatusCode(500, ex.Message);
        }
    }

    [HttpGet("kategorija")]
    public IActionResult Kategorija([FromQuery] string k)
    {
        using var conn = new SqliteConnection(Baza.ConnString);
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM Proizvodi WHERE Kategorija = @kat";
        cmd.Parameters.AddWithValue("@kat", k);
        return Ok(Citaj(cmd));
    }

    private static List<Dictionary<string, object>> Citaj(SqliteCommand cmd)
    {
        var rezultat = new List<Dictionary<string, object>>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            var red = new Dictionary<string, object>();
            for (int i = 0; i < reader.FieldCount; i++) red[reader.GetName(i)] = reader.GetValue(i);
            rezultat.Add(red);
        }
        return rezultat;
    }
}

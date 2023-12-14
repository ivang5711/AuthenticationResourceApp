using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.Sqlite;

namespace AuthFormApp.Pages;

[Authorize]
public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;

    public int MyProperty { get; set; } = 777;

    public IndexModel(ILogger<IndexModel> logger)
    {
        _logger = logger;
    }

    public void OnGet()
    {
        TestDatabase();



    }

    private static void TestDatabase()
    {
        SqliteConnection con = new("DataSource=C:\\Users\\Smith\\source\\repos\\pet_projects\\AuthFormApp\\app.db");
        con.Open();
        using var cmd = con.CreateCommand();
        cmd.CommandText = "SELECT * FROM AspNetUsers;";
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            reader.IsDBNull(0);
            Console.WriteLine($"guid: {reader.GetString(0)} email: {reader.GetString(3)} " +
                $"LockoutEnabled: {reader.GetInt32(5)}");
        }

        using var cmd2 = con.CreateCommand();
        cmd2.CommandText = $"UPDATE AspNetUsers SET LockoutEnd = '{DateTime.MaxValue}' WHERE Email = 'kar@kar.kar';";
        cmd2.ExecuteNonQuery();
        con.Close();

    }
}
using Microsoft.AspNetCore.Mvc;
using System;
using System.Data.SQLite;

[ApiController]
[Route("[controller]/[action]")]
public class UserController : Controller
{
    [HttpPost]
    public IActionResult Create([FromForm] string username, [FromForm] string password)
    {
        string salt = PasswordManager.GenerateSalt();
        string hashedPassword = PasswordManager.GeneratePasswordHash(password, salt);

        SQLiteConnection connection = DatabaseConnector.Db();
        string insertSql = "INSERT INTO User (UserName, PasswordHash, PasswordSalt, IsAdmin) VALUES (@UserName, @PasswordHash, @PasswordSalt, 0)";

        try
        {
            using (SQLiteCommand cmd = new SQLiteCommand(insertSql, connection))
            {
                cmd.Parameters.AddWithValue("@UserName", username);
                cmd.Parameters.AddWithValue("@PasswordHash", hashedPassword);
                cmd.Parameters.AddWithValue("@PasswordSalt", salt);
                cmd.ExecuteNonQuery();
            }
            return Ok("User created successfully");
        }
        catch (Exception ex)
        {
            return BadRequest("Error: " + ex.Message);
        }
    }

    [HttpPost]
    public IActionResult Login([FromForm] string username, [FromForm] string password)
    {
        SQLiteConnection connection = DatabaseConnector.Db();
        string selectSql = "SELECT UserID, PasswordHash, PasswordSalt FROM User WHERE UserName = @UserName";

        using (SQLiteCommand cmd = new SQLiteCommand(selectSql, connection))
        {
            cmd.Parameters.AddWithValue("@UserName", username);
            using (SQLiteDataReader reader = cmd.ExecuteReader())
            {
                if (reader.Read())
                {
                    string? storedPasswordHash = reader["PasswordHash"].ToString();
                    string? storedSalt = reader["PasswordSalt"].ToString();

                    if (!string.IsNullOrEmpty(storedPasswordHash) && !string.IsNullOrEmpty(storedSalt) &&
                        PasswordManager.Verify(password, storedSalt, storedPasswordHash))
                    {
                        Int64 userID = Convert.ToInt64(reader["UserID"]);
                        string sessionCookie = SessionManager.CreateSession(userID);

                        Response.Cookies.Append("id", sessionCookie);
                        return Ok("Login successful. Session ID: " + sessionCookie);
                    }
                }
            }
        }
        return Unauthorized("Invalid username or password");
    }

    [HttpPost]
    public IActionResult Logout()
    {
        var sessionId = Request.Cookies["id"];
        if (string.IsNullOrEmpty(sessionId))
        {
            return Unauthorized();
        }

        SessionManager.InvalidateSession(sessionId);
        Response.Cookies.Delete("id");
        return Ok("Logout successful");
    }

    static public bool IsLoggedIn(string sessionCookie)
    {
        Int64 userID = SessionManager.GetUserID(sessionCookie);
        return userID != -1;
    }

    [HttpGet]
    public IActionResult GetUser()
    {
        var sessionId = Request.Cookies["id"];
        if (string.IsNullOrEmpty(sessionId))
        {
            return new UnauthorizedResult();
        }
        Int64 userID = SessionManager.GetUserID(sessionId);
        if (userID == -1)
        {
            return new UnauthorizedResult();
        }
        return Json(userID);
    }

    [HttpGet]
    public IActionResult CheckSession()
    {
        var sessionId = Request.Cookies["id"];
        if (string.IsNullOrEmpty(sessionId))
        {
            return Json(new { userID = -1 });
        }
        Int64 userID = SessionManager.GetUserID(sessionId);
        return Json(new { userID });
    }
}
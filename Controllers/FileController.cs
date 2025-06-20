using System;
using System.Collections.Generic;
using System.IO;
using System.Data.SQLite;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

[ApiController]
[Route("[controller]/[action]")]
public class FileController : Controller
{
    private static string fileUploadPath = "C://Users//Mark//Documents//adatbazis_alkalmazasok_keszitese//Share";

    private int GetUserIDFromSession()
    {
        var sessionId = Request.Cookies["id"];
        if (string.IsNullOrEmpty(sessionId))
        {
            return -1;
        }

        // Convert long to int within this method
        long userID = SessionManager.GetUserID(sessionId);
        return (int)userID; // Convert long to int
    }

    private bool IsAdmin(int userID)
    {
        using (var connection = DatabaseConnector.CreateNewConnection())
        {
            string selectSql = "SELECT IsAdmin FROM User WHERE UserID = @UserID";
            using (var cmd = new SQLiteCommand(selectSql, connection))
            {
                cmd.Parameters.AddWithValue("@UserID", userID);
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return reader.GetInt32(0) == 1; // 1 means admin
                    }
                }
            }
        }
        return false;
    }

    // Upload a file
    [HttpPost]
    public IActionResult UploadFile(IFormFile file)
    {
        var userID = GetUserIDFromSession();
        if (userID == -1)
        {
            return Unauthorized("User not logged in.");
        }
        if (file == null || file.Length == 0)
        {
            return BadRequest("No file uploaded.");
        }

        using (var connection = DatabaseConnector.CreateNewConnection())
        {
            string fileName = $"{Path.GetFileName(file.FileName)}";
            string filePath = Path.Combine(fileUploadPath, fileName);

            try
            {
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    file.CopyTo(stream);
                }

                string insertSql = "INSERT INTO File (UserID, NameFile, FilePath, UploadDate) VALUES (@UserID, @NameFile, @FilePath, @UploadDate)";
                using (var cmd = new SQLiteCommand(insertSql, connection))
                {
                    cmd.Parameters.AddWithValue("@UserID", userID);
                    cmd.Parameters.AddWithValue("@NameFile", fileName);
                    cmd.Parameters.AddWithValue("@FilePath", filePath);
                    cmd.Parameters.AddWithValue("@UploadDate", DateTimeOffset.UtcNow.ToUnixTimeSeconds());
                    cmd.ExecuteNonQuery();
                }

                return Ok("File uploaded successfully!");
            }
            catch (Exception ex)
            {
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
                return StatusCode(500, "File upload failed: " + ex.Message);
            }
        }
    }

    // Delete a file
    [HttpPost]
    public IActionResult DeleteFile([FromForm] int fileID)
    {
        var userID = GetUserIDFromSession();
        bool isAdmin = IsAdmin(userID);

        using (var connection = DatabaseConnector.CreateNewConnection())
        {
            string selectSql = "SELECT FilePath FROM File WHERE FileID = @FileID" +
                               (isAdmin ? "" : " AND UserID = @UserID");
            string? filePath = null;

            using (var cmd = new SQLiteCommand(selectSql, connection))
            {
                cmd.Parameters.AddWithValue("@FileID", fileID);
                if (!isAdmin)
                {
                    cmd.Parameters.AddWithValue("@UserID", userID);
                }

                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        filePath = reader.GetString(0);
                    }
                }
            }

            if (!string.IsNullOrEmpty(filePath))
            {
                string deleteSql = "DELETE FROM File WHERE FileID = @FileID" +
                                   (isAdmin ? "" : " AND UserID = @UserID");

                using (var cmd = new SQLiteCommand(deleteSql, connection))
                {
                    cmd.Parameters.AddWithValue("@FileID", fileID);
                    if (!isAdmin)
                    {
                        cmd.Parameters.AddWithValue("@UserID", userID);
                    }
                    cmd.ExecuteNonQuery();
                }

                // Delete the file from the server
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }

                return Ok("File deleted successfully!");
            }

            return NotFound("File not found or permission denied.");
        }
    }

    // List user files
    [HttpGet]
    public IActionResult ListUserFiles()
    {
        var userID = GetUserIDFromSession();
        bool isAdmin = IsAdmin(userID);

        var files = new List<FileRecord>();
        using (SQLiteConnection connection = DatabaseConnector.CreateNewConnection())
        {
            string selectSql = @"
                SELECT f.FileID, f.UserID, f.NameFile, f.FilePath, f.UploadDate, u.UserName
                FROM File f
                LEFT JOIN User u ON f.UserID = u.UserID
                WHERE @UserID = -1 OR f.UserID = @UserID";

            if (isAdmin)
            {
                selectSql = @"
                    SELECT f.FileID, f.UserID, f.NameFile, f.FilePath, f.UploadDate, u.UserName
                    FROM File f
                    LEFT JOIN User u ON f.UserID = u.UserID";
            }

            using (SQLiteCommand cmd = new SQLiteCommand(selectSql, connection))
            {
                if (!isAdmin)
                {
                    cmd.Parameters.AddWithValue("@UserID", userID);
                }
                using (SQLiteDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var data = new FileRecord
                        {
                            FileID = reader.GetInt32(0),
                            UserID = reader.GetInt32(1),
                            NameFile = reader.GetString(2),
                            FilePath = reader.GetString(3),
                            UploadDate = reader.GetInt32(4),
                            UserName = reader.GetString(5)
                        };
                        files.Add(data);
                    }
                }
            }
        }
        return Json(files);
    }

    // Download a file
    [HttpGet]
    public IActionResult DownloadFile(int fileID)
    {
        var userID = GetUserIDFromSession();
        bool isAdmin = IsAdmin(userID);

        using (var connection = DatabaseConnector.CreateNewConnection())
        {
            string selectSql = "SELECT FilePath FROM File WHERE FileID = @FileID" +
                               (isAdmin ? "" : " AND UserID = @UserID");
            string? filePath = null;

            using (var cmd = new SQLiteCommand(selectSql, connection))
            {
                cmd.Parameters.AddWithValue("@FileID", fileID);
                if (!isAdmin)
                {
                    cmd.Parameters.AddWithValue("@UserID", userID);
                }

                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        filePath = reader.GetString(0);
                    }
                }
            }

            if (filePath != null && System.IO.File.Exists(filePath))
            {
                var fileBytes = System.IO.File.ReadAllBytes(filePath);
                var fileName = Path.GetFileName(filePath);
                return File(fileBytes, "application/octet-stream", fileName);
            }

            return NotFound("File not found or permission denied.");
        }
    }
}
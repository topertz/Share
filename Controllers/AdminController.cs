using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Data.SQLite;

[ApiController]
[Route("[controller]/[action]")]
public class AdminController : Controller
{
    [HttpGet]
    public IActionResult ListUsers()
    {
        var users = new List<User>();
            SQLiteConnection connection = DatabaseConnector.Db();
            string selectSql = "SELECT UserID, UserName, IsAdmin FROM User";

            using (SQLiteCommand cmd = new SQLiteCommand(selectSql, connection))
            {
            using (SQLiteDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var data = new User
                        {
                            UserID = reader.GetInt32(0),
                            UserName = reader.GetString(1),
                            IsAdmin = reader.GetBoolean(2)
                        };
                        users.Add(data);
                    }
                }
            }
        return Json(users);
    }

    [HttpPost]
    public IActionResult DeleteUser([FromForm] int userID)
    {
        try
        {
            using (var connection = DatabaseConnector.CreateNewConnection())
            {
                // Check if the user is an admin
                string checkSql = "SELECT IsAdmin FROM User WHERE UserID = @UserID";
                using (var checkCmd = new SQLiteCommand(checkSql, connection))
                {
                    checkCmd.Parameters.AddWithValue("@UserID", userID);
                    var result = checkCmd.ExecuteScalar();

                    if (result == null)
                    {
                        return NotFound("User not found.");
                    }

                    // Convert the result to long and then to int for comparison
                    long isAdminLong = (long)result;
                    if (isAdminLong == 1)
                    {
                        return BadRequest("Cannot delete an admin user.");
                    }
                }

                // Check if the user has any associated files
                string checkFilesSql = "SELECT COUNT(*) FROM File WHERE UserID = @UserID";
                using (var checkFilesCmd = new SQLiteCommand(checkFilesSql, connection))
                {
                    checkFilesCmd.Parameters.AddWithValue("@UserID", userID);
                    long fileCount = (long)checkFilesCmd.ExecuteScalar();

                    if (fileCount > 0)
                    {
                        return BadRequest("Cannot delete user with associated files. Please delete their files first.");
                    }
                }

                // Proceed with deletion if user is not an admin
                string deleteSql = "DELETE FROM User WHERE UserID = @UserID";
                using (var cmd = new SQLiteCommand(deleteSql, connection))
                {
                    cmd.Parameters.AddWithValue("@UserID", userID);
                    int rowsAffected = cmd.ExecuteNonQuery();
                    if (rowsAffected > 0)
                    {
                        return Ok("User deleted successfully!");
                    }
                    else
                    {
                        return NotFound("User not found.");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error deleting user: " + ex.Message);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPost]
    public IActionResult SetAdminRights([FromForm] int userID, [FromForm] bool isAdmin)
    {
        try
        {
            using (SQLiteConnection connection = DatabaseConnector.CreateNewConnection())
            {
                if(isAdmin)
                {
                    string checkAdminSql = "SELECT COUNT(*) FROM User WHERE IsAdmin = 1";
                    using (SQLiteCommand checkAdminCmd = new SQLiteCommand(checkAdminSql, connection))
                    {
                        long adminCount = (long)checkAdminCmd.ExecuteScalar();
                        if(adminCount > 0)
                        {
                            return BadRequest("Only one admin can be in the system at a time.");
                        }
                    }
                }
                string updateSql = "UPDATE User SET IsAdmin = @IsAdmin WHERE UserID = @UserID";
                using (SQLiteCommand cmd = new SQLiteCommand(updateSql, connection))
                {
                    cmd.Parameters.AddWithValue("@IsAdmin", isAdmin ? 1 : 0);
                    cmd.Parameters.AddWithValue("@UserID", userID);
                    cmd.ExecuteNonQuery();
                }
                return Ok("Admin rights updated!");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error updating admin rights: " + ex.Message);
            return StatusCode(500, "Internal server error");
        }
    }
}
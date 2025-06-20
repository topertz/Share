using System.Data.SQLite;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

app.UseStaticFiles();

app.MapGet("/", () => Results.Redirect("/index.html"));
SQLiteConnection connection = DatabaseConnector.Db();
SQLiteCommand command = connection.CreateCommand();
command.CommandText = "PRAGMA foreign_keys = ON;" +
    "CREATE TABLE if not Exists `User` " +
    "(`UserID` integer NOT NULL PRIMARY KEY, `UserName` text not NULL, `PasswordHash` text not NULL, `PasswordSalt` text not NULL, " +
    "`IsAdmin` integer not Null DEFAULT 0); " +
    "CREATE TABLE if not Exists `Session` " + "(`SessionID` integer NOT NULL PRIMARY KEY, " +
    "`SessionCookie` text NOT NULL UNIQUE, `UserID` integer NOT NULL, " +
    "`ValidUntil` integer NOT NULL, `LoginTime` integer NOT NULL, " +
    "FOREIGN KEY (`UserID`) REFERENCES `User`(`UserID`)); " +
    "CREATE TABLE if not Exists `File`" +
    " (`FileID` integer NOT NULL PRIMARY KEY,  `UserID` integer NOT NULL, " +
    " `NameFile` text not null, `FilePath` text not null, `UploadDate` integer not NULL, " +
    "FOREIGN KEY (`UserID`) REFERENCES `User`(`UserID`));";
command.ExecuteNonQuery();
command.Dispose();
app.Run();
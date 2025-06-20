PRAGMA foreign_keys = ON;

DROP TABLE IF EXISTS `User`;
CREATE TABLE `User` (
    `UserID` INTEGER NOT NULL PRIMARY KEY,
    `UserName` TEXT NOT NULL UNIQUE,
    `PasswordHash` TEXT NOT NULL,
    `PasswordSalt` TEXT NOT NULL,
    `IsAdmin` INTEGER NOT NULL DEFAULT 0
);

drop table if EXISTS `Session`;
CREATE TABLE `Session` (
    `SessionID` integer NOT NULL PRIMARY KEY,
    `SessionCookie` text not null UNIQUE,
    `UserID` integer not null,
    `ValidUntil` integer not null,
    `LoginTime` integer not null,
    FOREIGN KEY (`UserID`) REFERENCES `User`(`UserID`)
);

DROP TABLE IF EXISTS `File`;
CREATE TABLE `File` (
    `FileID` INTEGER NOT NULL PRIMARY KEY,
    `UserID` INTEGER NOT NULL,
    `NameFile` TEXT NOT NULL,
    `FilePath` TEXT NOT NULL,
    `UploadDate` INTEGER NOT NULL,
    FOREIGN KEY (`UserID`) REFERENCES `User`(`UserID`)
);
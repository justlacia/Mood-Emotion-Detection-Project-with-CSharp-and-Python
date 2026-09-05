-- Run inside a database selected by you. Existing project tables are untouched.
SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF OBJECT_ID('dbo.MoodSyncAccounts','U') IS NULL
CREATE TABLE dbo.MoodSyncAccounts (
 Id int IDENTITY PRIMARY KEY,
 DisplayName nvarchar(100) NOT NULL,
 Email nvarchar(254) NOT NULL UNIQUE,
 PasswordHash varchar(200) NOT NULL,
 CreatedAt datetime2 NOT NULL DEFAULT SYSUTCDATETIME()
);
IF OBJECT_ID('dbo.MoodSyncHistory','U') IS NULL
BEGIN
 CREATE TABLE dbo.MoodSyncHistory (
  Id int IDENTITY PRIMARY KEY,
  AccountId int NOT NULL REFERENCES dbo.MoodSyncAccounts(Id),
  Mood varchar(10) NOT NULL CHECK (Mood IN ('positive','negative','neutral')),
  Confidence float NOT NULL CHECK (Confidence BETWEEN 0 AND 1),
  CreatedAt datetime2 NOT NULL DEFAULT SYSUTCDATETIME()
 );
 CREATE INDEX IX_MoodSyncHistory_AccountDate ON dbo.MoodSyncHistory(AccountId,CreatedAt DESC);
END;
COMMIT;

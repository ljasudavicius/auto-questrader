CREATE TABLE [dbo].[Token]
(
    [LoginServer] VARCHAR(100) NOT NULL, 
    [AccessToken] VARCHAR(100) NULL, 
    [RefreshToken] VARCHAR(100) NOT NULL, 
    [TokenType] VARCHAR(100) NULL, 
    [ApiServer] VARCHAR(100) NULL, 
    [ExpiresIn] INT NULL, 
    PRIMARY KEY ([LoginServer])
)

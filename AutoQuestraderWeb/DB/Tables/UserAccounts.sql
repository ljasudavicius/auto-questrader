CREATE TABLE [dbo].[UserAccounts]
(
	[Number] VARCHAR(50) NOT NULL , 
    [UserEmail] VARCHAR(100) NOT NULL, 
    [Type] VARCHAR(50) NOT NULL, 
    PRIMARY KEY ([Number]), 
    CONSTRAINT [FK_UserAccounts_Users] FOREIGN KEY ([UserEmail]) REFERENCES [User]([Email])
)

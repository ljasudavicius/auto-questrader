CREATE TABLE [dbo].[AccountCategory]
(
	[AccountNumber] VARCHAR(50) NOT NULL , 
    [CategoryName] VARCHAR(50) NOT NULL, 
    [Percent] FLOAT NOT NULL, 
    PRIMARY KEY ([AccountNumber],CategoryName), 
    CONSTRAINT [FK_AccountCategory_Category] FOREIGN KEY ([CategoryName]) REFERENCES [Category]([Name]), 
)

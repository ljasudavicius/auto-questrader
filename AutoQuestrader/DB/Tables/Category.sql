CREATE TABLE [dbo].[Category]
(
	[Name] VARCHAR(50) NOT NULL , 
    [TargetPercent] FLOAT NOT NULL, 
    [AccountNumber] VARCHAR(50) NULL, 
    PRIMARY KEY ([Name])
)

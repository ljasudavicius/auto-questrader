CREATE TABLE [dbo].[StockTarget]
(
    [Symbol] VARCHAR(50) NOT NULL, 
    [TargetPercent] FLOAT NOT NULL, 
    [CategoryName] VARCHAR(50) NOT NULL, 
    [ShouldBuy] BIT NOT NULL, 
    [ShouldSell] BIT NOT NULL, 
	PRIMARY KEY (Symbol,CategoryName), 
    CONSTRAINT [FK_StockTarget_Category] FOREIGN KEY ([CategoryName]) REFERENCES [Category]([Name]), 
)

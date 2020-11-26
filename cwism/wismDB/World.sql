CREATE TABLE [dbo].[World]
(
	[GUID] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY, 
    [X] INT NULL, 
    [Y] INT NULL, 
    [TerrainID] NCHAR(10) NULL,
    [RandomSeed] INT NULL,
    [CurrentPlayer] INT NULL
)

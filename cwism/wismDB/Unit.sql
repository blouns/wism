CREATE TABLE [dbo].[Unit]
(
	[GUID] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY, 
    [ID] INT NULL, 
    [PlayerID] SMALLINT NULL, 
    [TileID] NCHAR(10) NULL,
    [Name] NVARCHAR(50) NULL, 
    [Strength] SMALLINT NULL, 
    [Moves] SMALLINT NULL, 
    [MoveRemaining] SMALLINT NULL, 
    [CanWalk] BIT NULL, 
    [CanFly] BIT NULL, 
    [CanFloat] BIT NULL, 
    [IsHero] BIT NULL, 
    [IsSpecial] BIT NULL, 
    [HitPointsRemaining] SMALLINT NULL, 
)

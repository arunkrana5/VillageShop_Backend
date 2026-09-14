USE VillageShopDb;
GO

UPDATE dbo.Users 
SET PasswordHash = '$2a$11$ahtmNRK.5eUxLK4OEFlG6eAArhJynM0cEpxPnuucuhr3ERLh2nGf6'
WHERE Username = 'admin';
GO

SELECT Username, PasswordHash FROM dbo.Users WHERE Username = 'admin';
GO

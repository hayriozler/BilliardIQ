using Microsoft.Data.Sqlite;

namespace BilliardIQ.Mobile.Services;


internal sealed class DatabaseMigrationService(DatabaseExecutor dbExecutor)
{
    internal abstract class BaseDatabaseMigrationService
    {
        internal static void DropDatabaseFileIfExists()
        {
            try
            {
                var fileName = $"{FileSystem.AppDataDirectory}/{Constants.DatabaseFileName}";
                if (File.Exists(fileName))
                {
                    File.Delete(fileName);
                    Console.WriteLine("Old database deleted.");
                }

            }
            catch (Exception)
            {
                throw;
            }
        }

        internal abstract Task RunAsync();
    }
    private readonly BaseDatabaseMigrationService[] _migrationServices = [
        new LocationTableMigrationService(dbExecutor),
        new GameTableMigrationService(dbExecutor),
        new PlayerTableMigrationService(dbExecutor),
    ];

    private static async Task InitializeDatabaseAsync()
    {
        if (AppSettings.DropDatabaseOnStartup)
            BaseDatabaseMigrationService.DropDatabaseFileIfExists();
        var connection = DatabaseExecutor.GetNewDbConnection();
        await connection.OpenAsync();
        await connection.DisposeAsync();
    }
    public async Task RunMigrationAsync()
    {
        await InitializeDatabaseAsync();
        foreach (var migration in _migrationServices)
        {
            await migration.RunAsync();
        }
    }
    private sealed class LocationTableMigrationService(DatabaseExecutor dbExecutor) : BaseDatabaseMigrationService
    {
        private readonly string _tableCreationSql = @"
CREATE TABLE IF NOT EXISTS Countries (
    Code TEXT NOT NULL PRIMARY KEY ,
    Name TEXT NOT NULL
);
CREATE TABLE IF NOT EXISTS Cities (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    CountryCode TEXT NOT NULL,
    Name TEXT NOT NULL
);";
        private static readonly (string Code, string Name)[] _seedCountries =
    [
        ("TR", "Türkiye"),
        ("NL", "Netherlands"),
    ];

        private static readonly (string CountryCode, string CityName)[] _seedCities =
        [
            ("TR", "Adana"), ("TR", "Ankara"), ("TR", "Antalya"), ("TR", "Aydın"),
        ("TR", "Balıkesir"), ("TR", "Bursa"), ("TR", "Denizli"), ("TR", "Diyarbakır"),
        ("TR", "Edirne"), ("TR", "Erzurum"), ("TR", "Eskişehir"), ("TR", "Gaziantep"),
        ("TR", "Hatay"), ("TR", "İstanbul"), ("TR", "İzmir"), ("TR", "Kahramanmaraş"),
        ("TR", "Kayseri"), ("TR", "Kocaeli"), ("TR", "Konya"), ("TR", "Malatya"),
        ("TR", "Manisa"), ("TR", "Mardin"), ("TR", "Mersin"), ("TR", "Muğla"),
        ("TR", "Ordu"), ("TR", "Sakarya"), ("TR", "Samsun"), ("TR", "Şanlıurfa"),
        ("TR", "Tekirdağ"), ("TR", "Trabzon"), ("TR", "Van"), ("TR", "Zonguldak"),
        ("TR", "Adıyaman"), ("TR", "Afyonkarahisar"), ("TR", "Aksaray"), ("TR", "Ardahan"),
        ("TR", "Artvin"), ("TR", "Batman"), ("TR", "Bartın"), ("TR", "Bingöl"),
        ("TR", "Bitlis"), ("TR", "Bolu"), ("TR", "Burdur"), ("TR", "Çanakkale"),
        ("TR", "Çorum"), ("TR", "Düzce"), ("TR", "Elazığ"), ("TR", "Giresun"),
        ("TR", "Iğdır"), ("TR", "Isparta"), ("TR", "Karabük"), ("TR", "Karaman"),
        ("TR", "Kilis"), ("TR", "Kırıkkale"), ("TR", "Kırklareli"), ("TR", "Kütahya"),
        ("TR", "Nevşehir"), ("TR", "Niğde"), ("TR", "Osmaniye"), ("TR", "Rize"),
        ("TR", "Siirt"), ("TR", "Sivas"), ("TR", "Şırnak"), ("TR", "Uşak"),
        ("TR", "Yalova"), ("TR", "Yozgat"),
        ("NL", "Amsterdam"), ("NL", "Rotterdam"), ("NL", "Den Haag"), ("NL", "Utrecht"),
        ("NL", "Eindhoven"), ("NL", "Tilburg"), ("NL", "Groningen"), ("NL", "Almere"),
        ("NL", "Breda"), ("NL", "Nijmegen"), ("NL", "Enschede"), ("NL", "Arnhem"),
        ("NL", "Haarlem"), ("NL", "Amersfoort"), ("NL", "Apeldoorn"), ("NL", "Zaandam"),
        ("NL", "Hoofddorp"), ("NL", "Maastricht"), ("NL", "Leiden"), ("NL", "Dordrecht"),
        ("NL", "Zoetermeer"), ("NL", "Zwolle"), ("NL", "Deventer"), ("NL", "Delft"),
        ("NL", "Alkmaar"), ("NL", "Heerlen"), ("NL", "Venlo"), ("NL", "Leeuwarden"),
        ("NL", "Emmen"), ("NL", "Helmond"), ("NL", "Ede"), ("NL", "Hilversum"),
        ("NL", "Gouda"), ("NL", "Roosendaal"), ("NL", "Almelo"), ("NL", "Lelystad"),
        ("NL", "Hoorn"), ("NL", "Purmerend"), ("NL", "Schiedam"), ("NL", "Vlaardingen"),
    ];

        internal async override Task RunAsync()
        {
            var created = await dbExecutor.ExecuteAsync(_tableCreationSql);
            if (created)
            {
                await EnsureSeededAsync();
            }
        }
        private async Task EnsureSeededAsync()
        {
            foreach (var (code, name) in _seedCountries)
            {
                var parameters = new List<SqliteParameter> { new("@Code", code), new("@Name", name) };
                await dbExecutor.ExecuteAsync("INSERT OR IGNORE INTO Countries(Code, Name) VALUES(@Code, @Name)", parameters);
            }

            foreach (var (countryCode, cityName) in _seedCities)
            {
                    await dbExecutor.ExecuteAsync("INSERT INTO Cities(CountryCode, Name) SELECT Code, @City FROM Countries WHERE Code=@Code",
                        [new("@City", cityName), new("@Code", countryCode)]);
            }
        }
    }

    private sealed class GameTableMigrationService(DatabaseExecutor dbExecutor) : BaseDatabaseMigrationService
    {
        //Opponent
        readonly string _tableCreationSql = @"
            CREATE TABLE IF NOT EXISTS Games (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                OpponentName TEXT NOT NULL,
                Ball TEXT NOT NULL,
                Location TEXT,
                Date DateTime NOT NULL,
                PlayerScore INTEGER NOT NULL DEFAULT 0,
                OpponentScore INTEGER NOT NULL DEFAULT 0,
                HighestRun INTEGER NOT NULL DEFAULT 0,
                Innings REAL NOT NULL DEFAULT 0,
                Notes TEXT,
                ScoreboardThumbnail BLOB,
                CreatedAt DateTime NOT NULL
            );
CREATE TABLE IF NOT EXISTS GamePhotos (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                GameId INTEGER NOT NULL,
                PhotoName TEXT NOT NULL,
                PhotoPath TEXT NOT NULL,
                CreatedAt DateTime NOT NULL
            );
CREATE TABLE IF NOT EXISTS GameStats (
                ID INTEGER PRIMARY KEY AUTOINCREMENT,
                GameId INTEGER NOT NULL,
                PlayerTotal INT NOT NULL,
                OpponentTotal INT NOT NULL
            );
CREATE TABLE IF NOT EXISTS PlayerStats (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                TotalMatches INTEGER NOT NULL DEFAULT 0,
                TotalScore INTEGER NOT NULL DEFAULT 0,
                TotalInnings REAL NOT NULL DEFAULT 0,
                Average REAL NOT NULL DEFAULT 0,
                BestAverage REAL NOT NULL DEFAULT 0,
                HighestRun INTEGER NOT NULL DEFAULT 0,
                UpdatedAt DateTime NOT NULL
            );
";

        internal override async Task RunAsync() =>await dbExecutor.ExecuteAsync(_tableCreationSql);
    }

    private sealed class PlayerTableMigrationService(DatabaseExecutor dbExecutor) : BaseDatabaseMigrationService
    {
        private readonly string _tableCreationSql = @"
             CREATE TABLE IF NOT EXISTS Player (
                 Id INTEGER PRIMARY KEY AUTOINCREMENT,
                 Name TEXT NOT NULL,
                 LastName TEXT NOT NULL,
                 Level INTEGER NOT NULL,
                 Country TEXT NOT NULL DEFAULT '',
                 City TEXT NOT NULL DEFAULT '',
                 Club TEXT,
                 Association TEXT,
                 Email TEXT NOT NULL DEFAULT '',
                 Phone TEXT,
                 CreatedAt DateTime NOT NULL
             );";

        internal async override Task RunAsync() =>await dbExecutor.ExecuteAsync(_tableCreationSql);
    }
}
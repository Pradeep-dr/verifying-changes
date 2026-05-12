using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using Dapper;

namespace MarsLite.Web.Data
{
    /// <summary>
    /// SQLite-backed data access for MarsLite. In real Mars this would be EF + SQL Server,
    /// hidden behind repositories / services injected via Castle Windsor. For the lite POC
    /// we keep it flat — a static helper that creates the schema, seeds demo data, and
    /// exposes a small set of query methods used by the Nancy modules.
    /// </summary>
    public static class MarsLiteDb
    {
        public static string DbFile =>
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "marslite.db");

        public static string ConnectionString => $"Data Source={DbFile};Version=3;";

        public static SQLiteConnection Open()
        {
            var c = new SQLiteConnection(ConnectionString);
            c.Open();
            return c;
        }

        /// <summary>Creates the schema (idempotent) and seeds initial rows when empty.</summary>
        public static void Initialise()
        {
            using (var c = Open())
            {
                c.Execute(@"
                    CREATE TABLE IF NOT EXISTS StaffUsers (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Username TEXT UNIQUE NOT NULL,
                        PasswordHash TEXT NOT NULL,
                        DisplayName TEXT NOT NULL,
                        Role TEXT NOT NULL
                    );
                    CREATE TABLE IF NOT EXISTS Providers (
                        Id INTEGER PRIMARY KEY,
                        Name TEXT NOT NULL,
                        Code TEXT UNIQUE NOT NULL
                    );
                    CREATE TABLE IF NOT EXISTS WaitingLists (
                        Id INTEGER PRIMARY KEY,
                        ProviderId INTEGER NOT NULL,
                        Name TEXT NOT NULL,
                        Specialty TEXT NOT NULL,
                        Status TEXT NOT NULL DEFAULT 'Active',
                        MaxCapacity INTEGER NOT NULL DEFAULT 100,
                        TargetWaitDays INTEGER NOT NULL DEFAULT 28,
                        DefaultPriority TEXT NOT NULL DEFAULT 'Routine',
                        AutoAssign INTEGER NOT NULL DEFAULT 1,
                        Notes TEXT NOT NULL DEFAULT ''
                    );
                    CREATE TABLE IF NOT EXISTS WaitingListEntries (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        WaitingListId INTEGER NOT NULL,
                        Ref TEXT UNIQUE NOT NULL,
                        PatientName TEXT NOT NULL,
                        PatientDob TEXT NOT NULL,
                        AddedOn TEXT NOT NULL,
                        Priority TEXT NOT NULL,
                        Status TEXT NOT NULL
                    );
                ");

                Seed(c);
            }
        }

        private static void Seed(SQLiteConnection c)
        {
            if (c.ExecuteScalar<int>("SELECT COUNT(*) FROM StaffUsers") == 0)
            {
                c.Execute(
                    "INSERT INTO StaffUsers (Username, PasswordHash, DisplayName, Role) VALUES (@Username, @PasswordHash, @DisplayName, @Role)",
                    new
                    {
                        Username     = "staff@drdoctor.dev",
                        PasswordHash = PasswordHasher.Hash("password123"),
                        DisplayName  = "Test Staff",
                        Role         = "Admin",
                    });
            }

            if (c.ExecuteScalar<int>("SELECT COUNT(*) FROM Providers") == 0)
            {
                c.Execute("INSERT INTO Providers (Id, Name, Code) VALUES (1, 'St Mary''s Hospital',    'STMARYS')");
                c.Execute("INSERT INTO Providers (Id, Name, Code) VALUES (2, 'Royal London Hospital', 'ROYLON')");
                c.Execute("INSERT INTO Providers (Id, Name, Code) VALUES (3, 'Manchester General',    'MGEN')");
            }

            if (c.ExecuteScalar<int>("SELECT COUNT(*) FROM WaitingLists") == 0)
            {
                c.Execute("INSERT INTO WaitingLists (Id, ProviderId, Name, Specialty, Status, Notes) VALUES (1, 1, 'General Surgery WL', 'General Surgery', 'Active', 'Routine surgical waiting list for Provider 1')");
                c.Execute("INSERT INTO WaitingLists (Id, ProviderId, Name, Specialty, Status, Notes) VALUES (2, 1, 'Orthopaedics WL',    'Orthopaedics',    'Active', '')");
                c.Execute("INSERT INTO WaitingLists (Id, ProviderId, Name, Specialty, Status, Notes) VALUES (3, 1, 'Cardiology WL',      'Cardiology',      'Paused', '')");
            }

            if (c.ExecuteScalar<int>("SELECT COUNT(*) FROM WaitingListEntries") == 0)
            {
                var today = DateTime.Today;
                var rows = new[]
                {
                    new { WaitingListId = 1, Ref = "WL-001", PatientName = "Sarah Johnson",  PatientDob = new DateTime(1975, 3, 12), AddedOn = today.AddDays(-48), Priority = "Urgent",  Status = "Waiting"  },
                    new { WaitingListId = 1, Ref = "WL-002", PatientName = "Michael Chen",   PatientDob = new DateTime(1982, 8,  5), AddedOn = today.AddDays(-43), Priority = "Routine", Status = "Waiting"  },
                    new { WaitingListId = 1, Ref = "WL-003", PatientName = "Emma Williams",  PatientDob = new DateTime(1990,11, 22), AddedOn = today.AddDays(-38), Priority = "Routine", Status = "Assigned" },
                    new { WaitingListId = 1, Ref = "WL-004", PatientName = "James Patel",    PatientDob = new DateTime(1967, 4, 17), AddedOn = today.AddDays(-25), Priority = "Urgent",  Status = "Waiting"  },
                    new { WaitingListId = 1, Ref = "WL-005", PatientName = "Lisa Thompson",  PatientDob = new DateTime(1955, 6, 30), AddedOn = today.AddDays(-13), Priority = "Routine", Status = "Assigned" },
                };

                c.Execute(
                    "INSERT INTO WaitingListEntries (WaitingListId, Ref, PatientName, PatientDob, AddedOn, Priority, Status) " +
                    "VALUES (@WaitingListId, @Ref, @PatientName, @PatientDob, @AddedOn, @Priority, @Status)",
                    rows);
            }
        }

        // ── Queries used by the Nancy modules ──────────────────────────────────

        public static StaffUser FindUserByUsername(string username)
        {
            using (var c = Open())
                return c.QueryFirstOrDefault<StaffUser>(
                    "SELECT * FROM StaffUsers WHERE Username = @username", new { username });
        }

        public static Provider FindProvider(int providerId)
        {
            using (var c = Open())
                return c.QueryFirstOrDefault<Provider>(
                    "SELECT * FROM Providers WHERE Id = @providerId", new { providerId });
        }

        public static List<WaitingListSummary> GetWaitingListsForProvider(int providerId)
        {
            using (var c = Open())
                return c.Query<WaitingListSummary>(@"
                    SELECT wl.Id, wl.Name, wl.Specialty, wl.Status,
                           (SELECT COUNT(*) FROM WaitingListEntries e WHERE e.WaitingListId = wl.Id) AS EntryCount
                    FROM WaitingLists wl
                    WHERE wl.ProviderId = @providerId
                    ORDER BY wl.Id", new { providerId }).ToList();
        }

        public static WaitingList GetFirstListForProvider(int providerId)
        {
            using (var c = Open())
                return c.QueryFirstOrDefault<WaitingList>(
                    "SELECT * FROM WaitingLists WHERE ProviderId = @providerId LIMIT 1",
                    new { providerId });
        }

        public static List<WaitingListEntry> GetEntriesForProvider(int providerId)
        {
            using (var c = Open())
                return c.Query<WaitingListEntry>(@"
                    SELECT e.* FROM WaitingListEntries e
                    JOIN WaitingLists wl ON wl.Id = e.WaitingListId
                    WHERE wl.ProviderId = @providerId
                    ORDER BY e.AddedOn", new { providerId }).ToList();
        }
    }
}

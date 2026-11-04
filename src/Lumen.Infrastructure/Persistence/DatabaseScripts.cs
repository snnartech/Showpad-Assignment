namespace Lumen.Infrastructure.Persistence;

internal static class DatabaseScripts
{
    // Schema excerpt from the assignment (Appendix A2), kept as found (e.g. the global UX_Users_Email index,
    // no tenant FKs on most tables). UserCredentials is an addition so the local auth service can log users in.
    public static readonly string[] Schema =
    [
        """
        IF OBJECT_ID(N'dbo.ProcessedQuizAttempts', N'U') IS NULL
        CREATE TABLE dbo.ProcessedQuizAttempts (
        AttemptKey UNIQUEIDENTIFIER NOT NULL
        CONSTRAINT PK_ProcessedQuizAttempts PRIMARY KEY,
        UserId INT NOT NULL,
        QuizId INT NOT NULL,
        Points INT NOT NULL,
        ProcessedAt DATETIME2 NOT NULL
        CONSTRAINT DF_ProcessedQuizAttempts_ProcessedAt DEFAULT SYSUTCDATETIME());
        """, // RD - Added table for processed quiz attempts with Idempotency Key condition
        """
        IF OBJECT_ID(N'dbo.Organisations', N'U') IS NULL
        CREATE TABLE dbo.Organisations (
            Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Organisations PRIMARY KEY,
            Name NVARCHAR(200) NOT NULL,
            Subdomain NVARCHAR(63) NOT NULL CONSTRAINT UQ_Organisations_Subdomain UNIQUE,
            FeatureFlags NVARCHAR(MAX) NULL);
        """,
        """
        IF OBJECT_ID(N'dbo.Users', N'U') IS NULL
        BEGIN
            CREATE TABLE dbo.Users (
                Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Users PRIMARY KEY,
                OrganisationId INT NOT NULL CONSTRAINT FK_Users_Organisations REFERENCES dbo.Organisations(Id),
                Email NVARCHAR(320) NOT NULL,
                DisplayName NVARCHAR(200) NOT NULL,
                Department NVARCHAR(100) NULL,
                Region NVARCHAR(50) NULL,
                HireDate DATE NULL,
                IsActive BIT NOT NULL CONSTRAINT DF_Users_IsActive DEFAULT 1);
            CREATE UNIQUE INDEX UX_Users_Email ON dbo.Users(Email);   -- hmm, global? (TODO ask the author)
        END
        """,
        """
        IF OBJECT_ID(N'dbo.AudienceRules', N'U') IS NULL
        CREATE TABLE dbo.AudienceRules (
            Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_AudienceRules PRIMARY KEY,
            OrganisationId INT NOT NULL,
            Name NVARCHAR(200) NOT NULL,
            RuleExpression NVARCHAR(MAX) NOT NULL);
        """,
        """
        IF OBJECT_ID(N'dbo.Leaderboards', N'U') IS NULL
        CREATE TABLE dbo.Leaderboards (
            Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Leaderboards PRIMARY KEY,
            OrganisationId INT NOT NULL,
            Name NVARCHAR(200) NOT NULL,
            AudienceRuleId INT NULL,
            PeriodStart DATE NOT NULL,
            PeriodEnd DATE NOT NULL);
        """,
        """
        IF OBJECT_ID(N'dbo.LeaderboardScores', N'U') IS NULL
        CREATE TABLE dbo.LeaderboardScores (
            Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_LeaderboardScores PRIMARY KEY,
            LeaderboardId INT NOT NULL CONSTRAINT FK_LeaderboardScores_Leaderboards REFERENCES dbo.Leaderboards(Id),
            UserId INT NOT NULL CONSTRAINT FK_LeaderboardScores_Users REFERENCES dbo.Users(Id),
            Points INT NOT NULL,
            UpdatedAt DATETIME2 NOT NULL);
        -- no table records which LeaderboardScores row was produced by which attempt
        """,
        """
        IF OBJECT_ID(N'dbo.Quizzes', N'U') IS NULL
        CREATE TABLE dbo.Quizzes (
            Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Quizzes PRIMARY KEY,
            OrganisationId INT NOT NULL,
            Title NVARCHAR(300) NOT NULL,
            PointsOnPass INT NOT NULL,
            PassMark INT NOT NULL);
        """,
        """
        IF OBJECT_ID(N'dbo.QuizAttempts', N'U') IS NULL
        CREATE TABLE dbo.QuizAttempts (
            Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_QuizAttempts PRIMARY KEY,
            AttemptKey UNIQUEIDENTIFIER NOT NULL CONSTRAINT UQ_QuizAttempts_AttemptKey UNIQUE,
            UserId INT NOT NULL,
            QuizId INT NOT NULL,
            Score INT NOT NULL,
            Passed BIT NOT NULL,
            CompletedAt DATETIME2 NOT NULL);
        """,
        """
        IF OBJECT_ID(N'dbo.UserCredentials', N'U') IS NULL
        CREATE TABLE dbo.UserCredentials (
            UserId INT NOT NULL CONSTRAINT PK_UserCredentials PRIMARY KEY CONSTRAINT FK_UserCredentials_Users REFERENCES dbo.Users(Id),
            PasswordHash NVARCHAR(500) NOT NULL,
            Roles NVARCHAR(200) NOT NULL);
        """
    ];

    // Two tenants (142 = acme, 77 = globex). Board 905 belongs to org 77 - see ticket LMN-4471.
    public const string SeedData = """
        IF NOT EXISTS (SELECT 1 FROM dbo.Organisations)
        BEGIN
            SET IDENTITY_INSERT dbo.Organisations ON;
            INSERT INTO dbo.Organisations (Id, Name, Subdomain, FeatureFlags) VALUES
                (142, N'Acme Corp', N'acme',   N'{"leaderboards":true}'),
                (77,  N'Globex',    N'globex', N'{"leaderboards":true}');
            SET IDENTITY_INSERT dbo.Organisations OFF;

            SET IDENTITY_INSERT dbo.Users ON;
            INSERT INTO dbo.Users (Id, OrganisationId, Email, DisplayName, Department, Region, HireDate, IsActive) VALUES
                (88213, 142, N'alice@acme.test',   N'Alice Smith',        N'Sales',       N'EU', @hireDate, 1),
                (88214, 142, N'bob@acme.test',     N'Bob Jones',          N'Sales',       N'US', @hireDate, 1),
                (88215, 142, N'carol@acme.test',   N'Carol White',        N'Engineering', N'EU', @hireDate, 1),
                (88216, 142, N'admin@acme.test',   N'Dana Admin',         N'HR',          N'EU', @hireDate, 1),
                (88217, 142, N'scim@acme.test',    N'SCIM Service',       NULL,           NULL,  @hireDate, 1),
                (70001, 77,  N'jpatel@globex.test',N'J. Patel',           N'Sales',       N'US', @hireDate, 1),
                (70002, 77,  N'erin@globex.test',  N'Erin Diaz',          N'Support',     N'US', @hireDate, 1);
            SET IDENTITY_INSERT dbo.Users OFF;

            SET IDENTITY_INSERT dbo.AudienceRules ON;
            INSERT INTO dbo.AudienceRules (Id, OrganisationId, Name, RuleExpression) VALUES
                (17, 142, N'Sales team',  N'{"department":"Sales"}'),
                (18, 142, N'EU region',   N'{"region":"EU"}'),
                (5,  77,  N'US region',   N'{"region":"US"}');
            SET IDENTITY_INSERT dbo.AudienceRules OFF;

            SET IDENTITY_INSERT dbo.Leaderboards ON;
            INSERT INTO dbo.Leaderboards (Id, OrganisationId, Name, AudienceRuleId, PeriodStart, PeriodEnd) VALUES
                (903, 142, N'Weekly leaderboard',  NULL, @periodStart, @periodEnd),
                (911, 142, N'Sales leaderboard',   17,   @periodStart, @periodEnd),
                (905, 77,  N'Weekly leaderboard',  NULL, @periodStart, @periodEnd);
            SET IDENTITY_INSERT dbo.Leaderboards OFF;

            INSERT INTO dbo.LeaderboardScores (LeaderboardId, UserId, Points, UpdatedAt) VALUES
                (903, 88213, 120, SYSUTCDATETIME()),
                (903, 88214,  90, SYSUTCDATETIME()),
                (903, 88215,  60, SYSUTCDATETIME()),
                (903, 88216,  30, SYSUTCDATETIME()),
                (911, 88213, 120, SYSUTCDATETIME()),
                (911, 88214,  90, SYSUTCDATETIME()),
                (905, 70001, 300, SYSUTCDATETIME()),
                (905, 70002, 150, SYSUTCDATETIME());

            SET IDENTITY_INSERT dbo.Quizzes ON;
            INSERT INTO dbo.Quizzes (Id, OrganisationId, Title, PointsOnPass, PassMark) VALUES
                (1, 142, N'Compliance quiz', 50, 80),
                (2, 77,  N'Safety quiz',     40, 70);
            SET IDENTITY_INSERT dbo.Quizzes OFF;
        END
        """;
}

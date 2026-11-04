namespace Lumen.Infrastructure.Options;

public class SqsOptions
{
    public const string Section = "Sqs";

    /// <summary>Local SQS emulator (LocalStack) endpoint. Leave empty to talk to real AWS.</summary>
    public string? ServiceUrl { get; set; } = "http://localhost:4567";
    public string Region { get; set; } = "us-east-1";
    public string AccessKey { get; set; } = "local";
    public string SecretKey { get; set; } = "local";
    public string QueueName { get; set; } = "quiz-attempts";
    public string DeadLetterQueueName { get; set; } = "quiz-attempts-dlq";
    public int VisibilityTimeoutSeconds { get; set; } = 30;
    public int ReceiveWaitTimeSeconds { get; set; } = 20;
    public int MaxReceiveCount { get; set; } = 5;
}

public class SmtpOptions
{
    public const string Section = "Smtp";

    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 1026;
    public string From { get; set; } = "no-reply@lumen.example";
    public int TimeoutMs { get; set; } = 30000;
}

public class SeedOptions
{
    public const string Section = "Seed";

    public bool Enabled { get; set; } = true;
    /// <summary>Password given to every seeded local user.</summary>
    public string DefaultPassword { get; set; } = "Lumen#2026";
}

public class SigningKeyOptions
{
    public const string Section = "Jwt";

    /// <summary>Where the auth service stores its RSA private key (relative to content root if not rooted).</summary>
    public string SigningKeyPath { get; set; } = "App_Data/signing-key.pem";
}

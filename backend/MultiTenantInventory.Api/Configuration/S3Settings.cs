namespace MultiTenantInventory.Api.Configuration;

// Bound from the "AWS" configuration section:
//   AWS:BucketName, AWS:Region, AWS:S3:AccessKey, AWS:S3:SecretKey
// Put the keys in user-secrets (dev) or environment variables (AWS__S3__AccessKey), never in appsettings.json.
public class S3Settings
{
    public string? BucketName { get; set; }
    public string? Region { get; set; }
    public int PresignedUrlMinutes { get; set; } = 60;

    public S3CredentialSettings S3 { get; set; } = new();

    public bool HasExplicitCredentials =>
        !string.IsNullOrWhiteSpace(S3.AccessKey) && !string.IsNullOrWhiteSpace(S3.SecretKey);
}

public class S3CredentialSettings
{
    public string? AccessKey { get; set; }
    public string? SecretKey { get; set; }
}

namespace MultiTenantInventory.Api.Interfaces;

public interface IS3Service
{
    // False when AWS:BucketName / AWS:Region are missing. The rest of the app keeps working.
    bool IsConfigured { get; }

    Task UploadAsync(string key, Stream content, string contentType, CancellationToken cancellationToken = default);

    Task DeleteAsync(string key, CancellationToken cancellationToken = default);

    // Short-lived URL for viewing a private object, or null if S3 isn't available.
    string? GetPresignedUrl(string key);
}

// Thrown by the storage service for any AWS/S3 failure (bad credentials, missing bucket, ...).
public class StorageException : Exception
{
    public StorageException(string message, Exception? inner = null) : base(message, inner)
    {
    }
}

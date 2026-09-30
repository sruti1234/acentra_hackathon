using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using MultiTenantInventory.Api.Configuration;
using MultiTenantInventory.Api.Interfaces;

namespace MultiTenantInventory.Api.Services;

public class S3Service : IS3Service, IDisposable
{
    private readonly S3Settings _settings;
    private readonly ILogger<S3Service> _logger;
    private readonly bool _isDevelopment;
    private readonly Lazy<IAmazonS3> _client;

    public S3Service(IOptions<S3Settings> options, ILogger<S3Service> logger, IHostEnvironment environment)
    {
        _settings = options.Value;
        _logger = logger;
        _isDevelopment = environment.IsDevelopment();

        // Created on first use, so a missing AWS setup never stops the app from starting.
        _client = new Lazy<IAmazonS3>(CreateClient);
    }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(_settings.BucketName) && !string.IsNullOrWhiteSpace(_settings.Region);

    public async Task UploadAsync(string key, Stream content, string contentType, CancellationToken cancellationToken = default)
    {
        EnsureConfigured();

        try
        {
            var request = new PutObjectRequest
            {
                BucketName = _settings.BucketName,
                Key = key,
                InputStream = content,
                ContentType = contentType
            };

            await _client.Value.PutObjectAsync(request, cancellationToken);
            _logger.LogInformation("Uploaded s3://{Bucket}/{Key}", _settings.BucketName, key);
        }
        catch (Exception ex) when (ex is AmazonClientException or AmazonServiceException)
        {
            _logger.LogError(ex, "S3 upload failed for key {Key}", key);
            throw new StorageException($"S3 upload failed: {ex.Message}", ex);
        }
    }

    public async Task DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        EnsureConfigured();

        try
        {
            await _client.Value.DeleteObjectAsync(_settings.BucketName, key, cancellationToken);
        }
        catch (Exception ex) when (ex is AmazonClientException or AmazonServiceException)
        {
            _logger.LogError(ex, "S3 delete failed for key {Key}", key);
            throw new StorageException($"S3 delete failed: {ex.Message}", ex);
        }
    }

    public string? GetPresignedUrl(string key)
    {
        if (!IsConfigured || (_isDevelopment && !_settings.HasExplicitCredentials))
        {
            return null;
        }

        try
        {
            var request = new GetPreSignedUrlRequest
            {
                BucketName = _settings.BucketName,
                Key = key,
                Verb = HttpVerb.GET,
                Expires = DateTime.UtcNow.AddMinutes(_settings.PresignedUrlMinutes)
            };

            return _client.Value.GetPreSignedURL(request);
        }
        catch (Exception ex) when (ex is AmazonClientException or AmazonServiceException)
        {
            _logger.LogWarning(ex, "Could not create a pre-signed URL for {Key}", key);
            return null;
        }
    }

    public void Dispose()
    {
        if (_client.IsValueCreated)
        {
            _client.Value.Dispose();
        }
    }

    private IAmazonS3 CreateClient()
    {
        var config = new AmazonS3Config
        {
            RegionEndpoint = RegionEndpoint.GetBySystemName(_settings.Region!.Trim())
        };

        if (_settings.HasExplicitCredentials)
        {
            // Local development: explicit keys from AWS:S3:AccessKey / AWS:S3:SecretKey (user-secrets).
            _logger.LogInformation(
                "S3 client: explicit credentials from configuration (AWS:S3:AccessKey), region {Region}, bucket {Bucket}",
                config.RegionEndpoint.SystemName, _settings.BucketName);

            var credentials = new BasicAWSCredentials(_settings.S3.AccessKey!.Trim(), _settings.S3.SecretKey!.Trim());
            return new AmazonS3Client(credentials, config);
        }

        // Production (e.g. EC2/ECS/Lambda): default AWS credential chain, typically an IAM role.
        _logger.LogInformation("S3 client: default AWS credential chain, region {Region}", config.RegionEndpoint.SystemName);
        return new AmazonS3Client(config);
    }

    private void EnsureConfigured()
    {
        if (!IsConfigured)
        {
            throw new StorageException("AWS S3 is not configured. Set AWS:BucketName and AWS:Region.");
        }

        // In Development never fall back to EC2 instance metadata — require the user-secrets keys.
        if (_isDevelopment && !_settings.HasExplicitCredentials)
        {
            throw new StorageException(
                "AWS credentials are not configured. Set AWS:S3:AccessKey and AWS:S3:SecretKey with dotnet user-secrets.");
        }
    }
}

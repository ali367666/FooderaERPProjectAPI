using Application.Common.Interfaces.Abstracts.İnterfaces;
using Microsoft.Extensions.Configuration;
using Minio;
using Minio.DataModel.Args;

namespace Infrastructure.Services;

public class MinioFileStorageService : IFileStorageService
{
    private readonly IConfiguration _configuration;
    private bool _bucketEnsured;

    public MinioFileStorageService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    private IMinioClient CreateClient()
    {
        var endpoint = _configuration["Minio:Endpoint"]!;
        var accessKey = _configuration["Minio:AccessKey"]!;
        var secretKey = _configuration["Minio:SecretKey"]!;
        var useSsl = bool.TryParse(_configuration["Minio:UseSSL"], out var ssl) && ssl;

        return new MinioClient()
            .WithEndpoint(endpoint)
            .WithCredentials(accessKey, secretKey)
            .WithSSL(useSsl)
            .Build();
    }

    private async Task EnsureBucketAsync(IMinioClient client, string bucket, CancellationToken cancellationToken)
    {
        if (_bucketEnsured) return;

        var exists = await client.BucketExistsAsync(
            new BucketExistsArgs().WithBucket(bucket), cancellationToken);

        if (!exists)
        {
            await client.MakeBucketAsync(new MakeBucketArgs().WithBucket(bucket), cancellationToken);

            var publicReadPolicy = $$"""
            {
                "Version": "2012-10-17",
                "Statement": [
                    {
                        "Effect": "Allow",
                        "Principal": { "AWS": ["*"] },
                        "Action": ["s3:GetObject"],
                        "Resource": ["arn:aws:s3:::{{bucket}}/*"]
                    }
                ]
            }
            """;

            await client.SetPolicyAsync(
                new SetPolicyArgs().WithBucket(bucket).WithPolicy(publicReadPolicy), cancellationToken);
        }

        _bucketEnsured = true;
    }

    public async Task<string> UploadAsync(
        Stream stream,
        string fileName,
        string contentType,
        CancellationToken cancellationToken)
    {
        var bucket = _configuration["Minio:Bucket"]!;
        var client = CreateClient();

        await EnsureBucketAsync(client, bucket, cancellationToken);

        var extension = Path.GetExtension(fileName);
        var objectName = $"{Guid.NewGuid():N}{extension}";

        await client.PutObjectAsync(
            new PutObjectArgs()
                .WithBucket(bucket)
                .WithObject(objectName)
                .WithStreamData(stream)
                .WithObjectSize(stream.Length)
                .WithContentType(contentType),
            cancellationToken);

        var endpoint = _configuration["Minio:Endpoint"]!;
        var useSsl = bool.TryParse(_configuration["Minio:UseSSL"], out var ssl) && ssl;
        var scheme = useSsl ? "https" : "http";

        return $"{scheme}://{endpoint}/{bucket}/{objectName}";
    }
}

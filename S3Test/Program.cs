using Minio;
using Minio.Exceptions;
using Minio.DataModel;
using Minio.Credentials;
using Minio.DataModel.Args;

// See https://aka.ms/new-console-template for more information

var endpoint = "172.17.0.2:9000";
var accessKey = "ZRf79WqJZN5FG2sdvxF4";
var secretKey = "KIr4vrLhMuLa5g5hqsSUFuXNVp2W2N5sGDmPP1tT";

IMinioClient c = new MinioClient()
        .WithEndpoint(endpoint)
        .WithCredentials(accessKey, secretKey)
        .Build()
    ;

await using var fileStream = System.IO.File.OpenRead("/home/chaosmac/Downloads/penis.jpg");

var bucketName = "zoo";
var location = "OsuDroidServer";
var objectName = "penis.jpg";
var contentType = "image/jpg";

var putObjectArgs = new PutObjectArgs()
    .WithBucket(bucketName)
    .WithObject(objectName)
    .WithObjectSize(fileStream.Length)
    .WithStreamData(fileStream)
    .WithContentType(contentType)
    ;

var v = await (c.PutObjectAsync(putObjectArgs));

Console.WriteLine(v);
Console.WriteLine(v.ResponseStatusCode);

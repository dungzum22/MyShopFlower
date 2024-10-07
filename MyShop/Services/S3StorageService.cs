using Amazon.S3;
using Amazon.S3.Model;
using System.IO;
using System.Threading.Tasks;
using System.Net.Mime;
using MimeMapping;

public class S3StorageService
{
    private readonly IAmazonS3 _s3Client;
    private const string BucketName = "flower-shopz";

    public S3StorageService(IAmazonS3 s3Client)
    {
        _s3Client = s3Client;
    }

    public async Task<string> UploadFileAsync(Stream inputStream, string fileName, bool isPublic = true)
    {
        // Xác định contentType dựa vào phần mở rộng của file
        string contentType = MimeUtility.GetMimeMapping(fileName);

        inputStream.Position = 0;

        var request = new PutObjectRequest
        {
            BucketName = BucketName,
            Key = fileName,
            InputStream = inputStream,
            ContentType = contentType,
            //CannedACL = isPublic ? S3CannedACL.PublicRead : S3CannedACL.Private
        };

        try
        {
            var response = await _s3Client.PutObjectAsync(request);
            return $"https://{BucketName}.s3.amazonaws.com/{fileName}";
        }
        catch (AmazonS3Exception ex)
        {
            throw new Exception($"Có lỗi khi upload ảnh lên S3: {ex.Message}");
        }
    }

    public async Task DeleteFileAsync(string fileName)
    {
        var request = new DeleteObjectRequest
        {
            BucketName = BucketName,
            Key = fileName
        };

        try
        {
            await _s3Client.DeleteObjectAsync(request);
        }
        catch (AmazonS3Exception ex)
        {
            throw new Exception($"Có lỗi khi xóa ảnh khỏi S3: {ex.Message}");
        }
    }

    public async Task<bool> FileExistsAsync(string fileName)
    {
        var request = new GetObjectMetadataRequest
        {
            BucketName = BucketName,
            Key = fileName
        };

        try
        {
            var response = await _s3Client.GetObjectMetadataAsync(request);
            return true;
        }
        catch (AmazonS3Exception ex)
        {
            if (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return false;
            }
            throw new Exception($"Có lỗi khi kiểm tra tệp trong S3: {ex.Message}");
        }
    }
}

// Lưu ý: Để sử dụng MimeUtility, bạn có thể thêm thư viện "MimeMapping" thông qua NuGet.
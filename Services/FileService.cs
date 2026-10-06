using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace dugunsalonu.Services
{
    public class FileService : IFileService
    {
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<FileService> _logger;

        // İzin verilen dosya uzantıları
        private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".gif", ".webp"
        };

        // İzin verilen MIME türleri
        private static readonly HashSet<string> AllowedMimeTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "image/jpeg", "image/png", "image/gif", "image/webp"
        };

        // Dosya imza kontrolleri (Magic Bytes) - gerçek dosya tipini doğrular
        private static readonly Dictionary<string, byte[][]> FileSignatures = new()
        {
            { ".jpg", new[] { new byte[] { 0xFF, 0xD8, 0xFF } } },
            { ".jpeg", new[] { new byte[] { 0xFF, 0xD8, 0xFF } } },
            { ".png", new[] { new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A } } },
            { ".gif", new[] { new byte[] { 0x47, 0x49, 0x46, 0x38 } } },
            { ".webp", new[] { new byte[] { 0x52, 0x49, 0x46, 0x46 } } }
        };

        // Maksimum dosya boyutu: 10MB
        private const long MaxFileSize = 10 * 1024 * 1024;

        public FileService(IWebHostEnvironment environment, ILogger<FileService> logger)
        {
            _environment = environment;
            _logger = logger;
        }

        public async Task<string> SaveFileAsync(IFormFile file, string folderName)
        {
            // 1. Null / boş kontrol
            if (file == null || file.Length == 0)
                throw new ArgumentException("Dosya boş olamaz.");

            // 2. Dosya boyutu kontrolü
            if (file.Length > MaxFileSize)
                throw new InvalidOperationException($"Dosya boyutu çok büyük. Maksimum {MaxFileSize / (1024 * 1024)}MB yüklenebilir.");

            // 3. Dosya uzantısı kontrolü
            string extension = Path.GetExtension(file.FileName);
            if (string.IsNullOrEmpty(extension) || !AllowedExtensions.Contains(extension))
                throw new InvalidOperationException($"Bu dosya türü desteklenmiyor. İzin verilen: {string.Join(", ", AllowedExtensions)}");

            // 4. MIME türü kontrolü
            if (!AllowedMimeTypes.Contains(file.ContentType))
                throw new InvalidOperationException($"Geçersiz dosya türü: {file.ContentType}");

            // 5. Dosya imzası (Magic Bytes) kontrolü
            if (!await IsValidFileSignature(file, extension))
                throw new InvalidOperationException("Dosya içeriği, uzantısıyla eşleşmiyor. Güvenlik nedeniyle reddedildi.");

            // 6. Klasör adını temizle (path traversal önleme)
            folderName = SanitizeFolderName(folderName);

            string uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", folderName);
            
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            string uniqueFileName = Guid.NewGuid().ToString() + ".jpg"; // Force JPG
            string filePath = Path.Combine(uploadsFolder, uniqueFileName);

            // 7. Resmi işle ve kaydet (ImageSharp zararlı dosyaları da filtreler)
            try
            {
                using (var image = await Image.LoadAsync(file.OpenReadStream()))
                {
                    // Resize if larger than 1920x1080 (HD)
                    int maxWidth = 1920;
                    int maxHeight = 1080;
                    
                    if (image.Width > maxWidth || image.Height > maxHeight)
                    {
                       image.Mutate(x => x.Resize(new ResizeOptions
                       {
                           Size = new Size(maxWidth, maxHeight),
                           Mode = ResizeMode.Max
                       }));
                    }

                    await image.SaveAsJpegAsync(filePath);
                }
            }
            catch (Exception ex) when (ex is not InvalidOperationException)
            {
                _logger.LogWarning("Geçersiz resim dosyası yükleme girişimi. Hata: {Error}", ex.Message);
                throw new InvalidOperationException("Yüklenen dosya geçerli bir resim değil.");
            }

            _logger.LogInformation("Dosya başarıyla yüklendi: {FileName}, Boyut: {Size}KB, Klasör: {Folder}",
                uniqueFileName, file.Length / 1024, folderName);

            return $"/uploads/{folderName}/{uniqueFileName}";
        }

        public void DeleteFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath)) return;

            // Path traversal koruması
            string cleanPath = filePath.TrimStart('/').Replace("..", "");
            string fullPath = Path.Combine(_environment.WebRootPath, cleanPath);

            // Dosyanın wwwroot/uploads altında olduğundan emin ol
            string uploadsRoot = Path.Combine(_environment.WebRootPath, "uploads");
            string resolvedPath = Path.GetFullPath(fullPath);
            string resolvedUploadsRoot = Path.GetFullPath(uploadsRoot);

            if (!resolvedPath.StartsWith(resolvedUploadsRoot, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Path traversal girişimi engellendi: {Path}", filePath);
                return;
            }

            if (File.Exists(resolvedPath))
            {
                File.Delete(resolvedPath);
                _logger.LogInformation("Dosya silindi: {Path}", resolvedPath);
            }
        }

        /// <summary>
        /// Dosyanın gerçek içeriğinin uzantısıyla eşleşip eşleşmediğini kontrol eder (Magic Bytes)
        /// </summary>
        private static async Task<bool> IsValidFileSignature(IFormFile file, string extension)
        {
            extension = extension.ToLowerInvariant();
            if (!FileSignatures.TryGetValue(extension, out var signatures))
                return false;

            using var reader = new BinaryReader(file.OpenReadStream());
            var headerBytes = reader.ReadBytes(signatures.Max(s => s.Length));
            
            // Stream'i başa sar
            file.OpenReadStream().Position = 0;

            return signatures.Any(signature =>
                headerBytes.Length >= signature.Length &&
                headerBytes.Take(signature.Length).SequenceEqual(signature));
        }

        /// <summary>
        /// Klasör adından tehlikeli karakterleri temizler
        /// </summary>
        private static string SanitizeFolderName(string folderName)
        {
            // Path traversal ve zararlı karakter temizliği
            return System.Text.RegularExpressions.Regex.Replace(
                folderName.Replace("..", "").Replace("/", "").Replace("\\", ""),
                @"[^a-zA-Z0-9_-]", "");
        }
    }
}

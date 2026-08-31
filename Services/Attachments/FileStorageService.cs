using Microsoft.AspNetCore.DataProtection;

namespace Ibtikar.Services.Attachments
{
    public class FileStorageOptions
    {
        public string Root { get; set; } = string.Empty;
    }

    public sealed class FileStorageService
    {
        private readonly FileStorageOptions _options;
        private readonly ILogger<FileStorageService> _logger;

        public FileStorageService(Microsoft.Extensions.Options.IOptions<FileStorageOptions> options, ILogger<FileStorageService> logger)
        {
            _options = options.Value;
            _logger = logger;
            EnsureRoot();
        }

        public string Root => _options.Root;

        public string BuildStoredPath(Guid ideaId, string originalFileName)
        {
            if (ideaId == Guid.Empty) throw new ArgumentException("Idea id is required.", nameof(ideaId));
            if (string.IsNullOrWhiteSpace(originalFileName)) throw new ArgumentException("File name is required.", nameof(originalFileName));
            EnsureRoot();
            var extension = Path.GetExtension(originalFileName);
            var storedFileName = Guid.NewGuid().ToString("N") + (string.IsNullOrEmpty(extension) ? ".bin" : extension.ToLowerInvariant());
            var ideaFolder = ideaId.ToString("N");
            var path = Path.Combine(_options.Root, ideaFolder, storedFileName);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            return path;
        }

        public async Task SaveAsync(string storedPath, Stream content, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(storedPath)) throw new ArgumentException("Stored path is required.", nameof(storedPath));
            if (content is null) throw new ArgumentNullException(nameof(content));
            EnsureRoot();
            await using var fs = new FileStream(storedPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, useAsync: true);
            await content.CopyToAsync(fs, ct);
            _logger.LogInformation("Stored attachment at {Path}", storedPath);
        }

        public void Delete(string storedPath)
        {
            if (string.IsNullOrWhiteSpace(storedPath)) return;
            var full = Path.GetFullPath(storedPath);
            if (!full.StartsWith(Path.GetFullPath(_options.Root), StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Refused to delete path outside root: {Path}", full);
                return;
            }
            if (File.Exists(full)) File.Delete(full);
        }

        private void EnsureRoot()
        {
            if (string.IsNullOrWhiteSpace(_options.Root))
                throw new InvalidOperationException("Integrations:AttachmentRoot is not configured.");
            Directory.CreateDirectory(_options.Root);
        }
    }
}

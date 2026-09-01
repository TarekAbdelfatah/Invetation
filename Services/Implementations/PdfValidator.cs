namespace Ibtikar.Services.Implementations
{
    public sealed class PdfValidator
    {
        private static ReadOnlySpan<byte> PdfSignature => new byte[] { 0x25, 0x50, 0x44, 0x46 }; // %PDF

        public bool IsPdf(Stream content)
        {
            if (content is null) return false;
            if (!content.CanSeek)
            {
                using var ms = new MemoryStream();
                content.CopyTo(ms);
                ms.Position = 0;
                return MatchesSignature(ms);
            }
            return MatchesSignature(content);
        }

        public static bool IsPdfHeader(ReadOnlySpan<byte> header) => header.StartsWith(PdfSignature);

        private static bool MatchesSignature(Stream s)
        {
            Span<byte> header = stackalloc byte[4];
            var pos = s.Position;
            try
            {
                s.Position = 0;
                var read = s.Read(header);
                return read == 4 && IsPdfHeader(header);
            }
            finally
            {
                s.Position = pos;
            }
        }
    }
}

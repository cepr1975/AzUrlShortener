namespace Cloud5mins.ShortenerTools.Core.Messages
{
    public class LSShortResponse
    {
        public string ShortUrl { get; set; }
        public string LongUrl { get; set; }
        public string Title { get; set; }

        public Nullable<DateTime> ExpiresAt { get; set; }//LS

        public LSShortResponse() { }
        public LSShortResponse(string host, string longUrl, string endUrl, string title, Nullable<DateTime> expiresat)
        {
            LongUrl = longUrl;
            ShortUrl = string.Concat(host, "/", endUrl);
            Title = title;
            ExpiresAt = expiresat;//LS
        }
    }
}
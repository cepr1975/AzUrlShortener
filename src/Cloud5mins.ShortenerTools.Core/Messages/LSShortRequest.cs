using Cloud5mins.ShortenerTools.Core.Domain;

namespace Cloud5mins.ShortenerTools.Core.Messages
{
    public class LSShortRequest
    {
        public string Vanity { get; set; }

        public string Url { get; set; }

        public string Title { get; set; }

        public Nullable<DateTime> ExpiresAt{ get; set; }//LS

        public Schedule[] Schedules { get; set; }
    }
}
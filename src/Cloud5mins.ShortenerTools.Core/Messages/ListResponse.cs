using System.Collections.Generic;
using Cloud5mins.ShortenerTools.Core.Domain;

namespace Cloud5mins.ShortenerTools.Core.Messages
{
    public class ListResponse
    {
        public List<ShortUrlEntity> UrlList { get; set; }

        public List<MyShortUrlEntity> LSUrlList { get; set; }//LS

        public ListResponse() { }
        public ListResponse(List<ShortUrlEntity> list)
        {
            UrlList = list;
        }

        public ListResponse(List<MyShortUrlEntity> list)
        {
            LSUrlList = list;
        }
    }
}
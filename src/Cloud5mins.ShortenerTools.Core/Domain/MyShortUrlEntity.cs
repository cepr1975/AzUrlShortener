using Microsoft.Azure.Cosmos.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Cloud5mins.ShortenerTools.Core.Domain
{
    public class MyShortUrlEntity : TableEntity
    {
        public string Url { get; set; }
        private string _activeUrl { get; set; }

        public string ActiveUrl
        {
            get
            {
                if (String.IsNullOrEmpty(_activeUrl))
                    _activeUrl = GetActiveUrl();
                return _activeUrl;
            }
        }

        public Nullable<DateTime> ExpiresAt { get; set; }

        public string Title { get; set; }

        public string ShortUrl { get; set; }

        public int Clicks { get; set; }

        public bool? IsArchived { get; set; }
        public string SchedulesPropertyRaw { get; set; }

        private List<Schedule> _schedules { get; set; }

        [IgnoreProperty]
        public List<Schedule> Schedules
        {
            get
            {
                if (_schedules == null)
                {
                    if (String.IsNullOrEmpty(SchedulesPropertyRaw))
                    {
                        _schedules = new List<Schedule>();
                    }
                    else
                    {
                        _schedules = JsonSerializer.Deserialize<Schedule[]>(SchedulesPropertyRaw).ToList<Schedule>();
                    }
                }
                return _schedules;
            }
            set
            {
                _schedules = value;
            }
        }

        public MyShortUrlEntity() { }

        public MyShortUrlEntity(string longUrl, string endUrl)
        {
            Initialize(longUrl, endUrl, string.Empty, null, null);
        }

        public MyShortUrlEntity(string longUrl, string endUrl, Nullable<DateTime> expiresat)
        {
            Initialize(longUrl, endUrl, string.Empty, expiresat, null);
        }

        public MyShortUrlEntity(string longUrl, string endUrl, Schedule[] schedules)
        {
            Initialize(longUrl, endUrl, string.Empty, null, schedules);
        }

        public MyShortUrlEntity(string longUrl, string endUrl, string title, Schedule[] schedules)
        {
            Initialize(longUrl, endUrl, title, null, schedules);
        }

        public MyShortUrlEntity(string longUrl, string endUrl, string title, Nullable<DateTime> expiresat, Schedule[] schedules)
        {
            Initialize(longUrl, endUrl, title, expiresat, schedules);
        }

        private void Initialize(string longUrl, string endUrl, string title, Nullable<DateTime> expiresat, Schedule[] schedules)
        {
            PartitionKey = endUrl.First().ToString();
            RowKey = endUrl;
            Url = longUrl;
            Title = title;
            Clicks = 0;
            IsArchived = false;
            ExpiresAt = expiresat;

            if (schedules?.Length>0)
            {
                Schedules = schedules.ToList<Schedule>();
                SchedulesPropertyRaw = JsonSerializer.Serialize<List<Schedule>>(Schedules);
            }
        }

        public static MyShortUrlEntity GetEntity(string longUrl, string endUrl, string title, Nullable<DateTime> expiresat, Schedule[] schedules)
        {
            return new MyShortUrlEntity
            {
                PartitionKey = endUrl.First().ToString(),
                RowKey = endUrl,
                Url = longUrl,
                Title = title,
                ExpiresAt=expiresat,
                Schedules = schedules.ToList<Schedule>()
            };
        }

        private string GetActiveUrl()
        {
            if (Schedules != null)
                return GetActiveUrl(DateTime.UtcNow);
            return Url;
        }
        private string GetActiveUrl(DateTime pointInTime)
        {
            var link = Url;
            var active = Schedules.Where(s =>
                s.End > pointInTime && //hasn't ended
                s.Start < pointInTime //already started
                ).OrderBy(s => s.Start); //order by start to process first link

            foreach (var sched in active.ToArray())
            {
                if (sched.IsActive(pointInTime))
                {
                    link = sched.AlternativeUrl;
                    break;
                }
            }
            return link;
        }
    }

}
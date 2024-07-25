namespace Cloud5mins.ShortenerTools.Core.Domain
{
    public class ShortenerSettings
    {
        public string DefaultRedirectUrl { get; set; }
        public string CustomDomain { get; set; }
        public string DataStorage { get; set; }

        public string ClickTimeintervalinMinutes { get; set; }

        public string MaxClicksPerPeriod { get; set; }

        public string DeleteEntitiesCreatedNNumberDaysBeforeToday { get; set; }

    }
}
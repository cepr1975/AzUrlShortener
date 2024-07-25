using Microsoft.Azure.Cosmos.Table;
using Microsoft.Azure.Documents;
using System.Net;
using System.Text.Json;
using System.Xml;

namespace Cloud5mins.ShortenerTools.Core.Domain
{
    public class StorageTableHelper
    {
        private string StorageConnectionString { get; set; }

        public StorageTableHelper() { }

        public StorageTableHelper(string storageConnectionString)
        {
            StorageConnectionString = storageConnectionString;
        }
        public CloudStorageAccount CreateStorageAccountFromConnectionString()
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(StorageConnectionString);
            return storageAccount;
        }

        private CloudTable GetTable(string tableName)
        {
            CloudStorageAccount storageAccount = CreateStorageAccountFromConnectionString();
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient(new TableClientConfiguration());
            CloudTable table = tableClient.GetTableReference(tableName);
            table.CreateIfNotExists();

            return table;
        }
        private CloudTable GetUrlsTable()
        {
            CloudTable table = GetTable("UrlsDetails");
            return table;
        }

        private CloudTable GetStatsTable()
        {
            CloudTable table = GetTable("ClickStats");
            return table;
        }

        public async Task<ShortUrlEntity> GetShortUrlEntity(ShortUrlEntity row)
        {
            TableOperation selOperation = TableOperation.Retrieve<ShortUrlEntity>(row.PartitionKey, row.RowKey);
            TableResult result = await GetUrlsTable().ExecuteAsync(selOperation);
            ShortUrlEntity eShortUrl = result.Result as ShortUrlEntity;
            return eShortUrl;
        }

        public async Task<MyShortUrlEntity> GetShortUrlEntity(MyShortUrlEntity row)
        {
            TableOperation selOperation = TableOperation.Retrieve<MyShortUrlEntity>(row.PartitionKey, row.RowKey);
            TableResult result = await GetUrlsTable().ExecuteAsync(selOperation);
            MyShortUrlEntity eShortUrl = result.Result as MyShortUrlEntity;
            return eShortUrl;
        }

        public async Task<List<ShortUrlEntity>> GetAllShortUrlEntities()
        {
            var tblUrls = GetUrlsTable();
            TableContinuationToken token = null;
            var lstShortUrl = new List<ShortUrlEntity>();
            do
            {
                // Retreiving all entities that are NOT the NextId entity 
                // (it's the only one in the partion "KEY")
                TableQuery<ShortUrlEntity> rangeQuery = new TableQuery<ShortUrlEntity>().Where(
                    filter: TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.NotEqual, "KEY"));

                var queryResult = await tblUrls.ExecuteQuerySegmentedAsync(rangeQuery, token);
                lstShortUrl.AddRange(queryResult.Results as List<ShortUrlEntity>);
                token = queryResult.ContinuationToken;
            } while (token != null);
            return lstShortUrl;
        }

        public async Task<List<MyShortUrlEntity>> LSGetAllShortUrlEntities()
        {
            var tblUrls = GetUrlsTable();
            TableContinuationToken token = null;
            var lstShortUrl = new List<MyShortUrlEntity>();
            do
            {
                // Retreiving all entities that are NOT the NextId entity 
                // (it's the only one in the partion "KEY")
                TableQuery<MyShortUrlEntity> rangeQuery = new TableQuery<MyShortUrlEntity>().Where(
                    filter: TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.NotEqual, "KEY"));

                var queryResult = await tblUrls.ExecuteQuerySegmentedAsync(rangeQuery, token);
                lstShortUrl.AddRange(queryResult.Results as List<MyShortUrlEntity>);
                token = queryResult.ContinuationToken;
            } while (token != null);
            return lstShortUrl;
        }

        /// <summary>
        /// Returns the ShortUrlEntity of the <paramref name="vanity"/>
        /// </summary>
        /// <param name="vanity"></param>
        /// <returns>ShortUrlEntity</returns>
        public async Task<ShortUrlEntity> GetShortUrlEntityByVanity(string vanity)
        {
            var tblUrls = GetUrlsTable();
            TableContinuationToken token = null;
            ShortUrlEntity shortUrlEntity = null;
            do
            {
                TableQuery<ShortUrlEntity> query = new TableQuery<ShortUrlEntity>().Where(
                    filter: TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, vanity));
                var queryResult = await tblUrls.ExecuteQuerySegmentedAsync(query, token);
                shortUrlEntity = queryResult.Results.FirstOrDefault();
            } while (token != null);

            return shortUrlEntity;
        }
        public async Task SaveClickStatsEntity(ClickStatsEntity newStats)
        {
            TableOperation insOperation = TableOperation.InsertOrMerge(newStats);
            TableResult result = await GetStatsTable().ExecuteAsync(insOperation);
        }

        public async Task<ShortUrlEntity> SaveShortUrlEntity(ShortUrlEntity newShortUrl)
        {
           
            // serializing the collection easier on json shares
            //newShortUrl.SchedulesPropertyRaw = JsonSerializer.Serialize<List<Schedule>>(newShortUrl.Schedules);

            TableOperation insOperation = TableOperation.InsertOrMerge(newShortUrl);
            TableResult result = await GetUrlsTable().ExecuteAsync(insOperation);
            ShortUrlEntity eShortUrl = result.Result as ShortUrlEntity;
            return eShortUrl;
        }

        public async Task<MyShortUrlEntity> SaveShortUrlEntity(MyShortUrlEntity newShortUrl)
        {

            // serializing the collection easier on json shares
            //newShortUrl.SchedulesPropertyRaw = JsonSerializer.Serialize<List<Schedule>>(newShortUrl.Schedules);

            TableOperation insOperation = TableOperation.InsertOrMerge(newShortUrl);
            TableResult result = await GetUrlsTable().ExecuteAsync(insOperation);
            MyShortUrlEntity eShortUrl = result.Result as MyShortUrlEntity;
            return eShortUrl;
        }

        public async Task<bool> IfShortUrlEntityExistByVanity(string vanity)
        {
            ShortUrlEntity shortUrlEntity = await GetShortUrlEntityByVanity(vanity);
            return (shortUrlEntity != null);
        }

        public async Task<bool> IfShortUrlEntityExist(ShortUrlEntity row)
        {
            ShortUrlEntity eShortUrl = await GetShortUrlEntity(row);
            return (eShortUrl != null);
        }

        public async Task<bool> IfShortUrlEntityExist(MyShortUrlEntity row)
        {
            MyShortUrlEntity eShortUrl = await GetShortUrlEntity(row);
            return (eShortUrl != null);
        }

        public async Task<int> GetNextTableId()
        {
            //Get current ID
            TableOperation selOperation = TableOperation.Retrieve<NextId>("1", "KEY");
            TableResult result = await GetUrlsTable().ExecuteAsync(selOperation);
            NextId entity = result.Result as NextId;

            if (entity == null)
            {
                entity = new NextId
                {
                    PartitionKey = "1",
                    RowKey = "KEY",
                    Id = 1024
                };
            }
            entity.Id++;

            //Update
            TableOperation updOperation = TableOperation.InsertOrMerge(entity);

            // Execute the operation.
            await GetUrlsTable().ExecuteAsync(updOperation);

            return entity.Id;
        }


        public async Task<ShortUrlEntity> UpdateShortUrlEntity(ShortUrlEntity urlEntity)
        {
            ShortUrlEntity originalUrl = await GetShortUrlEntity(urlEntity);
            originalUrl.Url = urlEntity.Url;
            originalUrl.Title = urlEntity.Title;
            originalUrl.SchedulesPropertyRaw = JsonSerializer.Serialize<List<Schedule>>(urlEntity.Schedules);

            return await SaveShortUrlEntity(originalUrl);
        }


        public async Task<List<ClickStatsEntity>> GetAllStatsByVanity(string vanity)
        {
            var tblUrls = GetStatsTable();
            TableContinuationToken token = null;
            var lstShortUrl = new List<ClickStatsEntity>();
            do
            {
                TableQuery<ClickStatsEntity> rangeQuery;

                if(string.IsNullOrEmpty(vanity)){
                    rangeQuery = new TableQuery<ClickStatsEntity>();
                }
                else{
                    rangeQuery = new TableQuery<ClickStatsEntity>().Where(
                    filter: TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, vanity));
                }

                var queryResult = await tblUrls.ExecuteQuerySegmentedAsync(rangeQuery, token);
                lstShortUrl.AddRange(queryResult.Results as List<ClickStatsEntity>);
                token = queryResult.ContinuationToken;
            } while (token != null);
            return lstShortUrl;
        }


        public async Task<ShortUrlEntity> ArchiveShortUrlEntity(ShortUrlEntity urlEntity)
        {
            ShortUrlEntity originalUrl = await GetShortUrlEntity(urlEntity);
            originalUrl.IsArchived = true;

            return await SaveShortUrlEntity(originalUrl);
        }

        public async Task<MyShortUrlEntity> ArchiveShortUrlEntity(MyShortUrlEntity urlEntity)
        {
            MyShortUrlEntity originalUrl = await GetShortUrlEntity(urlEntity);
            originalUrl.IsArchived = true;

            return await SaveShortUrlEntity(originalUrl);
        }

        /// <summary>
        /// Deletes all rows from the table
        /// </summary>
        /// <param name="tableClient">The authenticated TableClient</param>
        /// <returns></returns>
       
        public async Task<string> DeleteExpiredItemsAsync()
        {
            int TotalItens = 0;
            CloudTable Urlstable = GetUrlsTable();

            TableQuery<MyShortUrlEntity> query = new TableQuery<MyShortUrlEntity>()
            .Where(TableQuery.GenerateFilterCondition("ExpiresAt", QueryComparisons.LessThan, DateTime.UtcNow.ToString()));

            var items = Urlstable.ExecuteQuery(query);
            TotalItens = items.Count();
            
            int totalDeleted = 0;
            foreach (var row in items)
            {
                var tableOperation = TableOperation.Delete(row);
                TableResult tableResult = await Urlstable.ExecuteAsync(tableOperation);

                //if (tableResult.HttpStatusCode == (int)HttpStatusCode.OK)
                totalDeleted++;
            }

            return "Deleted " + totalDeleted + " of " + TotalItens + " items with expired date.";

        }

        public string[] CountClicksByClientIP(string partitionKey, string clientIP, int ClickTimeintervalinMinutes, int MaxClicksPerPeriod)
        {
            int TotalItens = 0;

            CloudTable Urlstable = GetStatsTable();
            DateTimeOffset GreaterThanOffset = DateTimeOffset.UtcNow.AddMinutes(ClickTimeintervalinMinutes);
            DateTimeOffset LessThanOffset = DateTimeOffset.UtcNow;

            string partitionKeyFilter  = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, partitionKey);
            string clientipFilter = TableQuery.GenerateFilterCondition("ClientIP", QueryComparisons.Equal, clientIP);
            string DateTimeOffsetValGreaterFilter = TableQuery.GenerateFilterConditionForDate("Timestamp", QueryComparisons.GreaterThan, GreaterThanOffset);
            string DateTimeOffsetValLessFilter = TableQuery.GenerateFilterConditionForDate("Timestamp", QueryComparisons.LessThan, LessThanOffset);
            string finalFilter = TableQuery.CombineFilters(TableQuery.CombineFilters(partitionKeyFilter, TableOperators.And, clientipFilter), TableOperators.And,
                TableQuery.CombineFilters(DateTimeOffsetValGreaterFilter, TableOperators.And, DateTimeOffsetValLessFilter));
             

            TableQuery<ClickStatsEntity> query = new TableQuery<ClickStatsEntity>().Where(finalFilter);

            var items = Urlstable.ExecuteQuery(query);
            TotalItens = items.Count();

            string[] response = new string[2];
            

            if (TotalItens < MaxClicksPerPeriod)
                response[1] = "AllowRedirectOK";
            else
                response[1] = "AllowRedirectNOK";

            response[0] = "#Clicks: " + TotalItens + " of partitionKey: " + partitionKey + " from clientIP : " + clientIP +
                "  between " + GreaterThanOffset.ToString() + " and " + LessThanOffset.ToString()
                + " ClickTimeintervalinMinutes (setting): " + ClickTimeintervalinMinutes + " MaxClicksPerPeriod (setting): " + MaxClicksPerPeriod + " response: " + response[1];


            return response.ToArray();
        }

        public async Task<string> CountExpiredItemsAsync(int DeleteEntitiesCreatedNNumberDaysBeforeToday)
        {
            int TotalItens = 0;
            CloudTable Urlstable = GetUrlsTable();

            DateTimeOffset dateTimeFilter = DateTime.UtcNow.AddDays(DeleteEntitiesCreatedNNumberDaysBeforeToday * -1);
            //string partitionKeyFilter = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, partitionKey);
            //string startDateFilter = TableQuery.GenerateFilterConditionForDate("Timestamp", QueryComparisons.GreaterThanOrEqual, startDate);
            //string endDateFilter = TableQuery.GenerateFilterConditionForDate("Timestamp", QueryComparisons.LessThanOrEqual, endDate);
            //string combinedFilter = TableQuery.CombineFilters(
            //    TableQuery.CombineFilters(partitionKeyFilter, TableOperators.And, startDateFilter),
            //    TableOperators.And,
            //    endDateFilter
            //);
            string Filter = TableQuery.GenerateFilterConditionForDate("Timestamp", QueryComparisons.LessThan, dateTimeFilter);
            //string Filter = TableQuery.GenerateFilterConditionForDate("ExpiresAt", QueryComparisons.LessThan, dateTimeFilter); Não reconhece como DateTime

            TableQuery<MyShortUrlEntity> query = new TableQuery<MyShortUrlEntity>().Where(Filter);

            int TotalEntities = 0;
            TableContinuationToken token = null;
            do
            {
                TableQuerySegment<MyShortUrlEntity> segment = await Urlstable.ExecuteQuerySegmentedAsync(query, token);
                token = segment.ContinuationToken;
                TotalEntities += segment.Results.Count;
            } while (token != null);

            return "#Itens: " + TotalEntities + " for the DateFilter: " + dateTimeFilter.ToString() + "| DeleteEntitiesCreatedNNumberDaysBeforeToday: " + DeleteEntitiesCreatedNNumberDaysBeforeToday;

            //return "Deleted " + totalDeleted + " of " + TotalItens + " items with expired date.";

        }
    }
}
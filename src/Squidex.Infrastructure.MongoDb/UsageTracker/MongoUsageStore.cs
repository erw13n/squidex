﻿// ==========================================================================
//  MongoUsageStore.cs
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex Group
//  All rights reserved.
// ==========================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Driver;
using Squidex.Infrastructure.UsageTracking;

namespace Squidex.Infrastructure.MongoDb.UsageTracker
{
    public sealed class MongoUsageStore : MongoRepositoryBase<MongoUsage>, IUsageStore
    {
        private static readonly UpdateOptions Upsert = new UpdateOptions { IsUpsert = true };

        public MongoUsageStore(IMongoDatabase database) 
            : base(database)
        {
        }
        
        protected override string CollectionName()
        {
            return "Usages";
        }

        protected override Task SetupCollectionAsync(IMongoCollection<MongoUsage> collection)
        {
            return collection.Indexes.CreateOneAsync(IndexKeys.Ascending(x => x.Key).Ascending(x => x.Date));
        }

        public Task TrackUsagesAsync(DateTime date, string key, long count, long elapsedMs)
        {
            return Collection.UpdateOneAsync(x => x.Key == key && x.Date == date, 
                Update
                    .Inc(x => x.TotalCount, count)
                    .Inc(x => x.TotalElapsedMs, elapsedMs)
                    .SetOnInsert(x => x.Key, key)
                    .SetOnInsert(x => x.Date, date),
                Upsert);
        }

        public async Task<IReadOnlyList<StoredUsage>> FindAsync(string key, DateTime fromDate, DateTime toDate)
        {
            var entities = await Collection.Find(x => x.Key == key && x.Date >= fromDate && x.Date <= toDate).ToListAsync();

            return entities.Select(x => new StoredUsage(x.Date, x.TotalCount, x.TotalElapsedMs)).ToList();
        }
    }
}

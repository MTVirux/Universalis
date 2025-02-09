﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Universalis.DbAccess.Queries.MarketBoard;
using Universalis.Entities.MarketBoard;

namespace Universalis.DbAccess.MarketBoard;

public class HistoryDbAccess : IHistoryDbAccess
{
    private readonly IMarketItemStore _marketItemStore;
    private readonly ISaleStore _saleStore;

    public HistoryDbAccess(IMarketItemStore marketItemStore, ISaleStore saleStore)
    {
        _marketItemStore = marketItemStore;
        _saleStore = saleStore;
    }

    public async Task Create(History document, CancellationToken cancellationToken = default)
    {
        await _marketItemStore.Insert(new MarketItem
        {
            WorldId = document.WorldId,
            ItemId = document.ItemId,
            LastUploadTime =
                DateTimeOffset.FromUnixTimeMilliseconds(Convert.ToInt64(document.LastUploadTimeUnixMilliseconds))
                    .UtcDateTime,
        }, cancellationToken);
        await _saleStore.InsertMany(document.Sales, cancellationToken);
    }

    public async Task<History> Retrieve(HistoryQuery query, CancellationToken cancellationToken = default)
    {
        var marketItem =
            await _marketItemStore.Retrieve(new MarketItemQuery { ItemId = query.ItemId, WorldId = query.WorldId },
                cancellationToken);
        if (marketItem == null)
        {
            return null;
        }

        var sales = await _saleStore.RetrieveBySaleTime(query.WorldId, query.ItemId, query.Count ?? 1000,
            cancellationToken: cancellationToken);
        return new History
        {
            WorldId = marketItem.WorldId,
            ItemId = marketItem.ItemId,
            LastUploadTimeUnixMilliseconds = new DateTimeOffset(marketItem.LastUploadTime).ToUnixTimeMilliseconds(),
            Sales = sales.ToList(),
        };
    }

    public async Task<IEnumerable<History>> RetrieveMany(HistoryManyQuery query,
        CancellationToken cancellationToken = default)
    {
        var worldItemTuples = query.WorldIds.SelectMany(worldId =>
                query.ItemIds.Select(itemId => (worldId, itemId)))
            .ToList();

        // Get upload times
        var marketItems =
            await _marketItemStore.RetrieveMany(
                new MarketItemManyQuery { ItemIds = query.ItemIds, WorldIds = query.WorldIds },
                cancellationToken);
        var marketItemsList = marketItems.ToList();
        var marketItemsDict = marketItemsList.ToDictionary(mi => (mi.WorldId, mi.ItemId), mi => mi);

        // Get sales where an upload time is known
        var sales = new Dictionary<(int, int), List<Sale>>();
        foreach (var (worldId, itemId) in worldItemTuples.Where(marketItemsDict.ContainsKey))
        {
            sales[(worldId, itemId)] = (await _saleStore.RetrieveBySaleTime(worldId, itemId, query.Count ?? 1000,
                cancellationToken: cancellationToken)).ToList();
        }

        return marketItemsList
            .Select(mi => new History
            {
                WorldId = mi.WorldId,
                ItemId = mi.ItemId,
                LastUploadTimeUnixMilliseconds = new DateTimeOffset(mi.LastUploadTime).ToUnixTimeMilliseconds(),
                Sales = sales[(mi.WorldId, mi.ItemId)].ToList(),
            });
    }

    public async Task InsertSales(IEnumerable<Sale> sales, HistoryQuery query,
        CancellationToken cancellationToken = default)
    {
        await _marketItemStore.Insert(new MarketItem
        {
            WorldId = query.WorldId,
            ItemId = query.ItemId,
            LastUploadTime = DateTime.UtcNow,
        }, cancellationToken);
        await _saleStore.InsertMany(sales, cancellationToken);
    }
}
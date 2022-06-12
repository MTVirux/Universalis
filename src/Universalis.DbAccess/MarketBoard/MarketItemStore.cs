﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;
using Universalis.Entities.MarketBoard;

namespace Universalis.DbAccess.MarketBoard;

public class MarketItemStore : IMarketItemStore
{
    private readonly string _connectionString;

    public MarketItemStore(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task Insert(MarketItem marketItem, CancellationToken cancellationToken = default)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);
        await using var command =
            new NpgsqlCommand(
                "INSERT INTO market_item (world_id, item_id, updated) VALUES ($1, $2, $3)", conn)
            {
                Parameters =
                {
                    new NpgsqlParameter { Value = marketItem.WorldId },
                    new NpgsqlParameter { Value = marketItem.ItemId },
                    new NpgsqlParameter { Value = marketItem.LastUploadTime },
                },
            };
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
    
    public async Task Update(MarketItem marketItem, CancellationToken cancellationToken = default)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);
        await using var command =
            new NpgsqlCommand(
                "UPDATE market_item SET updated = $1 WHERE world_id = $2 AND item_id = $3", conn)
            {
                Parameters =
                {
                    new NpgsqlParameter { Value = marketItem.LastUploadTime },
                    new NpgsqlParameter { Value = marketItem.WorldId },
                    new NpgsqlParameter { Value = marketItem.ItemId },
                },
            };

        if (await Retrieve(marketItem.WorldId, marketItem.ItemId, cancellationToken) == null)
        {
            await Insert(marketItem, cancellationToken);
            return;
        }
        
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<MarketItem> Retrieve(uint worldId, uint itemId, CancellationToken cancellationToken = default)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);
        
        await using var command =
            new NpgsqlCommand(
                "SELECT world_id, item_id, updated FROM market_item WHERE world_id = $1 AND item_id = $2", conn)
            {
                Parameters =
                {
                    new NpgsqlParameter { Value = Convert.ToInt32(worldId) },
                    new NpgsqlParameter { Value = Convert.ToInt32(itemId) },
                },
            };
        
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!reader.HasRows)
        {
            return null;
        }

        await reader.ReadAsync(cancellationToken);

        return new MarketItem
        {
            WorldId = Convert.ToUInt32(reader.GetInt32(0)),
            ItemId = Convert.ToUInt32(reader.GetInt32(1)),
            LastUploadTime = (DateTimeOffset)reader.GetValue(2),
        };
    }
}
using System.Data.Common;
using Wolverine.Logging;
using Wolverine.Persistence.Durability;
using Wolverine.RDBMS.Polling;
using DbCommandBuilder = Weasel.Core.DbCommandBuilder;

namespace Wolverine.RDBMS;

public interface IMessageDatabase : IMessageStore
{
    string Name { get; }

    bool IsMaster { get; }

    string SchemaName { get; set; }
    DatabaseSettings Settings { get; }

    Task StoreIncomingAsync(DbTransaction tx, Envelope[] envelopes);
    Task StoreOutgoingAsync(DbTransaction tx, Envelope[] envelopes);

    Task ReassignIncomingAsync(int ownerId, IReadOnlyList<Envelope> incoming);

    /// <summary>
    ///     Access the current count of persisted envelopes
    /// </summary>
    /// <returns></returns>
    Task<PersistedCounts> FetchCountsAsync();

    Task<IReadOnlyList<Envelope>> LoadPageOfGloballyOwnedIncomingAsync(Uri listenerAddress, int limit);


    DbConnection CreateConnection();
    DbCommandBuilder ToCommandBuilder();

    Task EnqueueAsync(IDatabaseOperation operation);
    void WriteLoadScheduledEnvelopeSql(DbCommandBuilder builder, DateTimeOffset utcNow);
}
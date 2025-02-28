using System.Data.Common;
using JasperFx.Core;
using JasperFx.Core.Reflection;
using Wolverine.Runtime.Serialization;
using DbCommandBuilder = Weasel.Core.DbCommandBuilder;

namespace Wolverine.RDBMS;

public static class DatabasePersistence
{
    public static DbCommand BuildOutgoingStorageCommand(Envelope envelope, int ownerId,
        IMessageDatabase database)
    {
        var builder = database.ToCommandBuilder();

        var owner = builder.AddNamedParameter("owner", ownerId);
        ConfigureOutgoingCommand(database, builder, envelope, owner);
        return builder.Compile();
    }

    public static DbCommand BuildOutgoingStorageCommand(Envelope[] envelopes, int ownerId,
        IMessageDatabase database)
    {
        var builder = database.ToCommandBuilder();

        var owner = builder.AddNamedParameter("owner", ownerId);

        foreach (var envelope in envelopes) ConfigureOutgoingCommand(database, builder, envelope, owner);

        return builder.Compile();
    }

    private static void ConfigureOutgoingCommand(IMessageDatabase settings, DbCommandBuilder builder, Envelope envelope,
        DbParameter owner)
    {
        var list = new List<DbParameter>();

        list.Add(builder.AddParameter(EnvelopeSerializer.Serialize(envelope)));
        list.Add(builder.AddParameter(envelope.Id));
        list.Add(owner);
        list.Add(builder.AddParameter(envelope.Destination!.ToString()));
        list.Add(builder.AddParameter(envelope.DeliverBy));

        list.Add(builder.AddParameter(envelope.Attempts));
        list.Add(builder.AddParameter(envelope.MessageType));

        var parameterList = list.Select(x => $"@{x.ParameterName}").Join(", ");

        builder.Append(
            $"insert into {settings.SchemaName}.{DatabaseConstants.OutgoingTable} ({DatabaseConstants.OutgoingFields}) values ({parameterList});");
    }

    public static DbCommand BuildIncomingStorageCommand(IEnumerable<Envelope> envelopes,
        IMessageDatabase settings)
    {
        var builder = settings.ToCommandBuilder();

        foreach (var envelope in envelopes) BuildIncomingStorageCommand(settings, builder, envelope);

        return builder.Compile();
    }

    public static void BuildIncomingStorageCommand(IMessageDatabase settings, DbCommandBuilder builder,
        Envelope envelope)
    {
        var list = new List<DbParameter>
        {
            builder.AddParameter(EnvelopeSerializer.Serialize(envelope)),
            builder.AddParameter(envelope.Id),
            builder.AddParameter(envelope.Status.ToString()),
            builder.AddParameter(envelope.OwnerId),
            builder.AddParameter(envelope.ScheduledTime),
            builder.AddParameter(envelope.Attempts),
            builder.AddParameter(envelope.MessageType),
            builder.AddParameter(envelope.Destination?.ToString())
        };


        var parameterList = list.Select(x => $"@{x.ParameterName}").Join(", ");

        builder.Append(
            $@"insert into {settings.SchemaName}.{DatabaseConstants.IncomingTable}({DatabaseConstants.IncomingFields}) values ({parameterList});");
    }

    public static async Task<Envelope> ReadIncomingAsync(DbDataReader reader, CancellationToken cancellation = default)
    {
        var body = await reader.GetFieldValueAsync<byte[]>(0, cancellation);
        var envelope = EnvelopeSerializer.Deserialize(body);
        envelope.Status = Enum.Parse<EnvelopeStatus>(await reader.GetFieldValueAsync<string>(2, cancellation));
        envelope.OwnerId = await reader.GetFieldValueAsync<int>(3, cancellation);

        if (!await reader.IsDBNullAsync(4, cancellation))
        {
            envelope.ScheduledTime = await reader.GetFieldValueAsync<DateTimeOffset>(4, cancellation);
        }

        envelope.Attempts = await reader.GetFieldValueAsync<int>(5, cancellation);

        return envelope;
    }

    public static void ConfigureDeadLetterCommands(Envelope envelope, Exception? exception, DbCommandBuilder builder,
        IMessageDatabase wolverineDatabase)
    {
        var list = new List<DbParameter>();

        list.Add(builder.AddParameter(envelope.Id));
        list.Add(builder.AddParameter(envelope.ScheduledTime));
        list.Add(builder.AddParameter(EnvelopeSerializer.Serialize(envelope)));
        list.Add(builder.AddParameter(envelope.MessageType));
        list.Add(builder.AddParameter(envelope.Destination?.ToString()));
        list.Add(builder.AddParameter(envelope.Source));
        list.Add(builder.AddParameter(exception?.GetType().FullNameInCode()));
        list.Add(builder.AddParameter(exception?.Message));
        list.Add(builder.AddParameter(envelope.SentAt.ToUniversalTime()));
        list.Add(builder.AddParameter(false));

        var parameterList = list.Select(x => $"@{x.ParameterName}").Join(", ");

        builder.Append(
            $"insert into {wolverineDatabase.SchemaName}.{DatabaseConstants.DeadLetterTable} ({DatabaseConstants.DeadLetterFields}) values ({parameterList});");
    }

    public static async Task<Envelope> ReadOutgoingAsync(DbDataReader reader, CancellationToken cancellation = default)
    {
        var body = await reader.GetFieldValueAsync<byte[]>(0, cancellation);
        var envelope = EnvelopeSerializer.Deserialize(body);
        envelope.OwnerId = await reader.GetFieldValueAsync<int>(2, cancellation);

        if (!await reader.IsDBNullAsync(4, cancellation))
        {
            envelope.DeliverBy = await reader.GetFieldValueAsync<DateTimeOffset>(4, cancellation);
        }

        envelope.Attempts = await reader.GetFieldValueAsync<int>(5, cancellation);

        return envelope;
    }
}
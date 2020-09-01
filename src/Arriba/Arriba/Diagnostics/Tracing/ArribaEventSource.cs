using System;
using Arriba.Model;
using Arriba.Model.Column;

namespace Arriba.Diagnostics.Tracing
{
    public interface ISymmetricEvent : IDisposable
    {
    }

    public interface IConsistencyEvent : IDisposable
    {
        void Failure(ExecutionDetails details);
    }

    public interface IServiceIdentity
    {
        string FriendlyServiceName { get; }
    }

    public interface IItemIdentifier
    {
        string FriendlyName { get; }
    }

    public interface ILoggingContextFactory
    {
        ILoggingContext Initialize<T>();
    }

    public interface ILoggingContext: ILoggingContextFactory
    {
        void ServiceStart<T>();
        void ServiceComplete<T>();
        void TrackFatalException(Exception ex, IServiceIdentity id);
        ISymmetricEvent TrackExecutionTime(string name);
        ISymmetricEvent TrackExecutionTime<T>(T payload);
        void UsingCachePath(string value);
        void TableMiss(string tableName);
        void TableHit(string tableName);
        ISymmetricEvent TrackSave(IServiceIdentity service);
        ISymmetricEvent LoadTable(string tableName);
        IConsistencyEvent VerifyingTableConsistencyOnSave(IServiceIdentity table);
        IConsistencyEvent VerifyingTableConsistencyOnRead(IServiceIdentity table);
        void TrackPermissionOverride();
        void ExceptionOnIndexing(ColumnDetails column, IItemIdentifier item, Exception ex);
        void SkipIndexingField(ColumnDetails c, IItemIdentifier item);
        void ProcessingComplete<T>();
        void LoadFile<T>(string filePath);
        void LastItemReadOccuredAt(DateTimeOffset previousLastChangedItem);
        void PerformIncrementalRead(DateTimeOffset start, DateTimeOffset end);
        void DownloadItems(int count);
        void TrackExceptionOnRead(Exception e, IServiceIdentity id);
        void TrakExceptionOnWrite(Exception e, IServiceIdentity id);
        void TrackExceptionOnSave(Exception e, IServiceIdentity id);
        void DuplicateInstanceDetected();
        void TokenResult(string result);
    }
}

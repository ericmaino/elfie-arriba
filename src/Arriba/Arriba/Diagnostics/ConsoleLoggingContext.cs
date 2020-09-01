using System;
using System.Diagnostics.Tracing;
using Arriba.Diagnostics.Tracing;
using Arriba.Model.Column;

namespace Arriba.Diagnostics
{
    public class ConsoleLoggingContext : ILoggingContext, ILoggingContextFactory
    {
        public void DownloadItems(int count)
        {
            throw new NotImplementedException();
        }

        public void DuplicateInstanceDetected()
        {
            throw new NotImplementedException();
        }

        public EventListener EnableEvents(EventListener listener, EventLevel level)
        {
            throw new NotImplementedException();
        }

        public void ExceptionOnIndexing(ColumnDetails column, IItemIdentifier item, Exception ex)
        {
            throw new NotImplementedException();
        }

        public ILoggingContext Initialize<T>()
        {
            throw new NotImplementedException();
        }

        public void LastItemReadOccuredAt(DateTimeOffset previousLastChangedItem)
        {
            throw new NotImplementedException();
        }

        public void LoadFile<T>(string filePath)
        {
            throw new NotImplementedException();
        }

        public ISymmetricEvent LoadTable(string tableName)
        {
            throw new NotImplementedException();
        }

        public void PerformIncrementalRead(DateTimeOffset start, DateTimeOffset end)
        {
            throw new NotImplementedException();
        }

        public void ProcessingComplete<T>()
        {
            throw new NotImplementedException();
        }

        public void ServiceComplete<T>()
        {
            throw new NotImplementedException();
        }

        public void ServiceStart<T>()
        {
            throw new NotImplementedException();
        }

        public void SkipIndexingField(ColumnDetails c, IItemIdentifier item)
        {
            throw new NotImplementedException();
        }

        public void TableHit(string tableName)
        {
            throw new NotImplementedException();
        }

        public void TableMiss(string tableName)
        {
            throw new NotImplementedException();
        }

        public void TokenResult(string result)
        {
            throw new NotImplementedException();
        }

        public void TrackExceptionOnRead(Exception e, IServiceIdentity id)
        {
            throw new NotImplementedException();
        }

        public void TrackExceptionOnSave(Exception e, IServiceIdentity id)
        {
            throw new NotImplementedException();
        }

        public ISymmetricEvent TrackExecutionTime<T>(T payload)
        {
            return new ArribaSymmetricEvent<T>(payload);
        }
        public ISymmetricEvent TrackExecutionTime(string name)
        {
            return new ArribaSymmetricEvent(name);
        }

        public void TrackFatalException(Exception ex, IServiceIdentity id)
        {
            throw new NotImplementedException();
        }

        public void TrackPermissionOverride()
        {
            throw new NotImplementedException();
        }

        public ISymmetricEvent TrackSave(IServiceIdentity service)
        {
            throw new NotImplementedException();
        }

        public void TrakExceptionOnWrite(Exception e, IServiceIdentity id)
        {
            throw new NotImplementedException();
        }

        public void UsingCachePath(string value)
        {
            throw new NotImplementedException();
        }

        public IConsistencyEvent VerifyingTableConsistencyOnRead(IServiceIdentity table)
        {
            throw new NotImplementedException();
        }

        public IConsistencyEvent VerifyingTableConsistencyOnSave(IServiceIdentity table)
        {
            throw new NotImplementedException();
        }
    }

    public class LoggingContextFactory
    {
        public static ILoggingContext CreateDefaultLoggingContext()
        {
            return new ConsoleLoggingContext();
        }
    }
}

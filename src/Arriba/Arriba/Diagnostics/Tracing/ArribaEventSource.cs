using System;
using System.Diagnostics.Tracing;
using System.Net;
using System.Threading.Tasks;
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

    public interface IEventSource
    {
        EventListener EnableEvents(EventListener listener, EventLevel level);
    }

    public interface ILoggingContext : IEventSource
    {
        void ServiceStart<T>();
        void ServiceComplete<T>();
        void TrackFatalException(Exception ex, IServiceIdentity id);
        ISymmetricEvent TrackExecutionTime<T>(T payload);
        void UsingCachePath(string value);
        void TableMiss(string tableName);
        void TableHit(string tableName);
        ISymmetricEvent TrackSave(IServiceIdentity service);
        ISymmetricEvent LoadTable(string tableName);
        IConsistencyEvent VerifyingTableConsistencyOnSave(IServiceIdentity table);
        IConsistencyEvent VerifyingTableConsistencyOnRead(IServiceIdentity table);
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
    }

    public sealed class ArribaEventSource : EventSource, ILoggingContext
    {
        private ArribaEventSource()
            : base(nameof(ArribaEventSource))
        {
        }

        [NonEvent]
        public void ServiceStart<T>()
        {
            ServiceStart(typeof(T).Name);
        }

        [NonEvent]
        public void ServiceComplete<T>()
        {
            ServiceExit(typeof(T).Name);
        }

        [NonEvent]
        public void TrackFatalException(Exception ex)
        {
            FatalException(ex);
        }

        [NonEvent]
        public EventListener EnableEvents(EventListener listener, EventLevel level)
        {
            listener.EnableEvents(this, level);
            return listener;
        }

        [Event(1, Level = EventLevel.Informational, Message = "Starting service {0}")]
        private void ServiceStart(string serviceName)
        {
            WriteEvent(1, serviceName);
        }

        [Event(2, Level = EventLevel.Informational, Message = "Completing service {0}")]
        private void ServiceExit(string serviceName)
        {
            WriteEvent(2, serviceName);
        }

        [Event(3, Level = EventLevel.Critical)]
        private void FatalException(Exception exception)
        {
            WriteEvent(3, exception.ToString());
        }

        public void TrackFatalException(Exception ex, IServiceIdentity id)
        {
            throw new NotImplementedException();
        }

        ISymmetricEvent ILoggingContext.TrackExecutionTime<T>(T payload)
        {
            throw new NotImplementedException();
        }

        public void UsingCachePath(string value)
        {
            throw new NotImplementedException();
        }

        public void TableMiss(string tableName)
        {
            throw new NotImplementedException();
        }

        public void TableHit(string tableName)
        {
            throw new NotImplementedException();
        }

        public ISymmetricEvent TrackSave(IServiceIdentity service)
        {
            throw new NotImplementedException();
        }

        public ISymmetricEvent LoadTable(string tableName)
        {
            throw new NotImplementedException();
        }

        public IConsistencyEvent VerifyingTableConsistencyOnSave(IServiceIdentity table)
        {
            throw new NotImplementedException();
        }

        public IConsistencyEvent VerifyingTableConsistencyOnRead(IServiceIdentity table)
        {
            throw new NotImplementedException();
        }

        public void ExceptionOnIndexing(ColumnDetails column, IItemIdentifier item, Exception ex)
        {
            throw new NotImplementedException();
        }

        public void SkipIndexingField(ColumnDetails c, IItemIdentifier item)
        {
            throw new NotImplementedException();
        }

        public void ProcessingComplete<T>()
        {
            throw new NotImplementedException();
        }

        public void LoadFile<T>(string filePath)
        {
            throw new NotImplementedException();
        }

        public void LastItemReadOccuredAt(DateTimeOffset previousLastChangedItem)
        {
            throw new NotImplementedException();
        }

        public void PerformIncrementalRead(DateTimeOffset start, DateTimeOffset end)
        {
            throw new NotImplementedException();
        }

        public void DownloadItems(int count)
        {
            throw new NotImplementedException();
        }

        public void TrackExceptionOnRead(Exception e, IServiceIdentity id)
        {
            throw new NotImplementedException();
        }

        public void TrakExceptionOnWrite(Exception e, IServiceIdentity id)
        {
            throw new NotImplementedException();
        }

        public void TrackExceptionOnSave(Exception e, IServiceIdentity id)
        {
            throw new NotImplementedException();
        }
    }
}

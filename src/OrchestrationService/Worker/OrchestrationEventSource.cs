using System;
using System.Diagnostics;
using System.Diagnostics.Tracing;

namespace maskx.OrchestrationService.Worker
{
    [EventSource(
        Name = "OrchestrationService-OrchestrationWorker",
        Guid = "3BDF5A80-3552-42FA-B4CA-2845677CE91B")]
    [System.Runtime.InteropServices.ComVisible(false)]
    public class OrchestrationEventSource : EventSource
    {
        private const int TraceEventId = 1;
        private const int DebugEventId = 2;
        private const int InfoEventId = 3;
        private const int WarningEventId = 4;
        private const int ErrorEventId = 5;
        private const int CriticalEventId = 6;

        /// <summary>
        ///     EventKeywords for the event source
        /// </summary>
        public static class Keywords
        {
            /// <summary>
            /// Diagnostic keyword
            /// </summary>
            public const EventKeywords Diagnostics = (EventKeywords)1L;
        }

        /// <summary>
        /// Gets the static instance of the DefaultEventSource
        /// </summary>
        public static readonly OrchestrationEventSource Log = new OrchestrationEventSource();

        private readonly string processName;

        /// <summary>
        ///     Creates a new instance of the DefaultEventSource
        /// </summary>
        private OrchestrationEventSource()
        {
            using (Process process = Process.GetCurrentProcess())
            {
                this.processName = process.ProcessName.ToLowerInvariant();
            }
        }

        /// <summary>
        /// Gets whether trace logs are enabled
        /// </summary>
        public bool IsTraceEnabled => IsEnabled(EventLevel.Verbose, Keywords.Diagnostics);

        /// <summary>
        /// Gets whether debug logs are enabled
        /// </summary>
        public bool IsDebugEnabled => IsEnabled(EventLevel.Verbose, Keywords.Diagnostics);

        /// <summary>
        /// Gets whether informational logs are enabled
        /// </summary>
        public bool IsInfoEnabled => IsEnabled(EventLevel.Informational, Keywords.Diagnostics);

        /// <summary>
        /// Gets whether warning logs are enabled
        /// </summary>
        public bool IsWarningEnabled => IsEnabled(EventLevel.Warning, Keywords.Diagnostics);

        /// <summary>
        /// Gets whether error logs are enabled
        /// </summary>
        public bool IsErrorEnabled => IsEnabled(EventLevel.Error, Keywords.Diagnostics);

        /// <summary>
        /// Gets whether critical logs are enabled
        /// </summary>
        public bool IsCriticalEnabled => IsEnabled(EventLevel.Critical, Keywords.Diagnostics);

        /// <summary>
        /// Trace an event for the supplied event type and parameters
        /// </summary>
        [NonEvent]
        public void TraceEvent(TraceEventType eventLevel, string source,  string message, string info, string eventType)
        {
            switch (eventLevel)
            {
                case TraceEventType.Critical:
                    Critical(source,message, info, eventType);
                    break;

                case TraceEventType.Error:
                    Error(source,  message, info, eventType);
                    break;

                case TraceEventType.Warning:
                    Warning(source,  message, info, eventType);
                    break;

                case TraceEventType.Information:
                    Info(source, message, info, eventType);
                    break;

                default:
                    Trace(source, message, info, eventType);
                    break;
            }
        }

        /// <summary>
        /// Trace an event for the supplied parameters
        /// </summary>
        [Event(TraceEventId, Level = EventLevel.Verbose, Keywords = Keywords.Diagnostics, Version = 3)]
        public void Trace(string source,  string message, string info, string eventType)
        {
            if (IsTraceEnabled)
            {
                WriteEventInternal(TraceEventId, source, message, info, eventType);
            }
        }

        /// <summary>
        /// Log informational event for the supplied parameters
        /// </summary>
        [Event(InfoEventId, Level = EventLevel.Informational, Keywords = Keywords.Diagnostics, Version = 3)]
        public void Info(string source,  string message, string info, string eventType)
        {
            if (IsInfoEnabled)
            {
                WriteEventInternal(InfoEventId, source,  message, info, eventType);
            }
        }

        /// <summary>
        /// Log warning event for the supplied parameters
        /// </summary>
        [Event(WarningEventId, Level = EventLevel.Warning, Keywords = Keywords.Diagnostics, Version = 3)]
        public void Warning(string source,  string message, string exception, string eventType)
        {
            if (IsWarningEnabled)
            {
                WriteEventInternal(WarningEventId, source,  message, exception, eventType);
            }
        }

        /// <summary>
        /// Log error event for the supplied parameters
        /// </summary>
        [Event(ErrorEventId, Level = EventLevel.Error, Keywords = Keywords.Diagnostics, Version = 3)]
        public void Error(string source, string message, string exception, string eventType)
        {
            if (IsErrorEnabled)
            {
                WriteEventInternal(ErrorEventId, source,  message, exception, eventType);
            }
        }

        /// <summary>
        /// Log critical event for the supplied parameters
        /// </summary>
        [Event(CriticalEventId, Level = EventLevel.Critical, Keywords = Keywords.Diagnostics, Version = 3)]
        public void Critical(string source, string message, string exception, string eventType)
        {
            if (IsCriticalEnabled)
            {
                WriteEventInternal(CriticalEventId, source,  message, exception, eventType);
            }
        }

        [NonEvent]
        private unsafe void WriteEventInternal(int eventId, string source,  string message, string info, string eventType)
        {
            source = string.Concat(source, '-', this.processName);

            MakeSafe(ref source);
            MakeSafe(ref message);
            MakeSafe(ref info);
            MakeSafe(ref eventType);

            const int EventDataCount = 4;
            fixed (char* chPtrSource = source)
            fixed (char* chPtrMessage = message)
            fixed (char* chPtrInfo = info)
            fixed (char* chPtrEventType = eventType)
            {
                EventData* data = stackalloc EventData[EventDataCount];
                data[0].DataPointer = (IntPtr)chPtrSource;
                data[0].Size = (source.Length + 1) * 2;
                data[1].DataPointer = (IntPtr)chPtrMessage;
                data[1].Size = (message.Length + 1) * 2;
                data[2].DataPointer = (IntPtr)chPtrInfo;
                data[2].Size = (info.Length + 1) * 2;
                data[3].DataPointer = (IntPtr)chPtrEventType;
                data[3].Size = (eventType.Length + 1) * 2;

                WriteEventCore(eventId, EventDataCount, data);
            }
        }

        private static void MakeSafe(ref string value)
        {
            const int MaxLength = 0x7C00; // max event size is 64k, truncating to roughly 31k chars
            value = value ?? string.Empty;

            if (value.Length > MaxLength)
            {
                value = value.Remove(MaxLength);
            }
        }
    }
}
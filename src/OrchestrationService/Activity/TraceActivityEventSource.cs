using System;
using System.Diagnostics;
using System.Diagnostics.Tracing;

namespace maskx.OrchestrationService.Activity
{
    [EventSource(
        Name = "OrchestrationService-TraceActivity",
        Guid = "7B41C5EC-A3BF-4C7D-84DD-CE0706C527B3")]
    [System.Runtime.InteropServices.ComVisible(false)]
    public class TraceActivityEventSource : EventSource
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
        public static readonly TraceActivityEventSource Log = new TraceActivityEventSource();

        private readonly string processName;

        /// <summary>
        ///     Creates a new instance of the DefaultEventSource
        /// </summary>
        private TraceActivityEventSource()
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
        public void TraceEvent(TraceEventType eventLevel, string source, string instanceId, string executionId, string message, string info, string eventType)
        {
            switch (eventLevel)
            {
                case TraceEventType.Critical:
                    Critical(source, instanceId, executionId, message, info, eventType);
                    break;

                case TraceEventType.Error:
                    Error(source, instanceId, executionId, message, info, eventType);
                    break;

                case TraceEventType.Warning:
                    Warning(source, instanceId, executionId, message, info, eventType);
                    break;

                case TraceEventType.Information:
                    Info(source, instanceId, executionId, message, info, eventType);
                    break;

                default:
                    Trace(source, instanceId, executionId, message, info, eventType);
                    break;
            }
        }

        /// <summary>
        /// Trace an event for the supplied parameters
        /// </summary>
        [Event(TraceEventId, Level = EventLevel.Verbose, Keywords = Keywords.Diagnostics, Version = 3)]
        public void Trace(string source, string instanceId, string executionId, string message, string info, string eventType)
        {
            if (IsTraceEnabled)
            {
                WriteEventInternal(TraceEventId, source, instanceId, executionId, message, info, eventType);
            }
        }

        /// <summary>
        /// Log informational event for the supplied parameters
        /// </summary>
        [Event(InfoEventId, Level = EventLevel.Informational, Keywords = Keywords.Diagnostics, Version = 3)]
        public void Info(string source, string instanceId, string executionId, string message, string info, string eventType)
        {
            if (IsInfoEnabled)
            {
                WriteEventInternal(InfoEventId, source, instanceId, executionId, message, info, eventType);
            }
        }

        /// <summary>
        /// Log warning event for the supplied parameters
        /// </summary>
        [Event(WarningEventId, Level = EventLevel.Warning, Keywords = Keywords.Diagnostics, Version = 3)]
        public void Warning(string source, string instanceId, string executionId, string message, string exception, string eventType)
        {
            if (IsWarningEnabled)
            {
                WriteEventInternal(WarningEventId, source, instanceId, executionId, message, exception, eventType);
            }
        }

        /// <summary>
        /// Log error event for the supplied parameters
        /// </summary>
        [Event(ErrorEventId, Level = EventLevel.Error, Keywords = Keywords.Diagnostics, Version = 3)]
        public void Error(string source, string instanceId, string executionId, string message, string exception, string eventType)
        {
            if (IsErrorEnabled)
            {
                WriteEventInternal(ErrorEventId, source, instanceId, executionId, message, exception, eventType);
            }
        }

        /// <summary>
        /// Log critical event for the supplied parameters
        /// </summary>
        [Event(CriticalEventId, Level = EventLevel.Critical, Keywords = Keywords.Diagnostics, Version = 3)]
        public void Critical(string source, string instanceId, string executionId, string message, string exception, string eventType)
        {
            if (IsCriticalEnabled)
            {
                WriteEventInternal(CriticalEventId, source, instanceId, executionId, message, exception, eventType);
            }
        }

        [NonEvent]
        private unsafe void WriteEventInternal(int eventId, string source, string instanceId, string executionId, string message, string info, string eventType)
        {
            source = string.Concat(source, '-', this.processName);

            MakeSafe(ref source);
            MakeSafe(ref instanceId);
            MakeSafe(ref executionId);
            MakeSafe(ref message);
            MakeSafe(ref info);
            MakeSafe(ref eventType);

            const int EventDataCount = 6;
            fixed (char* chPtrSource = source)
            fixed (char* chPtrInstanceId = instanceId)
            fixed (char* chPtrExecutionId = executionId)
            fixed (char* chPtrMessage = message)
            fixed (char* chPtrInfo = info)
            fixed (char* chPtrEventType = eventType)
            {
                EventData* data = stackalloc EventData[EventDataCount];
                data[0].DataPointer = (IntPtr)chPtrSource;
                data[0].Size = (source.Length + 1) * 2;
                data[1].DataPointer = (IntPtr)chPtrInstanceId;
                data[1].Size = (instanceId.Length + 1) * 2;
                data[2].DataPointer = (IntPtr)chPtrExecutionId;
                data[2].Size = (executionId.Length + 1) * 2;
                data[3].DataPointer = (IntPtr)chPtrMessage;
                data[3].Size = (message.Length + 1) * 2;
                data[4].DataPointer = (IntPtr)chPtrInfo;
                data[4].Size = (info.Length + 1) * 2;
                data[5].DataPointer = (IntPtr)chPtrEventType;
                data[5].Size = (eventType.Length + 1) * 2;

                // todo: use WriteEventWithRelatedActivityIdCore for correlation
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
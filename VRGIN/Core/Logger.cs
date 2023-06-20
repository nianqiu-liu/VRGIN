using System;
using System.Diagnostics;
using System.IO;

namespace VRGIN.Core
{
    public class VRLog
    {
        public static ILoggerBackend Backend { get; set; }

        protected VRLog() { }

        public static LogMode Level = LogMode.Info;
        public enum LogMode
        {
            Debug,
            Info,
            Warning,
            Error
        }

        public static void Debug(string text, params object[] args)
        {
            LogInternal(text, args, LogMode.Debug);
        }

        public static void Info(string text, params object[] args)
        {
            LogInternal(text, args, LogMode.Info);
        }

        public static void Warn(string text, params object[] args)
        {
            LogInternal(text, args, LogMode.Warning);
        }

        public static void Error(string text, params object[] args)
        {
            LogInternal(text, args, LogMode.Error);
        }

        public static void Debug(string text)
        {
            LogInternal(text, null, LogMode.Debug);
        }

        public static void Info(string text)
        {
            LogInternal(text, null, LogMode.Info);
        }

        public static void Warn(string text)
        {
            LogInternal(text, null, LogMode.Warning);
        }

        public static void Error(string text)
        {
            LogInternal(text, null, LogMode.Error);
        }

        public static void Debug(object obj)
        {
            LogInternal("{0}", new object[] { obj }, LogMode.Debug);
        }

        public static void Info(object obj)
        {
            LogInternal("{0}", new object[] { obj }, LogMode.Info);
        }

        public static void Warn(object obj)
        {
            LogInternal("{0}", new object[] { obj }, LogMode.Warning);
        }

        public static void Error(object obj)
        {
            LogInternal("{0}", new object[] { obj }, LogMode.Error);
        }

        public static void Log(string text, object[] args, LogMode severity)
        {
            // LogInternal can't be called directly by game code because stack trace would be 1 shorter
            LogInternal(text, args, severity);
        }

        private static void LogInternal(string text, object[] args, LogMode severity)
        {
            try
            {
                if (severity < Level) return;

                if (Backend == null)
                {
                    Backend = new DefaultLoggerBackend();
                }
                Backend.Log(text, args, severity);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }

    public interface ILoggerBackend
    {
        /// <summary>
        /// If args are null, text should be used as is. If args are not null, text and args should be passed through string.Format
        /// </summary>
        void Log(string text, object[] args, VRLog.LogMode severity);
    }

    /// <summary>
    /// Very simple logger.
    /// </summary>
    public class DefaultLoggerBackend : ILoggerBackend
    {
        private static readonly object _LOCK = new object();
        private static readonly string LOG_PATH = "vr.log";
        private readonly StreamWriter _Handle;

        public DefaultLoggerBackend()
        {
            _Handle = new StreamWriter(File.OpenWrite(LOG_PATH));
            _Handle.BaseStream.SetLength(0);
            _Handle.AutoFlush = true;
        }

        public void Log(string text, object[] args, VRLog.LogMode severity)
        {
            lock (_LOCK)
            {
#if COLOR_SUPPORT
                ConsoleColor foregroundColor = ConsoleColor.White;
                ConsoleColor backgroundColor = ConsoleColor.Black;

                switch (severity)
                {
                    case LogMode.Debug:
                        foregroundColor = ConsoleColor.Gray;
                        break;
                    case LogMode.Warning:
                        foregroundColor = ConsoleColor.Yellow;
                        break;
                    case LogMode.Error:
                        backgroundColor = ConsoleColor.Red;
                        break;
                }

                Console.ForegroundColor = foregroundColor;
                Console.BackgroundColor = backgroundColor;
#endif
                var logText = args == null ? text : string.Format(text, args);
                var formatted = Format(logText, severity);
                Console.WriteLine(formatted);
                _Handle.WriteLine(formatted);
#if COLOR_SUPPORT
                Console.ResetColor();
#endif
            }
        }
        private static string Format(string text, VRLog.LogMode mode)
        {
            var trace = new StackTrace(4);
            var caller = trace.GetFrame(0);
            return string.Format(@"[{0:HH':'mm':'ss}][{1}][{3}#{4}] {2}", DateTime.Now, mode.ToString().ToUpper(), text, caller.GetMethod().DeclaringType?.Name ?? "???", caller.GetMethod().Name);
        }
    }

    [Obsolete]
    public class Logger : VRLog
    {
    }
}

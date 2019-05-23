using EemRdx.Helpers;
using EemRdx.Utilities;
using System;
using System.Text;

namespace EemRdx.SessionModules
{
    public interface ILogProvider : ISessionModule
    {
        ILog DebugLog { get; }
        ILog ProfilingLog { get; }
        ILog GeneralLog { get; }

        void WriteToLog(string caller, string message, LoggingLevelEnum LoggingLevel);
        void LogError(string source, string message, Exception Scrap, LoggingLevelEnum LoggingLevel);
    }

    public class LogProviderModule : SessionModuleBase<ISessionKernel>, InitializableModule, UnloadableModule, ILogProvider
    {
        public ILog DebugLog { get; private set; }
        public ILog ProfilingLog { get; private set; }
        public ILog GeneralLog { get; private set; }

        public override string DebugModuleName => "LogProviderModule";
        public LoggingLevelEnum DefaultLoggingLevel => MySessionKernel.DebugEnabled ? LoggingLevelEnum.DebugLog : LoggingLevelEnum.GeneralLog;

        public void Init()
        {
            if (Constants.DebugMode) DebugLog = new Log(Constants.DebugLogName);
            if (Constants.EnableProfilingLog) ProfilingLog = new Log(Constants.ProfilingLogName);
            if (Constants.EnableGeneralLog) GeneralLog = new Log(Constants.GeneralLogName);
        }

        public void UnloadData()
        {
            if (Constants.DebugMode) (DebugLog as Log).Close();
            if (Constants.EnableProfilingLog) (ProfilingLog as Log).Close();
            if (Constants.EnableGeneralLog) (GeneralLog as Log).Close();
        }

        public void WriteToLog(string caller, string message, LoggingLevelEnum LoggingLevel)
        {
            string Line = $"{Log.TimeStamp}{Log.Indent}{caller}{Log.Indent}{message}";

            if (LoggingLevel.HasFlag(LoggingLevelEnum.Default))
                LoggingLevel = DefaultLoggingLevel;

            if (LoggingLevel.HasFlag(LoggingLevelEnum.DebugLog) && MySessionKernel.DebugEnabled)
            {
                if (DebugLog != null)
                    DebugLog.WriteToLog(caller, message);
                else
                {
                    VRage.Utils.MyLog.Default.WriteLine(Line);
                }
            }
            if (LoggingLevel.HasFlag(LoggingLevelEnum.ProfilingLog))
                ProfilingLog.WriteToLog(caller, message);
            if (LoggingLevel.HasFlag(LoggingLevelEnum.GeneralLog))
            {
                if (GeneralLog != null)
                    GeneralLog.WriteToLog(caller, message);
                else
                {
                    VRage.Utils.MyLog.Default.WriteLine(Line);
                }
            }
        }

        public void LogError(string source, string message, Exception Scrap, LoggingLevelEnum LoggingLevel)
        {
            string ErrorMessage = $"{Log.TimeStamp}{Log.Indent}{source}{Log.Indent}{message}\r\n{Scrap.Message}\r\n{Scrap.StackTrace}";
            if (LoggingLevel.HasFlag(LoggingLevelEnum.Default))
                LoggingLevel = DefaultLoggingLevel;

            if (LoggingLevel.HasFlag(LoggingLevelEnum.DebugLog) && MySessionKernel.DebugEnabled)
            {
                if (DebugLog != null)
                    DebugLog.LogError(source, message, Scrap);
                else
                {
                    VRage.Utils.MyLog.Default.WriteLine(ErrorMessage);
                }
            }
            if (LoggingLevel.HasFlag(LoggingLevelEnum.ProfilingLog))
                ProfilingLog.LogError(source, message, Scrap);
            if (LoggingLevel.HasFlag(LoggingLevelEnum.GeneralLog))
            {
                if (GeneralLog != null)
                    GeneralLog.LogError(source, message, Scrap);
                else
                {
                    VRage.Utils.MyLog.Default.WriteLine(ErrorMessage);
                }
            }
        }

        public LogProviderModule(ISessionKernel MySessionKernel) : base(MySessionKernel) { }
    }

    [Flags]
    public enum LoggingLevelEnum
    {
        Default,
        DebugLog,
        ProfilingLog,
        GeneralLog,
    }
}

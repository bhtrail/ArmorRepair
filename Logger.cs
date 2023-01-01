using BattleTech.UI;
using HBS.Logging;
using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace ArmorRepair
{
    internal class Logger
    {
        private static ILog logger = null;
        private static FileLogAppender logAppender = null;
        private static readonly string ModName = "ArmorRepair";

        public static void InitLoggers(string Directory)
        {
            logger = HBS.Logging.Logger.GetLogger(ModName, LogLevel.Debug);
            SetupLogging(Directory);
        }

        internal static void SetupLogging(string Directory)
        {
            var logFilePath = Path.Combine(Directory, "log.txt");

            try
            {
                ShutdownLogging();
                AddLogFileForLogger(logFilePath);
            }
            catch (Exception e)
            {
                logger.Log($"{ModName}: can't create log file", e);
            }
        }

        internal static void ShutdownLogging()
        {
            if (logAppender == null)
            {
                return;
            }

            try
            {
                HBS.Logging.Logger.ClearAppender(ModName);
                logAppender.Flush();
                logAppender.Close();
            }
            catch
            {
            }
        }

        private static void AddLogFileForLogger(string logFilePath)
        {
            try
            {
                logAppender = new FileLogAppender(logFilePath, FileLogAppender.WriteMode.INSTANT);
                HBS.Logging.Logger.AddAppender(ModName, logAppender);

            }
            catch (Exception e)
            {
                logger.Log($"{ModName}: can't create log file", e);
            }
        }

        public static void LogError(Exception ex, [CallerMemberName] string callerName = "")
        {
            logger.LogError($"[{callerName}]", ex);

            //using (StreamWriter streamWriter = new StreamWriter(path, true))
            //{
            //    streamWriter.WriteLine("(ERR) ");
            //    streamWriter.WriteLine(string.Concat(new string[]
            //    {
            //        "Message :",
            //        ex.Message,
            //        Environment.NewLine,
            //        "Stacktrace :",
            //        ex.StackTrace
            //    }) + Environment.NewLine + "Date :" + DateTime.Now.ToString());
            //    streamWriter.WriteLine(Environment.NewLine + "------------------------------------------------------------------------" + Environment.NewLine);
            //}
        }

        public static void LogInfo(string line, [CallerMemberName] string callerName = "")
        {
            logger.Log($"[{callerName}]{line}");
            //using (StreamWriter streamWriter = new StreamWriter(path, true))
            //{
            //    streamWriter.WriteLine("(NFO) " + DateTime.Now.ToString() + " - " + line);
            //}
        }

        public static void LogDebug(string line, [CallerMemberName] string callerName = "")
        {
            logger.LogDebug($"[{callerName}]{line}");
            //if(ArmorRepair.ModSettings.Debug) { 
            //    using (StreamWriter streamWriter = new StreamWriter(path, true))
            //    {
            //        streamWriter.WriteLine("(DBG) " + DateTime.Now.ToString() + " - " + line);
            //    }
            //}
        }
    }
}
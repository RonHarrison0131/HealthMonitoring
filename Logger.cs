using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HealthMonitoring
{
    public enum LogLevel
    {
        Info = 1,//正常的记录
        Warn = 2,//警告的记录
        Error = 3//异常的记录
    }

    public enum LogType
    {
        SOFT = 0, //软件
        PLC = 1,//PLC相关
        PLC_Read = 11,//PLC读相关
        PLC_Write = 12,//PLC写相关
        Camera = 2,//相机相关
        Light = 3,//光源相关
        Net = 4,//网络相关
        SQL = 5,//SQL相关
        AI = 6,//AI算法相关
        AI_Timeout = 61,//AI算法相关
        AI_Error = 62,//AI算法相关
        Algorithm = 7, //传统算法相关
        Algorithm_Timeout = 71, //传统算法相关
        Algorithm_Error = 72, //传统算法相关
        Process = 8,//流程相关
        Process_Node = 81,//流程节点
        File = 9,//文件
        Image = 91,//图片文件
        Txt = 92, //文本文件
    }

    public class Logger
    {
        public static void Debug(object message)
        {
            NLog.Logger logger = LogManager.GetLogger("debug");
            logger.Debug(message);
        }

        public static void Info(object message)
        {
            NLog.Logger logger = LogManager.GetLogger("info");
            logger.Debug(message);
        }

        public static void Warn(object message)
        {
            NLog.Logger logger = LogManager.GetLogger("warn");
            logger.Warn(message);
        }

        public static void Error(object message)
        {
            NLog.Logger logger = LogManager.GetLogger("error");
            logger.Error(message);
        }

        public static void Error(Exception ex)
        {
            NLog.Logger logger = LogManager.GetLogger("error");
            logger.Error(ex);
        }

        public static void Fatal(object message)
        {
            NLog.Logger logger = LogManager.GetLogger("fatal");
            logger.Fatal(message);
        }

        public static void SaveLog(LogLevel logLevel, LogType logType, string id, string message, Exception exception = null)
        {
            NLog.Logger logger = LogManager.GetLogger("traceMessage");
            string _message = GetMessage(logType, id, message);
            switch (logLevel)
            {
                case LogLevel.Info:
                    logger.Info(_message);
                    break;
                case LogLevel.Warn:
                    logger.Warn(_message);
                    break;
                case LogLevel.Error:
                    if (exception == null)
                    {
                        logger.Error(_message);
                    }
                    else
                    {
                        logger.Error(exception, _message);
                    }
                    break;
                default:
                    logger.Debug(_message);
                    break;
            }
        }

        public static void SaveLog(LogLevel logLevel, LogType logType, string message, Exception exception = null)
        {
            NLog.Logger logger = LogManager.GetLogger("traceMessage");
            string _message = GetMessage(logType, null, message);
            switch (logLevel)
            {
                case LogLevel.Info:
                    logger.Info(_message);
                    break;
                case LogLevel.Warn:
                    logger.Warn(_message);
                    break;
                case LogLevel.Error:
                    if (exception == null)
                    {
                        logger.Error(_message);
                    }
                    else
                    {
                        logger.Error(exception, _message);
                    }
                    break;
                default:
                    logger.Debug(_message);
                    break;
            }
        }

        public static string GetMessage(LogType logType, string id, string message)
        {
            string _title = "";
            if (id != null)
            {
                _title = $"ID:{id} ";
            }
            string _message = getLogTypeString(logType) + _title + message;
            return _message;
        }

        private static string getLogTypeString(LogType logType)
        {
            string _message = "";
            switch (logType)
            {
                case LogType.SOFT:
                    _message += $"【软件】";
                    break;
                case LogType.PLC:
                    _message += $"【PLC】";
                    break;
                case LogType.PLC_Read:
                    _message += $"【PLC】【Read】";
                    break;
                case LogType.PLC_Write:
                    _message += $"【PLC】【Write】";
                    break;
                case LogType.Camera:
                    _message += $"【相机】";
                    break;
                case LogType.Light:
                    _message += $"【光源】";
                    break;
                case LogType.Net:
                    _message += $"【网络】";
                    break;
                case LogType.SQL:
                    _message += "【数据库】";
                    break;
                case LogType.AI:
                    _message += "【AI算法】";
                    break;
                case LogType.AI_Timeout:
                    _message += "【AI算法】【Timeout】";
                    break;
                case LogType.AI_Error:
                    _message += "【AI算法】【Error】";
                    break;
                case LogType.Algorithm:
                    _message += "【传统算法】";
                    break;
                case LogType.Algorithm_Timeout:
                    _message += "【传统算法】【Timeout】";
                    break;
                case LogType.Algorithm_Error:
                    _message += "【传统算法】【Error】";
                    break;
                case LogType.Process:
                    _message += "【流程】";
                    break;
                case LogType.Process_Node:
                    _message += "【流程】【节点】";
                    break;
                case LogType.File:
                    _message += "【文件】";
                    break;
                case LogType.Image:
                    _message += "【文件】【图片】";
                    break;
                case LogType.Txt:
                    _message += "【文件】【文本】";
                    break;
                default:
                    break;
            }
            return _message;
        }
    }
}

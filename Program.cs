using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Text;
using System.IO;
using System.Globalization;

namespace OfficinaPrinter
{

    static class Program
    {
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main()
        {
            Init();

            RunNewInstance();
        }

        /// <summary>
        /// 程序初始化
        /// </summary>
        static void Init()
        {

            Global.cc = new System.Drawing.ColorConverter();

            #region 日志文件初始化

            string logpath = System.AppDomain.CurrentDomain.SetupInformation.ApplicationBase + "log";
            logpath += "\\" + DateTime.Now.ToString("yyyyMMdd");
            Global.Logger = new Log(logpath, LogType.Daily);
            if (Global.Logger != null)
            {
                Global.Logger.INFO("Init Logger Success.");
            }

            #endregion 日志文件初始化

            #region 定时删除日志文件

            //定时删除超过七天的日志文件
            System.Timers.Timer logFileTimer = new System.Timers.Timer();

            logFileTimer.Interval = 24 * 60 * 60 * 1000;//周期为一天
            logFileTimer.Elapsed += new System.Timers.ElapsedEventHandler(logFileTimer_Elapsed);
            logFileTimer.AutoReset = true;//设置是执行一次（false）还是一直执行(true)；
            logFileTimer.Enabled = true;//是否执行System.Timers.Timer.Elapsed事件；
            logFileTimer.Start();
            //启动就先执行一次清除日志
            TimingDeleteLogFile();

            #endregion 定时删除日志文件

            //读取配置参数
            ReadConfig();
        }

        /// <summary>
        /// 创建新实例
        /// </summary>
        static void RunNewInstance()
        {
            Application.EnableVisualStyles();
            //处理UI线程异常  
            Application.ThreadException += new System.Threading.ThreadExceptionEventHandler(Application_ThreadException);
            //处理非UI线程异常  
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);  
            Global.mainFaceInstance = new MainFace();
            Application.Run(Global.mainFaceInstance);
        }

        /// <summary>
        /// 读取配置文件信息
        /// </summary>
        static void ReadConfig()
        {
            string section = "ServerParams";
            string serverAddrKey = "ServerAddr";
            string portKey = "Port";
            string printerKey = "SelectedPrinter";
            string printTimeKey = "PrintTime";
            string result;
            bool setConfig = true;
            try
            {
                //读取服务器地址
                result = WinIniAPI.INIGetStringValue(Global.filePath, section, serverAddrKey, "");
                if (result != "")
                {
                    Global.svrAddr = result.ToString();
                }
                else
                {
                    Global.Logger.WARNING("未配置服务器地址");
                    setConfig = false;
                }
                //读取端口值
                result = WinIniAPI.INIGetStringValue(Global.filePath, section, portKey, "");
                if (result != "")
                {
                    Global.port = int.Parse(result);
                    Global.Logger.INFO("读取配置文件成功，ServerIp: " + Global.svrAddr + " Port: " + Global.port);
                }
                else
                {
                    Global.Logger.WARNING("未配置服务器端口");
                    setConfig = false;
                }
                //读取所选药房
                result = WinIniAPI.INIGetStringValue(Global.filePath, "RoomParam", "SelectedRoom", "");
                if (result != "")
                {
                    Global.selectRoomName = result;
                    Global.Logger.INFO("读取配置文件成功，selectRoomName: " + Global.selectRoomName);
                }
                else
                {
                    Global.Logger.WARNING("未配置所选药房名称");
                    setConfig = false;
                }
                result = WinIniAPI.INIGetStringValue(Global.filePath, "RoomParam", "SelectedRoomId", "");
                if (result != "")
                {
                    Global.selectRoomId = int.Parse(result);
                    Global.Logger.INFO("读取配置文件成功，selectRoomName: " + Global.selectRoomId.ToString());
                }
                else
                {
                    Global.Logger.WARNING("未配置所选药房ID");
                    setConfig = false;
                }
                //读取所选打印机名称
                result = WinIniAPI.INIGetStringValue(Global.filePath, "PrinterParam", printerKey, "");
                if (result != "")
                {
                    Global.selectPrinterName = result;
                    Global.Logger.INFO("读取配置文件成功，PrinterName: " + Global.selectPrinterName);
                }
                else
                {
                    Global.Logger.WARNING("未配置打印机名称");               
                }

                //读取所选打印机名称
                result = WinIniAPI.INIGetStringValue(Global.filePath, "PrinterParam", printTimeKey, "");
                if (result != "")
                {
                    Global.beginPrintTime = result;
                    Global.Logger.INFO("读取配置文件成功，PrinterName: " + Global.selectPrinterName);
                }
                else
                {
                    Global.beginPrintTime = "00:00:00";  //默认为凌晨
                    Global.Logger.WARNING("未配置开始打印时间");
                }
                if (!setConfig)
                {
                    AlertBox.Show("提示", "请完成服务器配置");
                }
            }
            catch (ArgumentException e)
            {
                Global.Logger.ERROR(e.Message);
            }

        }

        private static void logFileTimer_Elapsed(object sender, EventArgs e)
        {
            TimingDeleteLogFile();
        }

        /// <summary>
        /// 定时删除超过7天的日志文件
        /// </summary>
        private static void TimingDeleteLogFile()
        {
            string logPath = System.AppDomain.CurrentDomain.SetupInformation.ApplicationBase + "log";
            DateTime nowTime = DateTime.Now.AddDays(-7);
            if (Directory.Exists(logPath))
            {
                foreach (string content in Directory.GetFileSystemEntries(logPath))
                {

                    DateTime date = DateTime.ParseExact(content.Substring(content.Length - 8, 8), "yyyyMMdd", CultureInfo.CurrentCulture);

                    if (date < nowTime)
                    {
                        if (Directory.Exists(content))
                        {
                            Directory.Delete(content, true);
                        }
                    }
                }
            }
        }

        #region 处理未捕获异常的挂钩函数
        static void Application_ThreadException(object sender, System.Threading.ThreadExceptionEventArgs e)
        {
            Exception error = e.Exception as Exception;
            if (error != null)
            {
                Global.Logger.ERROR(string.Format("出现应用程序未处理的异常\n异常类型：{0}\n异常消息：{1}\n异常位置：{2}\n",
                    error.GetType().Name, error.Message, error.StackTrace));
            }
            else
            {
                Global.Logger.ERROR(string.Format("Application ThreadError:{0}", e));
            }
        }

        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Exception error = e.ExceptionObject as Exception;
            if (error != null)
            {
                Global.Logger.ERROR(string.Format("Application UnhandledException:{0};\n堆栈信息:{1}", error.Message, error.StackTrace));
            }
            else
            {
                Global.Logger.ERROR(string.Format("Application UnhandledError:{0}", e));
            }
        }

        #endregion  
    }
}

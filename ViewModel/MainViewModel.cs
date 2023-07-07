using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using NLog;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Management;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Collections.Generic;
using System.Windows.Documents;
using System.Linq;
using System.Runtime.InteropServices;

namespace HealthMonitoring.ViewModel
{
    public class Disk : ViewModelBase
    {
        private string diskName;

        public string DiskName
        {
            get { return diskName; }
            set { Set(ref diskName, value); }
        }

        private string diskAvailability;

        public string DiskAvailability
        {
            get { return diskAvailability; }
            set { Set(ref diskAvailability, value); }
        }
    }


    public class MainViewModel : ViewModelBase
    {
        private string ip;

        public string Ip
        {
            get { return ip; }
            set { Set(ref ip, value); }
        }

        private string cpuAvailability;

        public string CPUAvailability
        {
            get { return cpuAvailability; }
            set { Set(ref cpuAvailability, value); }
        }

        private string memoryAvailability;

        public string MemoryAvailability
        {
            get { return memoryAvailability; }
            set { Set(ref memoryAvailability, value); }
        }

        private string allDiskAvailability;

        public string ALLDiskAvailability
        {
            get { return allDiskAvailability; }
            set { Set(ref allDiskAvailability, value); }
        }

        private Disk diskDisplay;

        public Disk DiskDisplay
        {
            get { return diskDisplay; }
            set { Set(ref diskDisplay, value); }
        }


        private ObservableCollection<Disk> diskList;

        public ObservableCollection<Disk> DiskList
        {
            get { return diskList; }
            set { Set(ref diskList, value); }
        }

        [DllImport("psapi.dll")]
        static extern int EmptyWorkingSet(IntPtr hwProc);

        private bool isCancel;
        private bool isCancelForHARDW;

        public ItemsControl DiskListDisplay;
        public TextBox log;
        public Label cpu;
        public Label memory;
        public Label disk;

        public RelayCommand ListenCommand { get; set; }
        public RelayCommand CancelCommand { get; set; }
        public RelayCommand ListenForHARDWCommand { get; set; }
        public RelayCommand CancelForHARDWCommand { get; set; }
        public RelayCommand CleanMemoryCommand { get; set; }
        public RelayCommand<MainWindow> LoadCommand { get; set; }

        static CancellationTokenSource pingTask = new CancellationTokenSource();
        static CancellationTokenSource HARDWTask = new CancellationTokenSource();

        public MainViewModel()
        {
            diskList = new ObservableCollection<Disk>();
            isCancel = false;
            isCancelForHARDW = false;
            ListenCommand = new RelayCommand(listen);
            CancelCommand = new RelayCommand(cancel);
            ListenForHARDWCommand = new RelayCommand(listenForHARDW);
            CancelForHARDWCommand = new RelayCommand(cancelForHARDW);
            CleanMemoryCommand = new RelayCommand(cleanMemory);
            LoadCommand = new RelayCommand<MainWindow>(load); 
        }

        private void cleanMemory()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            Process[] processes = Process.GetProcesses();
            foreach (Process process in processes)
            {
                //以下系统进程没有权限，所以跳过，防止出错影响效率。
                if ((process.ProcessName == "System") && (process.ProcessName == "Idle"))
                    continue;
                try
                {
                    EmptyWorkingSet(process.Handle);
                }
                catch
                {

                }
            }

        }

        private void cancelForHARDW()
        {
            isCancelForHARDW = true;
            System.Threading.Thread.Sleep(1000);
            CPUAvailability = "";
            MemoryAvailability = "";
            ALLDiskAvailability = "";
            DiskList.Clear();
            HARDWTask.Cancel();
            HARDWTask.Dispose();
        }

        private void listenForHARDW()
        {
            isCancelForHARDW = false;
            CreatediskList();
            HARDWTask=new CancellationTokenSource();
            Task.Factory.StartNew(Listening, HARDWTask.Token);
        }

        private void load(MainWindow window)
        {
            log = window.log;
            DiskListDisplay = window.DiskListDisplay;
        }

        private void cancel()
        {
            isCancel = true;
            pingTask.Cancel();
            pingTask.Dispose();
        }

        private void listen()
        {
            isCancel = false;
            if (Ip == null)
            {
                MessageBox.Show("IP不能为空", "提示", MessageBoxButton.OK, MessageBoxImage.Question);
            }
            pingTask=new CancellationTokenSource();
            Task.Factory.StartNew(PingTest, pingTask.Token);
        }

        public void Listening()
        {
            try
            {
                //获取CPU
                ManagementObjectSearcher mngObjSearch = new ManagementObjectSearcher("SELECT * FROM Win32_PerfFormattedData_PerfOS_Processor WHERE Name=\"_Total\"");
                //获取内存
                ManagementObjectSearcher wmiObject = new ManagementObjectSearcher("select * from Win32_OperatingSystem");
                //获取磁盘
                ManagementObjectSearcher diskSearcher = new ManagementObjectSearcher("SELECT * FROM Win32_PerfFormattedData_PerfDisk_PhysicalDisk");
                while (!isCancelForHARDW)
                {
                    System.Threading.Thread.Sleep(1000);


                    ManagementObjectCollection mngObjColl = mngObjSearch.Get();
                    if (mngObjColl != null)
                    {
                        foreach (ManagementObject mngObject in mngObjColl)
                        {
                            CPUAvailability = (100 - Convert.ToUInt32(mngObject["PercentIdleTime"])).ToString();
                            if (Convert.ToSingle(CPUAvailability) > 80)
                            {
                                Logger.SaveLog(LogLevel.Warn, LogType.SOFT, $"CPU: {CPUAvailability}%");
                            }
                        }
                    }



                    var memoryValues = wmiObject.Get().Cast<ManagementObject>().Select(mo => new
                    {
                        FreePhysicalMemory = Double.Parse(mo["FreePhysicalMemory"].ToString()),
                        TotalVisibleMemorySize = Double.Parse(mo["TotalVisibleMemorySize"].ToString())
                    }).FirstOrDefault();

                    if (memoryValues != null)
                    {
                        var percent = ((memoryValues.TotalVisibleMemorySize - memoryValues.FreePhysicalMemory) / memoryValues.TotalVisibleMemorySize) * 100;
                        MemoryAvailability = percent.ToString("0.00");
                        if (Convert.ToSingle(MemoryAvailability) > 80)
                        {
                            Logger.SaveLog(LogLevel.Warn, LogType.SOFT, $"内存: {MemoryAvailability}%");
                        }
                    }


                    foreach (ManagementObject disk in diskSearcher.Get())
                    {

                        string diskName = disk["Name"].ToString();
                        if (diskName == "_Total")
                        {
                            float diskActivity = float.Parse(disk["DiskBytesPersec"].ToString()) / 1024 / 1024;
                            ALLDiskAvailability = diskActivity.ToString("0.00") + " MB/S";
                            if (diskActivity > 80)
                            {
                                Logger.SaveLog(LogLevel.Warn, LogType.SOFT, $"磁盘 {diskName}: {ALLDiskAvailability}");
                            }
                        }
                        else
                        {
                            diskListUpdate(disk);
                        }
                    }
                }
            }
            catch (Exception ex)
            {

                Console.WriteLine(ex.ToString());
            }
        }

        public void PingTest()
        {
            while (!isCancel)
            {
                string host = Ip;
                Ping ping = new Ping();
                PingReply reply = ping.Send(host, 10);
                if (reply.Status == IPStatus.Success)
                {
                    sendTBlog(host + "请求连接成功,往返时间：" + reply.RoundtripTime + "，TTL:" + reply.Options.Ttl);
                }
                else if (reply.Status == IPStatus.TimedOut)
                {
                    sendTBlog(host + "请求连接超时！");
                }
                else
                {
                    sendTBlog(host + "请求连接失败！");
                }
                System.Threading.Thread.Sleep(5 * 1000);
            }
        }

        private void sendTBlog(string message)
        {

            log.Dispatcher.BeginInvoke(new Action(() => { log.AppendText(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "-" + message); }));
            log.Dispatcher.BeginInvoke(new Action(() =>
            {
                log.AppendText(Environment.NewLine);
                log.ScrollToEnd();
            }));
        }

        private void CreatediskList()
        {
            ManagementObjectSearcher diskSearcher = new ManagementObjectSearcher("SELECT * FROM Win32_PerfFormattedData_PerfDisk_PhysicalDisk WHERE Name != \"_Total\"");
            foreach (ManagementObject disk in diskSearcher.Get())
            {
                Disk disk1 = new Disk();
                string diskName = disk["Name"].ToString();
                disk1.DiskName = diskName;
                disk1.DiskAvailability = "";
                DiskListDisplay.Dispatcher.BeginInvoke(new Action(() => { DiskList.Add(disk1); }));
            }
        }

        private void diskListUpdate(ManagementObject disk)
        {
            Disk disk1 = new Disk();
            DiskListDisplay.Dispatcher.BeginInvoke(new Action(() =>
            {
                string diskName = disk["Name"].ToString();
                foreach (var item in DiskList)
                {
                    if (item.DiskName == diskName)
                    {
                        item.DiskAvailability = (float.Parse(disk["DiskBytesPersec"].ToString()) / 1024 / 1024).ToString("0.00") + " MB/S";
                    }
                }
            }));
        }


    }
}
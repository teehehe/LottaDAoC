using System.Diagnostics;
using System;
using System.IO;
using System.Management;
using System.Security.Principal;
using System.Threading;
using System.Windows;

namespace LottaDAoC
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            RunThisAsAdmin();
            new Thread(WaitForProcess) { IsBackground = true, Name = "worker" }.Start();
            InitializeComponent();
        }
        private static bool IsAdministrator()
        {
            var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        private static void RunThisAsAdmin()
        {
            if (!IsAdministrator())
            {
                var exe = Process.GetCurrentProcess().MainModule.FileName;
                var startInfo = new ProcessStartInfo(exe)
                {
                    UseShellExecute = true,
                    Verb = "runas",
                    WindowStyle = ProcessWindowStyle.Normal,
                    CreateNoWindow = false
                };
                Process.Start(startInfo);
                Process.GetCurrentProcess().Kill();
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        static void WaitForProcess()
        {
            try
            {
                var startWatch = new ManagementEventWatcher(new WqlEventQuery("SELECT * FROM Win32_ProcessStartTrace"));
                startWatch.EventArrived += new EventArrivedEventHandler(startWatch_EventArrived);
                startWatch.Start();
            }
            catch (Exception ex)
            {

                MessageBox.Show(ex.ToString());
            }
        }

        static void startWatch_EventArrived(object sender, EventArrivedEventArgs e)
        {
            try
            {
                var proc = GetProcessInfo(e);
                Console.ForegroundColor = ConsoleColor.Green;
                if (proc.ProcessName.Contains("game.dll") || proc.ProcessName.Contains("game1127.dll"))
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("+ DAoC Process Started");
                    killmutex(new Process());
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        static ProcessInfo GetProcessInfo(EventArrivedEventArgs e)
        {
            var p = new ProcessInfo();
            var pid = 0;
            int.TryParse(e.NewEvent.Properties["ProcessID"].Value.ToString(), out pid);
            p.PID = pid;
            p.ProcessName = e.NewEvent.Properties["ProcessName"].Value.ToString();
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Process WHERE ProcessId = " + pid))
                using (var results = searcher.Get())
                {
                    foreach (ManagementObject result in results)
                    {
                        try
                        {
                            p.CommandLine += result["CommandLine"].ToString() + " ";
                        }
                        catch { }
                        try
                        {
                            var user = result.InvokeMethod("GetOwner", null, null);
                            p.UserDomain = user["Domain"].ToString();
                            p.UserName = user["User"].ToString();
                        }
                        catch { }
                    }
                }
                if (!string.IsNullOrEmpty(p.CommandLine))
                {
                    p.CommandLine = p.CommandLine.Trim();
                }
            }
            catch (ManagementException) { }
            return p;
        }

        private static void killmutex(Process proc2)
        {
            try
            {
                proc2.StartInfo.WorkingDirectory = Directory.GetCurrentDirectory();
                proc2.StartInfo.FileName = "killmutex.exe";
                proc2.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                proc2.StartInfo.Arguments = "\\BaseNamedObjects\\DAoCi1 \\BaseNamedObjects\\DAoCi2";
                proc2.StartInfo.UseShellExecute = true;
                proc2.StartInfo.RedirectStandardOutput = false;
                proc2.Start();
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("+ mutex killed");
            }
            catch (Exception t)
            {
                MessageBox.Show(t.Message, "Error");
            }
        }

        internal class ProcessInfo
        {
            public string ProcessName { get; set; }
            public int PID { get; set; }
            public string CommandLine { get; set; }
            public string UserName { get; set; }
            public string UserDomain { get; set; }
            public string User
            {
                get
                {
                    if (string.IsNullOrEmpty(UserName))
                    {
                        return "";
                    }
                    if (string.IsNullOrEmpty(UserDomain))
                    {
                        return UserName;
                    }
                    return string.Format("{0}\\{1}", UserDomain, UserName);
                }
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using ZenithShared;

namespace ZenithInstaller
{
    class Program
    {
        public static bool Silent = false;

        [STAThread]
        static void Main(string[] args)
        {
            try
            {
                ZenithUpdates.KillAllProcesses();

                bool reopen = false;
                string reopenArg = "";

                if (args.Length == 0)
                {
                    reopen = true;
                    NormalInstall();
                }
                else
                {
                    string command = args[0];

                    if (!new[] { "install", "update", "uninstall" }.Contains(command))
                    {
                        Console.WriteLine("Invalid command " + command);
                        return;
                    }

                    string packagePath = ZenithUpdates.DefaultUpdatePackagePath;

                    for (int i = 1; i < args.Length; i++)
                    {
                        if (args[i] == "-Silent") Silent = true;
                        if (args[i] == "-PackagePath")
                        {
                            if (command != "update")
                            {
                                Console.WriteLine("-PackagePath flag only allowed on update command");
                                return;
                            }
                            i++;
                            if (i == args.Length)
                            {
                                Console.WriteLine("path expected after -PackagePath");
                                return;
                            }
                            packagePath = args[i];
                        }
                        if (args[i] == "-ReopenArg")
                        {
                            if (command == "uninstall")
                            {
                                Console.WriteLine("-ReopenArg flag not allowed on uninstall command");
                                return;
                            }
                            i++;
                            if (i == args.Length)
                            {
                                Console.WriteLine("argument expected after -ReopenArg");
                                return;
                            }
                            reopenArg = args[i];
                        }
                        if (args[i] == "-Reopen")
                        {
                            if (command == "uninstall")
                            {
                                Console.WriteLine("-Reopen flag not allowed on uninstall command");
                                return;
                            }
                            reopen = true;
                        }
                    }

                    if (command == "install")
                    {
                        if (Silent) SilentInstall();
                        else NormalInstall();
                    }
                    if (command == "update")
                    {
                        UpdateFromPackage(packagePath);
                        ZenithUpdates.WriteVersionSettings(ZenithUpdates.GetLatestVersion(), true, true);
                    }
                    if (command == "uninstall")
                    {
                        ZenithUpdates.DeleteStartShortcut();
                        ZenithUpdates.DeleteDesktopShortcut();
                        ZenithUpdates.DeleteProgramFolder();
                        if (!Silent) MessageBox.Show("Successfully uninstalled " + ZenithUpdates.ProgramName + "!");
                    }
                }

                if (reopen)
                {
                    string exePath = Path.Combine(ZenithUpdates.InstallPath, ZenithUpdates.ExeName);
                    if (reopenArg == "") Process.Start(new ProcessStartInfo()
                    {
                        FileName = exePath,
                        WorkingDirectory = Path.GetDirectoryName(exePath)
                    });
                    else Process.Start(new ProcessStartInfo()
                    {
                        FileName = exePath,
                        WorkingDirectory = Path.GetDirectoryName(exePath),
                        Arguments = "\"" + reopenArg + "\""
                    });
                }
            }
            catch (Exception e)
            {
                if (!Silent)
                {
                    string msg = e.Message + "\n" + e.Data + "\n";
                    msg += e.StackTrace;
                    MessageBox.Show(msg, ZenithUpdates.ProgramName + " installer has crashed!");
                }
            }
        }

        static void NormalInstall()
        {
            var window = new MainWindow();
            window.ShowDialog();
            if (window.exception != null) throw window.exception;
        }

        static void SilentInstall()
        {
            Stream data;
            if (Environment.Is64BitOperatingSystem) data = ZenithUpdates.DownloadAssetData(ZenithUpdates.DataAssetName64);
            else data = ZenithUpdates.DownloadAssetData(ZenithUpdates.DataAssetName32);
            ZenithUpdates.InstallFromStream(data);
            data.Close();
            FinalizeInstall();
        }

        static void UpdateFromPackage(string path)
        {
            if (!File.Exists(path))
            {
                if (!Silent) MessageBox.Show("Could not install update, update package file missing", "Update failed");
                return;
            }
            var f = File.OpenRead(path);
            ZenithUpdates.KillAllProcesses();
            ZenithUpdates.InstallFromStream(f);
            f.Close();
            File.Delete(path);
        }

        public static void FinalizeInstall()
        {
            ZenithUpdates.WriteVersionSettings(ZenithUpdates.GetLatestVersion(), true, true);
            ZenithUpdates.CopySelfInside(ZenithUpdates.InstallerPath);
            ZenithUpdates.CreateStartShortcut();
            ZenithUpdates.CreateUninstallScript();
            ZenithUpdates.CreateDesktopShortcut();
        }
    }
}

﻿using NHM.Common;
using System;
using System.Diagnostics;
using System.Windows.Forms;

namespace NHMCore.Utils
{
    public static class RunAsAdmin
    {
        public static void SelfElevate()
        {
            try
            {
                var current = Process.GetCurrentProcess();
                Execute(current.Id, Application.ExecutablePath);
            }
            catch (Exception)
            {
            }
        }

        public static void Execute(int pid, string path)
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = @"runnhmasadmin.exe",
                    Arguments = $"{pid} {path}",
                    Verb = "runas",
                    UseShellExecute = true,
                    CreateNoWindow = true
                };
                startInfo.WindowStyle = ProcessWindowStyle.Hidden; // used for hidden window
                using (var runAsAdmin = Process.Start(startInfo))
                {
                    runAsAdmin.WaitForExit();
                }
            }
            catch (Exception ex)
            {
                Logger.Error("NICEHASH", $"RunAsAdmin error: {ex.Message}");
            }
        }
    }
}

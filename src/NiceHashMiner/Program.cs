﻿using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using System.Net;
using NHM.Common;
using NHMCore;
using NHMCore.Configs;
using NHMCore.Utils;
using NHMCore.Stats;
using log4net.Core;
using NiceHashMiner.Forms;

namespace NiceHashMiner
{
    static class Program
    {
#if TESTNET
        private static readonly string BuildTag = "TESTNET";
#elif TESTNETDEV
        private static readonly string BuildTag = "TESTNETDEV";
#else
        private static readonly string BuildTag = "PRODUCTION";
#endif

    /// <summary>
    /// The main entry point for the application.
    /// </summary>
    [STAThread]
        static void Main(string[] argv)
        {
            NHMCore.BUILD_TAG.ASSERT_COMPATIBLE_BUILDS(BuildTag);
            // Set working directory to exe
            var pathSet = false;
            var path = Path.GetDirectoryName(Application.ExecutablePath);
            if (path != null)
            {
                Paths.SetRoot(path);
                Environment.CurrentDirectory = path;
                pathSet = true;
            }

            // Add common folder to path for launched processes
            var pathVar = Environment.GetEnvironmentVariable("PATH");
            pathVar += ";" + Path.Combine(Environment.CurrentDirectory, "common");
            Environment.SetEnvironmentVariable("PATH", pathVar);


            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            // set security protocols
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls
                   | SecurityProtocolType.Tls11
                   | SecurityProtocolType.Tls12
                   | SecurityProtocolType.Ssl3;

            // #1 first initialize config
            ConfigManager.InitializeConfig();

#warning "TODO Ensure that there is only a single instance running at time. Currenly the restart is broken if we close on multiple instances"
            // #2 check if multiple instances are allowed
            if (ConfigManager.GeneralConfig.AllowMultipleInstances == false)
            {
                try
                {
                    var current = Process.GetCurrentProcess();
                    foreach (var process in Process.GetProcessesByName(current.ProcessName))
                    {
                        if (process.Id != current.Id)
                        {
                            // already running instance, return from Main
                            return;
                        }
                    }
                }
                catch { }
            }


            // TODO set logging level
            Logger.ConfigureWithFile(ConfigManager.GeneralConfig.LogToFile, Level.Info, ConfigManager.GeneralConfig.LogMaxFileSize);

            if (ConfigManager.GeneralConfig.DebugConsole)
            {
                PInvokeHelpers.AllocConsole();
                Logger.ConfigureConsoleLogging(Level.Info);
            }

            // init active display currency after config load
            ExchangeRateApi.ActiveDisplayCurrency = ConfigManager.GeneralConfig.DisplayCurrency;

            Logger.Info("NICEHASH", $"Starting up {ApplicationStateManager.Title}");

            if (!pathSet)
            {
                Logger.Info("NICEHASH", "Path not set to executable");
            }

            // check TOS
            if (ConfigManager.GeneralConfig.agreedWithTOS != ApplicationStateManager.CurrentTosVer)
            {
                Logger.Info("NICEHASH", $"TOS differs! agreed: {ConfigManager.GeneralConfig.agreedWithTOS} != Current {ApplicationStateManager.CurrentTosVer}. Showing TOS Form.");

                Application.Run(new FormEula());
                // check TOS after 
                if (ConfigManager.GeneralConfig.agreedWithTOS != ApplicationStateManager.CurrentTosVer)
                {
                    Logger.Info("NICEHASH", "TOS differs AFTER TOS confirmation FORM");
                    // TOS not confirmed return from Main
                    return;
                }
            }

            // if config created show language select
            if (string.IsNullOrEmpty(ConfigManager.GeneralConfig.Language))
            {
                if (Translations.GetAvailableLanguagesNames().Count > 1)
                {
                    Application.Run(new Form_ChooseLanguage());
                }
                else
                {
                    ConfigManager.GeneralConfig.Language = "en";
                    ConfigManager.GeneralConfigFileCommit();
                }

            }
            Translations.LanguageChanged += (s, e) => FormHelpers.TranslateAllOpenForms();
            Translations.SelectedLanguage = ConfigManager.GeneralConfig.Language;

            // if system requirements are not ensured it will fail the program
            var canRun = ApplicationStateManager.SystemRequirementsEnsured();
            if (!canRun) return;

            // 3rdparty miners TOS check if setting set
            if (ConfigManager.GeneralConfig.Use3rdPartyMinersTOS != ApplicationStateManager.CurrentTosVer)
            {
                using (var secondTOS = new Form_3rdParty_TOS())
                {
                    Application.Run(secondTOS);
                    if (!secondTOS.Accepted) return;
                }
                ConfigManager.GeneralConfigFileCommit();
            }

#warning "Login form feature is missing (only discontinued old platform supports it)"
#if false
            // if no BTC address show login/register form
            if (ConfigManager.GeneralConfig.BitcoinAddress.Trim() == "") Application.Run(new EnterBTCDialogSwitch());
#endif

            Application.Run(new Form_Main());
        }
    }
}

﻿using System;
using System.Windows.Forms;
using NHMCore;
using NHMCore.Configs;

namespace NiceHashMiner.Forms
{
    public partial class Form_ChooseLanguage : Form
    {
        public Form_ChooseLanguage()
        {
            InitializeComponent();
            CenterToScreen();
            Icon = NHMCore.Properties.Resources.logo;
            // Add language selections list
            var langs = Translations.GetAvailableLanguagesNames();

            comboBox_Languages.Items.Clear();
            foreach(var lang in langs)
            {
                comboBox_Languages.Items.Add(lang);
            }

            comboBox_Languages.SelectedIndex = 0;
        }

        private void Button_OK_Click(object sender, EventArgs e)
        {
            ConfigManager.GeneralConfig.Language = Translations.GetLanguageCodeFromIndex(comboBox_Languages.SelectedIndex);
            ConfigManager.GeneralConfigFileCommit();
            Close();
        }
    }
}

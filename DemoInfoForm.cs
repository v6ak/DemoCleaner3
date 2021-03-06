﻿using DemoCleaner3.DemoParser.parser;
using DemoCleaner3.ExtClasses;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

namespace DemoCleaner3
{
    public partial class DemoInfoForm : Form
    {
        public FileInfo demoFile = null;
        public Form1 formLink = null;

        Demo demo = null;

        FileHelper fileHelper;
        Properties.Settings prop;

        public DemoInfoForm()
        {
            InitializeComponent();
        }

        private void DemoInfoForm_Load(object sender, EventArgs e)
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));

            try {
                demo = Demo.GetDemoFromFileRaw(demoFile);
                prop = Properties.Settings.Default;
                demo.useValidation = prop.renameValidation;

                loadFriendlyConfig(dataGridView);
                textNewName.Text = demo.demoNewName;

                fileHelper = new FileHelper();
            } catch (Exception ex) {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        void loadFriendlyConfig(DataGridView grid)
        {
            if (demo == null) {
                return;
            }
            var info = demo.rawInfo;
            if (info == null) {
                return;
            }

            var frInfo = info.getFriendlyInfo();

            textNewName.Text = new FileInfo(info.demoPath).Name;

            grid.Rows.Clear();
            foreach (var cType in frInfo) {
                grid.Rows.Add();
                grid.Rows[grid.RowCount - 1].Cells[0].Value = cType.Key;

                if (cType.Key == RawInfo.keyConsole && cType.Value.Count > 500) { //reduce huge lag with console duplicates spamming
                    var newDict = new Dictionary<string, string>();
                    foreach (var cKey in cType.Value) {
                        if (!newDict.ContainsKey(cKey.Value)) {
                            newDict.Add(cKey.Value, cKey.Key);  //just put k/v in new dict by value
                            grid.Rows.Add();
                            grid.Rows[grid.RowCount - 1].Cells[1].Value = cKey.Key;
                            grid.Rows[grid.RowCount - 1].Cells[2].Value = cKey.Value;
                        }
                    }
                } else {
                    foreach (var cKey in cType.Value) {
                        grid.Rows.Add();

                        grid.Rows[grid.RowCount - 1].Cells[1].Value = cKey.Key;
                        grid.Rows[grid.RowCount - 1].Cells[2].Value = cKey.Value;
                    }
                }
            }
        }

        private void buttonRename_Click(object sender, EventArgs e)
        {
            try {
                string newPath = fileHelper.renameFile(demo.file, textNewName.Text, prop.deleteIdentical);
                if (prop.renameFixCreationTime) {
                    fileHelper.fixCreationTime(demo.file, demo.recordTime);
                }
                MessageBox.Show("File was Renamed", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            } catch (Exception ex) {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void DemoInfoForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            formLink?.BringToFront();
        }

        
        bool needToExit = false;
        private void DemoInfoForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyData == Keys.Escape && !dataGridView.IsCurrentCellInEditMode) {
                needToExit = true;
            }
        }
        private void DemoInfoForm_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyData == Keys.Escape && needToExit) {
                this.Close();
            } else {
                needToExit = false;
            }
        }
    }
}

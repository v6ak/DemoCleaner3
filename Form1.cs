﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.IO;
using System.Threading;
using DemoCleaner2.DemoParser.huffman;
using DemoCleaner2.DemoParser.parser;
using DemoCleaner2.ExtClasses;

namespace DemoCleaner2
{
    public partial class Form1 : Form
    {
        enum JobType { CLEAN, MOVE, RENAME }
        JobType job = JobType.CLEAN;

        FileHelper fileHelper;
        Properties.Settings prop;

        //Используется в случае, если на данной ос нельзя использовать прогрессбар в таскбаре
        bool _useTaskBarProgress = true;

        private void setProgress(int num)
        {
            if (_useTaskBarProgress) {
                try {
                    TaskbarProgress.SetValue(this.Handle, num, 100);
                } catch (Exception) {
                    _useTaskBarProgress = false;
                }
            }
            toolStripProgressBar1.Value = num;
        }

        FolderBrowser2 folderBrowserDialog;

        public DirectoryInfo _currentDemoPath;
        public DirectoryInfo _currentMovePath;
        public DirectoryInfo _currentBadDemosPath;
        public DirectoryInfo _currentSlowDemosPath;

        Thread backgroundThread;

        string _badDemosDirName = ".incorrectly named";
        string _slowDemosDirName = ".slow demos";
        string _moveDemosdirName = "!demos";

        public Form1()
        {
            if (Environment.OSVersion.Version.Major < 6) {
                _useTaskBarProgress = false;
            }

            fileHelper = new FileHelper((dnumber) => {
                if (toolStripProgressBar1.Value != dnumber) {
                    this.Invoke(new SetItemInt(setProgress), dnumber);
                }
            });

            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            loadSettings();
        }

        //Загрузка настроек форм
        private void loadSettings()
        {
            try {
                prop = Properties.Settings.Default;

                //main
                var dir = prop.demosFolder;
                if (string.IsNullOrEmpty(dir)) {
                    dir = Application.ExecutablePath.Substring(0,
                        Application.ExecutablePath.Length
                        - Path.GetFileName(Application.ExecutablePath).Length);
                }

                _currentDemoPath = new DirectoryInfo(dir);

                textBoxDemosFolder.Text = dir;
                textBoxDemosFolder.SelectionStart = textBoxBadDemos.Text.Length;
                textBoxDemosFolder.ScrollToCaret();
                textBoxDemosFolder.Refresh();

                tabControl1.SelectedIndex = prop.tabSelectedIndex;

                //clean
                setRadioFromInt(prop.cleanOption,radioBestTimeOfEachPlayer,radioBestTimesOnMap);
                setRadioFromInt(prop.slowDemosOption,radioButtonDeleteSlow,radioButtonSkipSlow,radioButtonMoveSlow);
                textBoxSlowDemos.Enabled = radioButtonMoveSlow.Checked;
                buttonSlowDemosBrowse.Enabled = radioButtonMoveSlow.Checked;
                checkBoxProcessMdf.Checked = prop.processMdf;
                groupBoxSplit.Enabled = checkBoxSplitFolders.Checked;
                numericUpDownCountOfBest.Value = (decimal)prop.countOfBestDemos;

                //move
                numericUpDownMaxFiles.Value = prop.maxFiles;
                numericUpDownMaxFolders.Value = prop.maxFolders;
                checkBoxMoveOnlyYour.Checked = prop.moveOnlyYourTimes;
                groupBoxName.Enabled = checkBoxMoveOnlyYour.Checked;
                textBoxYourName.Text = prop.yourName;
                checkBoxSplitFolders.Checked = prop.splitBySmallFolders;
                checkBoxUseSubfolders.Checked = prop.useSubFolders;

                if (!string.IsNullOrEmpty(prop.moveDemoFolder)) {
                    textBoxMoveDemosFolder.Text = prop.moveDemoFolder;
                }
                if (!string.IsNullOrEmpty(prop.slowDemoFolder)) {
                    textBoxSlowDemos.Text = prop.slowDemoFolder;
                }
                if (!string.IsNullOrEmpty(prop.badDemoFolder)) {
                    textBoxBadDemos.Text = prop.badDemoFolder;
                }


                //rename
                setRadioFromInt(prop.renameOption, radioRenameBad, radioRenameAll);
                checkBoxFixCreationTime.Checked = prop.renameFixCreationTime;
                checkBoxRulesValidation.Checked = prop.renameValidation;
                checkBoxAddSign.Checked = prop.renameAddSign;
                openFileDialog1.InitialDirectory = dir;

                //additional
                checkBoxDeleteEmptyDirs.Checked = prop.deleteEmptyDirs;
                setRadioFromInt(prop.badDemosOption,
                    radioButtonDeleteBad,
                    radioButtonSkipBad,
                    radioButtonMoveBad);

                textBoxBadDemos.Enabled = radioButtonMoveBad.Checked;
                buttonBadDemosBrowse.Enabled = radioButtonMoveBad.Checked;
                checkBoxDeleteIdentical.Checked = prop.deleteIdentical;
            } catch (Exception) {

            }
        }

        //включаем одну радио кнопку из переданного списка
        private void setRadioFromInt(int check, params RadioButton[] radio)
        {
            for (int i = 0; i < radio.Length; i++) {
                radio[i].Checked = i == check;
            }
        }

        //Получаем интовое значение из массива булевых
        private int getIntFromParameters(params RadioButton[] t)
        {
            return t.TakeWhile(x => !x.Checked).Count();
        }

        //Сохранение настроек
        private void SaveSettings()
        {
            var prop = Properties.Settings.Default;

            //main
            prop.demosFolder = _currentDemoPath.FullName;
            prop.tabSelectedIndex = tabControl1.SelectedIndex;

            //clean
            prop.cleanOption = getIntFromParameters(radioBestTimeOfEachPlayer, radioBestTimesOnMap);
            prop.slowDemosOption = getIntFromParameters(radioButtonDeleteSlow, radioButtonSkipSlow, radioButtonMoveSlow);
            prop.useSubFolders = checkBoxUseSubfolders.Checked;
            prop.processMdf = checkBoxProcessMdf.Checked;
            prop.countOfBestDemos = (int)numericUpDownCountOfBest.Value;

            //move
            prop.splitBySmallFolders = checkBoxSplitFolders.Checked;
            prop.maxFiles = decimal.ToInt32(numericUpDownMaxFiles.Value);
            prop.maxFolders = decimal.ToInt32(numericUpDownMaxFolders.Value);
            prop.moveOnlyYourTimes = checkBoxMoveOnlyYour.Checked;
            prop.yourName = textBoxYourName.Text;

            prop.moveDemoFolder = _currentMovePath?.FullName ?? "";
            //prop.moveDemoFolder = _currentMovePath != null ? _currentMovePath.FullName : "";
            prop.badDemoFolder = _currentBadDemosPath != null ? _currentBadDemosPath.FullName : "";
            prop.slowDemoFolder = _currentSlowDemosPath != null ? _currentSlowDemosPath.FullName : "";

            //rename
            prop.renameOption = getIntFromParameters(radioRenameBad, radioRenameAll);
            prop.renameFixCreationTime = checkBoxFixCreationTime.Checked;
            prop.renameValidation = checkBoxRulesValidation.Checked;
            prop.renameAddSign = checkBoxAddSign.Checked;

            //additional
            prop.badDemosOption = getIntFromParameters(radioButtonDeleteBad, radioButtonSkipBad, radioButtonMoveBad);
            prop.deleteEmptyDirs = checkBoxDeleteEmptyDirs.Checked;
            prop.deleteIdentical = checkBoxDeleteIdentical.Checked;
            
            prop.Save();
        }

        private void checkBoxSplitFolders_CheckedChanged(object sender, EventArgs e)
        {
            var ch = (sender as CheckBox).Checked;
            groupBoxSplit.Enabled = ch;
        }

        private void checkBoxMoveOnlyYoyr_CheckedChanged(object sender, EventArgs e)
        {
            var ch = (sender as CheckBox).Checked;
            groupBoxName.Enabled = ch;
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            SaveSettings();

            if (backgroundThread != null && backgroundThread.IsAlive) {
                backgroundThread.Abort();
            }
        }


        //Показываем диалог выбора папки назначения и возвращаем путь
        private void ShowFolderBrowserDialog(string Title, ref DirectoryInfo path, Action action)
        {
            if (!path.Exists)
                path = path.Parent;

            if (folderBrowserDialog == null)
                folderBrowserDialog = new FolderBrowser2();
            folderBrowserDialog.Title = Title;
            folderBrowserDialog.DirectoryPath = path.FullName;

            if (folderBrowserDialog.ShowDialog(this) == DialogResult.OK && folderBrowserDialog.DirectoryPath.Length > 0) {
                path = new DirectoryInfo(folderBrowserDialog.DirectoryPath);
                action.Invoke();
            }
        }

        //По изменении основной папки с демками, меняем все остальные пути
        private void textBoxDemosFolder_TextChanged(object sender, EventArgs e)
        {
            var text = (sender as TextBox).Text;
            if (text.Length > 0) {
                var folder = new DirectoryInfo(text);
                if (folder.Exists) {
                    _currentDemoPath = folder;

                    textBoxBadDemos.Text = Path.Combine(_currentDemoPath.FullName, _badDemosDirName);
                    textBoxBadDemos.SelectionStart = textBoxBadDemos.Text.Length;
                    textBoxBadDemos.ScrollToCaret();
                    textBoxBadDemos.Refresh();

                    textBoxSlowDemos.Text = Path.Combine(_currentDemoPath.FullName, _slowDemosDirName);
                    textBoxSlowDemos.SelectionStart = textBoxSlowDemos.Text.Length;
                    textBoxSlowDemos.ScrollToCaret();
                    textBoxSlowDemos.Refresh();

                    textBoxMoveDemosFolder.Text = Path.Combine(_currentDemoPath.FullName, _moveDemosdirName);
                    textBoxMoveDemosFolder.SelectionStart = textBoxBadDemos.Text.Length;
                    textBoxMoveDemosFolder.ScrollToCaret();
                    textBoxMoveDemosFolder.Refresh();
                }
            }
        }

        //записываем путь медленных демок
        private void textBoxSlowDemos_TextChanged(object sender, EventArgs e)
        {
            var text = (sender as TextBox).Text;
            if (text.Length > 0) {
                var folder = new DirectoryInfo(text);
                _currentSlowDemosPath = folder;
            }
        }

        //записываем путь перемещения демок
        private void textBoxMoveDemosFolder_TextChanged(object sender, EventArgs e)
        {
            var text = (sender as TextBox).Text;
            if (text.Length > 0) {
                try {
                    var folder = new DirectoryInfo(text);
                    _currentMovePath = folder;
                } catch (Exception ex) {
                    
                }
            }
        }

        //записываем путь плохих демок
        private void textBoxBadDemos_TextChanged(object sender, EventArgs e)
        {
            var text = (sender as TextBox).Text;
            if (text.Length > 0) {
                var folder = new DirectoryInfo(text);
                _currentBadDemosPath = folder;
            }
        }

        //Диалог выбора основной папки с демками
        private void buttonBrowseDemos_Click(object sender, EventArgs e)
        {
            ShowFolderBrowserDialog("Choose demos directory", ref _currentDemoPath, () => {
                textBoxDemosFolder.Text = _currentDemoPath.FullName;
            });
        }

        //Диалог выбора папки с перемещаемыми демками
        private void buttonBrowseWhereMove_Click(object sender, EventArgs e)
        {
            ShowFolderBrowserDialog("Choose demos directory", ref _currentMovePath, () => {
                textBoxMoveDemosFolder.Text = _currentMovePath.FullName;
            });
        }

        //Диалог выбора папки с плохими демками
        private void buttonBadDemosBrowse_Click(object sender, EventArgs e)
        {
            ShowFolderBrowserDialog("Choose bad demos directory", ref _currentBadDemosPath, () => {
                textBoxBadDemos.Text = _currentBadDemosPath.FullName;
            });
        }

        //Диалог выбора папки с медленными демками
        private void buttonSlowDemosBrowse_Click(object sender, EventArgs e)
        {
            ShowFolderBrowserDialog("Choose slow demos directory", ref _currentSlowDemosPath, () => {
                textBoxSlowDemos.Text = _currentSlowDemosPath.FullName;
            });
        }

        delegate void SetItemInt(int num);

        delegate void SetItem(bool num);
        //выключаем доступ к элементам в процессе работы потока (а потом вкючаем)
        private void SetButtonCallBack(bool enabled)
        {
            tabControl1.Enabled = enabled;
            groupBox1.Enabled = enabled;
            toolStripProgressBar1.Visible = !enabled;
            if (enabled)
                toolStripStatusLabel1.Text = "Done!";
        }

        private void runBackgroundThread() {
            DirectoryInfo demosFolder = null;
            DirectoryInfo dirdemos = null;

            try {
                demosFolder = new DirectoryInfo(textBoxDemosFolder.Text);
            } catch (Exception) {}

            if (demosFolder == null || !demosFolder.Exists) {
                MessageBox.Show("Directory does not exist\n\n" + textBoxDemosFolder.Text, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            SaveSettings();
            SetButtonCallBack(false);
            switch (job) {
                case JobType.CLEAN: toolStripStatusLabel1.Text = "Cleaning..."; break;
                case JobType.MOVE: toolStripStatusLabel1.Text = "Moving..."; break;
                case JobType.RENAME: toolStripStatusLabel1.Text = "Renaming..."; break;
            }

            if (job == JobType.MOVE) {
                dirdemos = new DirectoryInfo(textBoxMoveDemosFolder.Text);
            }

            //Запускаем поток, в котором всё будем обрабатывать
            backgroundThread = new Thread(delegate () {
                try {
                    switch (job) {
                        case JobType.CLEAN: clean(_currentDemoPath); break;
                        case JobType.MOVE: moveDemos(demosFolder, dirdemos); break;
                        case JobType.RENAME: runRename(_currentDemoPath); break;
                    }

                    if (checkBoxDeleteEmptyDirs.Checked) {
                        fileHelper.deleteEmpty(demosFolder);
                    }

                    this.Invoke(new SetItem(SetButtonCallBack), true);
                    this.Invoke(new SetItemInt(showEndMessage), job);
                } catch (Exception ex) {
                    this.Invoke(new SetItem(SetButtonCallBack), true);
                    MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            });
            backgroundThread.Start();
        }

        //Обработка нажатия кнопки чистки демок
        private void buttonClean_Click(object sender, EventArgs e)
        {
            job = JobType.CLEAN;
            runBackgroundThread();
        }

        //форматирование текста месседжбокеса
        private string getMessageText(decimal counter, string text)
        {
            if (counter > 0) {
                var s = counter > 1 ? "s" : string.Empty;
                text = string.Format(text, counter, s);
                return text;
            }
            return string.Empty;
        }

        //вывод сообщения об окончании работы
        private void showEndMessage(int jobType)
        {
            string text = "";
            switch (jobType) {
                case (int)JobType.CLEAN: text = "Cleaning"; break;
                case (int)JobType.MOVE: text = "Moving"; break;
                case (int)JobType.RENAME: text = "Renaming"; break;
            }
            text += " demos finished\n";

            if (jobType == (int)JobType.RENAME) {
                text += getMessageText(fileHelper._countRenameFiles, "\n{0} file{1} were renamed");
                text += getMessageText(fileHelper._countDeleteFiles, "\n{0} file{1} were deleted");
            } else {
                text += getMessageText(fileHelper._countMoveFiles, "\n{0} file{1} were moved");
                text += getMessageText(fileHelper._countDeleteFiles, "\n{0} file{1} were deleted");
                text += getMessageText(fileHelper._countCreateDir, "\n{0} folder{1} were created");
                text += getMessageText(fileHelper._countDeleteDir, "\n{0} folder{1} were deleted");
            }

            if (_useTaskBarProgress) {
                try {
                    TaskbarProgress.SetState(this.Handle, TaskbarProgress.TaskbarStates.NoProgress);
                } catch (Exception) {
                    _useTaskBarProgress = false;
                }
            }

            MessageBox.Show(this, text, "Done", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        //Конвертация цифры в число с учётом количества знаков
        string numberToString(int number, int length)
        {
            var str = number.ToString();
            while (str.Length < length) {
                str = "0" + str;
            }
            return str;
        }

        //Обрабатываем плохие демки
        private void operateBadDemos(IEnumerable<Demo> demos)
        {
            if (radioButtonSkipBad.Checked) {
                return;
            }

            var badDemos = demos.Where(x => x.hasError == true);//.OrderBy(x => x.file.Name);

            var count = badDemos.Count();

            if (count > 0) {
                if (radioButtonDeleteBad.Checked) {
                    //Или всё удаляем
                    foreach (var item in badDemos) {
                        fileHelper.deleteCheckRules(item.file);
                    }
                }
                if (radioButtonMoveBad.Checked) {
                    //или перемещавем по подкатегориям (0-10)
                    _currentBadDemosPath = new DirectoryInfo(textBoxBadDemos.Text);
                    if (!_currentBadDemosPath.Exists)
                        _currentBadDemosPath.Create();

                    var max = (int)numericUpDownMaxFiles.Value;
                    var maxDirlength = (count / max).ToString().Length;

                    if (count > max) {
                        var ordered = badDemos.OrderBy(x => x.file.Name, StringComparer.OrdinalIgnoreCase).ToList();
                        for (int i = 0; i < count; i++) {
                            var fn = Path.Combine(_currentBadDemosPath.FullName, numberToString((i / max) + 1, maxDirlength));
                            fileHelper.moveFile(ordered[i].file, new DirectoryInfo(fn), checkBoxDeleteIdentical.Checked);
                        }
                    } else {
                        foreach (var item in badDemos) {
                            fileHelper.moveFile(item.file, _currentBadDemosPath, checkBoxDeleteIdentical.Checked);
                        }
                    }
                }
            }
        }

        //Чистим демки!
        private void clean(DirectoryInfo filedemos)
        {
            var files = filedemos.GetFiles("*.dm_??", checkBoxUseSubfolders.Checked ?
                SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);

            fileHelper.resetValues(files.Length);

            var demos = files.Select(x => Demo.GetDemoFromFile(x));

            operateBadDemos(demos);

            if (radioButtonSkipSlow.Checked) {
                return;
            }
            _currentSlowDemosPath.Refresh();

            var goodDemos = demos.Where(x => x.hasError == false);

            IEnumerable<IGrouping<string, Demo>> groups = null;

            if (radioBestTimeOfEachPlayer.Checked) {
                groups = goodDemos.GroupBy(
                    x => (x.mapName
                    + Demo.mdfToDf(x.modphysic, checkBoxProcessMdf.Checked)
                    + x.playerName).ToLower());
            }
            if (radioBestTimesOnMap.Checked) {
                groups = goodDemos.GroupBy(x => (
                    x.mapName + Demo.mdfToDf(x.modphysic, checkBoxProcessMdf.Checked)).ToLower());
            }

            foreach (var group in groups) {
                //var bestTime = group.Min(y => y.time);

                var ordered = group.OrderBy(x => x.time).ToList();

                var slow = ordered.Skip((int)numericUpDownCountOfBest.Value);

                foreach (var demo in slow) {
                    if (radioButtonDeleteSlow.Checked) {
                        fileHelper.deleteCheckRules(demo.file);
                    } else {
                        if (radioButtonMoveSlow.Checked) {
                            fileHelper.moveFile(demo.file, _currentSlowDemosPath, checkBoxDeleteIdentical.Checked);
                        }
                    }
                }
            }
        }

        //Включаем и выключаем поля ввода, при выборе радио кнопки
        private void radioButtonMoveSlow_CheckedChanged(object sender, EventArgs e)
        {
            textBoxSlowDemos.Enabled = radioButtonMoveSlow.Checked;
            buttonSlowDemosBrowse.Enabled = radioButtonMoveSlow.Checked;
        }

        //Включаем и выключаем поля ввода, при выборе радио кнопки
        private void radioButtonMoveBad_CheckedChanged(object sender, EventArgs e)
        {
            textBoxBadDemos.Enabled = radioButtonMoveBad.Checked;
            buttonBadDemosBrowse.Enabled = radioButtonMoveBad.Checked;
        }

        //Обработка нажатия кнопки перемещения демок
        private void buttonMove_Click(object sender, EventArgs e)
        {
            job = JobType.MOVE;
            runBackgroundThread();
        }

        //Группируем файлы
        private IEnumerable<IGrouping<string, Demo>> GroupFiles(IEnumerable<Demo> files, int indexInside)
        {
            var groups = files.GroupBy(x => x.file.Name.Substring(0, indexInside + 1).ToLower()).ToList();

            for (int i = 0; i < groups.Count; i++) {
                var group = groups.ElementAt(i);
                if (group.Count() > numericUpDownMaxFiles.Value) {
                    //Рекурсивный вызов этого же метода группировки
                    var subGroupFiles = GroupFiles(group, indexInside + 1);

                    groups.RemoveAt(i);
                    groups.InsertRange(i, subGroupFiles);
                    i--;
                }
            }

            return groups;
        }


        //Перемещаем демки
        private bool moveDemos(DirectoryInfo filedemos, DirectoryInfo dirdemos)
        {
            //из всех файлов выбираем только демки
            var files = filedemos.GetFiles("*.dm_??", checkBoxUseSubfolders.Checked ?
                SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);

            fileHelper.resetValues(files.Length);

            var demos = files.Select(x => Demo.GetDemoFromFile(x));

            //Отбираем бракованые файлы
            operateBadDemos(demos);

            var goodFiles = demos.Where(x => x.hasError == false);

            //ищем файлы c именем игрока, если имя вписано
            if (checkBoxMoveOnlyYour.Checked && textBoxYourName.Text.Length > 0) {
                goodFiles = goodFiles.Where(x => x.playerName.ToLower().Contains(textBoxYourName.Text.ToLower()));
            }

            //Создаём основную папку
            if (goodFiles.Count() > 0) {
                if (!dirdemos.Exists) {
                    dirdemos.Create();
                    fileHelper._countCreateDir++;
                }
            }

            var indexInside = 0;
            //группируем все файлы, key = название папки
            var groupedFiles = GroupFiles(goodFiles, indexInside);

            var onlyDirNames = groupedFiles.Select(x => new DemoFolder(x.Key));

            indexInside = 0;
            //Группируем все названия папок и там же получаем полные пути к ним
            var groupedFolders = groupFolders(onlyDirNames, indexInside);

            var ListFolders = Ext.MakeListFromGroups(groupedFolders);

            //Проходим по всем файлам и перемещаем их в каталоги
            for (int i = 0; i < groupedFiles.Count(); i++) {
                var group = groupedFiles.ElementAt(i);

                for (int j = 0; j < group.Count(); j++) {
                    var demo = group.ElementAt(j);

                    var folderName = ListFolders.First(x => x.Value._folderName == group.Key).Value.fullFolderName;

                    var newPath = Path.Combine(dirdemos.FullName, folderName);

                    if (!checkBoxSplitFolders.Checked) {
                        newPath = dirdemos.FullName;
                    }

                    fileHelper.moveFile(demo.file, new DirectoryInfo(newPath), checkBoxDeleteIdentical.Checked);
                }
            }
            return true;
        }


        //группировка папок
        private IEnumerable<IGrouping<string, DemoFolder>> groupFolders(IEnumerable<DemoFolder> folders, int indexInside)
        {
            var groups = folders.GroupBy(x => DemoFolder.GetKeyFromIndex(x._folderName, indexInside)).ToList();

            for (int i = 0; i < groups.Count; i++) {
                var group = groups.ElementAt(i);
                if (group.Count() > numericUpDownMaxFolders.Value) {
                    //Рекурсивный вызов этого же метода группировки
                    var subGroupFolders = groupFolders(group, indexInside + 1);

                    groups.RemoveAt(i);
                    groups.InsertRange(i, subGroupFolders);
                    i--;
                } else {
                    foreach (var item in group) {
                        if (item.fullFolderName == null) {
                            item.fullFolderName = DemoFolder.GetFullNameFromIndex(item._folderName, indexInside);
                        }
                    }
                }
            }
            return groups;
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            MessageBox.Show("Made by Enter"
                + "\nusing MS Visual Studio 2017"
                + "\nmail: wilerat@gmail.com"
                + "\nskype: Ivan.1010"
                + "\ndiscord: Enter#4725",
                "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        DemoInfoForm demoInfoForm;

        private void buttonSingleFileInfo_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog(this) == DialogResult.OK && openFileDialog1.FileName.Length > 0) {
                demoInfoForm = new DemoInfoForm();
                demoInfoForm.demoFile = new FileInfo(openFileDialog1.FileName);
                demoInfoForm.Show();
            }
        }

        //Обработка нажатия кнопки переименования демок
        private void buttonRename_Click(object sender, EventArgs e)
        {
            job = JobType.RENAME;
            runBackgroundThread();
        }


        //Фиксим демки!
        private void runRename(DirectoryInfo filedemos)
        {
            
            var files = filedemos.GetFiles("*.dm_??", checkBoxUseSubfolders.Checked ?
                SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
            if (radioRenameBad.Checked) {
                files = files.Where(x => Demo.GetDemoFromFile(x).hasError == true).ToArray();
            }

            fileHelper.resetValues(files.Length);

            Demo demo;

            Exception exception = null;

            foreach (var file in files) {
                try {
                    demo = Demo.GetDemoFromFileRaw(file);
                    if (!checkBoxAddSign.Checked) {
                        demo.errSymbol = "";
                    }
                    demo.useValidation = checkBoxRulesValidation.Checked;

                    string newPath = fileHelper.renameFile(file, demo.demoNewName, checkBoxDeleteIdentical.Checked);

                    if (checkBoxFixCreationTime.Checked && demo.recordTime.HasValue && File.Exists(newPath)) {
                        File.SetCreationTime(newPath, demo.recordTime.Value);
                    }

                } catch (Exception ex) {
                    exception = ex;
                }
            }
            if (exception != null) {
                throw exception;
            }
        }
    }
}

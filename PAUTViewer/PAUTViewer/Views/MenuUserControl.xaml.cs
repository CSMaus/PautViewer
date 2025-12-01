using Microsoft.Win32;
using PAUTViewer.Models;
using PAUTViewer.ProjectUtilities;
using PAUTViewer.ViewModels;
using System;
using Microsoft.WindowsAPICodePack.Dialogs;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using ToastNotifications.Messages;

namespace PAUTViewer.Views
{
    public partial class MenuUserControl : UserControl, INotifyPropertyChanged
    {
        #region Fileds (???)

        private MainWindow mainWindow;
        private DataLoader loadedData;
        PlotPAViewModel plotPAViewModel;


        private string _folderPath;
        public string FolderPath
        {
            get { return _folderPath; }
            set
            {
                _folderPath = value;
                OnPropertyChanged(nameof(FolderPath));
            }
        }

        // command to open
        // choose path to save 

        private string _savePath;
        public string SavePath
        {
            get { return _savePath; }
            set
            {
                _savePath = value;
                OnPropertyChanged(nameof(SavePath));
            }
        }
        // command to save path

        public List<string> Extension { get; set; } = new List<string>
        {
            ".json",
            ".txt",
            ".csv",
        };

        private string _selectedExtension = ".json";
        public string SelectedExtension
        {
            get => _selectedExtension;
            set
            {
                if (_selectedExtension != value)
                {
                    _selectedExtension = value;
                    SavePath = Path.ChangeExtension(SavePath, SelectedExtension);
                    OnPropertyChanged(nameof(SelectedExtension));
                }
            }
        }

        

        #endregion

        public MenuUserControl(MainWindow mainWindow, DataLoader loadedData, PlotPAViewModel sharedPlotPAViewModel)
        {
            InitializeComponent();
            this.mainWindow = mainWindow;
            this.loadedData = loadedData;
            this.plotPAViewModel = sharedPlotPAViewModel;
            DataContext = this;
        }


        public void LoadViaExplorer_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "OPD, FPD Files (*.opd, *.fpd)|*.opd;*.fpd|OPD Files (*.opd)|*.opd|FPD Files (*.fpd)|*.fpd";
            if (openFileDialog.ShowDialog() == true)
            {
                string filePath = openFileDialog.FileName;
                var extension = Path.GetExtension(filePath);
                if (extension == ".opd" || extension == ".fpd")
                {
                    LoadOPDData(filePath);
                }
                FolderPath = Path.GetDirectoryName(filePath);
                SavePath = Path.Combine(Path.GetDirectoryName(filePath), Path.GetFileNameWithoutExtension(filePath))+ SelectedExtension;
            }
        }

        public void ChooseSavePath_Click(object sender, RoutedEventArgs e)
        {
            string dialogueSavePath = "";
            var dialog = new CommonOpenFileDialog
            {
                IsFolderPicker = true,
                Title = "Choose path to save data"
            };

            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                dialogueSavePath = dialog.FileName;
            }
        }

        public void SaveData_Click(object sender, RoutedEventArgs e)
        {
            plotPAViewModel.SaveData2FileExt(SavePath, SelectedExtension);

            Console.WriteLine($"Opened File FolerPath: {FolderPath}");
            Console.WriteLine($"Save File Path: {SavePath}");
        }

        private async void LoadOPDData(string filePath)
        {
            // need to check file path and name before clearing data -> no need to load file if it's already loaded 
            string notificationText = "";
            if (plotPAViewModel.FilePath != null)
            {
                if (plotPAViewModel.FilePath == filePath)
                {
                    notificationText = Application.Current.Resources["notificationFileAlreadyOpened"] as string;
                    NotificationManager.Notifier.ShowWarning(notificationText);
                    isDataLoading = false;
                    return;
                }
            }

            // loadedData.ClearData();
            plotPAViewModel.ClearData();
            isDataLoading = true;
            //LoadingGif.Visibility = Visibility.Visible;

            try
            {
                await Task.Run(() => loadedData.ReadDataFromFile(filePath));
            }
            catch (Exception ex)
            {
                notificationText = string.Format(Application.Current.Resources["notificationErrorReadingFile"] as string, ex.Message);
                NotificationManager.Notifier.ShowError(notificationText);
                isDataLoading = false;
                return;
            }

            //LoadingGif.Visibility = Visibility.Hidden;

            try
            {
                plotPAViewModel.WriteLoadedDataIntoVariables(loadedData);
            }
            catch (Exception ex)
            {
                notificationText = string.Format(Application.Current.Resources["notificationErrorWritingData"] as string, ex.Message);
                NotificationManager.Notifier.ShowError(notificationText);
                isDataLoading = false;
                return;
            }

            try
            {
                plotPAViewModel.PlotData();
            }
            catch (Exception ex)
            {
                notificationText = string.Format(Application.Current.Resources["notificationErrorPlottingData"] as string, ex.Message);
                NotificationManager.Notifier.ShowError(notificationText);
                Console.WriteLine(ex.Message);
                isDataLoading = false;
                return;
            }


            //if (!GlobalSettings.IsTurnOffNotifications)
            //{
            //    notificationText = string.Format(Application.Current.Resources["notificationDataLoadedSuccessfully"] as string, filePath);
            //    NotificationManager.Notifier.ShowInformation(notificationText);
            //    // isDataLoading = false;
            //    // return;
            //}

            //try
            //{
            //    plotPAViewModel.SetWindowsLayoutAndCmaps();
            //}
            //catch (Exception ex)
            //{
            //    notificationText = string.Format(Application.Current.Resources["notificationErrorSettingConfig"] as string, ex.Message);
            //    NotificationManager.Notifier.ShowError(notificationText);
            //    isDataLoading = false;
            //    return;
            //}
            try
            {
                LoadDataCompleted?.Invoke();
            }
            catch (Exception ex)
            {
                notificationText = string.Format(Application.Current.Resources["notificationErrorLoadDataCompleted"] as string, ex.Message);
                NotificationManager.Notifier.ShowError(notificationText);
                isDataLoading = false;
                return;
            }

            try
            {
                mainWindow.UpdatePAPlotDataContext(plotPAViewModel);
            }
            catch (Exception ex)
            {
                notificationText = string.Format(Application.Current.Resources["notificationErrorUpdatingDataContext"] as string, ex.Message);
                NotificationManager.Notifier.ShowError(notificationText);
                isDataLoading = false;
                return;
            }
            isDataLoading = false;
        }
        #region External API controls
        private bool isDataLoading = false;
        public event Action LoadDataCompleted;


        #endregion

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

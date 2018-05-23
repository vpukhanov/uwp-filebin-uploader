using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.ApplicationModel.Resources;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// Документацию по шаблону элемента "Пустая страница" см. по адресу https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x419

namespace Filebin_Upload
{
    /// <summary>
    /// Пустая страница, которую можно использовать саму по себе или для перехода внутри фрейма.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private string uploadLink { get; set; }
        private ResourceLoader resourceLoader = ResourceLoader.GetForCurrentView();

        private const string SETTINGS_FILEBIN_BASE_URL = "FILEBIN_BASE_URL";
        private const string DEFAULT_FILEBIN_BASE = "https://filebin.net/";

        public MainPage()
        {
            this.InitializeComponent();
        }

        private void Grid_DragOver(object sender, DragEventArgs e)
        {
            e.AcceptedOperation = DataPackageOperation.Link;
            e.DragUIOverride.Caption = resourceLoader.GetString("UploadHint");
        }

        private async void Grid_Drop(object sender, DragEventArgs e)
        {
            BeforeUploadPrepareUI();

            try
            {
                if (e.DataView.Contains(StandardDataFormats.StorageItems))
                {
                    var items = await e.DataView.GetStorageItemsAsync();
                    var filteredItems = items.Where(item => item is StorageFile).ToList();

                    if (filteredItems.Count > 0)
                    {
                        BeginUploadStoryboard.Begin();
                        var result = await uploadAllFiles(filteredItems);
                        DisplayResult(result);
                    }

                    if (filteredItems.Count < items.Count)
                    {
                        // Some storage items were not files
                        await new MessageDialog(resourceLoader.GetString("OnlyUploadFilesHint")).ShowAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageDialog messageDialog = new MessageDialog(ex.Message + "\n" + resourceLoader.GetString("ApplicationErrorHint"), resourceLoader.GetString("UploadErrorHint"));
                await messageDialog.ShowAsync();
                ResetViewStoryboard.Begin();
            }

            AfterUploadPrepareUI();
        }

        private async Task<FilebinResponse> uploadAllFiles(IReadOnlyList<IStorageItem> items)
        {
            var filebinApi = new FilebinApi(getCurrentFilebinBaseUrl());
            var result = await uploadFile(filebinApi, items[0] as StorageFile);
            var uploadBin = result.BinName;

            for (var i = 1; i < items.Count; i++)
            {
                result = await uploadFile(filebinApi, items[i] as StorageFile, uploadBin);
            }

            return result;
        }

        private async Task<FilebinResponse> uploadFile(FilebinApi api, StorageFile file, string binName = null)
        {
            UpdateProgress(file);
            return await api.UploadFile(file, binName);
        }

        private void BeforeUploadPrepareUI()
        {
            MainGrid.AllowDrop = false;
            OpenSettingsFlyoutButton.Visibility = Visibility.Collapsed;
            SettingsFlyout.Hide();
        }

        private void AfterUploadPrepareUI()
        {
            MainGrid.AllowDrop = true;
            OpenSettingsFlyoutButton.Visibility = Visibility.Visible;
            OpenSettingsFlyoutButton.IsEnabled = true;
        }

        private void DisplayResult(FilebinResponse finalUpload)
        {
            foreach (var link in finalUpload.Links)
            {
                if (link.Relation == "bin")
                {
                    BinLinkTextBlock.Text = link.Href;
                    uploadLink = link.Href;
                    break;
                }
            }
            FinishUploadStoryboard.Begin();
        }

        private void UpdateProgress(StorageFile storageFile)
        {
            FileNameTextBlock.Text = storageFile.Name;
        }

        private void CopyLinkButton_Click(object sender, RoutedEventArgs e)
        {
            var dataPackage = new DataPackage();
            dataPackage.RequestedOperation = DataPackageOperation.Copy;
            dataPackage.SetText(uploadLink);
            Clipboard.SetContent(dataPackage);
        }

        private void SettingsFlyout_Opening(object sender, object e)
        {
            FilebinBaseUrlTextBox.Text = getCurrentFilebinBaseUrl();
            if (FilebinBaseUrlTextBox.Text == DEFAULT_FILEBIN_BASE)
            {
                ResetBaseUrlButton.Visibility = Visibility.Collapsed;
            }
            else
            {
                ResetBaseUrlButton.Visibility = Visibility.Visible;
            }
        }

        private void ResetBaseUrlButton_Click(object sender, RoutedEventArgs e)
        {
            FilebinBaseUrlTextBox.Text = DEFAULT_FILEBIN_BASE;
        }

        private async void SaveBaseUrlButton_Click(object sender, RoutedEventArgs e)
        {
            string filebinUrl = FilebinBaseUrlTextBox.Text;

            Uri newUri;
            if (Uri.TryCreate(filebinUrl, UriKind.Absolute, out newUri) &&
                newUri != null && (newUri.Scheme == "http" || newUri.Scheme == "https"))
            {
                SettingsManager.SetValue(SETTINGS_FILEBIN_BASE_URL, newUri.AbsoluteUri);
                SettingsFlyout.Hide();
            }
            else
            {
                await new MessageDialog(filebinUrl + " " + resourceLoader.GetString("NotAValidUrlHint")).ShowAsync();
            }
        }

        private string getCurrentFilebinBaseUrl()
        {
            return SettingsManager.GetValue<string>(SETTINGS_FILEBIN_BASE_URL, DEFAULT_FILEBIN_BASE);
        }
    }
}

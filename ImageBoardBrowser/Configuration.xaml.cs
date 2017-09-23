using System;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;

using Microsoft.Phone.Controls;

namespace ImageBoardBrowser
{
    public partial class Configuration
    {
        public Configuration()
        {
            this.InitializeComponent();
        }

        public decimal? UsedSpaceForHistory { get; set; }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            string view;

            if (this.NavigationContext.QueryString.TryGetValue("view", out view))
            {
                var item = this.Pivot.Items.OfType<PivotItem>().FirstOrDefault(i => i.Tag as string == view);

                if (item != null)
                {
                    this.Pivot.SelectedItem = item;
                }
            }

            this.ComputeHistorySize();
        }

        private void ComputeHistorySize()
        {
            try
            {
                decimal size = 0;

                using (var isolatedStorage = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    var historyFiles = isolatedStorage.GetFileNames("History_*.xml");

                    foreach (var fileName in historyFiles)
                    {
                        using (var file = isolatedStorage.OpenFile(fileName, FileMode.Open, FileAccess.Read, FileShare.Delete))
                        {
                            size += (decimal)file.Length / 1000;
                        }
                    }
                }

                this.UsedSpaceForHistory = Math.Round(size);
            }
            catch (Exception ex)
            {
#if DEBUG
                throw ex;
#endif

                this.UsedSpaceForHistory = null;
            }

            this.OnPropertyChanged("UsedSpaceForHistory");
        }

        private void DelayPickerValueChanging(object sender, Telerik.Windows.Controls.ValueChangingEventArgs<double> e)
        {
            if (e.NewValue < 1)
            {
                e.Cancel = true;
            }
        }

        private void ButtonClearHistoryClick(object sender, System.Windows.RoutedEventArgs e)
        {
            using (var isolatedStorage = IsolatedStorageFile.GetUserStoreForApplication())
            {
                var historyFiles = isolatedStorage.GetFileNames("History_*.xml");

                foreach (var fileName in historyFiles)
                {
                    try
                    {
                        isolatedStorage.DeleteFile(fileName);
                    }
                    catch (Exception ex)
                    {
#if DEBUG
                        throw ex;
#endif
                    }
                }
            }

            App.ViewModel.History = new System.Collections.Generic.Dictionary<string, ImageBoard.Parsers.Common.HistoryEntry>();

            this.ComputeHistorySize();
        }

        private void PersistHistoryCheckedChanged(object sender, Telerik.Windows.Controls.CheckedChangedEventArgs e)
        {
            if (!e.NewState)
            {
                Helper.ShowMessageBox("Please note that history entries already persisted on the phone aren't affected. Use the \"clear history\" button to delete any previous history entry");
            }
        }
    }
}
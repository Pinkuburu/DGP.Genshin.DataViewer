using DGP.Genshin.Common.Data.Json;
using DGP.Genshin.Common.Extensions.System;
using DGP.Genshin.DataViewer.Controls.Dialogs;
using DGP.Genshin.DataViewer.Helpers;
using DGP.Genshin.DataViewer.Services;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace DGP.Genshin.DataViewer.Views
{
    public partial class ExcelSplitView : UserControl, INotifyPropertyChanged
    {
        public ExcelSplitView()
        {
            DataContext = this;
            InitializeComponent();
            VisibilityList.DataContext = PresentDataGrid;
            SetupMemoryUsageTimer();
        }
        private void SetupMemoryUsageTimer()
        {
            DispatcherTimer timer = new()
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            timer.Tick += (s, e) => MemoryUsageText.Text = $"内存占用: {Process.GetCurrentProcess().WorkingSet64 / 1024 / 1024} MB";
            timer.Start();
        }

        #region event
        private void OpenFolderRequested(object sender, RoutedEventArgs e)
        {
            WorkingFolderService.SelectWorkingFolder();
            string path = WorkingFolderService.WorkingFolderPath;
            if (path == null)
            {
                return;
            }

            if (!Directory.Exists(path + @"\TextMap\") || !Directory.Exists(path + @"\Excel\"))
            {
                new SelectionSuggestDialog().ShowAsync();
            }
            else
            {
                TextMapCollection = Directory2.GetFileExs(path + @"\TextMap\");
                ExcelConfigDataCollection = Directory2.GetFileExs(path + @"\Excel\");
                if (ExcelConfigDataCollection.Count() == 0)
                {
                    new SelectionSuggestDialog().ShowAsync();
                }
                else
                {
                    //npcid
                    MapService.NPCMap = new Lazy<Dictionary<string, string>>(() =>
                    Json.ToObject<JArray>(
                        ExcelConfigDataCollection
                        .First(f => f.FileName == "Npc").Read())
                    .ToDictionary(t => t["Id"].ToString(), v => v["NameTextMapHash"].ToString()));
                }
            }
        }
        private void PaneStateChangeRequested(object sender, RoutedEventArgs e)
        {
            IsPaneOpen = !IsPaneOpen;
        }

        private void OnCurrentCellChanged(object sender, EventArgs e)
        {
            if (Keyboard.FocusedElement is System.Windows.Controls.DataGridCell cell)
            {
                if (cell.Content is TextBlock block)
                {
                    Readable = block.Text.ToString();
                }
                else
                {
                    this.Log(cell.Content);
                }
            }
        }
        private void OnSearchExcelList(ModernWpf.Controls.AutoSuggestBox sender, ModernWpf.Controls.AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason == ModernWpf.Controls.AutoSuggestionBoxTextChangeReason.UserInput)
            {
                ExcelConfigDataCollection =
                    originalExcelConfigDataCollection.Where(i => i.FileName.ToLower().Contains(sender.Text.ToLower()));
            }
        }
        private void OnSearchTextMap(ModernWpf.Controls.AutoSuggestBox sender, ModernWpf.Controls.AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason == ModernWpf.Controls.AutoSuggestionBoxTextChangeReason.UserInput)
            {
                sender.ItemsSource = MapService.TextMap.Where(i => i.Key.Contains(sender.Text) || i.Value.Contains(sender.Text));
            }
        }
        #endregion

        #region Update dataview

        private readonly object processingData = new object();

        private DataTable currentTable;
        public DataTable CurrentTable
        {
            get => currentTable;
            set => currentTable = value;
        }
        private async void SetPresentDataAsync(File2 value)
        {
            BackgroundIndicatorVisibility = Visibility.Visible;

            await Task.Run(() =>
            {
                lock (processingData)
                {
                    PresentDataString = value.Read();
                    JArray data = Json.ToObject<JArray>(PresentDataString);

                    CurrentTable = new DataTable();
                    foreach (JObject o in data)
                    {
                        SetColumns(CurrentTable, o);
                    }

                    foreach (JObject o in data)
                    {
                        DataRow row = CurrentTable.NewRow();
                        SetRow(row, o);
                        CurrentTable.Rows.Add(row);
                    }
                    Dispatcher.Invoke(() =>
                    {
                        PresentDataGrid.ItemsSource = CurrentTable.AsDataView();
                    });
                }
            });
            BackgroundIndicatorVisibility = Visibility.Collapsed;
        }
        private void SetRow(DataRow row, JObject o, string parentColumnName = "")
        {
            foreach (JProperty p in o.Properties())
            {
                string columnName = $"{parentColumnName}{p.Name}";
                if (p.Value.Type == JTokenType.Object)
                {
                    SetRow(row, p.Value as JObject, $"{columnName}:");
                }
                else if (p.Value.Type == JTokenType.Array)
                {
                    SetRow(row, p.Value as JArray, $"{columnName}:");
                }
                else if (columnName.Contains("Text") || columnName.Contains("Tips"))
                {
                    row[columnName] = MapService.GetMappedTextBy(p);
                }
                else if (columnName == "TalkRole:Id" || columnName.ToLower() == "npcid")
                {
                    row[columnName] = MapService.GetMappedNPCBy(p.Value.ToString());
                }
                else
                {
                    row[columnName] = p.Value;
                }
            }
        }
        private void SetRow(DataRow row, JArray a, string parentColumnName = "")
        {
            for (int i = 0; i < a.Count; i++)
            {
                JToken t = a[i];
                string columnName = $"{parentColumnName}{i}";
                if (t.Type == JTokenType.Object)
                {
                    SetRow(row, t as JObject, $"{columnName}:");
                }
                else if (t.Type == JTokenType.Array)
                {
                    SetRow(row, t as JArray, $"{columnName}:");
                }
                else if (columnName.Contains("Text") || columnName.Contains("Tips"))
                {
                    row[columnName] = MapService.GetMappedTextBy((t as JValue).Value.ToString());
                }
                else if (columnName == "TalkRole:Id" || columnName.ToLower() == "npcid")
                {
                    row[columnName] = MapService.GetMappedNPCBy((t as JValue).Value.ToString());
                }
                else
                {
                    row[columnName] = (t as JValue).Value;
                }
            }
        }
        private void SetColumns(DataTable table, JObject o, string parentColumnName = "")
        {
            if (o != null)
            {
                foreach (JProperty p in o.Properties())
                {
                    string columnName = $"{parentColumnName}{p.Name}";
                    if (p.Value.Type == JTokenType.Object)
                    {
                        SetColumns(table, p.Value as JObject, $"{columnName}:");
                    }
                    else if (p.Value.Type == JTokenType.Array)
                    {
                        SetColumns(table, p.Value as JArray, $"{columnName}:");
                    }
                    else if (!table.Columns.Contains(columnName))
                    {
                        table.Columns.Add(columnName);
                    }
                }
            }
        }
        private void SetColumns(DataTable table, JArray a, string parentColumnName = "")
        {
            if (a != null)
            {
                for (int i = 0; i < a.Count; i++)
                {
                    JToken t = a[i];
                    string columnName = $"{parentColumnName}{i}";
                    if (t.Type == JTokenType.Object)
                    {
                        SetColumns(table, t as JObject, $"{columnName}:");
                    }
                    else if (t.Type == JTokenType.Array)
                    {
                        SetColumns(table, t as JArray, $"{columnName}:");
                    }
                    else if (!table.Columns.Contains(columnName))
                    {
                        table.Columns.Add(columnName);
                    }
                }
            }
        }
        #endregion

        #region Property
        //复选框用TextMap枚举
        #region TextMapCollection
        private IEnumerable<File2> textMapCollection;
        public IEnumerable<File2> TextMapCollection
        {
            get => textMapCollection; set => Set(ref textMapCollection, value);
        }
        #endregion
        //后台用TextMap
        #region SelectedTextMap
        private File2 selectedTextMap;
        public File2 SelectedTextMap
        {
            get => selectedTextMap; set
            {
                Set(ref selectedTextMap, value);
                MapService.TextMap = Json.ToObject<Dictionary<string, string>>(selectedTextMap.Read());
            }
        }
        #endregion
        //左侧列表用ExcelConfigData枚举
        #region ExcelConfigDataCollection
        private IEnumerable<File2> originalExcelConfigDataCollection = new List<File2>();

        private IEnumerable<File2> excelConfigDataCollection = new List<File2>();
        public IEnumerable<File2> ExcelConfigDataCollection
        {
            get => excelConfigDataCollection; set
            {
                //search support
                if (originalExcelConfigDataCollection.Count() <= value.Count())
                {
                    originalExcelConfigDataCollection = value;
                }

                Set(ref excelConfigDataCollection, value);
            }
        }
        #endregion
        //后台用ExcelConfigData
        #region SelectedFile
        private File2 selectedFile;
        public File2 SelectedFile
        {
            get => selectedFile; set
            {
                if (value != null)
                {
                    TitleText.Text = value.FullFileName;
                    IsPaneOpen = false;
                    SetPresentDataAsync(value);
                    Set(ref selectedFile, value);
                }
                //TO DO:reselect the correct item in list
            }
        }
        #endregion
        //SplitView用pane状态
        #region IsPaneOpen
        private bool isPaneOpen = false;
        public bool IsPaneOpen
        {
            get => isPaneOpen; set => Set(ref isPaneOpen, value);
        }
        #endregion
        //后台任务指示
        #region BackgroundIndicatorVisibility
        private Visibility backgroundIndicatorVisibility = Visibility.Collapsed;
        public Visibility BackgroundIndicatorVisibility { get => backgroundIndicatorVisibility; set => Set(ref backgroundIndicatorVisibility, value); }
        #endregion
        //可读文字
        #region Readable
        private string readable;
        public string Readable { get => readable; set => Set(ref readable, value); }
        #endregion
        //原始数据视图
        #region PresentDataString
        private string presentDataString;
        public string PresentDataString { get => presentDataString; set => Set(ref presentDataString, value); }
        #endregion

        #endregion

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        protected void Set<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(storage, value))
            {
                return;
            }

            storage = value;
            OnPropertyChanged(propertyName);
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }
}

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Collections.Specialized;
using System.ComponentModel;
using Microsoft.Win32;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using AOSharp.Data;
using AOSharp.Models;
using Serilog;
using AOSharp.Bootstrap.IPC;

namespace AOSharp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow, INotifyPropertyChanged
    {
        //private ObservableCollection<Profile> _profiles;
        //public ObservableCollection<Profile> Profiles { get { return _profiles; } }

        //private ObservableCollection<Assembly> _assemblies;
        //public ObservableCollection<Assembly> Assemblies { get { return _assemblies; } }

        public Config Config;

        private Profile _activeProfile;

        public Profile ActiveProfile
        {
            get { return _activeProfile; }
            set
            {
                if (_activeProfile != null) _activeProfile.PluginStatusesChanged -= RefreshPluginStatuses;
                _activeProfile = value;
                if (_activeProfile != null) _activeProfile.PluginStatusesChanged += RefreshPluginStatuses;
                RefreshPluginStatuses(this, EventArgs.Empty);
                OnPropertyChanged("ActiveProfile");
            }
        }

        private bool _hasProfileSelected;

        public bool HasProfileSelected
        {
            get { return _hasProfileSelected; }
            set
            {
                _hasProfileSelected = value;
                OnPropertyChanged("HasProfileSelected");
            }
        }

        public MainWindow()
        {
            //_profiles = GetProfiles();
            //_assemblies = GetAssemblies();

            Log.Logger = new LoggerConfiguration()
                .WriteTo.File("Log.txt", rollingInterval: RollingInterval.Day)
                .CreateLogger();

            Config = Config.Load(Directories.ConfigFilePath);

            Config.Plugins.CollectionChanged += (object sender, NotifyCollectionChangedEventArgs e) =>
            {
                if(e.Action == NotifyCollectionChangedAction.Add || e.Action == NotifyCollectionChangedAction.Remove)
                Config.Save();
            };

            this.DataContext = this;

            InitializeComponent();

            PluginsDataGrid.DataContext = Config;
            ProfileListBox.DataContext = new ProfilesModel(Config);

            // Set default sort for PluginsDataGrid "Name" column descending
            ICollectionView view = CollectionViewSource.GetDefaultView(PluginsDataGrid.ItemsSource);
            if (view != null)
            {
                view.SortDescriptions.Clear();
                view.SortDescriptions.Add(new SortDescription("Value.Name", ListSortDirection.Ascending));
                view.Refresh();
            }
        }

        private void RefreshPluginStatuses(object sender, EventArgs e)
        {
            foreach (PluginModel plugin in Config.Plugins.Values)
            {
                PluginStatusMessage status = null;
                ActiveProfile?.PluginStatuses.TryGetValue(plugin.Path, out status);
                plugin.LoadStatus = status == null ? "Not injected" :
                    status.State == PluginLoadState.Initialized ? "Initialized" :
                    status.State == PluginLoadState.Failed ? "Failed" : "Loading";
                plugin.LoadDetail = status?.Detail ?? "";
            }
        }

        private async void ShowAddPluginDialog(object sender, RoutedEventArgs e)
        {
            BaseMetroDialog dialog = (BaseMetroDialog)this.Resources["AddPluginDialog"];
            dialog.DataContext = new AddAssemblyModel();
            await this.ShowMetroDialogAsync(dialog);
            await dialog.WaitUntilUnloadedAsync();
        }

        private async void CloseAddPluginDialog(object sender, RoutedEventArgs e)
        {
            BaseMetroDialog dialog = (BaseMetroDialog)this.Resources["AddPluginDialog"];
            await this.HideMetroDialogAsync(dialog);
        }

        private void LocalPathDialogButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog() { Filter = "DLL Files (*.dll)|*.dll" };

            if (dialog.ShowDialog() == true)
            {
                BaseMetroDialog addPluginDialog = (BaseMetroDialog)this.Resources["AddPluginDialog"];
                ((AddAssemblyModel)addPluginDialog.DataContext).DllPath = dialog.FileName;
            }
        }

        private async void AddPluginButton_Click(object sender, RoutedEventArgs e)
        {
            BaseMetroDialog addPluginDialog = (BaseMetroDialog)this.Resources["AddPluginDialog"];
            AddAssemblyModel dataModel = (AddAssemblyModel)addPluginDialog.DataContext;

            if (string.IsNullOrEmpty(dataModel.DllPath))
            {
                await this.ShowMessageAsync("Error", "No plugin path specified.");
                return;
            }

            if (dataModel.DllPath.ToLower().Contains("\\obj\\"))
            {
                await this.ShowMessageAsync("Error", $"Invalid plugin path specified: path should not include \\obj\\. Did you mean {dataModel.DllPath.Replace("\\obj\\", "\\bin\\")}?");
                return;
            }

            //Should never happen but just in case some idiot deletes a plugin after selecting it..
            if (!File.Exists(dataModel.DllPath))
                return;

            FileVersionInfo fileInfo = FileVersionInfo.GetVersionInfo(dataModel.DllPath);

            Config.Plugins.Add(Utils.HashFromFile(dataModel.DllPath), new PluginModel()
            {
                Name = fileInfo.ProductName,
                Version = fileInfo.FileVersion,
                Path = dataModel.DllPath
            });

            await this.HideMetroDialogAsync(addPluginDialog);
        }

        private void PluginEnabledCheckBox_Click(object sender, RoutedEventArgs e)
        {
            Profile selectedProfile = (Profile)ProfileListBox.SelectedItem;

            if (selectedProfile == null)
                return;

            KeyValuePair<string, PluginModel> plugin = (KeyValuePair<string, PluginModel>)((CheckBox)sender).DataContext;

            // These choices are for the next injection; the running loader owns its snapshot.
            if (plugin.Value.IsEnabled)
            {
                if (!selectedProfile.EnabledPlugins.Contains(plugin.Key))
                    selectedProfile.EnabledPlugins.Add(plugin.Key);
            }
            else
                selectedProfile.EnabledPlugins.Remove(plugin.Key);

            if (!Config.Profiles.Contains(selectedProfile))
                Config.Profiles.Add(selectedProfile);

            Config.Save();
        }

        private void ProfileListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Profile profile = (Profile)ProfileListBox.SelectedItem;

            HasProfileSelected = profile != null;
            ActiveProfile = profile;

            if(profile == null)
            {
                foreach (PluginModel plugin in Config.Plugins.Values)
                    plugin.IsEnabled = false;

                PluginsDataGrid.IsEnabled = false;
                return;
            }
            else if(!PluginsDataGrid.IsEnabled)
            {
                PluginsDataGrid.IsEnabled = true;
            }

            foreach (KeyValuePair<string, PluginModel> plugin in Config.Plugins)
                plugin.Value.IsEnabled = profile.EnabledPlugins.Contains(plugin.Key);
        }

        private async void InjectButton_Clicked(object sender, RoutedEventArgs e)
        {
            Profile profile = (Profile)ProfileListBox.SelectedItem;

            if (profile == null)
                return;

            IEnumerable<string> plugins = Config.Plugins.Where(x => profile.EnabledPlugins.Contains(x.Key)).Select(x => x.Value.Path);

            if(!plugins.Any())
            {
                await this.ShowMessageAsync("Error", "No plugins selected.");
                return;
            }

            if (!profile.Inject(plugins))
            {
                await this.ShowMessageAsync("Error", "Failed to inject.");
            }
        }

        private void EjectButton_Clicked(object sender, RoutedEventArgs e)
        {
            Profile profile = (Profile)ProfileListBox.SelectedItem;

            if (profile == null)
                return;

            profile.Eject();

            PluginsDataGrid.IsEnabled = true;
        }

        private async void InjectAllButton_Clicked(object sender, RoutedEventArgs e)
        {
            var profiles = ((ProfilesModel)ProfileListBox.DataContext).Profiles
                .Where(profile => profile.IsActive && !profile.IsInjected).ToArray();
            var failures = new List<string>();

            foreach (Profile profile in profiles)
            {
                string[] plugins = Config.Plugins
                    .Where(plugin => profile.EnabledPlugins.Contains(plugin.Key))
                    .Select(plugin => plugin.Value.Path).ToArray();

                if (plugins.Length == 0)
                    continue;

                if (!profile.Inject(plugins))
                    failures.Add(profile.Name);
            }

            if (failures.Count > 0)
                await this.ShowMessageAsync("Injection failed",
                    "Could not inject: " + string.Join(", ", failures));
        }

        private async void EjectAllButton_Clicked(object sender, RoutedEventArgs e)
        {
            var profiles = ((ProfilesModel)ProfileListBox.DataContext).Profiles
                .Where(profile => profile.IsActive && profile.IsInjected).ToArray();
            var failures = new List<string>();

            foreach (Profile profile in profiles)
            {
                try
                {
                    profile.Eject();
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Failed to eject profile {Profile}", profile.Name);
                    failures.Add(profile.Name);
                }
            }

            if (failures.Count > 0)
                await this.ShowMessageAsync("Ejection failed",
                    "Could not eject: " + string.Join(", ", failures));
        }

        private void TweaksButton_Click(object sender, RoutedEventArgs e)
        {
            var tweaksWindow = new TweaksWindow();
            tweaksWindow.Owner = this;
            tweaksWindow.ShowDialog();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }

    [ValueConversion(typeof(bool), typeof(Visibility))]
    public sealed class BoolToVisibilityConverter : IValueConverter
    {
        public Visibility TrueValue { get; set; }
        public Visibility FalseValue { get; set; }

        public BoolToVisibilityConverter()
        {
            // set defaults
            TrueValue = Visibility.Visible;
            FalseValue = Visibility.Collapsed;
        }

        public object Convert(object value, Type targetType,
            object parameter, CultureInfo culture)
        {
            if (!(value is bool))
                return null;
            return (bool)value ? TrueValue : FalseValue;
        }

        public object ConvertBack(object value, Type targetType,
            object parameter, CultureInfo culture)
        {
            if (Equals(value, TrueValue))
                return true;
            if (Equals(value, FalseValue))
                return false;
            return null;
        }
    }

    public class RemovePluginConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values[1] == DependencyProperty.UnsetValue)
            {
                return null;
            }

            Tuple<ObservableDictionary<string, PluginModel>, string> tuple = new Tuple<ObservableDictionary<string, PluginModel>, string>(
                (ObservableDictionary<string, PluginModel>)values[0], (string)values[1]);
            return tuple;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class PathIsInvalidConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string path)
            {
                return path.Contains(@"\obj\");
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

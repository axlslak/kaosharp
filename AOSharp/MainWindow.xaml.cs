using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
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
        public Config Config;

        private Profile _activeProfile;
        private Dictionary<string, Dictionary<string, List<string>>> _userPluginProfiles = new Dictionary<string, Dictionary<string, List<string>>>();
        private readonly ObservableCollection<string> _userProfileNames = new ObservableCollection<string>();
        private string _userProfilesPath => Path.Combine(Directories.CurrentDirectory, "userPluginProfiles.json");
        private string _selectedUserPluginProfile;

        public ObservableCollection<string> UserProfileNames => _userProfileNames;

        public string SelectedUserPluginProfile
        {
            get => _selectedUserPluginProfile;
            set
            {
                _selectedUserPluginProfile = value;
                OnPropertyChanged(nameof(SelectedUserPluginProfile));
            }
        }

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
            Log.Logger = new LoggerConfiguration()
                .WriteTo.File("Log.txt", rollingInterval: RollingInterval.Day)
                .CreateLogger();

            Config = Config.Load(Directories.ConfigFilePath);

            Config.Plugins.CollectionChanged += (object sender, NotifyCollectionChangedEventArgs e) =>
            {
                Config.Save();
            };

            this.DataContext = this;

            InitializeComponent();

            try
            {
                if (File.Exists(_userProfilesPath))
                    _userPluginProfiles = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, List<string>>>>(File.ReadAllText(_userProfilesPath))
                        ?? new Dictionary<string, Dictionary<string, List<string>>>();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to read plugin profiles from {Path}", _userProfilesPath);
                Loaded += async (sender, args) => await this.ShowMessageAsync("Plugin profiles", "Could not read saved plugin profiles: " + ex.Message);
            }
            foreach (string name in _userPluginProfiles.Keys)
                _userProfileNames.Add(name);
            SelectedUserPluginProfile = _userProfileNames.FirstOrDefault();

            PluginsDataGrid.DataContext = Config;
            ProfileListBox.DataContext = new ProfilesModel(Config);

            // Sort plugin names alphabetically.
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

        private void SaveUserPluginProfile(string profileName)
        {
            var data = new Dictionary<string, List<string>>();
            foreach (var profile in Config.Profiles)
            {
                data[profile.Name] = new List<string>(profile.EnabledPlugins);
            }

            _userPluginProfiles[profileName] = data;

            File.WriteAllText(_userProfilesPath, Newtonsoft.Json.JsonConvert.SerializeObject(_userPluginProfiles, Newtonsoft.Json.Formatting.Indented));
        }

        private void LoadUserPluginProfile(string profileName)
        {
            if (!_userPluginProfiles.ContainsKey(profileName))
                return;

            var selectedData = _userPluginProfiles[profileName];
            foreach (var profile in Config.Profiles)
            {
                if (selectedData.ContainsKey(profile.Name))
                {
                    profile.EnabledPlugins = new ObservableCollection<string>(selectedData[profile.Name]);
                }
                else
                {
                    profile.EnabledPlugins.Clear();
                }
            }

            if (ActiveProfile != null)
            {
                foreach (var plugin in Config.Plugins)
                {
                    plugin.Value.IsEnabled = ActiveProfile.EnabledPlugins.Contains(plugin.Key);
                }
            }

            Config.Save();
        }

        private async void ShowAddPluginDialog(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog
            {
                Filter = "DLL Files (*.dll)|*.dll",
                Multiselect = true,
                Title = "Select Plugin DLLs"
            };

            if (dialog.ShowDialog() != true)
                return;

            await AddPluginFilesAsync(dialog.FileNames);
        }

        private static bool IsDllPath(string path)
        {
            return string.Equals(Path.GetExtension(path), ".dll", StringComparison.OrdinalIgnoreCase);
        }

        private void PluginFiles_DragOver(object sender, DragEventArgs e)
        {
            e.Effects = DragDropEffects.None;
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var paths = e.Data.GetData(DataFormats.FileDrop) as string[];
                if (paths != null && paths.Any(IsDllPath))
                    e.Effects = DragDropEffects.Copy;
            }
            e.Handled = true;
        }

        private async void PluginFiles_Drop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop))
                return;

            e.Handled = true;
            var paths = e.Data.GetData(DataFormats.FileDrop) as string[];
            if (paths != null)
                await AddPluginFilesAsync(paths);
        }

        private async Task AddPluginFilesAsync(IEnumerable<string> paths)
        {
            var failures = new List<string>();
            foreach (string dllPath in paths.Where(IsDllPath))
            {
                try
                {
                    if (!File.Exists(dllPath))
                        continue;

                    if (dllPath.IndexOf("\\obj\\", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        failures.Add($"{dllPath}: use the DLL from \\bin\\ instead of \\obj\\.");
                        continue;
                    }

                    string key = Utils.HashFromFile(dllPath);
                    if (Config.Plugins.ContainsKey(key))
                        continue;

                    var fileInfo = FileVersionInfo.GetVersionInfo(dllPath);
                    Config.Plugins.Add(key, new PluginModel
                    {
                        Name = fileInfo.ProductName,
                        Version = fileInfo.FileVersion,
                        Path = dllPath,
                        IsEnabled = ActiveProfile?.EnabledPlugins.Contains(key) ?? false
                    });
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Failed to add plugin {Path}", dllPath);
                    failures.Add(dllPath + ": " + ex.Message);
                }
            }

            RefreshPluginStatuses(this, EventArgs.Empty);
            if (failures.Count > 0)
                await this.ShowMessageAsync("Could not add some plugins", string.Join(Environment.NewLine, failures));
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

                return;
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
        private async void SaveUserPluginProfileButton_Click(object sender, RoutedEventArgs e)
        {
            UserProfileComboBox.GetBindingExpression(ComboBox.TextProperty)?.UpdateSource();
            try
            {
                SaveCurrentUserProfile();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to save plugin profile");
                await this.ShowMessageAsync("Plugin profiles", "Could not save plugin profile: " + ex.Message);
            }
        }

        private async void LoadUserPluginProfileButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                LoadSelectedUserProfile();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to load plugin profile");
                await this.ShowMessageAsync("Plugin profiles", "Could not load plugin profile: " + ex.Message);
            }
        }

        private void SaveCurrentUserProfile()
        {
            if (string.IsNullOrWhiteSpace(SelectedUserPluginProfile))
                return;

            SaveUserPluginProfile(SelectedUserPluginProfile);

            if (!_userProfileNames.Contains(SelectedUserPluginProfile))
                _userProfileNames.Add(SelectedUserPluginProfile);

        }

        private void LoadSelectedUserProfile()
        {
            if (string.IsNullOrWhiteSpace(SelectedUserPluginProfile))
                return;

            LoadUserPluginProfile(SelectedUserPluginProfile);
        }
        private async void DeleteUserPluginProfileButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(SelectedUserPluginProfile) || !_userPluginProfiles.ContainsKey(SelectedUserPluginProfile))
                return;

            try
            {
                var remaining = new Dictionary<string, Dictionary<string, List<string>>>(_userPluginProfiles);
                remaining.Remove(SelectedUserPluginProfile);
                File.WriteAllText(_userProfilesPath, Newtonsoft.Json.JsonConvert.SerializeObject(remaining, Newtonsoft.Json.Formatting.Indented));
                _userPluginProfiles = remaining;
                _userProfileNames.Remove(SelectedUserPluginProfile);
                SelectedUserPluginProfile = _userProfileNames.FirstOrDefault();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to delete plugin profile");
                await this.ShowMessageAsync("Plugin profiles", "Could not delete plugin profile: " + ex.Message);
            }
        }

        private void RemoveAllPlugins_Click(object sender, RoutedEventArgs e)
        {
            // This stages the next injection; active loaders retain their snapshot.
            Config.Plugins.Clear();
        }
        protected override void OnClosing(CancelEventArgs e)
        {
            Properties.Settings.Default.WindowLocation = new Point(this.RestoreBounds.Left, this.RestoreBounds.Top);
            Properties.Settings.Default.WindowSize = new Size(this.RestoreBounds.Width, this.RestoreBounds.Height);
            Properties.Settings.Default.Save();
            base.OnClosing(e);
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            if (Properties.Settings.Default.WindowSize.Width > 0 && Properties.Settings.Default.WindowSize.Height > 0)
            {
                this.Width = Properties.Settings.Default.WindowSize.Width;
                this.Height = Properties.Settings.Default.WindowSize.Height;
            }

            if (Properties.Settings.Default.WindowLocation.X >= 0 && Properties.Settings.Default.WindowLocation.Y >= 0)
            {
                this.Left = Properties.Settings.Default.WindowLocation.X;
                this.Top = Properties.Settings.Default.WindowLocation.Y;
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

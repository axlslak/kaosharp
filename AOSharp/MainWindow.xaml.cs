using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using AOSharp.Bootstrap.IPC;
using AOSharp.Data;
using AOSharp.Models;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Win32;
using Serilog;
using Activity = AOSharp.Models.Activity;

namespace AOSharp
{
    public partial class MainWindow : MetroWindow, INotifyPropertyChanged
    {
        public Config Config { get; }
        private readonly ProfilesModel _profilesModel;
        private Profile _activeProfile;
        private Activity _activeActivity;
        private Activity _pendingActivity;
        private DataGridTemplateColumn _pendingColumn;
        private TextBox _activityNameEditor;
        private bool _ready;
        private bool _batchingLibraryChanges;
        private string _setupMessage = "Add your plugin DLLs, then use + to create an activity.";
        private string _saveStatus = "Changes save automatically.";
        private bool _saveFailed;
        private bool _saveErrorDialogOpen;
        private readonly HashSet<Profile> _observedProfiles = new HashSet<Profile>();
        private bool _tabStatusRefreshPending;
        private bool _closed;
        private readonly Dictionary<Activity, DataGridTemplateColumn> _activityColumns =
            new Dictionary<Activity, DataGridTemplateColumn>();
        private bool _activityDialogOpen;

        public ObservableCollection<KeyValuePair<string, PluginModel>> LibraryPlugins { get; } =
            new ObservableCollection<KeyValuePair<string, PluginModel>>();
        public ObservableCollection<ActivityPluginModel> ActivityPlugins { get; } =
            new ObservableCollection<ActivityPluginModel>();
        public ObservableCollection<Profile> Profiles => _profilesModel.Profiles;

        public string SetupMessage
        {
            get => _setupMessage;
            private set { _setupMessage = value; OnPropertyChanged(nameof(SetupMessage)); }
        }

        public string SaveStatus
        {
            get => _saveStatus;
            private set { _saveStatus = value; OnPropertyChanged(nameof(SaveStatus)); }
        }

        public bool SaveFailed
        {
            get => _saveFailed;
            private set { _saveFailed = value; OnPropertyChanged(nameof(SaveFailed)); }
        }

        public bool HasProfileSelected => _activeActivity != null && _activeProfile?.IsActive == true;
        public bool CanInject => HasProfileSelected && !_activeProfile.IsInjected;
        public bool CanEject => HasProfileSelected && _activeProfile.IsInjected &&
                                _activeProfile.LiveActivityId == _activeActivity.Id;
        public bool ActivityHasNoPlugins => ActivityPlugins.Count == 0;

        public string InjectionHint
        {
            get
            {
                if (!HasProfileSelected)
                    return "Choose a running character, then select its plugins for this activity.";
                if (_activeProfile.IsInjected)
                {
                    if (_activeProfile.LiveActivityId != _activeActivity.Id)
                        return _activeProfile.Name + " is running " + _activeProfile.LiveActivityName +
                               ". Open that tab to eject before switching activities.";
                    return "Running this activity. Selection changes apply to the next injection.";
                }
                return "Choose the plugins for " + _activeProfile.Name + ", then Inject.";
            }
        }

        public MainWindow()
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.File("Log.txt", rollingInterval: RollingInterval.Day)
                .CreateLogger();

            Config = Config.Load(Directories.ConfigFilePath);
            _profilesModel = new ProfilesModel();
            DataContext = this;
            InitializeComponent();

            if (Config.LibraryRecoveryError != null)
                Loaded += async (sender, args) =>
                    await this.ShowMessageAsync("Plugin list", Config.LibraryRecoveryError);

            foreach (Activity activity in Config.Activities)
            {
                AddActivityTab(activity);
                DataGridTemplateColumn column = CreateMembershipColumn(activity);
                _activityColumns.Add(activity, column);
                LibraryGrid.Columns.Insert(LibraryGrid.Columns.Count - 2, column);
            }

            ObserveProfiles();
            Profiles.CollectionChanged += Profiles_CollectionChanged;
            Config.Plugins.CollectionChanged += Plugins_CollectionChanged;
            _ready = true;
            WorkspaceTabs.SelectedItem = LibraryTab;
            RefreshLibraryPlugins();
            SelectActivity(null);
        }

        private void WorkspaceTabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_ready || !ReferenceEquals(e.Source, WorkspaceTabs))
                return;

            if (WorkspaceTabs.SelectedItem == NewActivityTab)
            {
                WorkspaceTabs.SelectedItem = LibraryTab;
                BeginActivity();
                return;
            }

            SelectActivity((WorkspaceTabs.SelectedItem as TabItem)?.Tag as Activity);
        }

        private void NewActivity_Click(object sender, RoutedEventArgs e)
        {
            WorkspaceTabs.SelectedItem = LibraryTab;
            BeginActivity();
        }

        private void SelectActivity(Activity activity)
        {
            _activeActivity = activity;
            LibraryPanel.Visibility = activity == null ? Visibility.Visible : Visibility.Collapsed;
            ActivityPanel.Visibility = activity == null ? Visibility.Collapsed : Visibility.Visible;
            RefreshActivityPlugins();
        }

        private void AddActivityTab(Activity activity)
        {
            var tab = new TabItem
            {
                Header = activity.Name, Tag = activity, Padding = new Thickness(14, 5, 14, 5),
                Style = (Style)FindResource("ActivityTabStyle")
            };
            var rename = new MenuItem { Header = "_Rename", Tag = activity };
            rename.Click += RenameActivity_Click;
            var delete = new MenuItem { Header = "_Delete", Tag = activity };
            delete.Click += DeleteActivity_Click;
            ToolTipService.SetShowOnDisabled(delete, true);
            var menu = new ContextMenu();
            menu.Items.Add(rename);
            menu.Items.Add(delete);
            menu.Opened += (sender, args) =>
            {
                bool running = HasLiveActivity(activity);
                rename.IsEnabled = !_activityDialogOpen;
                delete.IsEnabled = !_activityDialogOpen && !running;
                delete.ToolTip = running ? "Eject the characters running this activity before deleting it." :
                    "Remove this activity and its choices. Keep the shared plugin library.";
            };
            tab.ContextMenu = menu;
            WorkspaceTabs.Items.Insert(WorkspaceTabs.Items.Count - 1, tab);
            QueueTabStatusRefresh();
        }

        private bool HasLiveActivity(Activity activity)
        {
            return Profiles.Any(profile => profile.IsInjected && profile.LiveActivityId == activity.Id);
        }

        private string ActivityNameError(string name, Activity current = null)
        {
            if (string.IsNullOrWhiteSpace(name))
                return "Enter an activity name.";
            if (string.Equals(name, "Plugin list", StringComparison.OrdinalIgnoreCase) || name == "+")
                return "Choose an activity name other than Plugin list or +.";
            if (Config.Activities.Any(activity => !ReferenceEquals(activity, current) &&
                string.Equals(activity.Name, name, StringComparison.OrdinalIgnoreCase)))
                return "An activity named " + name + " already exists. Choose another name.";
            return null;
        }

        private async void RenameActivity_Click(object sender, RoutedEventArgs e)
        {
            var activity = (sender as MenuItem)?.Tag as Activity;
            if (_activityDialogOpen || activity == null || !Config.Activities.Contains(activity)) return;
            _activityDialogOpen = true;
            try
            {
                var settings = new MetroDialogSettings
                {
                    DefaultText = activity.Name, AffirmativeButtonText = "Rename", NegativeButtonText = "Cancel"
                };
                while (Config.Activities.Contains(activity))
                {
                    string name = await this.ShowInputAsync("Rename activity", "New activity name:", settings);
                    if (name == null || !Config.Activities.Contains(activity)) return;
                    name = name.Trim();
                    string error = ActivityNameError(name, activity);
                    if (error != null)
                    {
                        settings.DefaultText = name;
                        await this.ShowMessageAsync("Activity name", error);
                        continue;
                    }
                    if (name == activity.Name) return;
                    // The activity ID and its pool/character choices stay intact.
                    activity.Name = name;
                    if (_activityColumns.TryGetValue(activity, out DataGridTemplateColumn column))
                        column.Header = name;
                    foreach (Profile profile in Profiles)
                        profile.UpdateActivityName(activity.Id, name);
                    SetupMessage = "Activity renamed to " + name + ".";
                    QueueTabStatusRefresh();
                    RefreshRuntimeControls();
                    await SaveConfigurationAsync();
                    return;
                }
            }
            finally
            {
                _activityDialogOpen = false;
            }
        }

        private async void DeleteActivity_Click(object sender, RoutedEventArgs e)
        {
            var activity = (sender as MenuItem)?.Tag as Activity;
            if (_activityDialogOpen || activity == null || !Config.Activities.Contains(activity)) return;
            _activityDialogOpen = true;
            try
            {
                if (HasLiveActivity(activity))
                {
                    await this.ShowMessageAsync("Activity is running",
                        "Eject the characters running this activity before deleting it.");
                    return;
                }
                MessageDialogResult result = await this.ShowMessageAsync("Delete activity",
                    "Delete " + activity.Name + " and its plugin assignments and character choices?" +
                    "\n\nThe shared plugin library and DLL files will be kept.",
                    MessageDialogStyle.AffirmativeAndNegative, new MetroDialogSettings
                    {
                        AffirmativeButtonText = "Delete", NegativeButtonText = "Cancel",
                        DefaultButtonFocus = MessageDialogResult.Negative,
                        DialogResultOnCancel = MessageDialogResult.Negative
                    });
                if (result != MessageDialogResult.Affirmative || !Config.Activities.Contains(activity)) return;
                // A modal dialog yields to runtime callbacks: check ownership again before removal.
                if (HasLiveActivity(activity))
                {
                    await this.ShowMessageAsync("Activity is running",
                        "Eject the characters running this activity before deleting it.");
                    return;
                }
                TabItem tab = WorkspaceTabs.Items.OfType<TabItem>()
                    .FirstOrDefault(item => ReferenceEquals(item.Tag, activity));
                // Never let removing the selected last activity select + and create a new one.
                if (tab != null && ReferenceEquals(WorkspaceTabs.SelectedItem, tab))
                    WorkspaceTabs.SelectedItem = LibraryTab;
                if (_activityColumns.TryGetValue(activity, out DataGridTemplateColumn column))
                {
                    LibraryGrid.Columns.Remove(column);
                    _activityColumns.Remove(activity);
                }
                Config.Activities.Remove(activity);
                if (tab != null) WorkspaceTabs.Items.Remove(tab);
                SetupMessage = "Activity deleted. Its plugins remain in Plugin list.";
                QueueTabStatusRefresh();
                await SaveConfigurationAsync();
            }
            finally
            {
                _activityDialogOpen = false;
            }
        }

        private void QueueTabStatusRefresh()
        {
            if (_closed || _tabStatusRefreshPending)
                return;
            _tabStatusRefreshPending = true;
            // Discovery briefly marks every client inactive; render only its completed snapshot.
            Dispatcher.BeginInvoke(DispatcherPriority.DataBind, new Action(() =>
            {
                _tabStatusRefreshPending = false;
                if (!_closed)
                    RefreshTabStatuses();
            }));
        }

        private void RefreshTabStatuses()
        {
            Profile[] online = Profiles.Where(profile => profile.IsActive).ToArray();
            foreach (TabItem tab in WorkspaceTabs.Items.OfType<TabItem>())
            {
                var activity = tab.Tag as Activity;
                if (activity == null) continue;
                Profile[] injected = online.Where(profile => profile.IsInjected &&
                    profile.LiveActivityId == activity.Id).ToArray();
                Profile[] failed = online.Where(profile => profile.StatusActivityId == activity.Id &&
                    profile.PluginStatuses.Values.Any(status => status.State == PluginLoadState.Failed)).ToArray();
                // Runtime ownership/statuses, never the editable next-injection selection.
                string state = failed.Length > 0 ? "Failed" : injected.Length > 0 ? "Injected" : "";
                tab.Style = (Style)FindResource(state == "Failed" ? "FailedActivityTabStyle" :
                    state == "Injected" ? "InjectedActivityTabStyle" : "ActivityTabStyle");
                tab.Header = activity.Name + (state.Length == 0 ? "" : " · " + state);
                string detail = "Injected: " + (injected.Length == 0 ? "none" :
                    string.Join(", ", injected.Select(profile => profile.Name.Trim())));
                if (failed.Length > 0)
                    detail += "\nFailed plugins: " + string.Join(", ", failed.Select(profile => profile.Name.Trim()));
                tab.ToolTip = detail;
                AutomationProperties.SetItemStatus(tab, state.Length == 0 ? "Not injected" : state);
            }
        }

        private void BeginActivity()
        {
            if (_pendingActivity != null)
            {
                FocusActivityName();
                return;
            }

            _pendingActivity = new Activity();
            _activityNameEditor = new TextBox
            {
                MinWidth = 130,
                ToolTip = "Activity name; press Enter to create, or Esc to cancel",
                Padding = new Thickness(4, 2, 4, 2)
            };
            _activityNameEditor.KeyDown += ActivityName_KeyDown;
            _activityNameEditor.LostKeyboardFocus += ActivityName_LostKeyboardFocus;
            _activityNameEditor.Loaded += (sender, args) => FocusActivityName();
            _pendingColumn = CreateMembershipColumn(_pendingActivity, false);
            _pendingColumn.Header = _activityNameEditor;
            LibraryGrid.Columns.Insert(LibraryGrid.Columns.Count - 2, _pendingColumn);
            SetupMessage = "Name the new column and press Enter, then tick the plugins for that activity.";
            FocusActivityName();
        }

        private void FocusActivityName()
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (_activityNameEditor == null) return;
                _activityNameEditor.BringIntoView();
                _activityNameEditor.Focus();
            }));
        }

        private bool CommitPendingActivity()
        {
            if (_pendingActivity == null)
                return true;

            string name = _activityNameEditor.Text.Trim();
            string error = ActivityNameError(name);
            if (error != null)
            {
                SetupMessage = error;
                return false;
            }

            Activity activityToAdd = _pendingActivity;
            DataGridTemplateColumn column = _pendingColumn;
            // Clear pending state before replacing the focused header (which raises LostKeyboardFocus).
            _pendingActivity = null;
            _pendingColumn = null;
            _activityNameEditor = null;
            activityToAdd.Name = name;
            Config.Activities.Add(activityToAdd);
            column.Header = name;
            column.CellTemplate = CreateMembershipTemplate(activityToAdd, true);
            _activityColumns.Add(activityToAdd, column);
            AddActivityTab(activityToAdd);
            SetupMessage = "Tick the plugins available in " + name + ", then open its tab.";
            return true;
        }

        private async void ActivityName_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                e.Handled = true;
                DataGridTemplateColumn column = _pendingColumn;
                _pendingActivity = null;
                _pendingColumn = null;
                _activityNameEditor = null;
                LibraryGrid.Columns.Remove(column);
                SetupMessage = "Activity creation cancelled. Use + to add one.";
                LibraryGrid.Focus();
            }
            else if (e.Key == Key.Enter)
            {
                e.Handled = true;
                if (CommitPendingActivity())
                {
                    LibraryGrid.Focus();
                    await SaveConfigurationAsync();
                }
            }
        }

        private async void ActivityName_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (_pendingActivity != null && !string.IsNullOrWhiteSpace(_activityNameEditor.Text) &&
                CommitPendingActivity())
                await SaveConfigurationAsync();
        }

        private DataGridTemplateColumn CreateMembershipColumn(Activity activity, bool enabled = true)
        {
            return new DataGridTemplateColumn
            {
                Header = activity.Name,
                MinWidth = 100,
                CanUserSort = false,
                CellTemplate = CreateMembershipTemplate(activity, enabled)
            };
        }

        private DataTemplate CreateMembershipTemplate(Activity activity, bool enabled)
        {
            var box = new FrameworkElementFactory(typeof(CheckBox));
            box.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            box.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
            box.SetValue(FrameworkElement.TagProperty, activity);
            box.SetValue(UIElement.IsEnabledProperty, enabled);
            box.SetBinding(System.Windows.Controls.Primitives.ToggleButton.IsCheckedProperty,
                new Binding("Key") { Mode = BindingMode.OneWay, Converter = new ActivityMembershipConverter(activity) });
            box.AddHandler(System.Windows.Controls.Primitives.ButtonBase.ClickEvent,
                new RoutedEventHandler(ActivityMembership_Click));
            return new DataTemplate { VisualTree = box };
        }

        private async void ActivityMembership_Click(object sender, RoutedEventArgs e)
        {
            var box = (CheckBox)sender;
            var activity = (Activity)box.Tag;
            var plugin = (KeyValuePair<string, PluginModel>)box.DataContext;
            if (box.IsChecked == true)
            {
                if (!activity.PluginKeys.Contains(plugin.Key))
                    activity.PluginKeys.Add(plugin.Key);
            }
            else
                activity.RemovePlugin(plugin.Key);

            await SaveConfigurationAsync();
        }

        private async void RetrySave_Click(object sender, RoutedEventArgs e)
        {
            await SaveConfigurationAsync();
        }

        private Exception TrySaveConfiguration()
        {
            try
            {
                Config.Save();
                SaveFailed = false;
                SaveStatus = "Saved";
                return null;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to save launcher setup");
                SaveFailed = true;
                SaveStatus = "Changes could not be saved.";
                return ex;
            }
        }

        private async Task SaveConfigurationAsync()
        {
            Exception error = TrySaveConfiguration();
            if (error == null || _saveErrorDialogOpen)
                return;

            _saveErrorDialogOpen = true;
            try
            {
                await this.ShowMessageAsync("Could not save setup", error.Message +
                    "\n\nYour changes are still available in this window. Use Retry below.");
            }
            finally
            {
                _saveErrorDialogOpen = false;
            }
        }

        private void RefreshLibraryPlugins()
        {
            LibraryPlugins.Clear();
            foreach (var plugin in Config.Plugins.OrderBy(pair => pair.Value.Name, StringComparer.CurrentCultureIgnoreCase))
                LibraryPlugins.Add(plugin);
        }

        private void RefreshActivityPlugins()
        {
            ActivityPlugins.Clear();
            if (_activeActivity != null)
            {
                foreach (var plugin in Config.Plugins
                    .Where(pair => _activeActivity.PluginKeys.Contains(pair.Key))
                    .OrderBy(pair => pair.Value.Name, StringComparer.CurrentCultureIgnoreCase))
                {
                    ActivityPlugins.Add(new ActivityPluginModel(plugin.Key, plugin.Value)
                    {
                        IsEnabled = _activeProfile != null && _activeActivity.IsEnabled(_activeProfile.Name, plugin.Key)
                    });
                }
            }
            RefreshPluginStatuses(this, EventArgs.Empty);
            OnPropertyChanged(nameof(ActivityHasNoPlugins));
        }

        private void RefreshPluginStatuses(object sender, EventArgs e)
        {
            foreach (ActivityPluginModel row in ActivityPlugins)
            {
                PluginStatusMessage status = null;
                if (_activeProfile != null && _activeProfile.StatusActivityId == _activeActivity?.Id)
                    _activeProfile.PluginStatuses.TryGetValue(row.Plugin.Path, out status);
                row.LoadStatus = status == null ? "Not injected" :
                    status.State == PluginLoadState.Initialized ? "Initialized" :
                    status.State == PluginLoadState.Failed ? "Failed" : "Loading";
                row.LoadDetail = status?.Detail ?? "";
            }
            RefreshRuntimeControls();
        }

        private void RefreshRuntimeControls()
        {
            OnPropertyChanged(nameof(HasProfileSelected));
            OnPropertyChanged(nameof(CanInject));
            OnPropertyChanged(nameof(CanEject));
            OnPropertyChanged(nameof(InjectionHint));
        }

        private void Profiles_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            ObserveProfiles();
        }

        private void ObserveProfiles()
        {
            foreach (Profile profile in _observedProfiles.Where(profile => !Profiles.Contains(profile)).ToArray())
            {
                profile.PropertyChanged -= Profile_PropertyChanged;
                profile.PluginStatusesChanged -= Profile_PluginStatusesChanged;
                _observedProfiles.Remove(profile);
            }
            foreach (Profile profile in Profiles)
                if (_observedProfiles.Add(profile))
                {
                    profile.PropertyChanged += Profile_PropertyChanged;
                    profile.PluginStatusesChanged += Profile_PluginStatusesChanged;
                }
            QueueTabStatusRefresh();
        }

        private void Profile_PluginStatusesChanged(object sender, EventArgs e)
        {
            if (ReferenceEquals(sender, _activeProfile))
                RefreshPluginStatuses(sender, e);
            QueueTabStatusRefresh();
        }

        private void Profile_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (ReferenceEquals(sender, _activeProfile))
                RefreshRuntimeControls();
            if (e.PropertyName == nameof(Profile.IsActive) || e.PropertyName == nameof(Profile.IsInjected) ||
                e.PropertyName == nameof(Profile.LiveActivityId))
                QueueTabStatusRefresh();
        }

        private void ProfileListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _activeProfile = (Profile)ProfileListBox.SelectedItem;
            RefreshActivityPlugins();
        }

        private async void PluginEnabledCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (!HasProfileSelected)
                return;
            var box = (CheckBox)sender;
            var row = (ActivityPluginModel)box.DataContext;
            // Staging is independent of the loader's live snapshot and activity ownership.
            _activeActivity.SetEnabled(_activeProfile.Name, row.Key, box.IsChecked == true);
            await SaveConfigurationAsync();
        }

        private async void Plugins_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (_batchingLibraryChanges) return;
            ReconcileLibraryRemoval();
            RefreshLibraryPlugins();
            RefreshActivityPlugins();
            await SaveConfigurationAsync();
        }

        private void ReconcileLibraryRemoval()
        {
            foreach (Activity activity in Config.Activities)
                foreach (string key in activity.PluginKeys.Where(key => !Config.Plugins.ContainsKey(key)).ToArray())
                    activity.RemovePlugin(key);
        }

        private async void ShowAddPluginDialog(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "DLL Files (*.dll)|*.dll",
                Multiselect = true,
                Title = "Select Plugin DLLs"
            };
            if (dialog.ShowDialog() == true)
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
            if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;
            e.Handled = true;
            var paths = e.Data.GetData(DataFormats.FileDrop) as string[];
            if (paths != null)
                await AddPluginFilesAsync(paths);
        }

        private async Task AddPluginFilesAsync(IEnumerable<string> paths)
        {
            WorkspaceTabs.SelectedItem = LibraryTab;
            var failures = new List<string>();
            _batchingLibraryChanges = true;
            try
            {
                foreach (string dllPath in paths.Where(IsDllPath))
                {
                    try
                    {
                        if (!File.Exists(dllPath)) continue;
                        if (dllPath.IndexOf("\\obj\\", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            failures.Add(dllPath + ": use the DLL from \\bin\\ instead of \\obj\\.");
                            continue;
                        }

                        string key = Utils.HashFromFile(dllPath);
                        if (Config.Plugins.ContainsKey(key)) continue;
                        var fileInfo = FileVersionInfo.GetVersionInfo(dllPath);
                        Config.Plugins.Add(key, new PluginModel
                        {
                            Name = string.IsNullOrWhiteSpace(fileInfo.ProductName)
                                ? Path.GetFileNameWithoutExtension(dllPath) : fileInfo.ProductName,
                            Version = fileInfo.FileVersion,
                            Path = dllPath
                        });
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Failed to add plugin {Path}", dllPath);
                        failures.Add(dllPath + ": " + ex.Message);
                    }
                }
            }
            finally
            {
                _batchingLibraryChanges = false;
                RefreshLibraryPlugins();
                RefreshActivityPlugins();
            }
            await SaveConfigurationAsync();
            if (failures.Count > 0)
                await this.ShowMessageAsync("Could not add some plugins", string.Join(Environment.NewLine, failures));
        }

        private void LibraryGrid_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            var target = e.OriginalSource as DependencyObject;
            var row = target == null ? null : ItemsControl.ContainerFromElement(LibraryGrid, target) as DataGridRow;
            if (row != null && !row.IsSelected)
                LibraryGrid.SelectedItem = row.Item;
        }

        private void RemoveSelectedPlugins_Click(object sender, RoutedEventArgs e)
        {
            var selected = LibraryGrid.SelectedItems.Cast<KeyValuePair<string, PluginModel>>().ToArray();
            // Batch notifications/saving; removing entries never changes a running loader.
            _batchingLibraryChanges = true;
            try
            {
                foreach (var plugin in selected)
                    Config.Plugins.Remove(plugin.Key, plugin.Value);
            }
            finally
            {
                _batchingLibraryChanges = false;
            }
            FinishLibraryRemoval();
        }

        private async void FinishLibraryRemoval()
        {
            ReconcileLibraryRemoval();
            RefreshLibraryPlugins();
            RefreshActivityPlugins();
            await SaveConfigurationAsync();
        }

        private void RemoveAllPlugins_Click(object sender, RoutedEventArgs e)
        {
            Config.Plugins.Clear();
        }

        private string[] SelectedPaths(Activity activity, Profile profile)
        {
            return Config.Plugins.Where(plugin => activity.PluginKeys.Contains(plugin.Key) &&
                    activity.IsEnabled(profile.Name, plugin.Key))
                .Select(plugin => plugin.Value.Path).ToArray();
        }

        private async void InjectButton_Clicked(object sender, RoutedEventArgs e)
        {
            if (!CanInject) return;
            string[] plugins = SelectedPaths(_activeActivity, _activeProfile);
            if (plugins.Length == 0)
            {
                await this.ShowMessageAsync("No plugins selected", "Choose the plugins for this character first.");
                return;
            }
            if (!_activeProfile.Inject(plugins, _activeActivity.Id, _activeActivity.Name))
                await this.ShowMessageAsync("Injection failed", "Could not inject " + _activeProfile.Name + ".");
        }

        private async void EjectButton_Clicked(object sender, RoutedEventArgs e)
        {
            if (!CanEject) return;
            try
            {
                _activeProfile.Eject(_activeActivity.Id);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to eject character {Name}", _activeProfile.Name);
                await this.ShowMessageAsync("Ejection failed", ex.Message);
            }
        }

        private async void InjectAllButton_Clicked(object sender, RoutedEventArgs e)
        {
            if (_activeActivity == null) return;
            Activity activity = _activeActivity;
            var profiles = Profiles.Where(profile => profile.IsActive && !profile.IsInjected).ToArray();
            var failures = new List<string>();
            foreach (Profile profile in profiles)
            {
                string[] plugins = SelectedPaths(activity, profile);
                if (plugins.Length == 0) continue;
                if (!profile.Inject(plugins, activity.Id, activity.Name))
                    failures.Add(profile.Name);
            }
            if (failures.Count > 0)
                await this.ShowMessageAsync("Injection failed", "Could not inject: " + string.Join(", ", failures));
        }

        private async void EjectAllButton_Clicked(object sender, RoutedEventArgs e)
        {
            if (_activeActivity == null) return;
            string activityId = _activeActivity.Id;
            var profiles = Profiles.Where(profile => profile.IsActive && profile.IsInjected &&
                profile.LiveActivityId == activityId).ToArray();
            var failures = new List<string>();
            foreach (Profile profile in profiles)
            {
                try { profile.Eject(activityId); }
                catch (Exception ex)
                {
                    Log.Error(ex, "Failed to eject character {Name}", profile.Name);
                    failures.Add(profile.Name);
                }
            }
            if (failures.Count > 0)
                await this.ShowMessageAsync("Ejection failed", "Could not eject: " + string.Join(", ", failures));
        }

        private void TweaksButton_Click(object sender, RoutedEventArgs e)
        {
            new TweaksWindow { Owner = this }.ShowDialog();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            // Retry a failed write before offering to discard changes still held in memory.
            if (SaveFailed && TrySaveConfiguration() != null &&
                MessageBox.Show(this, "Your latest setup changes could not be saved. Close without saving them?",
                    "Unsaved setup", MessageBoxButton.YesNo, MessageBoxImage.Warning,
                    MessageBoxResult.No) != MessageBoxResult.Yes)
            {
                e.Cancel = true;
                base.OnClosing(e);
                return;
            }

            Properties.Settings.Default.WindowLocation = new Point(RestoreBounds.Left, RestoreBounds.Top);
            Properties.Settings.Default.WindowSize = new Size(RestoreBounds.Width, RestoreBounds.Height);
            Properties.Settings.Default.Save();
            base.OnClosing(e);
        }

        protected override void OnClosed(EventArgs e)
        {
            _closed = true;
            Profiles.CollectionChanged -= Profiles_CollectionChanged;
            foreach (Profile profile in _observedProfiles)
            {
                profile.PropertyChanged -= Profile_PropertyChanged;
                profile.PluginStatusesChanged -= Profile_PluginStatusesChanged;
            }
            _observedProfiles.Clear();
            base.OnClosed(e);
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            if (Properties.Settings.Default.WindowSize.Width > 0 && Properties.Settings.Default.WindowSize.Height > 0)
            {
                Width = Properties.Settings.Default.WindowSize.Width;
                Height = Properties.Settings.Default.WindowSize.Height;
            }
            if (Properties.Settings.Default.WindowLocation.X >= 0 && Properties.Settings.Default.WindowLocation.Y >= 0)
            {
                Left = Properties.Settings.Default.WindowLocation.X;
                Top = Properties.Settings.Default.WindowLocation.Y;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private sealed class ActivityMembershipConverter : IValueConverter
        {
            private readonly Activity _activity;
            public ActivityMembershipConverter(Activity activity) { _activity = activity; }
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                return value is string key && _activity.PluginKeys.Contains(key);
            }
            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                return Binding.DoNothing;
            }
        }
    }

    [ValueConversion(typeof(bool), typeof(Visibility))]
    public sealed class BoolToVisibilityConverter : IValueConverter
    {
        public Visibility TrueValue { get; set; } = Visibility.Visible;
        public Visibility FalseValue { get; set; } = Visibility.Collapsed;
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is bool flag ? (flag ? TrueValue : FalseValue) : Visibility.Collapsed;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Equals(value, TrueValue);
        }
    }

    public class PathIsInvalidConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is string path && path.IndexOf(@"\obj\", StringComparison.OrdinalIgnoreCase) >= 0;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

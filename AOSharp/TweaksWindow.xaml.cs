using System;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using AOSharp.Tweaks;

namespace AOSharp
{
    public partial class TweaksWindow : MetroWindow, INotifyPropertyChanged
    {
        private string _installDirectory;
        private bool _isProcessing;
        private FullSpeedBackgroundTweak.PatchState _backgroundModeState;
        private string _backgroundModeStatus;

        public bool IsIdle => !_isProcessing;
        public string BackgroundModeStatus => _backgroundModeStatus;
        public bool CanEnableFullSpeedBackground => IsValidInstallDirectory && IsIdle
            && _backgroundModeState == FullSpeedBackgroundTweak.PatchState.Disabled;
        public bool CanRestoreBackgroundThrottle => IsValidInstallDirectory && IsIdle
            && _backgroundModeState == FullSpeedBackgroundTweak.PatchState.Enabled;

        public string InstallDirectory
        {
            get => _installDirectory;
            set
            {
                _installDirectory = value;
                OnPropertyChanged(nameof(InstallDirectory));
                OnPropertyChanged(nameof(IsValidInstallDirectory));
                OnPropertyChanged(nameof(CanEnableLargeAddressAware));
                RefreshBackgroundMode();
            }
        }

        public bool IsValidInstallDirectory
        {
            get
            {
                if (string.IsNullOrEmpty(InstallDirectory))
                    return false;

                string versionFile = Path.Combine(InstallDirectory, "version.id");
                return Directory.Exists(InstallDirectory) && File.Exists(versionFile);
            }
        }

        public bool CanEnableLargeAddressAware => IsValidInstallDirectory && !_isProcessing;

        public TweaksWindow()
        {
            InitializeComponent();
            DataContext = this;
            InstallDirectory = "";
            _isProcessing = false;

            // Ensure button state is updated
            OnPropertyChanged(nameof(CanEnableLargeAddressAware));
        }

        private async void BrowseDirectoryButton_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Select AO Install Directory";
                dialog.ShowNewFolderButton = true;

                if (!string.IsNullOrEmpty(InstallDirectory) && Directory.Exists(InstallDirectory))
                {
                    dialog.SelectedPath = InstallDirectory;
                }

                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    // Check if version.id exists in the selected directory
                    string versionFile = Path.Combine(dialog.SelectedPath, "version.id");

                    if (File.Exists(versionFile))
                    {
                        InstallDirectory = dialog.SelectedPath;
                    }
                    else
                    {
                        await this.ShowMessageAsync(
                            "Invalid Directory",
                            "The selected directory does not appear to be a valid AO installation.\nThe file 'version.id' was not found.",
                            MessageDialogStyle.Affirmative,
                            new MetroDialogSettings
                            {
                                AffirmativeButtonText = "OK"
                            });
                    }
                }
            }
        }

        private async void EnableLargeAddressAwareButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isProcessing) return;
            var result = await this.ShowMessageAsync(
                "Confirmation",
                "Warning: This will make irreversible modifications to game files, do you want to proceed?",
                MessageDialogStyle.AffirmativeAndNegative,
                new MetroDialogSettings
                {
                    AffirmativeButtonText = "Yes",
                    NegativeButtonText = "No",
                    DefaultButtonFocus = MessageDialogResult.Negative
                });

            if (result == MessageDialogResult.Affirmative)
            {
                // Set processing flag
                SetProcessing(true);

                // Show progress dialog
                var progressController = await this.ShowProgressAsync(
                    "Processing",
                    "Enabling large address aware functionality...",
                    false);

                progressController.SetIndeterminate();

                try
                {
                    // Run the operation on a background thread
                    await Task.Run(() => new LargeAddressAwareTweak(InstallDirectory).Run());

                    await progressController.CloseAsync();

                    await this.ShowMessageAsync(
                        "Success",
                        "Large address aware functionality has been enabled successfully.",
                        MessageDialogStyle.Affirmative,
                        new MetroDialogSettings
                        {
                            AffirmativeButtonText = "OK"
                        });
                }
                catch (Exception ex)
                {
                    await progressController.CloseAsync();

                    await this.ShowMessageAsync(
                        "Error",
                        $"An error occurred while enabling large address aware:\n{ex.Message}",
                        MessageDialogStyle.Affirmative,
                        new MetroDialogSettings
                        {
                            AffirmativeButtonText = "OK"
                        });
                }
                finally
                {
                    // Clear processing flag
                    SetProcessing(false);
                    RefreshBackgroundMode();
                }
            }
        }

        private void SetProcessing(bool processing)
        {
            _isProcessing = processing;
            OnPropertyChanged(nameof(IsIdle));
            OnPropertyChanged(nameof(CanEnableLargeAddressAware));
            OnPropertyChanged(nameof(CanEnableFullSpeedBackground));
            OnPropertyChanged(nameof(CanRestoreBackgroundThrottle));
        }

        private void RefreshBackgroundMode()
        {
            _backgroundModeState = FullSpeedBackgroundTweak.PatchState.Missing;
            _backgroundModeStatus = "Select the new-client installation directory.";
            if (IsValidInstallDirectory)
            {
                try
                {
                    _backgroundModeState = new FullSpeedBackgroundTweak(InstallDirectory).Inspect();
                    switch (_backgroundModeState)
                    {
                        case FullSpeedBackgroundTweak.PatchState.Disabled:
                            _backgroundModeStatus = "Full-speed background: disabled.";
                            break;
                        case FullSpeedBackgroundTweak.PatchState.Enabled:
                            _backgroundModeStatus = "Full-speed background: enabled.";
                            break;
                        case FullSpeedBackgroundTweak.PatchState.Missing:
                            _backgroundModeStatus = "Cheetah.dll was not found. This option is for the new client.";
                            break;
                        default:
                            _backgroundModeStatus = "This Cheetah.dll build is not supported; it will not be modified.";
                            break;
                    }
                }
                catch (Exception error)
                {
                    _backgroundModeState = FullSpeedBackgroundTweak.PatchState.Unsupported;
                    _backgroundModeStatus = error.Message;
                }
            }
            OnPropertyChanged(nameof(BackgroundModeStatus));
            OnPropertyChanged(nameof(CanEnableFullSpeedBackground));
            OnPropertyChanged(nameof(CanRestoreBackgroundThrottle));
        }

        private async void EnableFullSpeedBackgroundButton_Click(object sender, RoutedEventArgs e)
        {
            await ChangeBackgroundModeAsync(true);
        }

        private async void RestoreBackgroundThrottleButton_Click(object sender, RoutedEventArgs e)
        {
            await ChangeBackgroundModeAsync(false);
        }

        private async Task ChangeBackgroundModeAsync(bool enabled)
        {
            if (_isProcessing) return;
            string directory = InstallDirectory;
            string title;
            string message;
            SetProcessing(true);
            try
            {
                await Task.Run(() => new FullSpeedBackgroundTweak(directory).SetEnabled(enabled));
                title = "Background mode updated";
                message = enabled
                    ? "Full-speed background is enabled. Start AO normally. The original DLL backup is retained beside Cheetah.dll."
                    : "The original background throttle has been restored. Start AO normally.";
            }
            catch (Exception error)
            {
                title = "Background mode could not be changed";
                message = error.Message;
            }
            finally
            {
                SetProcessing(false);
                RefreshBackgroundMode();
            }
            await this.ShowMessageAsync(title, message, MessageDialogStyle.Affirmative,
                new MetroDialogSettings { AffirmativeButtonText = "OK" });
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            if (_isProcessing) e.Cancel = true;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
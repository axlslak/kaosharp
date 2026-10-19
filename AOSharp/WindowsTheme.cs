using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using MahApps.Metro;
using Microsoft.Win32;

namespace AOSharp
{
    // Keep the existing control templates, but take their colour roles from Windows.
    // UISettings supplies modern app colours; SystemColors supplies contrast/legacy schemes.
    internal sealed class WindowsTheme : IDisposable
    {
        private readonly Application _app;
        private readonly ResourceDictionary _colours = new ResourceDictionary();
        private volatile bool _disposed;
        private int _refreshPending;

        public WindowsTheme(Application app)
        {
            _app = app;
            _app.Resources.MergedDictionaries.Add(_colours);
            Apply();
            // MahApps dialogs detect a registered theme/accent and retain its brushes.
            ThemeManager.AddAppTheme("Windows", _colours);
            SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;
            SystemParameters.StaticPropertyChanged += OnSystemPropertyChanged;
        }

        private void OnUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
        {
            // Includes colour, accessibility and application-mode preference broadcasts.
            QueueRefresh();
        }

        private void OnSystemPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (string.IsNullOrEmpty(e.PropertyName) || e.PropertyName == nameof(SystemParameters.HighContrast))
                QueueRefresh();
        }

        private void QueueRefresh()
        {
            if (_disposed || _app.Dispatcher.HasShutdownStarted || Interlocked.Exchange(ref _refreshPending, 1) != 0)
                return;

            try
            {
                // Let WPF process the system-colour invalidation before reading SystemColors.
                _app.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                {
                    Interlocked.Exchange(ref _refreshPending, 0);
                    if (!_disposed)
                        Apply();
                }));
            }
            catch (InvalidOperationException)
            {
                Interlocked.Exchange(ref _refreshPending, 0); // Dispatcher is shutting down.
            }
        }

        private void Apply()
        {
            var p = Palette.Read();
            var r = _colours;
            // Status colours supplement Windows surfaces; contrast schemes retain their own ink/paper.
            bool contrast = SystemParameters.HighContrast;
            SetBrushes(r, contrast ? p.Surface : Colors.White, "ActivityNeutralBrush");
            SetBrushes(r, contrast ? p.Text : Colors.Black, "ActivityNeutralTextBrush");
            SetBrushes(r, contrast ? p.Surface : Colors.Orange, "ActivityUnknownBrush");
            SetBrushes(r, contrast ? p.Text : Colors.Black, "ActivityUnknownTextBrush");
            SetBrushes(r, contrast ? p.Surface : Colors.Green, "ActivityInjectedBrush");
            SetBrushes(r, contrast ? p.Surface : Colors.Firebrick, "ActivityFailedBrush");
            SetBrushes(r, contrast ? p.Text : Colors.White, "ActivityStatusTextBrush");
            SetBrushes(r, contrast ? p.Text : StatusText(Colors.Green, p.Surface), "PluginInitializedTextBrush");
            SetBrushes(r, contrast ? p.Text : StatusText(Colors.Firebrick, p.Surface), "PluginFailedTextBrush");
            SetBrushes(r, contrast ? p.SelectionText : StatusText(Colors.Green, p.Selection), "PluginInitializedSelectedTextBrush");
            SetBrushes(r, contrast ? p.SelectionText : StatusText(Colors.Firebrick, p.Selection), "PluginFailedSelectedTextBrush");
            SetBrushes(r, p.Surface, "WindowBackgroundBrush", "ControlBackgroundBrush", "WhiteBrush",
                "WhiteColorBrush", "DisabledWhiteBrush", "CheckBoxBackgroundBrush", "FlatButtonBackgroundBrush",
                "FlyoutBackgroundBrush", "MahApps.Metro.HamburgerMenu.PaneBackgroundBrush");
            SetBrushes(r, p.Text, "BlackBrush", "BlackColorBrush", "TextBrush", "LabelTextBrush",
                "FlatButtonForegroundBrush", "FlyoutForegroundBrush", "GrayBrush1", "GrayNormalBrush",
                "GrayHoverBrush", "MahApps.Metro.HamburgerMenu.PaneForegroundBrush");
            SetBrushes(r, p.Control, "WindowsControlBrush", "GrayBrush8", "GrayBrush10");
            SetBrushes(r, p.ControlText, "WindowsControlTextBrush");
            SetBrushes(r, p.Surface, "WindowTitleColorBrush", "NonActiveWindowTitleColorBrush");
            SetBrushes(r, p.Border, "GrayBrush2", "GrayBrush3", "GrayBrush4", "GrayBrush5", "GrayBrush6",
                "WindowsBorderBrush", "GrayBrush9", "ControlBorderBrush", "TextBoxBorderBrush",
                "ComboBoxPopupBorderBrush", "ContextMenuBorderBrush", "SubMenuBorderBrush", "SeperatorBrush",
                "ThumbBrush", "NonActiveBorderColorBrush", "CheckBoxBrush",
                "MahApps.Metro.Brushes.ToggleSwitchButton.OffBorderBrush.Win10");
            SetBrushes(r, p.Text, "TextBoxMouseOverInnerBorderBrush", "TextBoxFocusBorderBrush",
                "TextBoxMouseOverBorderBrush", "ButtonMouseOverBorderBrush", "ButtonMouseOverInnerBorderBrush",
                "ComboBoxMouseOverBorderBrush", "ComboBoxMouseOverInnerBorderBrush", "CheckBoxMouseOverBrush");
            SetBrushes(r, p.Disabled, "MenuItemDisabledBrush", "DisabledMenuItemForeground",
                "DisabledMenuItemGlyphPanel", "DarkIdealForegroundDisabledBrush", "MetroDataGrid.DisabledHighlightBrush",
                "IdealForegroundDisabledBrush", "SliderValueDisabled", "SliderTrackDisabled", "SliderThumbDisabled",
                "MahApps.Metro.Brushes.ToggleSwitchButton.OffDisabledBorderBrush.Win10",
                "MahApps.Metro.Brushes.ToggleSwitchButton.OnSwitchDisabledBrush.Win10",
                "MahApps.Metro.Brushes.ToggleSwitchButton.ThumbIndicatorDisabledBrush.Win10");
            SetBrushes(r, p.Popup, "ComboBoxPopupBrush", "MenuBackgroundBrush", "ContextMenuBackgroundBrush",
                "SubMenuBackgroundBrush", "MenuItemBackgroundBrush");
            SetBrushes(r, p.Text, "HighlightBrush"); // Checkbox ink must contrast with its window surface.
            SetBrushes(r, p.PopupText, "CheckmarkFill", "RightArrowFill");
            SetBrushes(r, p.Accent, "AccentBaseColorBrush", "AccentColorBrush",
                "AccentColorBrush2", "AccentColorBrush3", "AccentColorBrush4",
                "MetroDataGrid.FocusBorderBrush", "SliderTrackHover", "SliderTrackNormal",
                "MahApps.Metro.Brushes.ToggleSwitchButton.OnSwitchBrush.Win10",
                "MahApps.Metro.Brushes.ToggleSwitchButton.OnSwitchMouseOverBrush.Win10");
            SetBrushes(r, p.Selection, "WindowsSelectionBrush", "GrayBrush7", "MetroDataGrid.HighlightBrush",
                "MetroDataGrid.MouseOverHighlightBrush", "MetroDataGrid.InactiveSelectionHighlightBrush",
                "MenuItemSelectionFill", "MenuItemSelectionStroke", "TopMenuItemPressedFill",
                "TopMenuItemPressedStroke", "TopMenuItemSelectionStroke", "FlatButtonPressedBackgroundBrush",
                "CleanWindowCloseButtonBackgroundBrush", "CleanWindowCloseButtonPressedBackgroundBrush");
            SetBrushes(r, p.SelectionText, "WindowsSelectionTextBrush", "IdealForegroundColorBrush",
                "AccentSelectedColorBrush", "MetroDataGrid.HighlightTextBrush",
                "MetroDataGrid.InactiveSelectionHighlightTextBrush", "FlatButtonPressedForegroundBrush",
                "MahApps.Metro.Brushes.ToggleSwitchButton.ThumbIndicatorCheckedBrush.Win10");
            SetBrushes(r, p.Text, "ControlsValidationBrush", "ValidationBrush1", "ValidationBrush2",
                "ValidationBrush3", "ValidationBrush4", "ValidationBrush5", "ValidationSummaryColor1",
                "ValidationSummaryColor2", "ValidationSummaryColor3", "ValidationSummaryColor4",
                "ValidationSummaryColor5");
            SetBrushes(r, p.Surface, "ValidationSummaryFillColor1", "ValidationSummaryFillColor2", "ControlsDisabledBrush");
            SetBrushes(r, p.Text, "ValidationTextBrush", "MahApps.Metro.Brushes.ToggleSwitchButton.ThumbIndicatorBrush.Win10",
                "MahApps.Metro.Brushes.ToggleSwitchButton.ThumbIndicatorMouseOverBrush.Win10",
                "MahApps.Metro.Brushes.ToggleSwitchButton.ThumbIndicatorPressedBrush.Win10",
                "MahApps.Metro.Brushes.ToggleSwitchButton.OffMouseOverBorderBrush.Win10");
            SetBrushes(r, p.Control, "MahApps.Metro.Brushes.ToggleSwitchButton.OffSwitchBrush.Win10",
                "MahApps.Metro.Brushes.ToggleSwitchButton.PressedBrush.Win10", "MahApps.Metro.Brushes.Badged.DisabledBackgroundBrush");
            // These resources are hit-test/disabled overlays, not independent surface colours.
            r["TransparentWhiteBrush"] = Brushes.Transparent;
            r["SemiTransparentWhiteBrush"] = Brushes.Transparent;
            r["SemiTransparentGreyBrush"] = Brushes.Transparent;
            var progress = new LinearGradientBrush(p.Accent, p.Accent, 0);
            progress.Freeze();
            r["ProgressBrush"] = progress; // Retain the toolkit's expected brush type.

            r["BlackColor"] = p.Text;
            r["WhiteColor"] = p.Surface;
            r["IdealForegroundColor"] = p.SelectionText;
            r["HighlightColor"] = p.Accent;
            r["AccentBaseColor"] = p.Accent;
            for (int i = 1; i <= 4; i++)
            {
                r[i == 1 ? "AccentColor" : "AccentColor" + i] = p.Accent;
                r["ProgressIndeterminateColor" + i] = p.Accent;
            }
            for (int i = 1; i <= 10; i++)
                r["Gray" + i] = i == 1 ? p.Text : i == 7 ? p.Selection : i == 8 || i == 10 ? p.Control : p.Border;
            r["GrayNormal"] = p.Text;
            r["GrayHover"] = p.Text;
            r["MenuShadowColor"] = p.Border;

            // MahApps also shadows a few WPF system keys. Override the whole used set together.
            r[SystemColors.WindowBrushKey] = Brush(p.Surface);
            r[SystemColors.WindowTextBrushKey] = Brush(p.Text);
            r[SystemColors.ControlBrushKey] = Brush(p.Control);
            r[SystemColors.ControlTextBrushKey] = Brush(p.ControlText);
            r[SystemColors.MenuBrushKey] = Brush(p.Popup);
            r[SystemColors.MenuTextBrushKey] = Brush(p.PopupText);
            r[SystemColors.GrayTextBrushKey] = Brush(p.Disabled);
            r[SystemColors.HighlightBrushKey] = Brush(p.Selection);
            r[SystemColors.HighlightTextBrushKey] = Brush(p.SelectionText);
            r[SystemColors.InactiveSelectionHighlightBrushKey] = Brush(p.Selection);
            r[SystemColors.InactiveSelectionHighlightTextBrushKey] = Brush(p.SelectionText);
            r[SystemColors.ControlDarkBrushKey] = Brush(p.Border);
            r[SystemColors.ControlDarkDarkBrushKey] = Brush(p.Border);
            r[SystemColors.ControlLightBrushKey] = Brush(p.Control);
            r[SystemColors.ControlLightLightBrushKey] = Brush(p.Control);
        }

        private static Color StatusText(Color colour, Color background)
        {
            Color ink = WindowsColours.Contrast(Colors.White, background) >=
                WindowsColours.Contrast(Colors.Black, background) ? Colors.White : Colors.Black;
            // Keep the status hue as far as the actual Windows background permits.
            for (int step = 0; step <= 20; step++)
            {
                double amount = step / 20.0;
                Color candidate = Color.FromRgb(
                    (byte)Math.Round(colour.R + (ink.R - colour.R) * amount),
                    (byte)Math.Round(colour.G + (ink.G - colour.G) * amount),
                    (byte)Math.Round(colour.B + (ink.B - colour.B) * amount));
                if (WindowsColours.Contrast(candidate, background) >= 4.5)
                    return candidate;
            }
            return ink;
        }

        private static SolidColorBrush Brush(Color colour)
        {
            var brush = new SolidColorBrush(colour);
            brush.Freeze();
            return brush;
        }

        private static void SetBrushes(ResourceDictionary resources, Color colour, params string[] keys)
        {
            foreach (string key in keys)
            {
                // Preserve brush identity for open dialogs which take a local brush reference.
                var brush = resources[key] as SolidColorBrush;
                if (brush != null && !brush.IsFrozen)
                    brush.Color = colour;
                else
                    resources[key] = new SolidColorBrush(colour);
            }
        }

        public void Dispose()
        {
            _disposed = true;
            SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged;
            SystemParameters.StaticPropertyChanged -= OnSystemPropertyChanged;
        }

        private sealed class Palette
        {
            public Color Surface, Text, Control, ControlText, Border, Disabled, Popup, PopupText, Accent, Selection, SelectionText;

            public static Palette Read()
            {
                var p = new Palette
                {
                    Surface = SystemColors.WindowColor,
                    Text = SystemColors.WindowTextColor,
                    Control = SystemColors.ControlColor,
                    ControlText = SystemColors.ControlTextColor,
                    Border = SystemColors.WindowFrameColor,
                    Disabled = SystemColors.GrayTextColor,
                    Popup = SystemColors.MenuColor,
                    PopupText = SystemColors.MenuTextColor,
                    Accent = SystemColors.HotTrackColor,
                    Selection = SystemColors.HighlightColor,
                    SelectionText = SystemColors.HighlightTextColor
                };
                // A contrast scheme is an explicit user palette, including its selection pair.
                if (!SystemParameters.HighContrast)
                    WindowsColours.TryRead(p);
                return p;
            }
        }

        private static class WindowsColours
        {
            // IUISettings3.GetColorValue and IUISettings.UIElementColor from the Windows SDK ABI.
            private static readonly Guid SettingsId = new Guid("85361600-1C63-4627-BCB1-3A89E0BC9C55");
            private static readonly Guid Settings3Id = new Guid("03021BE4-5254-4781-8194-5168F7D06D7B");

            [StructLayout(LayoutKind.Sequential)]
            private struct NativeColour { public byte A, R, G, B; }

            [UnmanagedFunctionPointer(CallingConvention.StdCall)]
            private delegate int GetColour(IntPtr instance, int role, out NativeColour colour);

            public static void TryRead(Palette palette)
            {
                IntPtr name = IntPtr.Zero, instance = IntPtr.Zero, settings = IntPtr.Zero, settings3 = IntPtr.Zero;
                bool initialized = false;
                try
                {
                    // UISettings is available to desktop apps. No Windows SDK/build dependency needed.
                    if (RoInitialize(0) < 0) // RO_INIT_SINGLETHREADED; called only on the WPF dispatcher.
                        return;
                    initialized = true; // S_OK and S_FALSE must both be balanced.
                    const string className = "Windows.UI.ViewManagement.UISettings";
                    Marshal.ThrowExceptionForHR(WindowsCreateString(className, className.Length, out name));
                    Marshal.ThrowExceptionForHR(RoActivateInstance(name, out instance));
                    var iid = Settings3Id;
                    Marshal.ThrowExceptionForHR(Marshal.QueryInterface(instance, ref iid, out settings3));
                    var getColour = Method(settings3, 6); // IUnknown(3), IInspectable(3), GetColorValue.
                    Color background = Read(getColour, settings3, 0);
                    Color foreground = Read(getColour, settings3, 1);
                    Color accent = Read(getColour, settings3, 5);

                    Color surface = background, popup = background, border = foreground, disabled = foreground;
                    iid = SettingsId;
                    if (Marshal.QueryInterface(instance, ref iid, out settings) >= 0)
                    {
                        var element = Method(settings, 18); // IInspectable + 12 UISettings properties.
                        surface = Optional(element, settings, 1010, background); // PageBackground
                        popup = Optional(element, settings, 1011, surface); // PopupBackground
                        border = Optional(element, settings, 1007, foreground); // NonTextMedium
                        disabled = Optional(element, settings, 1003, foreground); // TextLow
                    }

                    // Optional page roles must remain readable with Windows' authoritative ink.
                    // Retain its foreground/background pair if an optional surface conflicts.
                    if (Contrast(surface, foreground) < 4.5)
                        surface = background;
                    if (Contrast(popup, foreground) < 4.5)
                        popup = surface;

                    // Commit only after the required Windows reads succeed. No RGB theme presets.
                    palette.Surface = surface;
                    palette.Control = surface;
                    palette.ControlText = foreground;
                    palette.Popup = popup;
                    palette.PopupText = foreground;
                    palette.Text = foreground;
                    palette.Border = border;
                    palette.Disabled = disabled;
                    palette.Accent = accent;
                    palette.Selection = accent;
                    // Windows supplies both candidate inks. Choose the readable one for its accent.
                    palette.SelectionText = Contrast(accent, foreground) >= Contrast(accent, background)
                        ? foreground : background;
                }
                catch (Exception e) when (e is COMException || e is DllNotFoundException || e is EntryPointNotFoundException)
                {
                    // Older Windows/API failure: retain actual native system colours, never force dark.
                    Trace.WriteLine("Windows app colours unavailable; using system colours. " + e.Message);
                }
                finally
                {
                    if (settings != IntPtr.Zero) Marshal.Release(settings);
                    if (settings3 != IntPtr.Zero) Marshal.Release(settings3);
                    if (instance != IntPtr.Zero) Marshal.Release(instance);
                    if (name != IntPtr.Zero) WindowsDeleteString(name);
                    if (initialized) RoUninitialize();
                }
            }

            private static GetColour Method(IntPtr instance, int slot)
            {
                var address = Marshal.ReadIntPtr(Marshal.ReadIntPtr(instance), slot * IntPtr.Size);
                return (GetColour)Marshal.GetDelegateForFunctionPointer(address, typeof(GetColour));
            }

            private static Color Read(GetColour method, IntPtr instance, int role)
            {
                NativeColour c;
                Marshal.ThrowExceptionForHR(method(instance, role, out c));
                return Color.FromArgb(c.A, c.R, c.G, c.B);
            }

            private static Color Optional(GetColour method, IntPtr instance, int role, Color fallback)
            {
                NativeColour c;
                return method(instance, role, out c) >= 0 && c.A != 0
                    ? Color.FromArgb(c.A, c.R, c.G, c.B) : fallback;
            }

            internal static double Contrast(Color a, Color b)
            {
                double x = Luminance(a), y = Luminance(b);
                return (Math.Max(x, y) + 0.05) / (Math.Min(x, y) + 0.05);
            }

            private static double Luminance(Color c)
            {
                return 0.2126 * Linear(c.R) + 0.7152 * Linear(c.G) + 0.0722 * Linear(c.B);
            }

            private static double Linear(byte value)
            {
                double s = value / 255.0;
                return s <= 0.04045 ? s / 12.92 : Math.Pow((s + 0.055) / 1.055, 2.4);
            }

            [DllImport("combase.dll", ExactSpelling = true)]
            private static extern int RoInitialize(uint type);
            [DllImport("combase.dll", ExactSpelling = true)]
            private static extern void RoUninitialize();
            [DllImport("combase.dll", ExactSpelling = true, CharSet = CharSet.Unicode)]
            private static extern int WindowsCreateString(string value, int length, out IntPtr hstring);
            [DllImport("combase.dll", ExactSpelling = true)]
            private static extern int WindowsDeleteString(IntPtr hstring);
            [DllImport("combase.dll", ExactSpelling = true)]
            private static extern int RoActivateInstance(IntPtr className, out IntPtr instance);
        }
    }
}

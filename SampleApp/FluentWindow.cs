using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;

namespace SampleApp
{
    public class FluentWindow : Window
    {
        #region Native Methods and Declarations

        [StructLayout(LayoutKind.Sequential)]
        public struct MARGINS
        {
            public int leftWidth;
            public int rightWidth;
            public int topHeight;
            public int bottomHeight;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct NCCALCSIZE_PARAMS
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public RECT[] rgrc;
            public WINDOWPOS lppos;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left, Top, Right, Bottom;

            public RECT(int left, int top, int right, int bottom)
            {
                Left = left;
                Top = top;
                Right = right;
                Bottom = bottom;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct WINDOWPOS
        {
            public IntPtr hwnd;
            public IntPtr hwndInsertAfter;
            public int x;
            public int y;
            public int cx;
            public int cy;
            public uint flags;
        }

        [Flags]
        public enum DwmWindowAttribute : uint
        {
            DWMWA_USE_IMMERSIVE_DARK_MODE = 20,
            DWMWA_MICA_EFFECT = 1029
        }

        [DllImport("dwmapi.dll")]
        static extern bool DwmDefWindowProc(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam, ref IntPtr plResult);
        [DllImport("dwmapi.dll")]
        static extern int DwmExtendFrameIntoClientArea(IntPtr hwnd, ref MARGINS margins);
        [DllImport("dwmapi.dll")]
        static extern int DwmSetWindowAttribute(IntPtr hwnd, DwmWindowAttribute dwAttribute, ref int pvAttribute, int cbAttribute);

        [DllImport("user32.dll")]
        static extern int GetSystemMetrics(int smIndex);
        [DllImport("user32.dll", SetLastError = true)]
        static extern bool GetWindowRect(IntPtr hwnd, out RECT lpRect);
        [DllImport("user32.dll")]
        static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll")]
        static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        #endregion

        #region Dependency Properties

        public bool ExtendViewIntoTitlebar
        {
            get { return (bool)GetValue(ExtendViewIntoTitlebarProperty); }
            set { SetValue(ExtendViewIntoTitlebarProperty, value); }
        }
        public static readonly DependencyProperty ExtendViewIntoTitlebarProperty =
            DependencyProperty.Register("ExtendViewIntoTitlebar", typeof(bool), typeof(FluentWindow), new UIPropertyMetadata(false, FluentWindowPropertyChanged));

        public int CaptionHeight
        {
            get { return (int)GetValue(CaptionHeightProperty); }
            set { SetValue(CaptionHeightProperty, value); }
        }
        public static readonly DependencyProperty CaptionHeightProperty =
            DependencyProperty.Register("CaptionHeight", typeof(int), typeof(FluentWindow), new UIPropertyMetadata(28, FluentWindowPropertyChanged));

        public AppThemeColorMode AppThemeColor
        {
            get { return (AppThemeColorMode)GetValue(ThemeColorProperty); }
            set { SetValue(ThemeColorProperty, value); }
        }
        public static readonly DependencyProperty ThemeColorProperty =
            DependencyProperty.Register("AppThemeColor", typeof(AppThemeColorMode), typeof(FluentWindow), new UIPropertyMetadata(AppThemeColorMode.System, FluentWindowPropertyChanged));
        public bool IsDarkMode
        {
            get { return (bool)GetValue(IsDarkModeProperty); }
            private set { SetValue(IsDarkModeProperty, value); }
        }
        public static readonly DependencyProperty IsDarkModeProperty =
            DependencyProperty.Register("IsDarkMode", typeof(bool), typeof(FluentWindow), new UIPropertyMetadata(false));
        public bool EnableMica
        {
            get { return (bool)GetValue(EnableMicaProperty); }
            set { SetValue(EnableMicaProperty, value); }
        }
        public static readonly DependencyProperty EnableMicaProperty =
            DependencyProperty.Register("EnableMica", typeof(bool), typeof(FluentWindow), new UIPropertyMetadata(true, FluentWindowPropertyChanged));
        public bool UseNativeCaptionButtons
        {
            get { return (bool)GetValue(UseNativeCaptionButtonsProperty); }
            private set { SetValue(UseNativeCaptionButtonsProperty, value); }
        }
        public static readonly DependencyProperty UseNativeCaptionButtonsProperty =
            DependencyProperty.Register("UseNativeCaptionButtons", typeof(bool), typeof(FluentWindow), new UIPropertyMetadata(true, FluentWindowPropertyChanged));

        public CaptionButtonHitTest CaptionButtonMouseOver
        {
            get { return (CaptionButtonHitTest)GetValue(CaptionButtonMouseOverProperty); }
            private set { SetValue(CaptionButtonMouseOverProperty, value); }
        }
        public static readonly DependencyProperty CaptionButtonMouseOverProperty =
            DependencyProperty.Register("CaptionButtonMouseOver", typeof(CaptionButtonHitTest), typeof(FluentWindow), new UIPropertyMetadata(CaptionButtonHitTest.None));
        public CaptionButtonHitTest CaptionButtonPressed
        {
            get { return (CaptionButtonHitTest)GetValue(CaptionButtonPressedProperty); }
            private set { SetValue(CaptionButtonPressedProperty, value); }
        }
        public static readonly DependencyProperty CaptionButtonPressedProperty =
            DependencyProperty.Register("CaptionButtonPressed", typeof(CaptionButtonHitTest), typeof(FluentWindow), new UIPropertyMetadata(CaptionButtonHitTest.None));
        public bool HasCaptionButtonMouseLeave
        {
            get { return (bool)GetValue(HasCaptionButtonMouseLeaveProperty); }
            private set { SetValue(HasCaptionButtonMouseLeaveProperty, value); }
        }
        public static readonly DependencyProperty HasCaptionButtonMouseLeaveProperty =
            DependencyProperty.Register("HasCaptionButtonMouseLeave", typeof(bool), typeof(FluentWindow), new UIPropertyMetadata(false));


        private static void FluentWindowPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FluentWindow window)
            {
                window.WindowSettingChanged();
            }
        }

        #endregion

        #region Enums and Flags
        public enum AppThemeColorMode
        {
            Light,
            Dark,
            System
        }

        public enum WindowSizeParam
        {
            Restored = 0,
            Minimized = 1,
            Maximized = 2,
        }

        public enum CaptionButtonHitTest
        {
            None,
            Minimize,
            Maximize,
            Close
        }

        #endregion

        #region Internal Fields

        private IntPtr _hwnd;
        private HwndSource? _hwndSource;
        private DpiScale _dpiScale = new DpiScale(1, 1);
        private int _nativeWindowBorderThickness = 4;
        private WindowSizeParam _windowState = WindowSizeParam.Restored;
        private Border? _windowBorderTemplate;
        private StackPanel? _windowCaptionAreaTemplate;
        private Button? _customMinimizeCaptionButton;
        private Button? _customMaximizeCaptionButton;
        private Button? _customCloseCaptionButton;

        #endregion

        #region Helper Methods

        public static int LogicalToDevicePixels(double value, double dpi)
        {
            return (int)Math.Floor(value * dpi);
        }

        public static double DeviceToLogicalPixels(int value, double dpi)
        {
            return value / dpi;
        }

        public static double DeviceToLogicalPixels(double value, double dpi)
        {
            return value / dpi;
        }

        #endregion

        protected override void OnSourceInitialized(EventArgs e)
        {
            _hwnd = new WindowInteropHelper(this).Handle;
            _hwndSource = HwndSource.FromHwnd(_hwnd);

            _hwndSource.CompositionTarget.BackgroundColor = System.Windows.Media.Color.FromArgb(0, 0, 0, 0);

            _dpiScale = new DpiScale(
                _hwndSource.CompositionTarget.TransformToDevice.M11,
               _hwndSource.CompositionTarget.TransformToDevice.M22
            );

            WindowSettingChanged();
            _hwndSource.AddHook(WndProc);

            base.OnSourceInitialized(e);
        }

        protected override void OnDpiChanged(DpiScale oldDpi, DpiScale newDpi)
        {
            _dpiScale = newDpi;
            base.OnDpiChanged(oldDpi, newDpi);
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (_hwndSource != null)
            {
                _hwndSource.RemoveHook(WndProc);
                _hwndSource.Dispose();
            }

            base.OnClosing(e);
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            IntPtr result = IntPtr.Zero;
            bool dwmDefWndProcCalled = DwmDefWindowProc(hwnd, msg, wParam, lParam, ref result);

            switch (msg)
            {
                case 5: // WM_SIZE
                    WindowSizeChanged(hwnd, (WindowSizeParam)wParam);
                    break;
                case 26: // WM_SETTINGCHANGE
                    WindowSettingChanged();
                    break;
            }

            if (this.WindowStyle != WindowStyle.None)
            {
                switch (msg)
                {
                    case 131: // WM_NCCALCSIZE

                        if (Marshal.PtrToStructure(lParam, typeof(NCCALCSIZE_PARAMS)) is NCCALCSIZE_PARAMS nccsp)
                        {
                            nccsp.rgrc[0].Left += _nativeWindowBorderThickness;
                            nccsp.rgrc[0].Right -= _nativeWindowBorderThickness;
                            nccsp.rgrc[0].Bottom -= _nativeWindowBorderThickness;

                            Marshal.StructureToPtr(nccsp, lParam, false);

                            handled = true;
                            return result;
                        }

                        break;
                    case 132: // WM_NCHITTEST

                        // if DwmDefWindowProc did not handle the message
                        if (result == IntPtr.Zero)
                        {

                            int HTNOWHERE = 0;
                            int HTCLIENT = 1;
                            int HTCAPTION = 2;
                            int HTMINBUTTON = 8;
                            int HTMAXBUTTON = 9;
                            int HTLEFT = 10;
                            int HTRIGHT = 11;
                            int HTTOP = 12;
                            int HTTOPLEFT = 13;
                            int HTTOPRIGHT = 14;
                            int HTCLOSE = 20;

                            int hitTest = HTNOWHERE;
                            int captionHeight = LogicalToDevicePixels(CaptionHeight, _dpiScale.DpiScaleY);

                            short mouseX = (short)(lParam.ToInt32() & 0xFFFF);
                            short mouseY = (short)(lParam.ToInt32() >> 16);

                            _ = GetWindowRect(hwnd, out RECT windowRect);

                            Point windowMousePoint = new(mouseX - windowRect.Left, mouseY - windowRect.Top);

                            if (windowMousePoint.Y <= captionHeight)
                            {
                                hitTest = HTCAPTION;

                                if (windowMousePoint.X <= _nativeWindowBorderThickness)
                                {
                                    hitTest = HTLEFT;
                                }
                                else if (windowRect.Right - mouseX <= _nativeWindowBorderThickness)
                                {
                                    hitTest = HTRIGHT;
                                }

                            }
                            if (windowMousePoint.Y <= _nativeWindowBorderThickness)
                            {
                                hitTest = HTTOP;

                                if (windowMousePoint.X <= _nativeWindowBorderThickness * 2)
                                {
                                    hitTest = HTTOPLEFT;
                                }
                                else if (windowRect.Right - mouseX <= _nativeWindowBorderThickness * 2)
                                {
                                    hitTest = HTTOPRIGHT;
                                }
                            }

                            Point windowClientMousePoint = new(windowMousePoint.X - _nativeWindowBorderThickness, windowMousePoint.Y);

                            Point logicalWindowClientMousePoint =
                                new(
                                    DeviceToLogicalPixels(windowClientMousePoint.X, _dpiScale.DpiScaleX),
                                    DeviceToLogicalPixels(windowClientMousePoint.Y, _dpiScale.DpiScaleY)
                                );

                            #region Custom Caption Buttons Hit Testing

                            if (!UseNativeCaptionButtons && CaptionButtonPressed == CaptionButtonHitTest.None)
                            {

                                if (_windowCaptionAreaTemplate != null)
                                {
                                    Point captionAreaBounds = _windowCaptionAreaTemplate.TransformToAncestor(this).Transform(new Point(0, 0));
                                    if (logicalWindowClientMousePoint.X >= captionAreaBounds.X &&
                                        logicalWindowClientMousePoint.X <= captionAreaBounds.X + _windowCaptionAreaTemplate.ActualWidth &&
                                        logicalWindowClientMousePoint.Y <= _windowCaptionAreaTemplate.ActualHeight)
                                    {
                                        hitTest = GetCaptionButtonHitTest(hwnd, lParam) switch
                                        {
                                            CaptionButtonHitTest.Minimize => HTMINBUTTON,
                                            CaptionButtonHitTest.Maximize => HTMAXBUTTON,
                                            CaptionButtonHitTest.Close => HTCLOSE,
                                            _ => HTNOWHERE,
                                        };
                                    }
                                }
                            }

                            #endregion

                            #region WindowChrome.IsHitTestVisibleInChrome hit test

                            IInputElement inputElement = this.InputHitTest(logicalWindowClientMousePoint);

                            if (inputElement != null)
                            {
                                if (System.Windows.Shell.WindowChrome.GetIsHitTestVisibleInChrome(inputElement))
                                {
                                    hitTest = HTCLIENT;
                                }
                            }

                            #endregion

                            if (hitTest != HTNOWHERE)
                            {
                                handled = true;
                                return (IntPtr)hitTest;
                            }
                        }

                        break;

                    case 160: // WM_NCMOUSEMOVE

                        if (!UseNativeCaptionButtons)
                        {
                            CaptionButtonMouseOver = GetCaptionButtonHitTest(hwnd, lParam);

                            HasCaptionButtonMouseLeave = false;

                            if (CaptionButtonPressed != CaptionButtonHitTest.None &&
                                CaptionButtonMouseOver != CaptionButtonPressed)
                            {
                                CaptionButtonMouseOver = CaptionButtonHitTest.None;
                                HasCaptionButtonMouseLeave = true;
                            }
                        }

                        break;
                    case 161: // WM_NCLBUTTONDOWN

                        if (!UseNativeCaptionButtons)
                        {
                            CaptionButtonPressed = GetCaptionButtonHitTest(hwnd, lParam);

                            if (CaptionButtonPressed != CaptionButtonHitTest.None)
                            {
                                handled = true;
                                return result;
                            }
                        }

                        break;

                    case 162: // WM_NCLBUTTONUP

                        if (!UseNativeCaptionButtons)
                        {
                            CaptionButtonMouseOver = GetCaptionButtonHitTest(hwnd, lParam);

                            if (CaptionButtonMouseOver == CaptionButtonPressed)
                            {
                                switch (CaptionButtonPressed)
                                {
                                    case CaptionButtonHitTest.Minimize:
                                        this.WindowState = WindowState.Minimized;
                                        break;
                                    case CaptionButtonHitTest.Maximize:
                                        if (this.WindowState == WindowState.Normal)
                                        {
                                            this.WindowState = WindowState.Maximized;
                                        }
                                        else
                                        {
                                            this.WindowState = WindowState.Normal;
                                        }
                                        break;
                                    case CaptionButtonHitTest.Close:
                                        this.Close();
                                        break;
                                }
                            }

                            CaptionButtonPressed = CaptionButtonHitTest.None;
                        }

                        break;

                    case 674: // WM_NCLMOUSELEAVE

                        if (!UseNativeCaptionButtons)
                        {
                            CaptionButtonMouseOver = CaptionButtonHitTest.None;
                            CaptionButtonPressed = CaptionButtonHitTest.None;
                        }

                        break;
                }
            }

            handled = dwmDefWndProcCalled;
            return result;
        }

        private CaptionButtonHitTest GetCaptionButtonHitTest(IntPtr hwnd, IntPtr lParam)
        {
            short mouseX = (short)(lParam.ToInt32() & 0xFFFF);
            short mouseY = (short)(lParam.ToInt32() >> 16);
            _ = GetWindowRect(hwnd, out RECT windowRect);

            Point windowMousePoint = new(mouseX - windowRect.Left, mouseY - windowRect.Top);
            Point windowClientMousePoint = new(windowMousePoint.X - _nativeWindowBorderThickness, windowMousePoint.Y);
            Point logicalWindowClientMousePoint =
            new(
                DeviceToLogicalPixels(windowClientMousePoint.X, _dpiScale.DpiScaleX),
                DeviceToLogicalPixels(windowClientMousePoint.Y, _dpiScale.DpiScaleY)
            );

            if (_customMinimizeCaptionButton != null &&
                _customMinimizeCaptionButton.IsEnabled &&
                IsCaptionButtonHover(logicalWindowClientMousePoint, _customMinimizeCaptionButton))
            {
                return CaptionButtonHitTest.Minimize;
            }

            if (_customMaximizeCaptionButton != null &&
                _customMaximizeCaptionButton.IsEnabled &&
                IsCaptionButtonHover(logicalWindowClientMousePoint, _customMaximizeCaptionButton))
            {
                return CaptionButtonHitTest.Maximize;
            }

            if (_customCloseCaptionButton != null &&
                _customCloseCaptionButton.IsEnabled &&
                IsCaptionButtonHover(logicalWindowClientMousePoint, _customCloseCaptionButton))
            {
                return CaptionButtonHitTest.Close;
            }

            return CaptionButtonHitTest.None;
        }

        private bool IsCaptionButtonHover(Point logicalMouseRelativeToWindow, Button? button)
        {
            if (button == null)
                return false;

            Point buttonPoint = button.TransformToAncestor(this).Transform(new Point(0, 0));
            if (logicalMouseRelativeToWindow.X >= buttonPoint.X &&
                logicalMouseRelativeToWindow.X <= buttonPoint.X + button.ActualWidth &&
                logicalMouseRelativeToWindow.Y <= button.ActualHeight)
            {
                return true;
            }

            return false;
        }

        private void RefreshWindowInnerBorder()
        {
            if (_windowBorderTemplate == null)
                return;

            switch (_windowState)
            {
                case WindowSizeParam.Restored:
                    _windowBorderTemplate.Padding =
                        new Thickness(0);
                    break;
                case WindowSizeParam.Maximized:
                    _windowBorderTemplate.Padding =
                        new Thickness(0, DeviceToLogicalPixels(_nativeWindowBorderThickness, _dpiScale.DpiScaleY), 0, 0);
                    break;
            }
        }

        private void WindowSizeChanged(IntPtr hwnd, WindowSizeParam param)
        {
            if (_windowState != param)
            {
                _windowState = param;
                RefreshWindowInnerBorder();
            }
        }

        private void WindowSettingChanged()
        {
            if (_hwnd == IntPtr.Zero)
                return;

            int darkModeEnabled = 0;
            int micaEnabled = EnableMica ? 1 : 0;

            #region Hide Native Caption Buttons

            int GWL_STYLE = -16;
            int WS_SYSMENU = 0x80000;

            if (ExtendViewIntoTitlebar &&
                CaptionHeight > 28 &&
                this.WindowStyle != WindowStyle.ToolWindow
            )
            {
                UseNativeCaptionButtons = false;
                _ = SetWindowLong(_hwnd, GWL_STYLE, GetWindowLong(_hwnd, GWL_STYLE) & ~WS_SYSMENU);
            }
            else
            {
                UseNativeCaptionButtons = true;
                _ = SetWindowLong(_hwnd, GWL_STYLE, GetWindowLong(_hwnd, GWL_STYLE) | WS_SYSMENU);
            }

            switch (AppThemeColor)
            {
                case AppThemeColorMode.Dark:
                    darkModeEnabled = 1;
                    break;
                case AppThemeColorMode.Light:
                    darkModeEnabled = 0;
                    break;
                case AppThemeColorMode.System:
                    try
                    {
                        using (Microsoft.Win32.RegistryKey? key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("Software")?.OpenSubKey("Microsoft")?.OpenSubKey("Windows")?.OpenSubKey("CurrentVersion")?.OpenSubKey("Themes")?.OpenSubKey("Personalize"))
                        {
                            if (key != null)
                            {
                                string? keyValue = key.GetValue("AppsUseLightTheme")?.ToString();
                                if (keyValue == "0")
                                {
                                    darkModeEnabled = 1;
                                }
                            }
                        }
                    }
                    catch
                    {
                        darkModeEnabled = 0;
                    }
                    break;
            }

            #endregion

            switch (darkModeEnabled)
            {
                case 0:
                    IsDarkMode = false;
                    break;
                case 1:
                    IsDarkMode = true;
                    break;
            }

            _ = DwmSetWindowAttribute(_hwnd, DwmWindowAttribute.DWMWA_MICA_EFFECT, ref micaEnabled, Marshal.SizeOf(typeof(int)));
            _ = DwmSetWindowAttribute(_hwnd, DwmWindowAttribute.DWMWA_USE_IMMERSIVE_DARK_MODE, ref darkModeEnabled, Marshal.SizeOf(typeof(int)));

            // int SM_CYCAPTION = 4;
            int SM_CXSIZEFRAME = 32;
            int SM_CXPADDEDBORDER = 92;

            _nativeWindowBorderThickness = GetSystemMetrics(SM_CXSIZEFRAME) + GetSystemMetrics(SM_CXPADDEDBORDER);

            MARGINS dwmMargins = new()
            {
                leftWidth = -1,
                rightWidth = -1,
                topHeight = -1,
                bottomHeight = -1
            };

            _ = DwmExtendFrameIntoClientArea(_hwnd, ref dwmMargins);
            RefreshWindowInnerBorder();

        }

        public override void OnApplyTemplate()
        {
            if (GetTemplateChild("PART_Border") is Border templatedBorder)
            {
                _windowBorderTemplate = templatedBorder;
            }

            if (GetTemplateChild("PART_CaptionButtonArea") is StackPanel panel)
            {
                _windowCaptionAreaTemplate = panel;
            }

            if (GetTemplateChild("PART_MinButton") is Button minButton)
            {
                _customMinimizeCaptionButton = minButton;
            }

            if (GetTemplateChild("PART_MaxButton") is Button maxButton)
            {
                _customMaximizeCaptionButton = maxButton;
            }

            if (GetTemplateChild("PART_CloseButton") is Button closeButton)
            {
                _customCloseCaptionButton = closeButton;
            }

            base.OnApplyTemplate();
        }

        static FluentWindow()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(FluentWindow),
                new FrameworkPropertyMetadata(typeof(FluentWindow)));
        }
    }
}

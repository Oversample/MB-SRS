using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;
using Caliburn.Micro;
using Ciribob.DCS.SimpleRadio.Standalone.Client.Singletons;
using Ciribob.DCS.SimpleRadio.Standalone.Client.UI.ClientWindow.ClientSettingsControl.Model;
using Ciribob.DCS.SimpleRadio.Standalone.Client.UI.ClientWindow.MBOverlayWindow;
using Ciribob.DCS.SimpleRadio.Standalone.Common.Models.Player;
using Ciribob.DCS.SimpleRadio.Standalone.Common.Settings;
using Ciribob.DCS.SimpleRadio.Standalone.Client.UI.ClientWindow.RadioOverlayWindow;
using NLog;
using LogManager = NLog.LogManager;
using Windows.Devices.Radios;

namespace Ciribob.DCS.SimpleRadio.Standalone.Client.UI.ClientWindow.MBOverlayWindow
{

    public partial class MBOverlay : Window
    {
        private readonly ClientStateSingleton _clientStateSingleton = ClientStateSingleton.Instance;

        private readonly GlobalSettingsStore _globalSettings = GlobalSettingsStore.Instance;
        private readonly RadioControlGroup[] radioControlGroup = new RadioControlGroup[1];
        private double _aspectRatio;
        private readonly double _originalMinHeight;
        private readonly double _radioHeight;
        private long _lastFocus;
        public MBOverlay()
        {
            InitializeComponent();
            WindowStartupLocation = WindowStartupLocation.Manual;
            _aspectRatio = MinWidth / MinHeight;


            _originalMinHeight = MinHeight;
            _radioHeight = Radio1.Height;

            AllowsTransparency = true;
            radioControlGroup[0] = Radio1;
            ContainerPanel.MouseLeftButtonDown += WrapPanel_MouseLeftButtonDown;
        }

        private void RadioRefresh(object sender, EventArgs eventArgs)
        {
            var dcsPlayerRadioInfo = _clientStateSingleton.DcsPlayerRadioInfo;

            foreach (var radio in radioControlGroup)
            {
                radio.RepaintRadioStatus();
                radio.RepaintRadioReceive();
            }

            //Intercom.RepaintRadioStatus();
            //TransponderPanel.RepaintTransponderStatus();

            if (dcsPlayerRadioInfo != null && dcsPlayerRadioInfo.IsCurrent())
            {
                var availableRadios = 0;

                for (var i = 0; i < dcsPlayerRadioInfo.radios.Length; i++)
                    if (dcsPlayerRadioInfo.radios[i].modulation != Modulation.DISABLED)
                        availableRadios++;

                if (availableRadios == 6
                    || (dcsPlayerRadioInfo.radios.Length >= 6
                        && dcsPlayerRadioInfo.radios[5].modulation != Modulation.DISABLED))
                {

                    if (MinHeight != _originalMinHeight + _radioHeight * 2)
                    {
                        MinHeight = _originalMinHeight + _radioHeight * 2;
                        Recalculate();
                    }
                }
                else if (availableRadios == 5
                         || (dcsPlayerRadioInfo.radios.Length >= 5
                             && dcsPlayerRadioInfo.radios[4].modulation != Modulation.DISABLED))
                {
                    if (MinHeight != _originalMinHeight + _radioHeight)
                    {
                        MinHeight = _originalMinHeight + _radioHeight;
                        Recalculate();
                    }
                }
                else
                {
                    ResetHeight();
                }

            }
            else
            {
                ResetHeight();
                ControlText.Text = "";
            }

        }

        private void CalculateScale()
        {
            var yScale = ActualHeight / MBOverlayWin.MinWidth;
            var xScale = ActualWidth / MBOverlayWin.MinWidth;
            var value = Math.Min(xScale, yScale);
            ScaleValue = (double)OnCoerceScaleValue(MBOverlayWin, value);
        }

        private static object OnCoerceScaleValue(DependencyObject o, object value)
        {
            var mainWindow = o as MBOverlay;
            if (mainWindow != null)
                return mainWindow.OnCoerceScaleValue((double)value);
            return value;
        }

        protected virtual double OnCoerceScaleValue(double value)
        {
            if (double.IsNaN(value))
                return 1.0f;

            value = Math.Max(0.1, value);
            return value;
        }
        public static readonly DependencyProperty ScaleValueProperty = DependencyProperty.Register("ScaleValue",
            typeof(double), typeof(MBOverlay),
            new UIPropertyMetadata(1.0, OnScaleValueChanged,
                OnCoerceScaleValue));

        public double ScaleValue
        {
            get => (double)GetValue(ScaleValueProperty);
            set => SetValue(ScaleValueProperty, value);
        }

        private static void OnScaleValueChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            var mainWindow = o as MBOverlay;
            if (mainWindow != null)
                mainWindow.OnScaleValueChanged((double)e.OldValue, (double)e.NewValue);
        }

        protected virtual void OnScaleValueChanged(double oldValue, double newValue)
        {
        }

        private void containerPanel_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            //force aspect ratio
            CalculateScale();

            WindowState = WindowState.Normal;
        }

        private void Button_Minimise(object sender, RoutedEventArgs e)
        {
            // Minimising a window without a taskbar icon leads to the window's menu bar still showing up in the bottom of screen
            // Since controls are unusable, but a very small portion of the always-on-top window still showing, we're closing it instead, similar to toggling the overlay
            if (_globalSettings.GetClientSettingBool(GlobalSettingsKeys.RadioOverlayTaskbarHide))
                Close();
            else
                WindowState = WindowState.Minimized;
        }

        private void Button_Close(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void windowOpacitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Opacity = e.NewValue;
        }
        private void RadioOverlayWindow_OnLocationChanged(object sender, EventArgs e)
        {
            //reset last focus so we dont switch back to dcs while dragging
            _lastFocus = DateTime.Now.Ticks;
        }
        private void WrapPanel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }
        private void ResetHeight()
        {
            if (MinHeight != _originalMinHeight)
            {
                MinHeight = _originalMinHeight;
                Recalculate();
            }
        }

        private void Recalculate()
        {
            _aspectRatio = MinWidth / MinHeight;
            containerPanel_SizeChanged(null, null);
            Height = Height + 1;
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            if (sizeInfo.WidthChanged)
                Width = sizeInfo.NewSize.Height * _aspectRatio;
            else
                Height = sizeInfo.NewSize.Width / _aspectRatio;


            // Console.WriteLine(this.Height +" width:"+ this.Width);
        }
    }
}

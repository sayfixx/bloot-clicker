using System;
using System.Windows.Media;
using SW = System.Windows;

namespace Autoclicker
{
    public partial class CharImageEditor : SW.Window
    {
        private readonly MainWindow _mainWindow;

        public CharImageEditor(MainWindow mw)
        {
            InitializeComponent();
            _mainWindow = mw;

            SetAccentColor((Color)_mainWindow.Resources["ThemeAccentColor"]);
            ApplyTheme(_mainWindow.IsDarkTheme);
            UpdatePosition();
            LoadValues();
        }

        public void SetAccentColor(Color color)
        {
            Resources["ThemeAccentColor"] = color;

            var brush = new SolidColorBrush(color);
            brush.Freeze();
            Resources["ThemeAccentBrush"] = brush;
        }

        public void ApplyTheme(bool dark)
        {
            RootBorder.Background = new SolidColorBrush(dark ? Color.FromRgb(24,24,24) : Colors.White);
            RootBorder.BorderBrush = new SolidColorBrush(dark ? Color.FromRgb(55,55,55) : Color.FromRgb(216,216,216));
            ApplyTextTheme(RootBorder, dark);
        }

        private void ApplyTextTheme(SW.DependencyObject parent, bool dark)
        {
            if (parent == null) return;
            for (int i = 0; i < SW.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = SW.Media.VisualTreeHelper.GetChild(parent, i);
                var tb = child as SW.Controls.TextBlock;
                if (tb != null && tb.Name != "CloseXBtn") tb.Foreground = new SolidColorBrush(dark ? Color.FromRgb(205,205,205) : Color.FromRgb(85,85,85));
                ApplyTextTheme(child, dark);
            }
        }

        private void UpdatePosition()
        {
            if (_mainWindow == null) return;

            Left = _mainWindow.Left + _mainWindow.Width + 10;
            Top = _mainWindow.Top + 20;
        }

        private void LoadValues()
        {
            CharPosXSlider.Value = _mainWindow.CharacterOffsetX;
            CharPosYSlider.Value = _mainWindow.CharacterOffsetY;
            CharWidthSlider.Value = _mainWindow.CharacterWidth;
            CharHeightSlider.Value = _mainWindow.CharacterHeight;
        }

        private void Slider_ValueChanged(object sender, SW.RoutedPropertyChangedEventArgs<double> e)
        {
            if (_mainWindow == null) return;

            var slider = sender as SW.Controls.Slider;
            if (slider == null) return;

            switch (slider.Name)
            {
                case "CharPosXSlider":
                    _mainWindow.CharacterOffsetX = e.NewValue;
                    break;
                case "CharPosYSlider":
                    _mainWindow.CharacterOffsetY = e.NewValue;
                    break;
                case "CharWidthSlider":
                    _mainWindow.CharacterWidth = e.NewValue;
                    break;
                case "CharHeightSlider":
                    _mainWindow.CharacterHeight = e.NewValue;
                    break;
            }

            _mainWindow.ApplyCharacterImageSettings();
        }

        private void ResetBtn_Click(object sender, SW.RoutedEventArgs e)
        {
            _mainWindow.CharacterImagePath = "";

            var image = _mainWindow.FindName("CharacterImage") as SW.Controls.Image;
            if (image != null)
            {
                image.Source = null;
                image.Visibility = SW.Visibility.Collapsed;
            }

            Config.ConfigIO.SaveSilent(_mainWindow);
            Close();
        }

        private void SaveBtn_Click(object sender, SW.RoutedEventArgs e)
        {
            Config.ConfigIO.SaveSilent(_mainWindow);
            Close();
        }

        private void CloseXBtn_Click(object sender, SW.RoutedEventArgs e)
        {
            Close();
        }

        private void Window_MouseLeftButtonDown(object sender, SW.Input.MouseButtonEventArgs e)
        {
            if (e.OriginalSource is SW.Controls.Button)
                return;

            DragMove();
        }

        public void FollowMainWindow()
        {
            UpdatePosition();
        }
    }
}

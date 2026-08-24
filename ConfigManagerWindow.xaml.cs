using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Autoclicker
{
    public partial class ConfigManagerWindow : System.Windows.Window
    {
        private readonly MainWindow _mainWindow;

        public ConfigManagerWindow(MainWindow mainWindow)
        {
            InitializeComponent();
            _mainWindow = mainWindow;
            SetAccentColor((Color)mainWindow.Resources["ThemeAccentColor"]);
            ApplyTheme(mainWindow.IsDarkTheme);
            RefreshList();
            Left = mainWindow.Left + 18;
            Top = mainWindow.Top + 70;
        }

        private void SetAccentColor(Color color)
        {
            var brush = new SolidColorBrush(color);
            brush.Freeze();
            Resources["ThemeAccentBrush"] = brush;
            Resources["ThemeAccentColor"] = color;
        }

        private void ApplyTheme(bool dark)
        {
            Root.Background = new SolidColorBrush(dark ? Color.FromRgb(24,24,24) : Colors.White);
            Root.BorderBrush = new SolidColorBrush(dark ? Color.FromRgb(55,55,55) : Color.FromRgb(216,216,216));
            ConfigList.Background = new SolidColorBrush(dark ? Color.FromRgb(42,42,42) : Color.FromRgb(243,243,245));
            ConfigList.Foreground = new SolidColorBrush(dark ? Color.FromRgb(225,225,225) : Color.FromRgb(26,26,26));
            NameBox.Background = new SolidColorBrush(dark ? Color.FromRgb(42,42,42) : Colors.White);
            NameBox.Foreground = new SolidColorBrush(dark ? Color.FromRgb(225,225,225) : Color.FromRgb(26,26,26));
        }

        private void RefreshList()
        {
            ConfigList.Items.Clear();
            foreach (string name in Config.ConfigProfiles.GetNames()) ConfigList.Items.Add(name);
        }

        private string SelectedName()
        {
            return ConfigList.SelectedItem as string;
        }

        private void ConfigList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SelectedName() != null) NameBox.Text = SelectedName();
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            string name = NameBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(name)) { StatusText.Text = "enter a config name"; return; }
            if (Config.ConfigProfiles.Save(_mainWindow, name)) { StatusText.Text = "saved"; RefreshList(); ConfigList.SelectedItem = name; }
            else StatusText.Text = "save failed";
        }

        private void Load_Click(object sender, RoutedEventArgs e)
        {
            string name = SelectedName() ?? NameBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(name)) { StatusText.Text = "select a config"; return; }
            if (Config.ConfigProfiles.Load(_mainWindow, name)) { StatusText.Text = "loaded"; _mainWindow.ApplyLoadedState(); }
            else StatusText.Text = "load failed";
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            string name = SelectedName();
            if (string.IsNullOrWhiteSpace(name)) { StatusText.Text = "select a config"; return; }
            if (Config.ConfigProfiles.Delete(name)) { StatusText.Text = "deleted"; RefreshList(); }
            else StatusText.Text = "delete failed";
        }

        private void Rename_Click(object sender, RoutedEventArgs e)
        {
            string oldName = SelectedName();
            string newName = NameBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(oldName) || string.IsNullOrWhiteSpace(newName)) { StatusText.Text = "select and enter a name"; return; }
            if (Config.ConfigProfiles.Rename(oldName, newName)) { StatusText.Text = "renamed"; RefreshList(); ConfigList.SelectedItem = newName; }
            else StatusText.Text = "rename failed";
        }
    }
}

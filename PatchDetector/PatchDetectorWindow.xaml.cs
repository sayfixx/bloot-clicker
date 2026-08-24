using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Autoclicker.PatchDetector.Models;
using Autoclicker.PatchDetector.Services;
using Autoclicker.PatchDetector.ViewModels;

namespace Autoclicker.PatchDetector
{
    public partial class PatchDetectorWindow : System.Windows.Window
    {
        private readonly ObservableCollection<PatchCardViewModel> _patches = new();
        private readonly ObservableCollection<PatchCardViewModel> _visiblePatches = new();
        private readonly ProcessMemoryService _memory = new();
        private readonly RuntimePatchService _runtime = new();
        private readonly IReadOnlyList<PatchDefinition> _catalog = McPatchCatalog.Create();

        public PatchDetectorWindow()
        {
            InitializeComponent();
            PatchItemsControl.ItemsSource = _visiblePatches;

            foreach (var patch in _catalog)
            {
                _patches.Add(new PatchCardViewModel(patch));
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.F5)
            {
                _ = RefreshAsync();
            }

            base.OnKeyDown(e);
        }

        private async void ScanButton_OnClick(object sender, RoutedEventArgs e)
        {
            await RefreshAsync();
        }

        private async Task RefreshAsync()
        {
            try
            {
                ScanButton.IsEnabled = false;
                ScanButton.Content = "SCANNING...";
                FooterStatusText.Text = "Scanning Minecraft.Windows.exe...";
                HashTextBlock.Text = "HASH: calculating...";

                await Task.Run(() =>
                {
                    if (!_memory.Attach("Minecraft.Windows"))
                    {
                        Dispatcher.Invoke(() =>
                        {
                            SetOfflineState();
                            MessageBox.Show("Minecraft: Bedrock Edition не запущен.\n\nЗапустите игру перед сканированием.",
                                "Minecraft не найден", MessageBoxButton.OK, MessageBoxImage.Warning);
                        });
                        return;
                    }

                    var regions = _memory.ReadLikelyPatchRegions();
                    var results = _runtime.Scan(regions, _catalog);
                    var hash = GetSha256WithCertUtil(_memory.ExecutablePath);
                    Dispatcher.Invoke(() => ApplyResults(results, hash));
                });
            }
            catch (Exception ex)
            {
                FooterStatusText.Text = ex.Message;
                HashTextBlock.Text = "HASH: -";
            }
            finally
            {
                ScanButton.Content = "SCAN PATCHES";
                ScanButton.IsEnabled = true;
            }
        }

        private void SetOfflineState()
        {
            _visiblePatches.Clear();
            FooterStatusText.Text = "Minecraft.Windows.exe not found.";
            HashTextBlock.Text = "HASH: -";
        }

        private void ApplyResults(IReadOnlyList<PatchScanResult> results, string hash)
        {
            _visiblePatches.Clear();
            foreach (var result in results)
            {
                if (result.Status == PatchStatus.Patched)
                {
                    var vm = new PatchCardViewModel(result.Definition)
                    {
                        StatusText = "patched"
                    };
                    _visiblePatches.Add(vm);
                }
            }

            FooterStatusText.Text = _visiblePatches.Count == 0
                ? "No patched patches found."
                : $"Found {_visiblePatches.Count} patched.";
            HashTextBlock.Text = $"HASH: {hash}";
        }

        private static string GetSha256WithCertUtil(string executablePath)
        {
            if (string.IsNullOrWhiteSpace(executablePath))
            {
                return "-";
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = "CertUtil",
                Arguments = $"-hashfile \"{executablePath}\" SHA256",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            if (process is null)
            {
                return "-";
            }

            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            var hashLine = output
                .Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .FirstOrDefault(line => line.All(ch => char.IsAsciiHexDigit(ch) || ch == ' '));

            return string.IsNullOrWhiteSpace(hashLine) ? "-" : hashLine.Replace(" ", string.Empty);
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Header_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                DragMove();
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            _memory.Dispose();
            base.OnClosed(e);
        }
    }
}

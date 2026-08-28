using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Autoclicker.Clicker;
using Autoclicker.Config;
using XamlAnimatedGif;
using Autoclicker.Deactivation;
using Autoclicker.Discord;
using Autoclicker.Hooks;
using Autoclicker.Minecraft;
using Autoclicker.Window;
using Microsoft.Win32;

namespace Autoclicker
{

			public partial class MainWindow : System.Windows.Window
	{

		public Autoclicker.Controls.DualRangeSlider CpsRangeSliderControl
		{
			get { return this.FindName("CpsRangeSlider") as Autoclicker.Controls.DualRangeSlider; }
		}

		private void MainWindow_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (IsVisible)
            {
                Keyboard.ClearFocus();
                FocusManager.SetFocusedElement(this, null);
            }
        }

        public MainWindow()
		{
			this.InitializeComponent();
			this._utilityRuntimePatcher = new MinecraftRuntimePatcher(this);
            this._patchPageRuntimePatcher = new MinecraftRuntimePatcher(this);
            ApplyAccentColor((Color)FindResource("ThemeAccentColor"));
            IsVisibleChanged += MainWindow_IsVisibleChanged;
			base.Loaded += this.OnLoaded;
			base.Title = "Bloot Clicker";
			this.CurrentCps = (this.TargetCps = (this.MinCps + this.MaxCps) / 2);
			this.InitUI();
			KeyboardHook.Install();
			MouseHook.Install();
			this.CpsChangeTimer.Start();
			this.StartClickerToggleSync();
			this.StartClickThread();
		}

		private void OnLoaded(object sender, RoutedEventArgs e)
		{
			ConfigIO.Load(this);
            string activeConfig = ConfigProfiles.GetActiveName();
            if (!string.IsNullOrWhiteSpace(activeConfig))
            {
                ConfigProfiles.Load(this, activeConfig);
            }
            RefreshConfigCombo(activeConfig);
            ApplyTheme(IsDarkTheme);
            UpdateMinecraftVersionText();
			StartMinecraftVersionDetector();

			if (!string.IsNullOrEmpty(this.BackgroundImagePath))
			{
				ApplyBackgroundImage();
			}

			try
			{
				this.AutoDetectMinecraftExecutable();
			}
			catch
			{
			}

			if (!string.IsNullOrEmpty(this.CharacterImagePath) && File.Exists(this.CharacterImagePath))
			{
				try
				{
					Image image = base.FindName("CharacterImage") as Image;
					if (image != null)
					{
						string ext = Path.GetExtension(this.CharacterImagePath).ToLowerInvariant();
						if (ext == ".gif")
						{
							try
							{
								AnimationBehavior.SetSourceUri(image, new Uri(this.CharacterImagePath, UriKind.Absolute));
							}
							catch
							{
								BitmapImage bitmapImage = new BitmapImage();
								bitmapImage.BeginInit();
								bitmapImage.UriSource = new Uri(this.CharacterImagePath, UriKind.Absolute);
								bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
								bitmapImage.EndInit();
								image.Source = bitmapImage;
							}
						}
						else
						{
							BitmapImage bitmapImage = new BitmapImage();
							bitmapImage.BeginInit();
							bitmapImage.UriSource = new Uri(this.CharacterImagePath, UriKind.Absolute);
							bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
							bitmapImage.EndInit();
							image.Source = bitmapImage;
						}
						image.Width = this.CharacterWidth;
						image.Height = this.CharacterHeight;
						image.Visibility = Visibility.Visible;
						image.Margin = new Thickness(0, 0, this.CharacterMarginRight, this.CharacterOffsetY);
						image.RenderTransform = new TranslateTransform(this.CharacterOffsetX, 0);
					}
				}
				catch { }
			}

			this.ApplyBackgroundImage();

			try
			{
				this.AutoDetectMinecraftExecutable();
			}
			catch
			{
			}
			try
			{
				if (this.DiscordRpcToggle != null)
				{
					this.DiscordRpcToggle.IsChecked = new bool?(this.DiscordRpcEnabled);
				}
				if (this.SettingsFastRefillToggle != null)
				{
					this.SettingsFastRefillToggle.IsChecked = new bool?(this.EasyRefilMode);
				}
				if (this.UtilityItemUseDelayToggle != null)
				{
					this.UtilityItemUseDelayToggle.IsChecked = new bool?(this.UtilityItemUseDelayEnabled);
				}
				if (this.UtilityNoCameraResetToggle != null)
				{
					this.UtilityNoCameraResetToggle.IsChecked = new bool?(this.UtilityNoCameraResetEnabled);
				}
				if (this.UtilityNoHurtCamToggle != null)
				{
					this.UtilityNoHurtCamToggle.IsChecked = new bool?(this.UtilityNoHurtCamEnabled);
				}
				if (this.UtilityPlayScreenFixToggle != null)
				{
					this.UtilityPlayScreenFixToggle.IsChecked = new bool?(this.UtilityPlayScreenFixEnabled);
				}
				UpdateUtilitySupportLabels();
				ApplyUtilityPatchSelection();
				if (this.DiscordRpcEnabled)
				{
					try
					{
						DiscordRpc.Init(this);
						DiscordRpc.Enable();
					}
					catch
					{
					}
				}
			}
			catch
			{
			}
			if (this.StreamerMode)
			{
				base.Dispatcher.BeginInvoke(new Action(delegate()
				{
					Autoclicker.Window.StreamerMode.Apply(this, true);
				}), DispatcherPriority.ApplicationIdle, Array.Empty<object>());
			}
            BeginStartupAnimation();
		}

        private void BeginStartupAnimation()
        {
            try
            {
                RootBorder.Opacity = 0;
                RootScale.ScaleX = 0.96;
                RootScale.ScaleY = 0.96;
                var sb = new Storyboard();
                var op = new DoubleAnimation(0, 1, new Duration(TimeSpan.FromMilliseconds(260)))
                { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } };
                var sx = new DoubleAnimation(0.96, 1, new Duration(TimeSpan.FromMilliseconds(280)))
                { EasingFunction = new BackEase { Amplitude = 0.18, EasingMode = EasingMode.EaseOut } };
                var sy = new DoubleAnimation(0.96, 1, new Duration(TimeSpan.FromMilliseconds(280)))
                { EasingFunction = new BackEase { Amplitude = 0.18, EasingMode = EasingMode.EaseOut } };
                Storyboard.SetTarget(op, RootBorder); Storyboard.SetTargetProperty(op, new PropertyPath(UIElement.OpacityProperty));
                Storyboard.SetTarget(sx, RootScale); Storyboard.SetTargetProperty(sx, new PropertyPath(ScaleTransform.ScaleXProperty));
                Storyboard.SetTarget(sy, RootScale); Storyboard.SetTargetProperty(sy, new PropertyPath(ScaleTransform.ScaleYProperty));
                sb.Children.Add(op); sb.Children.Add(sx); sb.Children.Add(sy); sb.Begin();
            }
            catch { RootBorder.Opacity = 1; }
        }

		private void InitCharacterImageSliders()
		{

		}

		private readonly DispatcherTimer _minecraftVersionTimer = new DispatcherTimer
		{
			Interval = TimeSpan.FromMilliseconds(1000)
		};
		private bool _minecraftVersionTimerStarted;

		private void StartMinecraftVersionDetector()
		{
			if (_minecraftVersionTimerStarted)
			{
				return;
			}
			_minecraftVersionTimerStarted = true;
			_minecraftVersionTimer.Tick += (s, e) => UpdateMinecraftVersionText();
			_minecraftVersionTimer.Start();
		}

		private void UpdateMinecraftVersionText()
		{
			string versionText = "Current Minecraft version: not detected";
			try
			{
				MinecraftInfo info = MinecraftVersionDetector.GetCached();
				if (info.IsRunning && !string.IsNullOrWhiteSpace(info.Version))
				{
					versionText = $"Current Minecraft version: {info.Version}";
				}
				else if (info.IsRunning)
				{
					versionText = "Current Minecraft version: version unknown";
				}
			}
			catch
			{
				versionText = "Current Minecraft version: not detected";
			}

			if (SettingsMinecraftVersionText != null)
			{
				SettingsMinecraftVersionText.Text = versionText;
			}
			if (UtilitiesMinecraftVersionText != null)
			{
				UtilitiesMinecraftVersionText.Text = versionText;
			}
			UpdateUtilitySupportLabels();
		}

		private bool _updatingUtilityToggles;

		private bool TryGetMinecraftVersion(out Version version)
		{
			version = null;

			try
			{
				MinecraftInfo info = MinecraftVersionDetector.GetCached();
				if (info.IsRunning && !string.IsNullOrWhiteSpace(info.Version) &&
					Version.TryParse(info.Version, out version))
				{
					return true;
				}
			}
			catch { }

			try
			{
				string path = this.LastMinecraftExePath;
				if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
					path = MinecraftDiskPatcher.GetDefaultExecutablePath();

				if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
				{
					FileVersionInfo vi = FileVersionInfo.GetVersionInfo(path);
					string raw = vi.ProductVersion;
					if (string.IsNullOrWhiteSpace(raw)) raw = vi.FileVersion;
					if (!string.IsNullOrWhiteSpace(raw))
					{
						raw = raw.Split(' ')[0];
						if (Version.TryParse(raw, out version))
						{
							this.LastMinecraftExePath = path;
							return true;
						}
					}
				}
			}
			catch { }

			return false;
		}

		private bool IsLegacyUtilityVersion()
		{
			Version version;
			if (!TryGetMinecraftVersion(out version))
			{
				return false;
			}

			return version <= new Version(1, 21, 114);
		}

		private bool TryGetRunningMinecraftVersion(out Version version)
		{
			version = null;
			try
			{
				MinecraftInfo info = MinecraftVersionDetector.GetCached();
				return info.IsRunning && !string.IsNullOrWhiteSpace(info.Version) &&
					Version.TryParse(info.Version, out version);
			}
			catch
			{
				return false;
			}
		}

		private void UpdateUtilitySupportLabels()
		{
			Version version;
			bool known = TryGetRunningMinecraftVersion(out version);

			if (this.UtilityItemUseDelaySupportText != null)
			{
				this.UtilityItemUseDelaySupportText.Text = known ? "Yes" : "?";
			}
			if (this.UtilityNoCameraResetSupportText != null)
			{
				this.UtilityNoCameraResetSupportText.Text = known ? "Yes" : "?";
			}
			if (this.UtilityNoHurtCamSupportText != null)
			{
				this.UtilityNoHurtCamSupportText.Text = known ? "Yes" : "?";
			}
			if (this.UtilityPlayScreenFixSupportText != null)
			{
				this.UtilityPlayScreenFixSupportText.Text = !known
					? "?"
					: (version <= new Version(1, 21, 114) ? "Yes" : "No");
			}
		}

		private void ApplyUtilityPatchSelection()
		{
			if (this._updatingUtilityToggles)
			{
				return;
			}

			bool itemUseDelay = this.UtilityItemUseDelayToggle?.IsChecked == true;
			bool noCameraReset = this.UtilityNoCameraResetToggle?.IsChecked == true;
			bool noHurtCam = this.UtilityNoHurtCamToggle?.IsChecked == true;
			bool playScreenFix = this.UtilityPlayScreenFixToggle?.IsChecked == true;

			Version version;
			bool known = TryGetMinecraftVersion(out version);
			if (!known)
			{
				if (this.PatchStatusBlock != null)
					this.PatchStatusBlock.Text = "Minecraft version not detected — launch Minecraft once or set its executable path";
				return;
			}

			bool legacy = version <= new Version(1, 21, 114);

			if (!legacy && playScreenFix)
			{
				this._updatingUtilityToggles = true;
				try
				{
					this.UtilityPlayScreenFixToggle.IsChecked = false;
					this.UtilityPlayScreenFixEnabled = false;
				}
				finally
				{
					this._updatingUtilityToggles = false;
				}
				playScreenFix = false;
			}

			this.UtilityItemUseDelayEnabled = itemUseDelay;
			this.UtilityNoCameraResetEnabled = noCameraReset;
			this.UtilityNoHurtCamEnabled = noHurtCam;
			this.UtilityPlayScreenFixEnabled = playScreenFix;
			try { ConfigIO.SaveSilent(this); } catch { }

			if (legacy)
			{
				this._utilityRuntimePatcher.SetEnabled(false);

				string exePath = this.LastMinecraftExePath;
				if (string.IsNullOrWhiteSpace(exePath) || !File.Exists(exePath))
				{
					exePath = MinecraftDiskPatcher.GetDefaultExecutablePath();
				}

				if (string.IsNullOrWhiteSpace(exePath) || !File.Exists(exePath))
				{
					return;
				}

				var selection = new PatchSelection(
					noHurtCam: noHurtCam,
					guiScale: false,
					teleportRotation: noCameraReset,
					delayFix: false,
					minimalViewBobbing: false,
					itemUseDelay: itemUseDelay,
					thirdPersonNametag: false,
					playScreenFix: playScreenFix);

				if (!itemUseDelay && !noCameraReset && !noHurtCam && !playScreenFix)
				{
					MinecraftDiskPatcher.Restore(exePath);
					return;
				}

				MinecraftDiskPatcher.Restore(exePath);
				MinecraftDiskPatcher.Apply(exePath, false, selection);
				this.LastMinecraftExePath = exePath;
				try { ConfigIO.SaveSilent(this); } catch { }
			}
			else
			{
				this._utilityRuntimePatcher.SetSelection(itemUseDelay, noCameraReset, noHurtCam);
				this._utilityRuntimePatcher.SetEnabled(itemUseDelay || noCameraReset || noHurtCam);
			}
		}

		private void UtilityPatchToggle_Checked(object sender, RoutedEventArgs e)
		{
			if (this._updatingUtilityToggles) return;
            PlayToggleFeedback(sender as UIElement);
			ApplyUtilityPatchSelection();
		}

		private void UtilityPatchToggle_Unchecked(object sender, RoutedEventArgs e)
		{
			if (this._updatingUtilityToggles) return;
			ApplyUtilityPatchSelection();
		}

		private void InitUI()
		{
			try
			{
				if (this.CurrentCpsText != null)
				{
					this.CurrentCpsText.Text = this.CurrentCps.ToString();
				}
							UpdateCpsRangeText();
				if (this.BindText != null)
				{
					this.BindText.Text = "F6";
				}
				if (this.BindGlowText != null)
				{
					this.BindGlowText.Text = "F6";
				}
				if (this.MinCpsSlider != null)
				{
					this.MinCpsSlider.Value = (double)this.MinCps;
				}
				if (this.MaxCpsSlider != null)
				{
					this.MaxCpsSlider.Value = (double)this.MaxCps;
				}
				if (this.CpsRangeSliderControl != null)
				{
					this.CpsRangeSliderControl.LowerValue = (double)this.MinCps;
					this.CpsRangeSliderControl.UpperValue = (double)this.MaxCps;
				}
				UpdateCpsRangeText();
				if (this.Streamermodeswitch != null)
				{
					this.Streamermodeswitch.IsChecked = new bool?(this.StreamerMode);
				}
				if (this.SettingsInventoryToggle != null)
				{
					this.SettingsInventoryToggle.IsChecked = new bool?(this.ClickInInventoryMode);
				}
				if (this.SettingsHitRegToggle != null)
				{
					this.SettingsHitRegToggle.IsChecked = new bool?(this.HitRegMode);
				}
				if (this.HitRegSwitch != null)
				{
					this.HitRegSwitch.IsChecked = new bool?(this.HitRegMode);
				}
				if (this.BreakingSwitch != null)
				{
					this.BreakingSwitch.IsChecked = new bool?(this.BreakingMode);
				}
				if (this.BreakingGdkSwitch != null)
				{
					this.BreakingGdkSwitch.IsChecked = new bool?(this.BreakingGdkMode);
				}
				if (this.OnlyMcGlowToggle != null)
				{
					this.OnlyMcGlowToggle.IsChecked = new bool?(this.OnlyMcbeMode);
				}
				if (this.BreakBlocksGlowToggle != null)
				{
					this.BreakBlocksGlowToggle.IsChecked = new bool?(this.BreakingMode);
				}
			}
			catch
			{
			}
		}

		protected override void OnClosed(EventArgs e)
		{
			try
			{
                ConfigIO.SaveAccentColor(this);
				if (this._minecraftVersionTimer != null)
				{
					this._minecraftVersionTimer.Stop();
				}
				this.StopClickerAndCleanup();
				DiscordRpc.Shutdown();
				this._utilityRuntimePatcher?.Dispose();
                this._patchPageRuntimePatcher?.Dispose();
			}
			catch
			{
			}
			finally
			{
				base.OnClosed(e);
			}
		}

		private void StartClickThread()
		{
			this._clickCts = new CancellationTokenSource();
			this._clickThread = new Thread(delegate()
			{
				ClickLoop.Run(this, this._clickCts.Token);
			});
			this._clickThread.IsBackground = true;
			this._clickThread.Priority = ThreadPriority.Highest;
			this._clickThread.Start();
		}

		public void StopClickerAndCleanup()
		{
			this.SetClickerEnabled(false);
			this.UserHoldingLMB = false;
			if (this.StreamerMode)
			{
				Autoclicker.Window.StreamerMode.Apply(this, false);
				this.StreamerMode = false;
			}
			CancellationTokenSource clickCts = this._clickCts;
			if (clickCts != null)
			{
				clickCts.Cancel();
			}
			KeyboardHook.Uninstall();
			MouseHook.Uninstall();
			Thread clickThread = this._clickThread;
			if (clickThread != null && clickThread.IsAlive)
			{
				this._clickThread.Join(500);
			}
			this.FrameStopwatch.Stop();
			this.CpsChangeTimer.Stop();
		}

		public void SetClickerEnabled(bool enabled)
		{
			this.ClickerEnabled = enabled;
			if (!enabled)
			{
				this.UserHoldingLMB = false;
			}
		}

		private string ResolveKnownMinecraftExecutablePath()
		{
			if (!string.IsNullOrWhiteSpace(this.LastMinecraftExePath) && File.Exists(this.LastMinecraftExePath))
			{
				return this.LastMinecraftExePath;
			}
			return this.TryGetRunningMinecraftExecutablePath();
		}

		private void AutoDetectMinecraftExecutable()
		{
			string text = this.TryGetRunningMinecraftExecutablePath();
			if (!string.IsNullOrWhiteSpace(text))
			{
				this.LastMinecraftExePath = text;
			}
		}

				private string TryGetRunningMinecraftExecutablePath()
		{
			string[] source = new string[]
			{
				"Minecraft.Windows",
				"bedrock",
				"mcbe",
				"Minecraft"
			};
			foreach (Process process in Process.GetProcesses())
			{
				try
				{
					string name = process.ProcessName;
					string text = null;
					try
					{
						ProcessModule mainModule = process.MainModule;
						text = ((mainModule != null) ? mainModule.FileName : null);
					}
					catch
					{
					}
					if (source.Any((string n) => string.Equals(n, name, StringComparison.OrdinalIgnoreCase)) || name.StartsWith("Minecraft-", StringComparison.OrdinalIgnoreCase) || (text != null && text.EndsWith("Minecraft.Windows.exe", StringComparison.OrdinalIgnoreCase)))
					{
						if (!string.IsNullOrWhiteSpace(text) && File.Exists(text))
						{
							return text;
						}
					}
				}
				catch
				{
				}
				finally
				{
					try
					{
						process.Dispose();
					}
					catch
					{
					}
				}
			}
			return null;
		}

		private async Task CloseMinecraftProcessesAsync()
		{
			var names = new[] { "Minecraft.Windows", "Minecraft", "MinecraftUWP", "bedrock", "mcbe" };
			foreach (var name in names)
			{
				foreach (var proc in Process.GetProcessesByName(name))
				{
					try
					{
						proc.Kill();
						await Task.Delay(500);
					}
					catch { }
					finally
					{
						try { proc.Dispose(); } catch { }
					}
				}
			}
		}

		private void TryLaunchMinecraft()
		{
			try
			{
				Process.Start(new ProcessStartInfo
				{
					FileName = "minecraft://",
					UseShellExecute = true
				});
			}
			catch
			{
			}
		}

		public void SetClickerBindFromKey(Key key)
		{
			this.SelectedKey = key;
			this.SelectedMouseButton = null;
			this.WaitingForKey = false;
			BindHelper.UpdateBindUI(this);
			this.ResetBindGlowToggle();
			base.Dispatcher.Invoke(delegate()
			{
				if (this.ChooseKeyBtn != null)
				{
					this.ChooseKeyBtn.Content = "CHANGE BIND";
				}
			});
		}

		public void SetClickerBindFromMouse(MouseButton btn)
		{
			this.SelectedMouseButton = new MouseButton?(btn);
			this.SelectedKey = Key.None;
			this.WaitingForKey = false;
			BindHelper.UpdateBindUI(this);
			this.ResetBindGlowToggle();
			base.Dispatcher.Invoke(delegate()
			{
				if (this.ChooseKeyBtn != null)
				{
					this.ChooseKeyBtn.Content = "CHANGE BIND";
				}
			});
		}


		public void ToggleWindowVisibility()
		{
			Autoclicker.Window.StreamerMode.Toggle(this);
		}

		private void Window_MouseDown(object sender, MouseButtonEventArgs e)
		{
			if (e.ChangedButton == MouseButton.Left)
			{
				base.DragMove();
			}
		}

        private void ClearFocusOnWindowShow()
        {
            try
            {
                Keyboard.ClearFocus();
                FocusManager.SetFocusedElement(this, null);
            }
            catch
            {
            }
        }

private void MinimizeBtn_Click(object sender, RoutedEventArgs e)
		{
			base.WindowState = WindowState.Minimized;
		}

		private bool _settingsPageVisible;

        public bool IsDarkTheme = false;

        private bool _applyingLoadedState;

        public void ApplyLoadedState()
        {
            try
            {
                _applyingLoadedState = true;
                ApplyTheme(IsDarkTheme);
                if (Resources.Contains("ThemeAccentColor") && Resources["ThemeAccentColor"] is Color loadedAccent)
                {
                    ApplyAccentColor(loadedAccent);
                }
                UpdateMinecraftVersionText();
                ApplyCharacterImageSettings();
                if (!string.IsNullOrWhiteSpace(this.BackgroundImagePath))
                    ApplyBackgroundImage();
                if (this.DiscordRpcToggle != null) this.DiscordRpcToggle.IsChecked = this.DiscordRpcEnabled;
                if (this.SettingsFastRefillToggle != null) this.SettingsFastRefillToggle.IsChecked = this.EasyRefilMode;
                if (this.OnlyMcGlowToggle != null) this.OnlyMcGlowToggle.IsChecked = this.OnlyMcbeMode;
                if (this.BreakBlocksGlowToggle != null) this.BreakBlocksGlowToggle.IsChecked = this.BreakingMode;
                if (this.UtilityItemUseDelayToggle != null) this.UtilityItemUseDelayToggle.IsChecked = this.UtilityItemUseDelayEnabled;
                if (this.UtilityNoCameraResetToggle != null) this.UtilityNoCameraResetToggle.IsChecked = this.UtilityNoCameraResetEnabled;
                if (this.UtilityNoHurtCamToggle != null) this.UtilityNoHurtCamToggle.IsChecked = this.UtilityNoHurtCamEnabled;
                if (this.UtilityPlayScreenFixToggle != null) this.UtilityPlayScreenFixToggle.IsChecked = this.UtilityPlayScreenFixEnabled;
                UpdateUtilitySupportLabels();
                SetCharacterImageVisibleForMainPage(!_settingsPageVisible);
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (Resources.Contains("ThemeAccentColor") && Resources["ThemeAccentColor"] is Color c)
                    {
                        UpdateColorPickerThumb(c);
                    }
                }), System.Windows.Threading.DispatcherPriority.Loaded);
            }
            catch { }
            finally
            {
                _applyingLoadedState = false;
            }
        }

        private void AnimatePanelIn(UIElement panel, TranslateTransform transform)
        {
            if (panel == null || transform == null) return;
            panel.Visibility = Visibility.Visible;
            panel.Opacity = 0;
            transform.X = 12;
            var sb = new Storyboard();
            var op = new DoubleAnimation(0, 1, new Duration(TimeSpan.FromMilliseconds(180))) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } };
            var slide = new DoubleAnimation(12, 0, new Duration(TimeSpan.FromMilliseconds(210))) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } };
            Storyboard.SetTarget(op, panel); Storyboard.SetTargetProperty(op, new PropertyPath(UIElement.OpacityProperty));
            Storyboard.SetTarget(slide, transform); Storyboard.SetTargetProperty(slide, new PropertyPath("X"));
            sb.Children.Add(op); sb.Children.Add(slide); sb.Begin();
        }

        private void PlayToggleFeedback(UIElement element)
        {
            try
            {
                var transform = element.RenderTransform as ScaleTransform;
                if (transform == null)
                {
                    transform = new ScaleTransform(1, 1);
                    element.RenderTransformOrigin = new Point(0.5, 0.5);
                    element.RenderTransform = transform;
                }
                var sb = new Storyboard();
                var up = new DoubleAnimation(1, 1.13, new Duration(TimeSpan.FromMilliseconds(80))) { AutoReverse = true, EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } };
                var up2 = new DoubleAnimation(1, 1.13, new Duration(TimeSpan.FromMilliseconds(80))) { AutoReverse = true, EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } };
                Storyboard.SetTarget(up, transform); Storyboard.SetTargetProperty(up, new PropertyPath(ScaleTransform.ScaleXProperty));
                Storyboard.SetTarget(up2, transform); Storyboard.SetTargetProperty(up2, new PropertyPath(ScaleTransform.ScaleYProperty));
                sb.Children.Add(up); sb.Children.Add(up2); sb.Begin();
            }
            catch { }
        }

        private void ApplyTheme(bool dark)
        {
            IsDarkTheme = dark;
            Resources["ThemeWindowBackgroundColor"] = dark ? Color.FromRgb(20,20,20) : Color.FromRgb(255,255,255);
            Resources["ThemeTextColor"] = dark ? Color.FromRgb(225,225,225) : Color.FromRgb(26,26,26);
            Resources["ThemeMutedColor"] = dark ? Color.FromRgb(150,150,150) : Color.FromRgb(154,154,154);
            Resources["ThemeControlColor"] = dark ? Color.FromRgb(45,45,45) : Color.FromRgb(229,229,234);
            Resources["ThemeHoverColor"] = dark ? Color.FromRgb(62,62,62) : Color.FromRgb(200,200,204);
            Resources["ConfigButtonBackgroundBrush"] = new SolidColorBrush(dark ? Color.FromRgb(48,48,48) : Color.FromRgb(21,21,21));
            Resources["ConfigButtonBorderBrush"] = new SolidColorBrush(dark ? Color.FromRgb(48,48,48) : Color.FromRgb(0,0,0));
            Resources["ConfigButtonHoverBorderBrush"] = new SolidColorBrush(dark ? Color.FromRgb(82,82,82) : Color.FromRgb(0,0,0));
            Resources["ConfigComboPopupBrush"] = new SolidColorBrush(dark ? Color.FromRgb(48,48,48) : Color.FromRgb(245,245,245));
            Resources["ConfigComboItemHoverBrush"] = new SolidColorBrush(dark ? Color.FromRgb(68,68,68) : Color.FromRgb(225,225,225));
            Resources["ColorPickerGlowColor"] = dark ? Colors.White : Colors.Black;
            if (_charEditorWindow != null && _charEditorWindow.IsLoaded) _charEditorWindow.ApplyTheme(dark);
        }

        private void CharacterImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                ApplyTheme(!IsDarkTheme);
                try { ConfigIO.SaveSilent(this); } catch { }
                e.Handled = true;
            }
        }

        private void ConfigsBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var w = new ConfigManagerWindow(this);
                w.Owner = this;
                w.Left = Left + 12;
                w.Top = Top + 55;
                w.Show();
            }
            catch { }
        }

        private void ConfigComboBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (ConfigComboBox == null)
                return;

            if (!ConfigComboBox.IsDropDownOpen)
            {
                ConfigComboBox.Focus();
                ConfigComboBox.IsDropDownOpen = true;
                e.Handled = true;
            }
        }

        private bool _suppressConfigSelection;

        private void RefreshConfigCombo(string selectedName = null)
        {
            if (ConfigComboBox == null) return;
            string previous = selectedName ?? ConfigProfiles.GetActiveName();
            _suppressConfigSelection = true;
            try
            {
                ConfigComboBox.Items.Clear();
                foreach (string name in ConfigProfiles.GetNames()) ConfigComboBox.Items.Add(name);
                if (!string.IsNullOrWhiteSpace(previous)) ConfigComboBox.SelectedItem = previous;
            }
            finally
            {
                _suppressConfigSelection = false;
            }
        }

        private void ConfigComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_suppressConfigSelection || ConfigComboBox == null) return;
            string name = ConfigComboBox.SelectedItem as string;
            if (string.IsNullOrWhiteSpace(name)) return;

            try
            {
                if (ConfigProfiles.Load(this, name))
                {
                    ApplyLoadedState();
                    RefreshConfigCombo(name);
                }
            }
            catch
            {
            }
        }

        private void SaveJsonConfigBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string selectedName = ConfigComboBox.SelectedItem as string;
                if (string.IsNullOrWhiteSpace(selectedName)) selectedName = "config";
                var dialog = new SaveFileDialog
                {
                    Filter = "JSON config (*.json)|*.json",
                    DefaultExt = ".json",
                    AddExtension = true,
                    FileName = selectedName + ".json",
                    Title = "Save config"
                };
                if (dialog.ShowDialog() != true) return;

                string name = Path.GetFileNameWithoutExtension(dialog.FileName);
                if (string.IsNullOrWhiteSpace(name)) name = selectedName;

                if (!ConfigProfiles.SaveJson(this, dialog.FileName, name))
                {
                    MessageBox.Show("Failed to save config.", "Config", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (!ConfigProfiles.Save(this, name))
                {
                    MessageBox.Show("JSON saved, but the config could not be added to the list.", "Config", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                ConfigProfiles.SetActiveName(name);
                RefreshConfigCombo(name);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to save config: " + ex.Message, "Config", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ImportConfigBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new OpenFileDialog
                {
                    Filter = "JSON config (*.json)|*.json|All files (*.*)|*.*",
                    Title = "Import config"
                };
                if (dialog.ShowDialog() != true) return;
                string importedName;
                if (!ConfigProfiles.ImportJson(dialog.FileName, out importedName))
                {
                    MessageBox.Show("Failed to import config.", "Config", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                if (ConfigProfiles.Load(this, importedName))
                {
                    ApplyLoadedState();
                    RefreshConfigCombo(importedName);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to import config: " + ex.Message, "Config", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteConfigBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string name = ConfigComboBox?.SelectedItem as string;
                if (string.IsNullOrWhiteSpace(name))
                {
                    MessageBox.Show("Select a config first.", "Config", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                if (!ConfigProfiles.Delete(name))
                {
                    MessageBox.Show("Failed to delete config.", "Config", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                string nextName = ConfigProfiles.GetNames().FirstOrDefault();
                RefreshConfigCombo(nextName);

                if (!string.IsNullOrWhiteSpace(nextName))
                {
                    if (ConfigProfiles.Load(this, nextName))
                    {
                        ApplyLoadedState();
                        RefreshConfigCombo(nextName);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to delete config: " + ex.Message, "Config", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportConfigBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string name = ConfigComboBox.SelectedItem as string;
                if (string.IsNullOrWhiteSpace(name))
                {
                    MessageBox.Show("Select a config first.", "Config", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
                var dialog = new SaveFileDialog
                {
                    Filter = "JSON config (*.json)|*.json",
                    DefaultExt = ".json",
                    AddExtension = true,
                    FileName = name + ".json",
                    Title = "Export config"
                };
                if (dialog.ShowDialog() == true && !ConfigProfiles.ExportJson(name, dialog.FileName))
                    MessageBox.Show("Failed to export config.", "Config", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to export config: " + ex.Message, "Config", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

		private void SetCharacterImageVisibleForMainPage(bool visible)
		{
			try
			{
				Image image = this.FindName("CharacterImage") as Image;
				if (image == null) return;
				if (visible && !string.IsNullOrWhiteSpace(this.CharacterImagePath) && File.Exists(this.CharacterImagePath))
				{
					image.Visibility = Visibility.Visible;
				}
				else if (!visible)
				{
					image.Visibility = Visibility.Collapsed;
				}
			}
			catch { }
		}

		private void SettingsBtn_Click(object sender, RoutedEventArgs e)
		{
			SetSettingsPageVisible(true);
		}

		private void SetSettingsPageVisible(bool visible)
		{
			_settingsPageVisible = visible;

			if (MainContentPanel == null || SettingsPanel == null)
			{
				return;
			}

			if (visible)
			{
				SetCharacterImageVisibleForMainPage(false);
				MainContentPanel.Visibility = Visibility.Collapsed;
				if (UtilitiesPanel != null)
				{
					UtilitiesPanel.Visibility = Visibility.Collapsed;
				}
				SettingsPanel.Visibility = Visibility.Visible;
				SettingsPanel.Opacity = 0.0;
				SettingsPanelTx.X = 12.0;

				var storyboard = new Storyboard();
				var opacity = new DoubleAnimation(0.0, 1.0, new Duration(TimeSpan.FromMilliseconds(150)));
				Storyboard.SetTarget(opacity, SettingsPanel);
				Storyboard.SetTargetProperty(opacity, new PropertyPath(UIElement.OpacityProperty));

				var slide = new DoubleAnimation(12.0, 0.0, new Duration(TimeSpan.FromMilliseconds(170)))
				{
					EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
				};
				Storyboard.SetTarget(slide, SettingsPanelTx);
				Storyboard.SetTargetProperty(slide, new PropertyPath("X"));

				storyboard.Children.Add(opacity);
				storyboard.Children.Add(slide);
				storyboard.Begin();
			}
			else
			{
				SettingsPanel.Visibility = Visibility.Collapsed;
				MainContentPanel.Visibility = Visibility.Visible;
				SetCharacterImageVisibleForMainPage(true);
			}
		}

		private void UtilitiesBtn_Click(object sender, RoutedEventArgs e)
		{
			SetCharacterImageVisibleForMainPage(false);

			_settingsPageVisible = false;
			if (MainContentPanel != null) MainContentPanel.Visibility = Visibility.Collapsed;
			if (SettingsPanel != null) SettingsPanel.Visibility = Visibility.Collapsed;
			if (UtilitiesPanel != null)
            {
                AnimatePanelIn(UtilitiesPanel, UtilitiesPanelTx);
            }
		}

		private void MenuBtn_Click(object sender, RoutedEventArgs e)
		{
			SetCharacterImageVisibleForMainPage(true);

			_settingsPageVisible = false;
			if (SettingsPanel != null) SettingsPanel.Visibility = Visibility.Collapsed;
			if (UtilitiesPanel != null) UtilitiesPanel.Visibility = Visibility.Collapsed;
			if (MainContentPanel != null)
            {
                MainContentPanel.Visibility = Visibility.Visible;
                MainContentPanel.Opacity = 0;
                var fade = new DoubleAnimation(0, 1, new Duration(TimeSpan.FromMilliseconds(180)));
                MainContentPanel.BeginAnimation(UIElement.OpacityProperty, fade);
            }
		}

		private bool _colorPickerDragging;

		private void ColorPickerBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			_colorPickerDragging = true;
			ColorPickerBar.CaptureMouse();
			SetAccentFromPickerPoint(e.GetPosition(ColorPickerBar).X);
			e.Handled = true;
		}

		private void ColorPickerBar_MouseMove(object sender, MouseEventArgs e)
		{
			if (_colorPickerDragging && e.LeftButton == MouseButtonState.Pressed)
			{
				SetAccentFromPickerPoint(e.GetPosition(ColorPickerBar).X);
			}
		}

		private void ColorPickerBar_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
		{
			_colorPickerDragging = false;
			ColorPickerBar.ReleaseMouseCapture();
            ConfigIO.SaveAccentColor(this);
			e.Handled = true;
		}

        private void ColorPickerBar_Loaded(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (Resources.Contains("ThemeAccentColor") && Resources["ThemeAccentColor"] is Color c)
                    UpdateColorPickerThumb(c);
            }), System.Windows.Threading.DispatcherPriority.Loaded);
        }

        private void ColorPickerBar_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (Resources.Contains("ThemeAccentColor") && Resources["ThemeAccentColor"] is Color c)
                UpdateColorPickerThumb(c);
        }

		private void SetAccentFromPickerPoint(double x)
		{
			if (ColorPickerBar == null || ColorPickerThumb == null || ColorPickerBar.ActualWidth <= 0)
			{
				return;
			}

			double thumbHalf = ColorPickerThumb.Width / 2.0;
			double usable = Math.Max(1.0, ColorPickerBar.ActualWidth - ColorPickerThumb.Width);
			double left = Math.Max(0.0, Math.Min(usable, x - thumbHalf));
			ColorPickerThumb.Margin = new Thickness(left, 0, 0, 0);

			double hue = left / usable * 360.0;
			Color color = HsvToColor(hue, 1.0, 1.0);
			ApplyAccentColor(color);
		}

		private static Color HsvToColor(double hue, double saturation, double value)
		{
			hue = ((hue % 360.0) + 360.0) % 360.0;
			double c = value * saturation;
			double x = c * (1.0 - Math.Abs((hue / 60.0 % 2.0) - 1.0));
			double m = value - c;
			double r, g, b;
			if (hue < 60) (r, g, b) = (c, x, 0);
			else if (hue < 120) (r, g, b) = (x, c, 0);
			else if (hue < 180) (r, g, b) = (0, c, x);
			else if (hue < 240) (r, g, b) = (0, x, c);
			else if (hue < 300) (r, g, b) = (x, 0, c);
			else (r, g, b) = (c, 0, x);
			return Color.FromRgb((byte)Math.Round((r + m) * 255), (byte)Math.Round((g + m) * 255), (byte)Math.Round((b + m) * 255));
		}

		private void UpdateColorPickerThumb(Color color)
		{
			if (ColorPickerBar == null || ColorPickerThumb == null || ColorPickerBar.ActualWidth <= 0) return;

			double hue = ColorToHue(color);
			double usable = Math.Max(1.0, ColorPickerBar.ActualWidth - ColorPickerThumb.Width);
			double left = usable * hue / 360.0;
			ColorPickerThumb.Margin = new Thickness(left, 0, 0, 0);
		}

		private static double ColorToHue(Color color)
		{
			double r = color.R / 255.0;
			double g = color.G / 255.0;
			double b = color.B / 255.0;
			double max = Math.Max(r, Math.Max(g, b));
			double min = Math.Min(r, Math.Min(g, b));
			double delta = max - min;
			if (delta == 0) return 0;

			double hue;
			if (max == r) hue = 60.0 * (((g - b) / delta) % 6.0);
			else if (max == g) hue = 60.0 * (((b - r) / delta) + 2.0);
			else hue = 60.0 * (((r - g) / delta) + 4.0);

			if (hue < 0) hue += 360.0;
			return hue;
		}

		public void ApplyAccentColor(Color color)
		{
			var brush = new SolidColorBrush(color);
			brush.Freeze();
			Resources["ThemeAccentBrush"] = brush;
			Resources["ThemeAccentColor"] = color;
			UpdateColorPickerThumb(color);

			if (CpsRangeSliderControl != null)
			{
				CpsRangeSliderControl.AccentBrush = brush;
				CpsRangeSliderControl.AccentColor = color;
			}

			if (_charEditorWindow != null && _charEditorWindow.IsLoaded)
			{
				_charEditorWindow.SetAccentColor(color);
			}
		}

		private bool _suppressClickerToggleEvent;

		private void ClickerToggleBtn_Checked(object sender, RoutedEventArgs e)
		{
			if (this._suppressClickerToggleEvent)
			{
				return;
			}
			this.SetClickerEnabled(true);
            PlayToggleFeedback(ClickerToggleBtn);
		}

		private void ClickerToggleBtn_Unchecked(object sender, RoutedEventArgs e)
		{
			if (this._suppressClickerToggleEvent)
			{
				return;
			}
			this.SetClickerEnabled(false);
		}

		private readonly DispatcherTimer _clickerToggleSyncTimer = new DispatcherTimer
		{
			Interval = TimeSpan.FromMilliseconds(150)
		};

		private void StartClickerToggleSync()
		{
			this._clickerToggleSyncTimer.Tick += (s, e) =>
			{
				if (this.ClickerToggleBtn == null)
				{
					return;
				}
				bool isChecked = this.ClickerToggleBtn.IsChecked == true;
				if (isChecked != this.ClickerEnabled)
				{
					this._suppressClickerToggleEvent = true;
					this.ClickerToggleBtn.IsChecked = new bool?(this.ClickerEnabled);
					this._suppressClickerToggleEvent = false;
				}
			};
			this._clickerToggleSyncTimer.Start();
		}

		private void BindGlowToggle_Click(object sender, RoutedEventArgs e)
		{
			this.WaitingForKey = true;
			try
			{
				base.Dispatcher.Invoke(delegate ()
				{
					if (this.BindGlowText != null)
					{
						this.BindGlowText.Text = "...";
					}
					if (this.BindGlowToggle != null)
					{
						this.BindGlowToggle.IsChecked = new bool?(true);
					}
					if (this.BindText != null)
					{
						this.BindText.Text = "...";
					}
					if (this.ChooseKeyBtn != null)
					{
						this.ChooseKeyBtn.Content = "PRESS KEY";
					}
				});
			}
			catch
			{
			}
		}

		private void ResetBindGlowToggle()
		{
			try
			{
				base.Dispatcher.Invoke(delegate ()
				{
					if (this.BindGlowToggle != null)
					{
						this.BindGlowToggle.IsChecked = new bool?(false);
					}
				});
			}
			catch
			{
			}
		}

		private void Nav_Checked(object sender, RoutedEventArgs e)
		{
			if (this.PageClicker == null)
			{
				return;
			}
			foreach (ValueTuple<StackPanel, bool> valueTuple in new ValueTuple<StackPanel, bool>[]
			{
				new ValueTuple<StackPanel, bool>(this.PageClicker, this.NavClicker.IsChecked.GetValueOrDefault()),
				new ValueTuple<StackPanel, bool>(this.PageModes, this.NavModes.IsChecked.GetValueOrDefault()),
				new ValueTuple<StackPanel, bool>(this.PagePatches, this.NavPatches.IsChecked.GetValueOrDefault()),
				new ValueTuple<StackPanel, bool>(this.PageSettings, this.NavSettings.IsChecked.GetValueOrDefault())
			})
			{
				StackPanel item = valueTuple.Item1;
				if (valueTuple.Item2)
				{
					item.Visibility = Visibility.Visible;
					item.Opacity = 0.0;
					((TranslateTransform)item.RenderTransform).X = 12.0;
					Storyboard storyboard = new Storyboard();
					DoubleAnimation doubleAnimation = new DoubleAnimation(0.0, 1.0, new Duration(TimeSpan.FromMilliseconds(160.0)));
					Storyboard.SetTarget(doubleAnimation, item);
					Storyboard.SetTargetProperty(doubleAnimation, new PropertyPath(UIElement.OpacityProperty));
					DoubleAnimation doubleAnimation2 = new DoubleAnimation(12.0, 0.0, new Duration(TimeSpan.FromMilliseconds(160.0)));
					doubleAnimation2.EasingFunction = new CubicEase
					{
						EasingMode = EasingMode.EaseOut
					};
					Storyboard.SetTarget(doubleAnimation2, item);
					Storyboard.SetTargetProperty(doubleAnimation2, new PropertyPath("RenderTransform.X", Array.Empty<object>()));
					storyboard.Children.Add(doubleAnimation);
					storyboard.Children.Add(doubleAnimation2);
					storyboard.Begin();
				}
				else
				{
					item.Visibility = Visibility.Collapsed;
				}
			}
		}

		private void CloseBtn_Click(object sender, RoutedEventArgs e)
		{
			base.Close();
		}

		private void UpdateCpsRangeText()
		{
			if (this.CpsRangeText == null)
			{
				return;
			}

			var slider = this.CpsRangeSliderControl;
			if (slider != null)
			{
				int min = Math.Max((int)Math.Round(slider.Minimum), (int)Math.Round(slider.LowerValue));
				int max = Math.Min((int)Math.Round(slider.Maximum), (int)Math.Round(slider.UpperValue));

				if (max < min)
				{
					(max, min) = (min, max);
				}

				this.CpsRangeText.Text = $"{min}-{max} CPS";
				return;
			}

			this.CpsRangeText.Text = $"{this.MinCps}-{this.MaxCps} CPS";
		}

		private void CpsRangeSlider_RangeChanged(object sender, EventArgs e)
		{
			try
			{
				var slider = sender as Autoclicker.Controls.DualRangeSlider;
				if (slider == null)
				{
					return;
				}

				this.MinCps = Math.Max(1, (int)Math.Round(slider.LowerValue));
				this.MaxCps = Math.Max(this.MinCps + (slider.MinimumGap >= 1 ? 1 : 0),
					(int)Math.Round(slider.UpperValue));

				if (this.MaxCps > (int)Math.Round(slider.Maximum))
				{
					this.MaxCps = (int)Math.Round(slider.Maximum);
				}

				UpdateCpsDisplay();
			}
			catch
			{
			}
		}

		private void UpdateCpsDisplay()
		{
			this.CurrentCps = (this.TargetCps = (this.MinCps + this.MaxCps) / 2);
						UpdateCpsRangeText();
			if (this.CurrentCpsText != null)
			{
				this.CurrentCpsText.Text = this.CurrentCps.ToString();
			}
		}

		private void MinCpsSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			try
			{
				this.MinCps = (int)e.NewValue;
				if (this.MinCps > this.MaxCps)
				{
					this.MaxCps = this.MinCps;
					if (this.MaxCpsSlider != null)
					{
						this.MaxCpsSlider.Value = (double)this.MaxCps;
					}
					if (this.MaxCpsValueText != null)
					{
						this.MaxCpsValueText.Text = this.MaxCps.ToString();
					}
				}
				this.CurrentCps = (this.TargetCps = (this.MinCps + this.MaxCps) / 2);
				if (this.MinCpsValueText != null)
				{
					this.MinCpsValueText.Text = this.MinCps.ToString();
				}
							UpdateCpsRangeText();
				if (this.CurrentCpsText != null)
				{
					this.CurrentCpsText.Text = this.CurrentCps.ToString();
				}
			}
			catch
			{
			}
		}

		private void MaxCpsSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			try
			{
				this.MaxCps = (int)e.NewValue;
				if (this.MaxCps < this.MinCps)
				{
					this.MinCps = this.MaxCps;
					if (this.MinCpsSlider != null)
					{
						this.MinCpsSlider.Value = (double)this.MinCps;
					}
					if (this.MinCpsValueText != null)
					{
						this.MinCpsValueText.Text = this.MinCps.ToString();
					}
				}
				this.CurrentCps = (this.TargetCps = (this.MinCps + this.MaxCps) / 2);
				if (this.MaxCpsValueText != null)
				{
					this.MaxCpsValueText.Text = this.MaxCps.ToString();
				}
							UpdateCpsRangeText();
				if (this.CurrentCpsText != null)
				{
					this.CurrentCpsText.Text = this.CurrentCps.ToString();
				}
			}
			catch
			{
			}
		}

		private void OnlyMcbeSwitch_Checked(object sender, RoutedEventArgs e)
		{
			this.OnlyMcbeMode = true;
            if (!_applyingLoadedState) PlayToggleFeedback(sender as UIElement);
		}

		private void OnlyMcbeSwitch_Unchecked(object sender, RoutedEventArgs e)
		{
			this.OnlyMcbeMode = false;
		}

		private void InventoryToggle_Checked(object sender, RoutedEventArgs e)
		{
			this.ClickInInventoryMode = true;
			if (this.SettingsInventoryToggle != null && !ReferenceEquals(sender, this.SettingsInventoryToggle)) this.SettingsInventoryToggle.IsChecked = true;
			if (this.InventoryToggle != null && !ReferenceEquals(sender, this.InventoryToggle)) this.InventoryToggle.IsChecked = true;
			if (this.EasyRefilMode)
			{
				this.EasyRefilMode = false;
				if (this.SettingsFastRefillToggle != null) this.SettingsFastRefillToggle.IsChecked = false;
			}
		}

		private void InventoryToggle_Unchecked(object sender, RoutedEventArgs e)
		{
			this.ClickInInventoryMode = false;
			if (this.SettingsInventoryToggle != null && !ReferenceEquals(sender, this.SettingsInventoryToggle)) this.SettingsInventoryToggle.IsChecked = false;
			if (this.InventoryToggle != null && !ReferenceEquals(sender, this.InventoryToggle)) this.InventoryToggle.IsChecked = false;
		}

		private void FastRefillToggle_Checked(object sender, RoutedEventArgs e)
		{
			this.EasyRefilMode = true;
			if (this.ClickInInventoryMode)
			{
				this.ClickInInventoryMode = false;
				if (this.InventoryToggle != null) this.InventoryToggle.IsChecked = false;
				if (this.SettingsInventoryToggle != null) this.SettingsInventoryToggle.IsChecked = false;
			}
			if (!_applyingLoadedState) PlayToggleFeedback(sender as UIElement);
		}

		private void FastRefillToggle_Unchecked(object sender, RoutedEventArgs e)
		{
			this.EasyRefilMode = false;
		}

		private void HitRegSwitch_Checked(object sender, RoutedEventArgs e)
		{
			this.HitRegMode = true;
			if (this.SettingsHitRegToggle != null && !ReferenceEquals(sender, this.SettingsHitRegToggle)) this.SettingsHitRegToggle.IsChecked = true;
			if (this.HitRegSwitch != null && !ReferenceEquals(sender, this.HitRegSwitch)) this.HitRegSwitch.IsChecked = true;
		}

		private void HitRegSwitch_Unchecked(object sender, RoutedEventArgs e)
		{
			this.HitRegMode = false;
			if (this.SettingsHitRegToggle != null && !ReferenceEquals(sender, this.SettingsHitRegToggle)) this.SettingsHitRegToggle.IsChecked = false;
			if (this.HitRegSwitch != null && !ReferenceEquals(sender, this.HitRegSwitch)) this.HitRegSwitch.IsChecked = false;
			try
			{
				base.Dispatcher.Invoke(delegate()
				{
					if (this.HitRegSwitch != null)
					{
						this.HitRegSwitch.Effect = null;
					}
				});
			}
			catch
			{
			}
		}

		private void BreakingSwitch_Checked(object sender, RoutedEventArgs e)
		{
			this.BreakingMode = true;
            if (!_applyingLoadedState) PlayToggleFeedback(sender as UIElement);
			this.BreakingGdkMode = false;
			if (this.BreakingGdkSwitch != null)
			{
				this.BreakingGdkSwitch.IsChecked = new bool?(false);
			}
		}

		private void BreakingSwitch_Unchecked(object sender, RoutedEventArgs e)
		{
			this.BreakingMode = false;
		}

		private void BreakingGdkSwitch_Checked(object sender, RoutedEventArgs e)
		{
			if (!Deactivator.CheckAdminRights())
			{
				this.BreakingGdkMode = false;
				if (this.BreakingGdkSwitch != null)
				{
					this.BreakingGdkSwitch.IsChecked = new bool?(false);
				}
				if (MessageBox.Show("Breaking GDK requires administrator rights.\nRestart as administrator?", "Administrator required", MessageBoxButton.YesNo, MessageBoxImage.Exclamation) == MessageBoxResult.Yes)
				{
					try
					{
						Process.Start(new ProcessStartInfo
						{
							FileName = Process.GetCurrentProcess().MainModule.FileName,
							Verb = "runas",
							UseShellExecute = true
						});
						Application.Current.Shutdown();
					}
					catch
					{
						MessageBox.Show("Failed to restart as administrator.", "Error", MessageBoxButton.OK, MessageBoxImage.Hand);
					}
				}
				return;
			}
			this.BreakingGdkMode = true;
			this.BreakingMode = false;
			if (this.BreakingSwitch != null)
			{
				this.BreakingSwitch.IsChecked = new bool?(false);
			}
		}

		private void BreakingGdkSwitch_Unchecked(object sender, RoutedEventArgs e)
		{
			this.BreakingGdkMode = false;
		}

		private void Streamermodeswitch_Checked(object sender, RoutedEventArgs e)
		{
			try
			{
				this.StreamerMode = true;
				Autoclicker.Window.StreamerMode.Apply(this, true);
			}
			catch
			{
				this.StreamerMode = false;
			}
		}

		private void Streamermodeswitch_Unchecked(object sender, RoutedEventArgs e)
		{
			try
			{
				this.StreamerMode = false;
				Autoclicker.Window.StreamerMode.Apply(this, false);
			}
			catch
			{
			}
		}

		private void ChooseKeyBtn_Click(object sender, RoutedEventArgs e)
		{
			this.WaitingForKey = true;
			try
			{
				base.Dispatcher.Invoke(delegate()
				{
					if (this.BindText != null)
					{
						this.BindText.Text = "...";
					}
					if (this.ChooseKeyBtn != null)
					{
						this.ChooseKeyBtn.Content = "PRESS KEY";
					}
				});
			}
			catch
			{
			}
		}

		private void ChooseMouseBtn_Click(object sender, RoutedEventArgs e)
		{
			this.WaitingForKey = true;
			try
			{
				base.Dispatcher.Invoke(delegate()
				{
					if (this.BindText != null)
					{
						this.BindText.Text = "...";
					}
					if (this.ChooseKeyBtn != null)
					{
						this.ChooseKeyBtn.Content = "PRESS MOUSE";
					}
				});
			}
			catch
			{
			}
		}
		private void SaveConfigBtn_Click(object sender, RoutedEventArgs e)
		{
			if (!Deactivator.CheckAdminRights())
			{
				if (MessageBox.Show("Требуются права администратора.\nЗапустить от имени администратора?", "Нет прав", MessageBoxButton.YesNo, MessageBoxImage.Exclamation) == MessageBoxResult.Yes)
				{
					try
					{
						Process.Start(new ProcessStartInfo
						{
							FileName = Process.GetCurrentProcess().MainModule.FileName,
							Verb = "runas",
							UseShellExecute = true
						});
						Application.Current.Shutdown();
					}
					catch
					{
						MessageBox.Show("Не удалось запустить с правами администратора.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Hand);
					}
				}
				return;
			}
			ConfigIO.Save(this);
		}

		private void DeactivationBtn_Click(object sender, RoutedEventArgs e)
		{
			DeactivationFullBtn_Click(sender, e);
		}

		private void DeactivationLiteBtn_Click(object sender, RoutedEventArgs e)
		{
			if (!Deactivator.CheckAdminRights())
			{
				MessageBox.Show("\r\nrun the program as administrator!", "Warning", MessageBoxButton.OK, MessageBoxImage.Exclamation);
				return;
			}
			if (MessageBox.Show("data cleaner will clear program logs", "Lite cleaner", MessageBoxButton.YesNo, MessageBoxImage.Asterisk) == MessageBoxResult.Yes)
			{
				Deactivator.PerformLite(this);
			}
		}

		private void DeactivationFullBtn_Click(object sender, RoutedEventArgs e)
		{
			if (!Deactivator.CheckAdminRights())
			{
				MessageBox.Show("\r\nrun the program as administrator!", "Warning", MessageBoxButton.OK, MessageBoxImage.Exclamation);
				return;
			}
			if (MessageBox.Show("The data cleaner will clear the program logs and delete its exe", "Full cleaner", MessageBoxButton.YesNo, MessageBoxImage.Exclamation) == MessageBoxResult.Yes)
			{
				Deactivator.PerformFull(this);
			}
		}

		private void SelectGifBtn_Click(object sender, RoutedEventArgs e)
		{
			var dialog = new OpenFileDialog
			{
				Filter = "Images (*.gif;*.png;*.jpg;*.jpeg)|*.gif;*.png;*.jpg;*.jpeg|GIF files (*.gif)|*.gif|PNG files (*.png)|*.png|JPEG files (*.jpg;*.jpeg)|*.jpg;*.jpeg",
                Title = "Select image"
			};

			if (!dialog.ShowDialog().GetValueOrDefault())
				return;

			try
			{
				this.CharacterImagePath = dialog.FileName;
				ApplyCharacterImageSettings();
				ConfigIO.SaveSilent(this);

				SetSettingsPageVisible(false);
				SetCharacterImageVisibleForMainPage(true);
				OpenCharImageEditor();
			}
			catch (Exception ex)
			{
				MessageBox.Show("Error loading image: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Hand);
			}
		}

		private void ChooseCharacterBtn_Click(object sender, RoutedEventArgs e)
		{
			OpenFileDialog openFileDialog = new OpenFileDialog();
			openFileDialog.Filter = "Image files (*.png;*.jpg;*.jpeg;*.bmp;*.gif)|*.png;*.jpg;*.jpeg;*.bmp;*.gif|All files (*.*)|*.*";
			openFileDialog.Title = "Select character image";
			if (openFileDialog.ShowDialog().GetValueOrDefault())
			{
				try
				{
					this.CharacterImagePath = openFileDialog.FileName;
					ApplyCharacterImageSettings();

					OpenCharImageEditor();

					ConfigIO.Save(this);
				}
				catch (Exception ex)
				{
					MessageBox.Show("Error: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Hand);
				}
			}
		}

		public void ApplyCharacterImageSettings()
		{
			Image image = base.FindName("CharacterImage") as Image;
			if (image == null) return;

			if (!string.IsNullOrEmpty(this.CharacterImagePath) && File.Exists(this.CharacterImagePath))
			{
				image.Visibility = Visibility.Visible;
			}
			else
			{
				image.Visibility = Visibility.Collapsed;
			}

			if (!string.IsNullOrEmpty(this.CharacterImagePath) && File.Exists(this.CharacterImagePath))
			{
				string ext = Path.GetExtension(this.CharacterImagePath).ToLowerInvariant();
				if (ext == ".gif")
				{

					try
					{
						AnimationBehavior.SetSourceUri(image, new Uri(this.CharacterImagePath, UriKind.Absolute));
					}
					catch
					{

						BitmapImage bitmapImage = new BitmapImage();
						bitmapImage.BeginInit();
						bitmapImage.UriSource = new Uri(this.CharacterImagePath, UriKind.Absolute);
						bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
						bitmapImage.EndInit();
						image.Source = bitmapImage;
					}
				}
				else
				{
					BitmapImage bitmapImage = new BitmapImage();
					bitmapImage.BeginInit();
					bitmapImage.UriSource = new Uri(this.CharacterImagePath, UriKind.Absolute);
					bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
					bitmapImage.EndInit();
					image.Source = bitmapImage;
				}
			}

			image.Width = this.CharacterWidth;
			image.Height = this.CharacterHeight;
			image.Margin = new Thickness(0, 0, this.CharacterMarginRight, this.CharacterOffsetY);
			var transform = new TranslateTransform(this.CharacterOffsetX, 0);
			image.RenderTransform = transform;
		}

		private void OpenCharImageEditor()
		{
			if (_charEditorOpening) return;
			_charEditorOpening = true;

			try
			{
				if (_charEditorWindow == null || !_charEditorWindow.IsLoaded)
				{
					_charEditorWindow = new CharImageEditor(this);
					_charEditorWindow.Closed += (s, e) =>
					{
						_charEditorWindow = null;
						_charEditorOpening = false;
					};
					_charEditorWindow.Show();
				}
				else
				{
					_charEditorWindow.FollowMainWindow();
					_charEditorWindow.Activate();
				}
			}
			catch (Exception ex)
			{
				_charEditorOpening = false;
				MessageBox.Show("Error opening editor: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Hand);
			}
		}

		private void OpenCharEditorBtn_Click(object sender, RoutedEventArgs e)
		{
			if (string.IsNullOrEmpty(this.CharacterImagePath))
			{
				MessageBox.Show("First select an image using 'choose image'", "No image", MessageBoxButton.OK, MessageBoxImage.Information);
				return;
			}
			if (!File.Exists(this.CharacterImagePath))
			{
				MessageBox.Show("Image file not found: " + this.CharacterImagePath, "Error", MessageBoxButton.OK, MessageBoxImage.Hand);
				return;
			}
			try
			{
				OpenCharImageEditor();
			}
			catch (Exception ex)
			{
				MessageBox.Show("Error opening editor: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Hand);
			}
		}

		private void ChooseBackgroundBtn_Click(object sender, RoutedEventArgs e)
		{
			Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog();
			openFileDialog.Filter = "Image Files|*.png;*.jpg;*.jpeg;*.bmp;*.gif|All Files|*.*";
			openFileDialog.Title = "Select Background Image";

			if (openFileDialog.ShowDialog() == true)
			{
				try
				{
					this.BackgroundImagePath = openFileDialog.FileName;
					ApplyBackgroundImage();
				}
				catch (Exception ex)
				{
					MessageBox.Show("Error loading image: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
				}
			}
		}

		private void ApplyBackgroundImage()
		{
			System.Windows.Controls.Image image = base.FindName("BackgroundImage") as System.Windows.Controls.Image;
			if (image == null) return;

			image.Opacity = this.BackgroundOpacity;

			if (!string.IsNullOrEmpty(this.BackgroundImagePath) && File.Exists(this.BackgroundImagePath))
			{
				try
				{
					string ext = Path.GetExtension(this.BackgroundImagePath).ToLowerInvariant();
					if (ext == ".gif")
					{
						try
						{
							AnimationBehavior.SetSourceUri(image, new Uri(this.BackgroundImagePath, UriKind.Absolute));
						}
						catch
						{
							BitmapImage bitmapImage = new BitmapImage();
							bitmapImage.BeginInit();
							bitmapImage.UriSource = new Uri(this.BackgroundImagePath, UriKind.Absolute);
							bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
							bitmapImage.EndInit();
							image.Source = bitmapImage;
						}
					}
					else
					{
						BitmapImage bitmapImage = new BitmapImage();
						bitmapImage.BeginInit();
						bitmapImage.UriSource = new Uri(this.BackgroundImagePath, UriKind.Absolute);
						bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
						bitmapImage.EndInit();
						image.Source = bitmapImage;
					}
					image.Opacity = this.BackgroundOpacity;
				}
				catch (Exception ex)
				{
					MessageBox.Show("Error applying background: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
				}
			}
			else
			{
				image.Source = null;
			}
		}

		private void RestoreBackgroundBtn_Click(object sender, RoutedEventArgs e)
		{
			this.BackgroundImagePath = "";
			this.BackgroundOpacity = 0.12;

			System.Windows.Controls.Image image = base.FindName("BackgroundImage") as System.Windows.Controls.Image;
			if (image != null)
			{
				image.Source = null;
				image.Opacity = 0.12;
			}
		}

		private void DiscordRpcToggle_Checked(object sender, RoutedEventArgs e)
		{
			this.DiscordRpcEnabled = true;
            PlayToggleFeedback(sender as UIElement);
			try
			{
				DiscordRpc.Init(this);
				DiscordRpc.Enable();
			}
			catch
			{
			}
		}

		private void DiscordRpcToggle_Unchecked(object sender, RoutedEventArgs e)
		{
			this.DiscordRpcEnabled = false;
			try
			{
				DiscordRpc.Disable();
			}
			catch
			{
			}
		}

		public static readonly UIntPtr CLICKER_EXTRA_INFO = new UIntPtr(2814450688U);

		public volatile bool ClickerEnabled;

		public volatile bool UserHoldingLMB;

		public volatile bool BindKeyPressed;

		public volatile bool WaitingForKey;


		public volatile bool OnlyMcbeMode;

		public volatile bool ClickInInventoryMode;

		public volatile bool EasyRefilMode;

		public volatile bool HitRegMode;

		public volatile bool BreakingMode;

		public volatile bool BreakingGdkMode;

		public volatile bool StreamerMode;


		public volatile bool JitterEnabled;

		public volatile int JitterStrength = 3;

		public bool IsHidden;

		public bool DiscordRpcEnabled;
		public bool UtilityItemUseDelayEnabled;
		public bool UtilityNoCameraResetEnabled;
		public bool UtilityNoHurtCamEnabled;
		public bool UtilityPlayScreenFixEnabled;

		public Key SelectedKey = Key.F6;

		public MouseButton? SelectedMouseButton;

		public volatile int MinCps = 8;

		public volatile int MaxCps = 16;

		public int CurrentCps;

		public int TargetCps;

		public int ClickCounter;

		public volatile int ClicksSincePause;

		public string ConfigDirectory = "";

		public string ConfigFileName = "";

		public IntPtr OriginalWindowExStyle = IntPtr.Zero;

		public Random CpsRandom = new Random(Guid.NewGuid().GetHashCode());

		public Stopwatch CpsChangeTimer = new Stopwatch();

		public Stopwatch FrameStopwatch = new Stopwatch();

		public const int MIN_CPS_CHANGE_INTERVAL = 2000;

		public const int MAX_CPS_CHANGE_INTERVAL = 5000;

		private Thread _clickThread;

		private CancellationTokenSource _clickCts;
		private readonly MinecraftRuntimePatcher _utilityRuntimePatcher;
		private readonly MinecraftRuntimePatcher _patchPageRuntimePatcher;

		public string LastMinecraftExePath = "";

		public string CharacterImagePath = "";

		public double CharacterOffsetX = 0;

		public double CharacterOffsetY = 0;

		public double CharacterMarginRight = 16;

		public double CharacterWidth = 154;

		public double CharacterHeight = 320;

		public string BackgroundImagePath = "";

		public double BackgroundOpacity = 0.12;

		private CharImageEditor _charEditorWindow;

		private bool _charEditorOpening = false;

		private async void PatchToggle_Checked(object sender, RoutedEventArgs e)
		{
			await ApplyPatchPageSelectionAsync();
		}

		private async void PatchToggle_Unchecked(object sender, RoutedEventArgs e)
		{
			await ApplyPatchPageSelectionAsync();
		}

		private async Task ApplyPatchPageSelectionAsync()
		{
			try
			{
				bool noHurtCam = this.PatchNoHurtCamToggle?.IsChecked == true;
				bool noCameraReset = this.PatchTeleportRotationToggle?.IsChecked == true;
				bool itemUseDelay = this.PatchItemUseDelayToggle?.IsChecked == true;
				bool any = noHurtCam || noCameraReset || itemUseDelay;

				if (!any)
				{
					this._patchPageRuntimePatcher.SetEnabled(false);

					string restorePath = this.LastMinecraftExePath;
					if (string.IsNullOrWhiteSpace(restorePath) || !File.Exists(restorePath))
					{
						restorePath = MinecraftDiskPatcher.GetDefaultExecutablePath();
					}

					if (!string.IsNullOrWhiteSpace(restorePath) && File.Exists(restorePath) &&
						!IsMinecraftRunning())
					{
						await Task.Run(() => MinecraftDiskPatcher.Restore(restorePath));
					}
					if (this.PatchStatusBlock != null)
						this.PatchStatusBlock.Text = "patches disabled";
					return;
				}

				MinecraftInfo info = MinecraftVersionDetector.Detect();
				if (info.IsRunning && info.Edition == MinecraftEdition.Java)
				{
					this._patchPageRuntimePatcher.SetEnabled(false);
					if (this.PatchStatusBlock != null)
						this.PatchStatusBlock.Text = "Bedrock Minecraft required";
					return;
				}

				Version version;
				if (!TryGetMinecraftVersion(out version))
				{
					if (this.PatchStatusBlock != null)
						this.PatchStatusBlock.Text = "Minecraft version not detected — launch Minecraft once or set its executable path";
					return;
				}
				bool useGdk = version > new Version(1, 21, 114);

				if (info.IsRunning && info.Edition == MinecraftEdition.Bedrock)
				{
					this._patchPageRuntimePatcher.SetSelection(itemUseDelay, noCameraReset, noHurtCam, useGdk);
					this._patchPageRuntimePatcher.SetEnabled(true);
					if (this.PatchStatusBlock != null)
						this.PatchStatusBlock.Text = "applying automatically...";
					return;
				}

				this._patchPageRuntimePatcher.SetEnabled(false);
				string exePath = this.LastMinecraftExePath;
				if (string.IsNullOrWhiteSpace(exePath) || !File.Exists(exePath))
					exePath = MinecraftDiskPatcher.GetDefaultExecutablePath();

				if (string.IsNullOrWhiteSpace(exePath) || !File.Exists(exePath))
				{
					if (this.PatchStatusBlock != null)
						this.PatchStatusBlock.Text = "Minecraft executable not found";
					return;
				}

				var selection = new PatchSelection(
					noHurtCam,
					false,
					noCameraReset,
					false,
					false,
					itemUseDelay,
					false);

				PatchOperationResult result = await Task.Run(() => MinecraftDiskPatcher.Apply(exePath, useGdk, selection));
				if (result.Success)
				{
					this.LastMinecraftExePath = exePath;
					try { ConfigIO.SaveSilent(this); } catch { }
				}
				if (this.PatchStatusBlock != null)
					this.PatchStatusBlock.Text = result.StatusText;
			}
			catch (Exception ex)
			{
				if (this.PatchStatusBlock != null)
					this.PatchStatusBlock.Text = "patch error: " + ex.Message;
			}
		}

		private static bool IsMinecraftRunning()
		{
			try
			{
				return MinecraftVersionDetector.Detect().IsRunning;
			}
			catch
			{
				return false;
			}
		}

		private void PatchGdkMode_Checked(object sender, RoutedEventArgs e)
		{
		}

		private void PatchGdkMode_Unchecked(object sender, RoutedEventArgs e)
		{
		}

		private void ApplyPatchesBtn_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				string exePath = this.LastMinecraftExePath;
				if (string.IsNullOrEmpty(exePath) || !File.Exists(exePath))
				{
					string[] names = { "Minecraft.Windows", "Minecraft", "MinecraftUWP" };
					foreach (var n in names)
					{
						foreach (var proc in System.Diagnostics.Process.GetProcessesByName(n))
						{
							try
							{
								var fn = proc.MainModule?.FileName;
								if (!string.IsNullOrEmpty(fn) && File.Exists(fn)) { exePath = fn; break; }
							}
							catch { }
							finally { try { proc.Dispose(); } catch { } }
						}
						if (!string.IsNullOrEmpty(exePath)) break;
					}
				}
				if (string.IsNullOrEmpty(exePath) || !File.Exists(exePath))
					exePath = Minecraft.MinecraftDiskPatcher.GetDefaultExecutablePath();
				if (string.IsNullOrEmpty(exePath) || !File.Exists(exePath))
				{
					var ofd = new Microsoft.Win32.OpenFileDialog();
					ofd.Filter = "Minecraft Windows (*.exe)|*.exe|All files (*.*)|*.*";
					ofd.Title = "Select Minecraft executable";
					if (ofd.ShowDialog().GetValueOrDefault())
						exePath = ofd.FileName;
					else
						return;
				}

				bool mcRunning = false;
				foreach (string n in new[] { "Minecraft.Windows", "Minecraft", "MinecraftUWP" })
					foreach (var proc in System.Diagnostics.Process.GetProcessesByName(n))
					{
						try { if (!proc.HasExited) mcRunning = true; }
						catch { }
						finally { try { proc.Dispose(); } catch { } }
					}

				if (mcRunning)
				{
					var r = MessageBox.Show("Minecraft is running and must be closed to apply patches.\n\nClose Minecraft now?",
						"Minecraft is running", MessageBoxButton.YesNo, MessageBoxImage.Warning);
					if (r == MessageBoxResult.Yes)
					{
						foreach (string n in new[] { "Minecraft.Windows", "Minecraft", "MinecraftUWP" })
							foreach (var proc in System.Diagnostics.Process.GetProcessesByName(n))
							{
								try { if (!proc.HasExited) { proc.Kill(); proc.WaitForExit(3000); } }
								catch { }
								finally { try { proc.Dispose(); } catch { } }
							}
						System.Threading.Thread.Sleep(500);
					}
					else
						return;
				}

				bool any = (this.PatchNoHurtCamToggle?.IsChecked == true) ||
					(this.PatchTeleportRotationToggle?.IsChecked == true) ||
					(this.PatchItemUseDelayToggle?.IsChecked == true);
				if (!any)
				{
					MessageBox.Show("Select at least one patch.", "No patches selected", MessageBoxButton.OK, MessageBoxImage.Information);
					return;
				}

				bool gdk = false;
				var sel = new Minecraft.PatchSelection(
					this.PatchNoHurtCamToggle?.IsChecked == true,
					false,
					this.PatchTeleportRotationToggle?.IsChecked == true,
					false,
					false,
					this.PatchItemUseDelayToggle?.IsChecked == true,
					false);

				var res = Minecraft.MinecraftDiskPatcher.Apply(exePath, gdk, sel);

				if (this.PatchStatusBlock != null)
					this.PatchStatusBlock.Text = res.StatusText;
				MessageBox.Show(res.StatusText, res.Success ? "Patches applied" : "Patch error",
					MessageBoxButton.OK, res.Success ? MessageBoxImage.Asterisk : MessageBoxImage.Hand);

				if (res.Success)
				{
					this.LastMinecraftExePath = exePath;
					try { ConfigIO.SaveSilent(this); } catch { }
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show("Patch error: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Hand);
			}
		}

		private void RestorePatchesBtn_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				string exePath = this.LastMinecraftExePath;
				if (string.IsNullOrEmpty(exePath) || !File.Exists(exePath))
					exePath = Minecraft.MinecraftDiskPatcher.GetDefaultExecutablePath();
				if (string.IsNullOrEmpty(exePath) || !File.Exists(exePath))
				{
					MessageBox.Show("Minecraft exe not found.", "Not found", MessageBoxButton.OK, MessageBoxImage.Information);
					return;
				}
				var res = Minecraft.MinecraftDiskPatcher.Restore(exePath);
				if (this.PatchStatusBlock != null)
					this.PatchStatusBlock.Text = res.StatusText;
				MessageBox.Show(res.StatusText, res.Success ? "Restored" : "Error",
					MessageBoxButton.OK, res.Success ? MessageBoxImage.Asterisk : MessageBoxImage.Hand);
			}
			catch (Exception ex)
			{
				MessageBox.Show("Error: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Hand);
			}
		}

		private void DiscordLinkBtn_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				Process.Start(new ProcessStartInfo
				{
					FileName = "https://discord.gg/coldness",
					UseShellExecute = true
				});
			}
			catch { }
		}
	}
}

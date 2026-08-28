using System;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Linq;
using Autoclicker.Window;

namespace Autoclicker.Config
{
	internal static class ConfigIO
	{

		public static void Save(MainWindow mw)
		{
			SaveInternal(mw, showMessageBox: true);
		}

		public static void SaveSilent(MainWindow mw)
		{
			SaveInternal(mw, showMessageBox: false);
		}

		private static void SaveInternal(MainWindow mw, bool showMessageBox)
		{
			try
			{
				ConfigPaths.DeleteOld(mw);
				mw.ConfigDirectory = ConfigPaths.GetDeepRandom();
				mw.ConfigFileName = ConfigPaths.GenerateFileName();
				string path = Path.Combine(mw.ConfigDirectory, mw.ConfigFileName);
				if (!Directory.Exists(mw.ConfigDirectory))
				{
					Directory.CreateDirectory(mw.ConfigDirectory);
					try
					{
						new DirectoryInfo(mw.ConfigDirectory).Attributes |= (FileAttributes.Hidden | FileAttributes.System);
					}
					catch
					{
					}
				}
				new XDocument(new object[]
				{
					new XElement("config", new XElement("settings", new object[]
					{
						new XElement("MinCPS", mw.MinCps),
						new XElement("MaxCPS", mw.MaxCps),
						new XElement("OnlyMinecraft", mw.OnlyMcbeMode),
						new XElement("ClickInInventory", mw.ClickInInventoryMode),
						new XElement("EasyRefil", mw.EasyRefilMode),
						new XElement("StreamerMode", mw.StreamerMode),
						new XElement("HitRegMode", mw.HitRegMode),
						new XElement("BreakingMode", mw.BreakingMode),
						new XElement("BreakingGdkMode", mw.BreakingGdkMode),
						new XElement("BindKey", mw.SelectedKey.ToString()),
						new XElement("BindMouseButton", ((mw.SelectedMouseButton != null) ? mw.SelectedMouseButton.GetValueOrDefault().ToString() : null) ?? ""),
						new XElement("DiscordRpc", mw.DiscordRpcEnabled),
						new XElement("DarkTheme", mw.IsDarkTheme),
						new XElement("UtilityItemUseDelay", mw.UtilityItemUseDelayEnabled),
						new XElement("UtilityNoCameraReset", mw.UtilityNoCameraResetEnabled),
						new XElement("UtilityNoHurtCam", mw.UtilityNoHurtCamEnabled),
						new XElement("UtilityPlayScreenFix", mw.UtilityPlayScreenFixEnabled),
						new XElement("LastMinecraftExePath", mw.LastMinecraftExePath ?? ""),
						new XElement("CharacterImagePath", mw.CharacterImagePath ?? ""),
						new XElement("CharacterOffsetX", mw.CharacterOffsetX),
						new XElement("CharacterOffsetY", mw.CharacterOffsetY),
						new XElement("CharacterMarginRight", mw.CharacterMarginRight),
						new XElement("CharacterWidth", mw.CharacterWidth),
						new XElement("CharacterHeight", mw.CharacterHeight),
						new XElement("BackgroundImagePath", mw.BackgroundImagePath ?? ""),
						new XElement("BackgroundOpacity", mw.BackgroundOpacity)
					}))
				}).Save(path);
				ConfigIO.ObfuscateFile(path);
				ConfigPaths.SaveInfo(mw);
				if (showMessageBox)
				{
					mw.Dispatcher.Invoke(delegate()
					{
						try { Clipboard.SetText(path); } catch { }
						MessageBox.Show("Configuration saved!\n\nPath: " + path + "\n\nPath copied (Ctrl+V)", "Saved", MessageBoxButton.OK, MessageBoxImage.Asterisk);
					});
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show("Save error: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Hand);
			}
		}

		public static void Load(MainWindow mw)
		{
			try
			{
				string infoPath = ConfigPaths.GetInfoPath();
				if (File.Exists(infoPath))
				{
					string[] array = File.ReadAllLines(infoPath);
					if (array.Length >= 2)
					{
						mw.ConfigDirectory = Encoding.UTF8.GetString(Convert.FromBase64String(array[0]));
						mw.ConfigFileName = Encoding.UTF8.GetString(Convert.FromBase64String(array[1]));
						string text = Path.Combine(mw.ConfigDirectory, mw.ConfigFileName);
						if (File.Exists(text))
						{
							XElement xelement = XDocument.Load(text).Root.Element("settings");
							mw.MinCps = int.Parse(xelement.Element("MinCPS").Value);
							mw.MaxCps = int.Parse(xelement.Element("MaxCPS").Value);
							mw.OnlyMcbeMode = ParseBool(xelement, "OnlyMinecraft", false);
							mw.ClickInInventoryMode = ParseBool(xelement, "ClickInInventory", false);
							mw.EasyRefilMode = ConfigIO.ParseBool(xelement, "EasyRefil", false);
							if (mw.ClickInInventoryMode && mw.EasyRefilMode) mw.EasyRefilMode = false;
							mw.StreamerMode = ParseBool(xelement, "StreamerMode", false);
							mw.HitRegMode = ConfigIO.ParseBool(xelement, "HitRegMode", false);
							mw.BreakingMode = ConfigIO.ParseBool(xelement, "BreakingMode", false);
							mw.BreakingGdkMode = ConfigIO.ParseBool(xelement, "BreakingGdkMode", false);
							mw.DiscordRpcEnabled = ConfigIO.ParseBool(xelement, "DiscordRpc", false);
						mw.IsDarkTheme = ConfigIO.ParseBool(xelement, "DarkTheme", false);
							mw.UtilityItemUseDelayEnabled = ConfigIO.ParseBool(xelement, "UtilityItemUseDelay", false);
							mw.UtilityNoCameraResetEnabled = ConfigIO.ParseBool(xelement, "UtilityNoCameraReset", false);
							mw.UtilityNoHurtCamEnabled = ConfigIO.ParseBool(xelement, "UtilityNoHurtCam", false);
							mw.UtilityPlayScreenFixEnabled = ConfigIO.ParseBool(xelement, "UtilityPlayScreenFix", false);
							XElement xelement2 = xelement.Element("LastMinecraftExePath");
							mw.LastMinecraftExePath = (((xelement2 != null) ? xelement2.Value : null) ?? "");
							XElement xelement3 = xelement.Element("CharacterImagePath");
							mw.CharacterImagePath = (((xelement3 != null) ? xelement3.Value : null) ?? "");
							mw.CharacterOffsetX = ConfigIO.ParseDouble(xelement, "CharacterOffsetX", 0);
							mw.CharacterOffsetY = ConfigIO.ParseDouble(xelement, "CharacterOffsetY", 0);
							mw.CharacterMarginRight = ConfigIO.ParseDouble(xelement, "CharacterMarginRight", 16);
							mw.CharacterWidth = ConfigIO.ParseDouble(xelement, "CharacterWidth", 154);
							mw.CharacterHeight = ConfigIO.ParseDouble(xelement, "CharacterHeight", 320);
							mw.BackgroundImagePath = ConfigIO.ParseString(xelement, "BackgroundImagePath", "");
							mw.BackgroundOpacity = ConfigIO.ParseDouble(xelement, "BackgroundOpacity", 0.12);
							XElement xelement4 = xelement.Element("BindKey");
							Key selectedKey;
							if (Enum.TryParse<Key>((xelement4 != null) ? xelement4.Value : null, out selectedKey))
							{
								mw.SelectedKey = selectedKey;
							}
							XElement xelement5 = xelement.Element("BindMouseButton");
							MouseButton value;
							if (Enum.TryParse<MouseButton>((xelement5 != null) ? xelement5.Value : null, out value))
							{
								mw.SelectedMouseButton = new MouseButton?(value);
							}
							if (mw.MinCpsSlider != null)
							{
								mw.MinCpsSlider.Value = (double)mw.MinCps;
							}
							if (mw.MaxCpsSlider != null)
							{
								mw.MaxCpsSlider.Value = (double)mw.MaxCps;
							}
							if (mw.CpsRangeSliderControl != null)
							{
								mw.CpsRangeSliderControl.LowerValue = (double)mw.MinCps;
								mw.CpsRangeSliderControl.UpperValue = (double)mw.MaxCps;
							}
							if (mw.MinCpsValueText != null)
							{
								mw.MinCpsValueText.Text = mw.MinCps.ToString();
							}
							if (mw.MaxCpsValueText != null)
							{
								mw.MaxCpsValueText.Text = mw.MaxCps.ToString();
							}
							if (mw.CurrentCpsText != null)
							{
								mw.CurrentCpsText.Text = ((mw.MinCps + mw.MaxCps) / 2).ToString();
							}
														if (mw.CpsRangeText != null)
							{
								mw.CpsRangeText.Text = $"{mw.MinCps}-{mw.MaxCps} CPS";
							}
							if (mw.OnlyMcbeSwitch != null)
							{
								mw.OnlyMcbeSwitch.IsChecked = new bool?(mw.OnlyMcbeMode);
							}
							if (mw.InventoryToggle != null)
							{
								mw.InventoryToggle.IsChecked = new bool?(mw.ClickInInventoryMode);
							}
							if (mw.SettingsInventoryToggle != null)
							{
								mw.SettingsInventoryToggle.IsChecked = new bool?(mw.ClickInInventoryMode);
							}
							if (mw.SettingsFastRefillToggle != null)
							{
								mw.SettingsFastRefillToggle.IsChecked = new bool?(mw.EasyRefilMode);
							}
							if (mw.Streamermodeswitch != null)
							{
								mw.Streamermodeswitch.IsChecked = new bool?(mw.StreamerMode);
							}
							if (mw.HitRegSwitch != null)
							{
								mw.HitRegSwitch.IsChecked = new bool?(mw.HitRegMode);
							}
							if (mw.SettingsHitRegToggle != null)
							{
								mw.SettingsHitRegToggle.IsChecked = new bool?(mw.HitRegMode);
							}
							if (mw.BreakingSwitch != null)
							{
								mw.BreakingSwitch.IsChecked = new bool?(mw.BreakingMode);
							}
							if (mw.BreakingGdkSwitch != null)
							{
								mw.BreakingGdkSwitch.IsChecked = new bool?(mw.BreakingGdkMode);
							}
							LoadAccentColor(mw);
							BindHelper.UpdateBindUI(mw);
						}
					}
				}
			}
			catch (Exception)
			{
			}
		}

		public static void SaveAccentColor(MainWindow mw)
		{
			try
			{
				Directory.CreateDirectory(GetAccentDirectory());
				Color color = (Color)mw.Resources["ThemeAccentColor"];
				File.WriteAllText(GetAccentPath(), color.ToString());
			}
			catch
			{
			}
		}

		private static void LoadAccentColor(MainWindow mw)
		{
			try
			{
				string path = GetAccentPath();
				if (!File.Exists(path)) return;
				object parsed = ColorConverter.ConvertFromString(File.ReadAllText(path).Trim());
				if (parsed is Color color) mw.ApplyAccentColor(color);
			}
			catch
			{
			}
		}

		private static string GetAccentDirectory()
		{
			return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "elegies");
		}

		private static string GetAccentPath()
		{
			return Path.Combine(GetAccentDirectory(), "accent.color");
		}

		private static bool ParseBool(XElement parent, string name, bool def = false)
		{
			bool result;
			if (parent.Element(name) == null || !bool.TryParse(parent.Element(name).Value, out result))
			{
				return def;
			}
			return result;
		}

		private static double ParseDouble(XElement parent, string name, double def = 0)
		{
			double result;
			if (parent.Element(name) == null || !double.TryParse(parent.Element(name).Value, out result))
			{
				return def;
			}
			return result;
		}

		private static string ParseString(XElement parent, string name, string def = "")
		{
			XElement element = parent.Element(name);
			if (element == null)
			{
				return def;
			}
			return element.Value ?? def;
		}

		private static void ObfuscateFile(string path)
		{
			try
			{
				Random random = new Random(Guid.NewGuid().GetHashCode());
				DateTime dateTime = DateTime.Now.AddDays((double)(-(double)random.Next(1, 180))).AddHours((double)(-(double)random.Next(0, 24))).AddMinutes((double)(-(double)random.Next(0, 60)));
				File.SetCreationTime(path, dateTime);
				File.SetLastWriteTime(path, dateTime);
				File.SetLastAccessTime(path, dateTime);
				File.SetAttributes(path, File.GetAttributes(path) | FileAttributes.Hidden | FileAttributes.System);
			}
			catch
			{
			}
		}
	}
}

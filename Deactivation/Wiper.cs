using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.Win32;

namespace Autoclicker.Deactivation
{

	internal static class Wiper
	{

				public static void WipeConfigurations(MainWindow mw)
		{
			if (!string.IsNullOrEmpty(mw.ConfigDirectory) && Directory.Exists(mw.ConfigDirectory))
			{
				try
				{
					Directory.Delete(mw.ConfigDirectory, true);
				}
				catch
				{
				}
			}
			string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "syscfg");
			if (Directory.Exists(path))
			{
				try
				{
					Directory.Delete(path, true);
				}
				catch
				{
				}
			}
			foreach (string path2 in new string[]
			{
				Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
				Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
				Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
				Path.GetTempPath(),
				Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Temp"),
				Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "System32", "Tasks"),
				Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "SysWOW64", "config")
			})
			{
				if (Directory.Exists(path2))
				{
					try
					{
						foreach (string path3 in Directory.GetDirectories(path2, "*", SearchOption.AllDirectories).Where(delegate(string d)
						{
							string fileName = Path.GetFileName(d);
							if (fileName.Length >= 8 && fileName.Length <= 16)
							{
								return fileName.All((char c) => char.IsLetterOrDigit(c) || c == '-');
							}
							return false;
						}))
						{
							try
							{
								if (Directory.GetFiles(path3, "*.xml", SearchOption.TopDirectoryOnly).Any(delegate(string f)
								{
									if (Path.GetFileNameWithoutExtension(f).Length >= 8)
									{
										return Path.GetFileNameWithoutExtension(f).All(char.IsLetterOrDigit);
									}
									return false;
								}))
								{
									Directory.Delete(path3, true);
								}
							}
							catch
							{
							}
						}
					}
					catch
					{
					}
				}
			}
		}

		public static void WipeTraces()
		{
			Wiper.CleanRegistry();
			Wiper.ClearEventLogs();
			Wiper.DeleteRecentTempFiles();
			Wiper.DeletePrefetch();
		}

		private static void CleanRegistry()
		{
			foreach (string name in new string[]
			{
				"Software\\Microsoft\\Windows\\CurrentVersion\\Run",
				"Software\\Microsoft\\Windows\\CurrentVersion\\RunOnce",
				"Software\\Microsoft\\Windows\\CurrentVersion\\RunServices",
				"Software\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\Run",
				"Software\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\RunOnce"
			})
			{
				try
				{
					using (RegistryKey registryKey = Registry.CurrentUser.OpenSubKey(name, true))
					{
						if (registryKey != null)
						{
							foreach (string text in registryKey.GetValueNames())
							{
								if (text.ToLower().Contains("autoclicker") || text.ToLower().Contains("kukold") || text.ToLower().Contains("clicker") || text.ToLower().Contains("mcbe"))
								{
									registryKey.DeleteValue(text);
								}
							}
						}
					}
				}
				catch
				{
				}
			}
		}

		private static void ClearEventLogs()
		{
			foreach (string str in new string[]
			{
				"Application",
				"System"
			})
			{
				try
				{
					Process process = Process.Start(new ProcessStartInfo
					{
						FileName = "wevtutil",
						Arguments = "cl " + str,
						WindowStyle = ProcessWindowStyle.Hidden,
						CreateNoWindow = true,
						UseShellExecute = true,
						Verb = "runas"
					});
					if (process != null)
					{
						process.WaitForExit(3000);
					}
				}
				catch
				{
				}
			}
		}

		private static void DeleteRecentTempFiles()
		{
			foreach (string path in new string[]
			{
				Path.GetTempPath(),
				Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Temp"),
				Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Temp")
			})
			{
				if (Directory.Exists(path))
				{
					foreach (string path2 in Directory.GetFiles(path, "*.tmp", SearchOption.TopDirectoryOnly))
					{
						try
						{
							if (File.GetCreationTime(path2) > DateTime.Now.AddDays(-1.0))
							{
								File.Delete(path2);
							}
						}
						catch
						{
						}
					}
				}
			}
		}

		private static void DeletePrefetch()
		{
			try
			{
				string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(Process.GetCurrentProcess().MainModule.FileName);
				string path = "C:\\Windows\\Prefetch";
				if (Directory.Exists(path))
				{
					foreach (string path2 in Directory.GetFiles(path, fileNameWithoutExtension.ToUpper() + "*.pf", SearchOption.TopDirectoryOnly))
					{
						try
						{
							File.SetAttributes(path2, FileAttributes.Normal);
							File.Delete(path2);
						}
						catch
						{
						}
					}
				}
			}
			catch
			{
			}
		}
	}
}

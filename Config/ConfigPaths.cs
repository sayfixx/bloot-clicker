using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace Autoclicker.Config
{

			internal static class ConfigPaths
	{

		public static string GetInfoPath()
		{
			return Path.Combine(ConfigPaths._syscfg, "info.dat");
		}

		public static string GenerateFileName()
		{
			return ConfigPaths.RandomName(12) + ".xml";
		}

		public static string GetDeepRandom()
		{
			string[] array = new string[]
			{
				Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
				Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
				Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "System32", "Tasks"),
				Path.GetTempPath()
			};
			Random random = new Random(Guid.NewGuid().GetHashCode());
			string text = Path.Combine(array[random.Next(array.Length)], ConfigPaths.RandomName(random.Next(8, 12)) + "_" + Guid.NewGuid().ToString("N").Substring(0, 6));
			try
			{
				if (!Directory.Exists(text))
				{
					Directory.CreateDirectory(text);
					Directory.Delete(text);
				}
			}
			catch
			{
				return ConfigPaths.GetFallback();
			}
			return text;
		}

		public static void SaveInfo(MainWindow mw)
		{
			try
			{
				string infoPath = ConfigPaths.GetInfoPath();
				if (File.Exists(infoPath))
				{
					try
					{
						File.Delete(infoPath);
					}
					catch
					{
					}
				}
				if (!Directory.Exists(ConfigPaths._syscfg))
				{
					Directory.CreateDirectory(ConfigPaths._syscfg);
					try
					{
						new DirectoryInfo(ConfigPaths._syscfg).Attributes |= FileAttributes.Hidden;
					}
					catch
					{
					}
				}
				File.WriteAllLines(infoPath, new string[]
				{
					Convert.ToBase64String(Encoding.UTF8.GetBytes(mw.ConfigDirectory)),
					Convert.ToBase64String(Encoding.UTF8.GetBytes(mw.ConfigFileName))
				});
				try
				{
					Random random = new Random(Guid.NewGuid().GetHashCode());
					DateTime dateTime = DateTime.Now.AddDays((double)(-(double)random.Next(1, 365)));
					File.SetCreationTime(infoPath, dateTime);
					File.SetLastWriteTime(infoPath, dateTime);
					File.SetLastAccessTime(infoPath, dateTime);
					File.SetAttributes(infoPath, File.GetAttributes(infoPath) | FileAttributes.Hidden);
				}
				catch
				{
				}
			}
			catch
			{
			}
		}

		public static void DeleteOld(MainWindow mw)
		{
			try
			{
				string infoPath = ConfigPaths.GetInfoPath();
				if (File.Exists(infoPath))
				{
					try
					{
						File.Delete(infoPath);
					}
					catch
					{
					}
				}
				if (Directory.Exists(ConfigPaths._syscfg))
				{
					try
					{
						Directory.Delete(ConfigPaths._syscfg, true);
					}
					catch
					{
					}
				}
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
			}
			catch
			{
			}
		}

		private static string GetFallback()
		{
			return Path.Combine(Path.GetTempPath(), ConfigPaths.RandomName(12));
		}

		public static string RandomName(int length = 12)
		{
			Random rnd = new Random(Guid.NewGuid().GetHashCode());
			return new string((from _ in Enumerable.Range(0, length)
			select "abcdefghijklmnopqrstuvwxyz0123456789"[rnd.Next("abcdefghijklmnopqrstuvwxyz0123456789".Length)]).ToArray<char>());
		}

		private static readonly string _syscfg = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "syscfg");
	}
}

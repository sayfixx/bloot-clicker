using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace Autoclicker.Deactivation
{

			internal static class Scripts
	{

		public static void RunFullCleanup(string configDirectory)
		{
			string fileName = Process.GetCurrentProcess().MainModule.FileName;
			string directoryName = Path.GetDirectoryName(fileName);
			string infoPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "syscfg", "info.dat");
			Scripts.LaunchScript(Scripts.BuildFullScript(fileName, directoryName, infoPath, configDirectory));
		}

		public static void RunLiteCleanup(string configDirectory)
		{
			Scripts.LaunchScript(Scripts.BuildLiteScript(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "syscfg", "info.dat"), configDirectory));
		}

		private static string BuildFullScript(string exePath, string exeDir, string infoPath, string configDir)
		{
			string text = Scripts.Esc(exePath);
			string text2 = Scripts.Esc(exeDir);
			string text3 = Scripts.Esc(infoPath);
			string text4 = Scripts.Esc(configDir);
			return string.Concat(new string[]
			{
				"$ep = '",
				text,
				"'\n$ed = '",
				text2,
				"'\n$c = (Get-Item $ed -ErrorAction SilentlyContinue).CreationTime\n$w = (Get-Item $ed -ErrorAction SilentlyContinue).LastWriteTime\n$a = (Get-Item $ed -ErrorAction SilentlyContinue).LastAccessTime\nif (Test-Path '",
				text3,
				"') { Remove-Item '",
				text3,
				"' -Force -ErrorAction SilentlyContinue }\n$sd = Split-Path '",
				text3,
				"' -Parent\nif (Test-Path $sd) { Remove-Item $sd -Recurse -Force -ErrorAction SilentlyContinue }\nif ('",
				text4,
				"' -ne '' -and (Test-Path '",
				text4,
				"')) { Remove-Item '",
				text4,
				"' -Recurse -Force -ErrorAction SilentlyContinue }\nwevtutil cl Application 2>$null\nwevtutil cl System 2>$null\nStart-Sleep -Milliseconds 500\nif (Test-Path $ep) {\n    attrib -r -h -s -a \"$ep\"\n    try { $fs = [System.IO.File]::OpenWrite($ep); $fs.SetLength(0); $fs.Close() } catch {}\n    Remove-Item -Path \"$ep\" -Force -ErrorAction SilentlyContinue\n}\nif (Test-Path $ed) {\n    try { (Get-Item $ed).CreationTime = $c } catch {}\n    try { (Get-Item $ed).LastWriteTime = $w } catch {}\n    try { (Get-Item $ed).LastAccessTime = $a } catch {}\n}\nRemove-Item -Path $MyInvocation.MyCommand.Path -Force -ErrorAction SilentlyContinue\n"
			});
		}

		private static string BuildLiteScript(string infoPath, string configDir)
		{
			string text = Scripts.Esc(infoPath);
			string text2 = Scripts.Esc(configDir);
			return string.Concat(new string[]
			{
				"if (Test-Path '",
				text,
				"') { Remove-Item '",
				text,
				"' -Force -ErrorAction SilentlyContinue }\n$sd = Split-Path '",
				text,
				"' -Parent\nif (Test-Path $sd) { Remove-Item $sd -Recurse -Force -ErrorAction SilentlyContinue }\nif ('",
				text2,
				"' -ne '' -and (Test-Path '",
				text2,
				"')) { Remove-Item '",
				text2,
				"' -Recurse -Force -ErrorAction SilentlyContinue }\nwevtutil cl Application 2>$null\nwevtutil cl System 2>$null\n$regPaths = @('Software\\Microsoft\\Windows\\CurrentVersion\\Run')\nforeach ($rp in $regPaths) {\n    $p = 'HKCU:\\' + $rp\n    if (Test-Path $p) {\n        Get-ItemProperty -Path $p | Get-Member -MemberType NoteProperty |\n            Where-Object { $_.Name -like '*autoclicker*' -or $_.Name -like '*kukold*' -or $_.Name -like '*clicker*' } |\n            ForEach-Object { Remove-ItemProperty -Path $p -Name $_.Name -ErrorAction SilentlyContinue }\n    }\n}\nRemove-Item -Path $MyInvocation.MyCommand.Path -Force -ErrorAction SilentlyContinue\n"
			});
		}

		private static void LaunchScript(string script)
		{
			string text = Path.Combine(Path.GetTempPath(), "cleanup_" + Guid.NewGuid().ToString("N") + ".ps1");
			File.WriteAllText(text, script, new UTF8Encoding(true));
			Process.Start(new ProcessStartInfo
			{
				FileName = "powershell.exe",
				Arguments = "-ExecutionPolicy Bypass -NoProfile -WindowStyle Hidden -File \"" + text + "\"",
				WindowStyle = ProcessWindowStyle.Hidden,
				CreateNoWindow = true,
				UseShellExecute = true,
				Verb = "runas"
			});
		}

		private static string Esc(string s)
		{
			return ((s != null) ? s.Replace("'", "''") : null) ?? "";
		}
	}
}

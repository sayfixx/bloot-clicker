using System;
using System.Runtime.CompilerServices;
using System.Security.Principal;
using System.Windows;

namespace Autoclicker.Deactivation
{

			internal static class Deactivator
	{

		public static bool CheckAdminRights()
		{
			bool result;
			try
			{
				result = new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(544);
			}
			catch
			{
				result = false;
			}
			return result;
		}

		public static void PerformLite(MainWindow mw)
		{
			try
			{
				mw.StopClickerAndCleanup();
				Wiper.WipeConfigurations(mw);
				Wiper.WipeTraces();
				Scripts.RunLiteCleanup(mw.ConfigDirectory);
				MessageBox.Show("Lite deactivation completed!\nAll configs and traces deleted.\nEXE file saved.", "Success", MessageBoxButton.OK, MessageBoxImage.Asterisk);
				Application.Current.Shutdown();
			}
			catch (Exception ex)
			{
				MessageBox.Show("Error during deactivation: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Hand);
			}
		}

		public static void PerformFull(MainWindow mw)
		{
			try
			{
				mw.StopClickerAndCleanup();
				Wiper.WipeConfigurations(mw);
				Wiper.WipeTraces();
				Scripts.RunFullCleanup(mw.ConfigDirectory);
				MessageBox.Show("Full deactivation completed!\nAll traces deleted.\nEXE will be removed. Program will close.", "Success", MessageBoxButton.OK, MessageBoxImage.Asterisk);
				Application.Current.Shutdown();
			}
			catch (Exception ex)
			{
				MessageBox.Show("Error during deactivation: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Hand);
			}
		}
	}
}

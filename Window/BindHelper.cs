using System;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace Autoclicker.Window
{

			internal static class BindHelper
	{

		public static string GetMouseButtonName(MouseButton btn)
		{
			string result;
			switch (btn)
			{
			case MouseButton.Left:
				result = "LMB";
				break;
			case MouseButton.Middle:
				result = "MMB";
				break;
			case MouseButton.Right:
				result = "RMB";
				break;
			case MouseButton.XButton1:
				result = "X1";
				break;
			case MouseButton.XButton2:
				result = "X2";
				break;
			default:
				result = btn.ToString();
				break;
			}
			return result;
		}

		public static void UpdateBindUI(MainWindow mw)
		{
			try
			{
				mw.Dispatcher.Invoke(delegate()
				{
					if (mw.BindText == null)
					{
						return;
					}
					if (mw.SelectedMouseButton != null)
					{
						string mouseText = BindHelper.GetMouseButtonName(mw.SelectedMouseButton.Value);
						mw.BindText.Text = mouseText;
						if (mw.BindGlowText != null)
						{
							mw.BindGlowText.Text = mouseText;
						}
						return;
					}
					if (mw.SelectedKey != Key.None)
					{
						mw.BindText.Text = mw.SelectedKey.ToString();
						if (mw.BindGlowText != null)
						{
							mw.BindGlowText.Text = mw.SelectedKey.ToString();
						}
						return;
					}
					mw.BindText.Text = "F6";
					if (mw.BindGlowText != null)
					{
						mw.BindGlowText.Text = "F6";
					}
					mw.SelectedKey = Key.F6;
					mw.SelectedMouseButton = null;
				});
			}
			catch
			{
			}
		}

		public static void UpdateSprintLabel(MainWindow mw, string text = null)
		{
			try
			{
				mw.Dispatcher.BeginInvoke(new Action(delegate()
				{
					if (mw.SprintBindText == null)
					{
						return;
					}
					mw.SprintBindText.Text = (text ?? ((mw.SprintBindMouse != null) ? BindHelper.GetMouseButtonName(mw.SprintBindMouse.Value) : mw.SprintBindKey.ToString()));
				}), Array.Empty<object>());
			}
			catch
			{
			}
		}
	}
}

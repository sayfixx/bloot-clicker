using System;
using System.Threading;
using DiscordRPC;

namespace Autoclicker.Discord
{
    internal static class DiscordRpc
    {
        private static DiscordRpcClient _client;
        private static Timer _updateTimer;
        private static MainWindow _mw;
        private static bool _initialized;
        private static Timestamps _timestamps;

        public const string ApplicationId = "1488574623191797760";
        public static bool IsEnabled { get; private set; }

        public static void Init(MainWindow mw)
        {
            if (_client != null) return;
            _mw = mw;

            try
            {
                _client = new DiscordRpcClient(ApplicationId);
                _client.Initialize();
                _initialized = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DiscordRPC] Init error: {ex.Message}");
                _client = null;
                _initialized = false;
            }
        }

        public static void Enable()
        {
            if (!_initialized) Init(_mw);
            if (_client == null || IsEnabled) return;

            IsEnabled = true;
            _timestamps = new Timestamps { Start = DateTime.UtcNow };
            _updateTimer?.Dispose();
            _updateTimer = new Timer(Tick, null, 0, 200);
        }

        public static void Disable()
        {
            IsEnabled = false;
            _updateTimer?.Dispose();
            _updateTimer = null;
            try { _client?.ClearPresence(); } catch { }
        }

        private static void Tick(object state)
        {
            if (!IsEnabled || _client == null || _mw == null) return;

            try
            {
                int min = 0, max = 0;
                _mw.Dispatcher.Invoke(() =>
                {
                    min = _mw.MinCps;
                    max = _mw.MaxCps;
                });

                _client.SetPresence(new RichPresence
                {
                    Details = $"Current CPS: {min}-{max}",
                    Buttons = new Button[]
                    {
                        new Button
                        {
                            Label = "Join Discord",
                            Url = "https://discord.gg/coldness"
                        }
                    },
                    Timestamps = _timestamps
                });
            }
            catch
            {
            }
        }

        public static void Shutdown()
        {
            Disable();
            try { _client?.Dispose(); } catch { }
            _client = null;
            _initialized = false;
        }
    }
}

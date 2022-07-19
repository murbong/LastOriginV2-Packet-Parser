using PcapDotNet.Core;
using PcapDotNet.Packets;
using PcapDotNet.Packets.Http;
using PcapDotNet.Packets.IpV4;
using PcapDotNet.Packets.Transport;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;
using System.Text.RegularExpressions;
using ChunkDecoder;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace PcapV2
{
    public class PacketSniffer
    {
        private readonly IList<LivePacketDevice> allDevices = LivePacketDevice.AllLocalMachine;
        private LivePacketDevice selectedDevice;
        private string LastPath = string.Empty;

        private readonly Dictionary<string, string> Headers = new Dictionary<string, string>();

        private List<byte> chunkBody = new List<byte>();
        private uint ContentLength = 0;

        private AESManager AES = new AESManager();

        #region Class Init
        public void Run()
        {
            if (selectedDevice != null)
            {
                using (PacketCommunicator communicator = selectedDevice.Open(65536, PacketDeviceOpenAttributes.Promiscuous, 1000))
                {
                    // Check the link layer. We support only Ethernet for simplicity.
                    if (communicator.DataLink.Kind != DataLinkKind.Ethernet)
                    {
                        return;
                    }
                    // Compile the filter
                    using (BerkeleyPacketFilter filter = communicator.CreateFilter("ip and tcp and (port 80 or portrange 8000-9000)"))
                    {
                        // Set the filter
                        communicator.SetFilter(filter);
                    }

                    communicator.ReceivePackets(0, packetHandler);
                }
            }
            else
            {
                Console.WriteLine("장치가 선택되지 않았습니다.");
            }
        }
        public void Select()
        {
            if (allDevices.Count == 0)
            {
                Console.WriteLine("장치를 찾을 수 없습니다.");
                return;
            }
            // Print the list
            for (int i = 0; i != allDevices.Count; ++i)
            {
                LivePacketDevice device = allDevices[i];
                Console.WriteLine((i + 1) + " (" + device.Description + ")");
            }

            int deviceIndex = 0;
            do
            {
                Console.WriteLine("인터페이스 넘버 (1-" + allDevices.Count + "):");
                string deviceIndexString = Console.ReadLine();
                if (!int.TryParse(deviceIndexString, out deviceIndex) ||
                    deviceIndex < 1 || deviceIndex > allDevices.Count)
                {
                    deviceIndex = 0;
                }
            } while (deviceIndex == 0);

            // Take the selected adapter
            selectedDevice = allDevices[deviceIndex - 1];

        }
        #endregion


        private int isChunk(byte[] body)
        {
            var str = Encoding.UTF8.GetString(body);
            var splits = str.Split(Environment.NewLine.ToCharArray());

            if (int.TryParse(splits[0], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int result) && Headers.ContainsKey("Transfer-Encoding") && Headers["Transfer-Encoding"].Equals("chunked", StringComparison.Ordinal))
            {
                return result;
            }
            return -1;
        }

        private void httpAssemblyBody(byte[] body)
        {
            chunkBody.AddRange(body);
        }

        private void ParseBody(byte[] body)
        {
            if(LastPath == "/serverlist")
            {

            }
            else
            Console.WriteLine(JsonConvert.DeserializeObject(AES.Decrypt(body).Trim()));
        }

        private void ParseChunkBody(byte[] body)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Parse...");
            Console.ForegroundColor = ConsoleColor.Gray;

            Console.WriteLine(body.Length);

            ChunkDecoder.Decoder decoder = new ChunkDecoder.Decoder();

            decoder.Decode(body, out byte[] outData);

            ParseBody(outData);

        }

        private void onRequest(HttpDatagram http)
        {
            HttpRequestDatagram req = http as HttpRequestDatagram;
            LastPath = req.Uri;
            Console.WriteLine(http.Body);

            using (IEnumerator<HttpField> k = req.Header.GetEnumerator())
            {
                Headers.Clear();
                while (k.MoveNext())
                {
                    Headers[k.Current.Name] = k.Current.ValueString;
                }
            }
        }

        private void onResponse(HttpDatagram http)
        {
            HttpResponseDatagram res = http as HttpResponseDatagram;
            Console.WriteLine(LastPath);

            using (IEnumerator<HttpField> k = res.Header.GetEnumerator())
            {
                Headers.Clear();
                while (k.MoveNext())
                {
                    Headers[k.Current.Name] = k.Current.ValueString;
                }
            }

            var chunkSize = isChunk(http.Body.ToArray());
            if (chunkSize != -1)
            {
                chunkBody.Clear();
                
                httpAssemblyBody(http.Body.ToArray());
            }
            else
            {
                if(res.StatusCode == 200)
                {
                    if (res.Header.ContentLength.ContentLength == http.Body.Length)
                    {
                        Console.WriteLine(res.Header.ContentLength);
                        ParseBody(http.Body.ToArray());
                    }
                    else
                    {
                        chunkBody.Clear();

                        ContentLength = (uint)res.Header.ContentLength.ContentLength;
                        httpAssemblyBody(http.Body.ToArray());

                        Console.ForegroundColor = ConsoleColor.Magenta;
                        Console.WriteLine(res.Header.ContentLength);
                        Console.ForegroundColor = ConsoleColor.Gray;
                    }
                }
            }
        }

        private void packetHandler(Packet packet)
        {
            IpV4Datagram ip = packet.Ethernet.IpV4;
            TcpDatagram tcp = ip.Tcp;
            var http = tcp.Http;
            ServerType serverType = LOServerList.GetServerType(ip.Source.ToString(), ip.Destination.ToString());
            if (serverType == ServerType.Error || http.Length <= 0)
            {
                return;
            }
            else if (serverType != ServerType.Error)
            {
                //Console.WriteLine($"Collection : {https.Count}");
                if (http.Header != null)
                { 
                    // 헤더가 있을 때 Req, Res
                    if (http.IsRequest) 
                    {
                        onRequest(http);
                    }
                    else if (http.IsResponse)
                    {
                        onResponse(http);
                    }
                }
                else
                {

                    httpAssemblyBody(http.ToArray());

                    var chunkSize = isChunk(http.ToArray());
                    if (chunkSize == 0)
                    {
                        ParseChunkBody(chunkBody.ToArray());
                    }
                    else if(chunkBody.Count == ContentLength)
                    {
                        ParseBody(chunkBody.ToArray());
                    }

                    
                }

            }
            Console.WriteLine("{0} : {1} => {2} : {3}", ip.Source, tcp.SourcePort, ip.Destination, tcp.DestinationPort);
            Console.WriteLine($"PayLoad : {tcp.PayloadLength}");
            Console.WriteLine("------------------------------");

        }

    }
}

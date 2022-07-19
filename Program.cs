using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace PcapV2
{
    /// <summary>
    /// Basic capture example
    /// </summary>
    /// 

    public static class Program
    {
        public static void Main(string[] args)
        {
            Console.Title = "AES Decryptor for Lastorigin";
            Console.CursorSize = 11;

            //var aes = new AESDecryptor();
           // Console.WriteLine(aes.AESDecrypt(aes.AESEncrypt("hello")));

            string str = @"{""ErrorCode"":0,""Result"":[{""GateServerVersion"":""1"",""ServerName"":""LIVE01"",""LoginServerIP"":""LiveLoLoginServerALB-1349436694.ap-northeast-2.elb.amazonaws.com"",""LoginServerPort"":""80"",""GateServerIP"":""LiveLoGateServerALB-668344614.ap-northeast-2.elb.amazonaws.com"",""GateServerPort"":""80"",""RankServerIP"":""LiveLoGateServerELB-771478060.ap-northeast-2.elb.amazonaws.com"",""RankServerPort"":""8000"",""ChatServerIP"":""Live-LoGateServerELB-366742435.ap-northeast-2.elb.amazonaws.com"",""ChatServerPort"":""9000"",""OrderedNumber"":""1"",""IsVisible"":""1""}],""WaitingIndex"":0,""WaitingPeopleCount"":0,""FrontAccessToken"":""LoGateFrontAccessToken_63df1dbd29470191ebef3eb9cd95194f_22269062019-10-2200:20:11.785276482+0000UTCm=+337218.828358409"",""Sequence"":3}";
            LOServerList.ParseServer(str);
            PacketSniffer sniffer = new PacketSniffer();

            Console.WriteLine();
            sniffer.Select();
            Console.OutputEncoding = Encoding.Unicode;
           
            sniffer.Run();
        }
    }
}


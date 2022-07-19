using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace PcapV2
{
    public enum ServerType
    {
        List, Login, Gate, Rank, Chat, Battle, Error
    }
    public static class LOServerList
    {
        private static readonly string ListServerIP = "livelolistserveralb-140120882.ap-northeast-2.elb.amazonaws.com"; //불변 리스트 서버
        private static string GateServerVersion { get; set; }
        private static string LoginServerIP { get; set; }
        private static string LoginServerPort { get; set; }
        private static string GateServerIP { get; set; }
        private static string GateServerPort { get; set; }
        private static string RankServerIP { get; set; }
        private static string RankServerPort { get; set; }
        private static string ChatServerIP { get; set; }
        private static string ChatServerPort { get; set; }

        private static readonly Dictionary<ServerType, string[]> ServerIPList = new Dictionary<ServerType, string[]>();
        static LOServerList()
        {
            ServerIPList.Add(ServerType.List, Dns2IPArray(ListServerIP));
        }
        private static string[] Dns2IPArray(string dns)
        {
            try
            {
                return Dns.GetHostAddresses(dns).Select(ip => ip.ToString()).ToArray();
            }
            catch
            {
                return null;
            }
        }
        public static void ParseServer(string body)
        {
            JObject json = JObject.Parse(body);
            JToken error = json["ErrorCode"];

            if ((int)error == 27)
            {
            }
            else if ((int)error == 0)
            {
                JToken token = json["Result"][0];
                GateServerVersion = (string)token["GateServerVersion"];
                LoginServerIP = (string)token["LoginServerIP"];
                LoginServerPort = (string)token["LoginServerPort"];
                GateServerIP = (string)token["GateServerIP"];
                GateServerPort = (string)token["GateServerPort"];
                RankServerIP = (string)token["RankServerIP"];
                RankServerPort = (string)token["RankServerPort"];
                ChatServerIP = (string)token["ChatServerIP"];
                ChatServerPort = (string)token["ChatServerPort"];

                ServerIPList[ServerType.Login] = Dns2IPArray(LoginServerIP);
                ServerIPList[ServerType.Gate] = Dns2IPArray(GateServerIP);
                ServerIPList[ServerType.Rank] = Dns2IPArray(RankServerIP);
                ServerIPList[ServerType.Chat] = Dns2IPArray(ChatServerIP);
            }

        }

        public static void ParserBattleServer(string body)
        {
            JObject json = JObject.Parse(body);
            JToken token = json["ServerIP"];

            ServerIPList[ServerType.Battle] = new string[] { (string)token };
        }
        public static ServerType GetServerType(params string[] ip)
        {
            foreach (KeyValuePair<ServerType, string[]> i in ServerIPList)
            {
                if (i.Value == null)
                {
                    continue;
                }
                //Console.WriteLine(i.Key);
                if (i.Value.Any((str) => ip.Any((arr) => str.Equals(arr))))
                {
                    return i.Key;
                }
            }
            return ServerType.Error;
        }

    }
}

using System;
using System.Xml;

namespace PacketGenerator
{
    internal class Program
    {
        // 누적 string concatenation 패턴 — 빈 문자열로 초기화 (nullable 가드).
        static string genPackets = "";
        static ushort packetID;
        static string packetEnums = "";

        static string serverRegister = "";
        static string clientRegister = "";

        static void Main(string[] args)
        {
            string PDL_PATH = "../PDL.xml";

            XmlReaderSettings settings = new XmlReaderSettings()
            {
                IgnoreComments = true,
                IgnoreWhitespace = true
            };

            if (args.Length >= 1)
            {
                PDL_PATH = args[0];
            }

            using (XmlReader r = XmlReader.Create(PDL_PATH, settings))
            {
                r.MoveToContent();

                while (r.Read())
                {
                    if (r.Depth == 1 && r.NodeType == XmlNodeType.Element)
                        ParsePacket(r);
                    //Console.WriteLine(r.Name + " " + r["name"]);
                }

                string fileText = string.Format(PacketFormat.fileFormat, packetEnums, genPackets);
                string clientManagerText = string.Format(PacketFormat.managerFormat, clientRegister);
                string serverManagerText = string.Format(PacketFormat.managerFormat, serverRegister);
                try
                {
                    File.WriteAllText("GenPackets.cs", fileText);
                    File.WriteAllText("ClientPacketManager.cs", clientManagerText);
                    File.WriteAllText("ServerPacketManager.cs", serverManagerText);

                    Console.WriteLine("[GEN] - Packet Generate Success!!\n");
                    Console.WriteLine("1. - DummyClient/Packet/GenPackets.cs\n");
                    Console.WriteLine("2. - Server/Packet/GenPackets.cs\n");
                    Console.WriteLine("3. - DummyClient/Packet/ClientPacketManager.cs\n");
                    Console.WriteLine("4. - Server/Packet/ServerPacketManager.cs\n\n");
                    Console.WriteLine("Press AnyKey to quit...");
                    Console.ReadKey(true);
                }
                catch (Exception ex) 
                {
                    Console.WriteLine("[GEN] - Packet Generate FAIL!!\n");
                    Console.WriteLine(ex.Message);
                    Console.ReadKey(true);
                }
            }
        }


        public static void ParsePacket(XmlReader _r)
        {
            if (_r.NodeType == XmlNodeType.EndElement)
                return;

            if (_r.Name.ToLower() != "packet")
            {
                Console.WriteLine("[Gen] - Invalid packet node.");
                return;
            }
            string? packetName = _r["name"];

            if (string.IsNullOrEmpty(packetName))
            {
                Console.WriteLine("[Gen] - Packet name is missing.");
                return;
            }

            Tuple<string, string, string> t = ParseMembers(_r);
            genPackets += string.Format(PacketFormat.packetFormat,
                packetName, t.Item1, t.Item2, t.Item3);
            packetEnums += string.Format(PacketFormat.packetEnumFormat, packetName, ++packetID) + Environment.NewLine + "\t";

            // 패킷 이름이 S_ 또는 s_로 시작하면 클라이언트에서 받는 패킷으로 간주하고,
            // 그렇지 않으면 서버에서 받는 패킷으로 간주
            if (packetName.StartsWith("S_") || packetName.StartsWith("s_"))
                clientRegister += string.Format(PacketFormat.mangerRegisterFormat, packetName) + Environment.NewLine;
            else
                serverRegister += string.Format(PacketFormat.mangerRegisterFormat, packetName) + Environment.NewLine;
        }

        // {1} : 패킷 멤버 변수들
        // {2} : 멤버 변수 Read
        // {3} : 멤버 변수 Write
        public static Tuple<string, string, string> ParseMembers(XmlReader _r)
        {
            string? packetName = _r["name"];

            string memberCode = "";
            string readCode = "";
            string writeCode = "";

            int depth = _r.Depth + 1;
            while (_r.Read())
            {
                if (_r.Depth != depth)
                    break;

                string? memberName = _r["name"];
                if (string.IsNullOrEmpty(memberName))
                {
                    // 잘못된 PDL → 즉시 throw. null 반환은 호출자에 nullable 부담.
                    throw new InvalidDataException($"[Gen] Member name is missing in packet {packetName}.");
                }

                if (string.IsNullOrEmpty(memberCode) == false)
                    memberCode += Environment.NewLine;
                if (string.IsNullOrEmpty(memberCode) == false)
                    readCode += Environment.NewLine;
                if (string.IsNullOrEmpty(memberCode) == false)
                    writeCode += Environment.NewLine;

                string memberType = _r.Name.ToLower();
                switch (memberType)
                {
                    case "byte":
                    case "sbyte":
                        memberCode += string.Format(PacketFormat.MemberFormat, memberType, memberName);
                        readCode += string.Format(PacketFormat.ReadByteFormat, memberName, memberType);
                        writeCode += string.Format(PacketFormat.WriteByteFormat, memberName, memberType);
                        break;
                    case "bool":
                    case "short":
                    case "ushort":
                    case "int":
                    case "long":
                    case "float":
                    case "double":
                        memberCode += string.Format(PacketFormat.MemberFormat, memberType, memberName);
                        readCode += string.Format(PacketFormat.ReadFormat, memberName, ToMemberType(memberType), memberType);
                        writeCode += string.Format(PacketFormat.WriteFormat, memberName, memberType);
                        break;
                    case "string":
                        memberCode += string.Format(PacketFormat.MemberFormat, memberType, memberName);
                        readCode += string.Format(PacketFormat.ReadStringFormat, memberName);
                        writeCode += string.Format(PacketFormat.WriteStringFormat, memberName);
                        break;
                    case "list":
                        Tuple<string, string, string> t = ParseList(_r);
                        memberCode += t.Item1;
                        readCode += t.Item2;
                        writeCode += t.Item3;
                        break;
                    default:
                        break;
                }
            }

            memberCode = memberCode.Replace("\n", "\n\t");
            readCode = readCode.Replace("\n", "\n\t\t");
            writeCode = writeCode.Replace("\n", "\n\t\t");
            return new Tuple<string, string, string>(memberCode, readCode, writeCode);
        }

        public static Tuple<string, string, string> ParseList(XmlReader _r)
        {
            string? listName = _r["name"];

            if (string.IsNullOrEmpty(listName))
            {
                throw new InvalidDataException("[GEN] List without name");
            }

            Tuple<string, string, string> t = ParseMembers(_r);

            string memberCode = string.Format(PacketFormat.MemberListFormat,
                FirstCharToUpper(listName),
                FirstCharToLower(listName),
                t.Item1,
                t.Item2,
                t.Item3);

            string readCode = string.Format(PacketFormat.ReadListFormat,
                FirstCharToUpper(listName),
                FirstCharToLower(listName));

            string writeCode = string.Format(PacketFormat.WriteListFormat,
                FirstCharToUpper(listName),
                FirstCharToLower(listName));

            return new Tuple<string, string, string>(memberCode, readCode, writeCode);
        }

        public static string ToMemberType(string _memberType)
        {
            switch (_memberType.ToLower())
            {
                case "bool":
                    return "ToBoolean";
                case "short":
                    return "ToInt16";
                case "ushort":
                    return "ToUInt16";
                case "int":
                    return "ToInt32";
                case "long":
                    return "ToInt64";
                case "float":
                    return "ToSingle";
                case "double":
                    return "ToDouble";
                default:
                    return "";
            }
        }

        public static string FirstCharToUpper(string _s)
        {
            if (string.IsNullOrEmpty(_s))
                return "";

            return _s[0].ToString().ToUpper() + _s.Substring(1);
        }

        public static string FirstCharToLower(string _s)
        {
            if (string.IsNullOrEmpty(_s))
                return "";

            return _s[0].ToString().ToLower() + _s.Substring(1);
        }
    }
}

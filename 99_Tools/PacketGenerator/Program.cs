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
            // 인자 파싱: 첫 비-옵션 인자 = PDL 경로. 옵션: --no-manager (manager 출력 skip),
            // --no-wait (CI/스크립트용, ReadKey 대기 안 함).
            string pdlPath = "PDL.xml";
            bool noManager = false;
            bool noWait = false;
            foreach (string a in args)
            {
                if (a == "--no-manager") noManager = true;
                else if (a == "--no-wait") noWait = true;
                else if (!a.StartsWith("--")) pdlPath = a;
            }

            XmlReaderSettings settings = new XmlReaderSettings()
            {
                IgnoreComments = true,
                IgnoreWhitespace = true
            };

            using (XmlReader r = XmlReader.Create(pdlPath, settings))
            {
                r.MoveToContent();

                while (r.Read())
                {
                    if (r.Depth == 1 && r.NodeType == XmlNodeType.Element)
                        ParsePacket(r);
                }
            }

            string fileText = string.Format(PacketFormat.fileFormat, packetEnums, genPackets);

            // 출력 디렉토리 = PDL.xml 위치 기준 ../../ (= 프로젝트 루트).
            // Phase 07 책임 단위 분리 채택:
            //   - GenPackets.cs (패킷 정의) → 98_Shared/Protocol/Generated/  (양쪽 통합)
            //   - ServerPacketManager.cs   → 02_Server/GameServer/Network/Generated/  (서버 분리)
            //   - ClientPacketManager.cs   → 04_ClientNet/Generated/  (클라 분리)
            string pdlAbs = Path.GetFullPath(pdlPath);
            string pdlDir = Path.GetDirectoryName(pdlAbs)!;
            string projectRoot = Path.GetFullPath(Path.Combine(pdlDir, "..", ".."));

            try
            {
                string genPacketsDir = Path.Combine(projectRoot, "98_Shared", "Protocol", "Generated");
                Directory.CreateDirectory(genPacketsDir);
                File.WriteAllText(Path.Combine(genPacketsDir, "GenPackets.cs"), fileText);
                Console.WriteLine($"[GEN] GenPackets.cs → 98_Shared/Protocol/Generated/");

                if (!noManager)
                {
                    string serverManagerText = string.Format(PacketFormat.managerFormat, serverRegister);
                    string clientManagerText = string.Format(PacketFormat.managerFormat, clientRegister);

                    string serverDir = Path.Combine(projectRoot, "02_Server", "GameServer", "Network", "Generated");
                    string clientDir = Path.Combine(projectRoot, "04_ClientNet", "Generated");
                    Directory.CreateDirectory(serverDir);
                    Directory.CreateDirectory(clientDir);
                    File.WriteAllText(Path.Combine(serverDir, "ServerPacketManager.cs"), serverManagerText);
                    File.WriteAllText(Path.Combine(clientDir, "ClientPacketManager.cs"), clientManagerText);
                    Console.WriteLine($"[GEN] ServerPacketManager.cs → 02_Server/GameServer/Network/Generated/");
                    Console.WriteLine($"[GEN] ClientPacketManager.cs → 04_ClientNet/Generated/");
                }
                else
                {
                    Console.WriteLine("[GEN] --no-manager: PacketManager 출력 skip (Phase 08+에서 manager 도입 예정)");
                }

                Console.WriteLine("[GEN] Packet Generate Success.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("[GEN] Packet Generate FAIL!!");
                Console.WriteLine(ex.Message);
            }

            if (!noWait)
            {
                Console.WriteLine("Press AnyKey to quit...");
                Console.ReadKey(true);
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
                        memberCode += string.Format(PacketFormat.MemberFormat, memberType, memberName);
                        // ReadFormat/WriteFormat 둘 다 {0}변수명 / {1}BinaryPrimitives 메서드 / {2}sizeof 형식
                        readCode += string.Format(PacketFormat.ReadFormat, memberName, ToMemberType(memberType), memberType);
                        writeCode += string.Format(PacketFormat.WriteFormat, memberName, ToMemberType(memberType), memberType);
                        break;
                    case "float":
                    case "double":
                        // .NET Standard 2.1 호환 — BitConverter.Int32BitsToSingle 경유 (PacketFormat 주석 참조).
                        // 현재 float만 지원. double은 추후 ReadDoubleFormat 추가 시 분기.
                        memberCode += string.Format(PacketFormat.MemberFormat, memberType, memberName);
                        readCode += string.Format(PacketFormat.ReadFloatFormat, memberName);
                        writeCode += string.Format(PacketFormat.WriteFloatFormat, memberName);
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

        // BinaryPrimitives.Read*LittleEndian / TryWrite*LittleEndian 의 * 부분 반환.
        // 예: long → "Int64" → BinaryPrimitives.ReadInt64LittleEndian / TryWriteInt64LittleEndian
        // bool은 BinaryPrimitives에 LittleEndian 변종이 없음 (1byte라 endian 무관).
        // 본 Phase(07) 시점엔 Ping/Pong에 bool 없어 미사용. 미래 bool 사용 시 ReadByteFormat 패턴으로 별도 처리.
        public static string ToMemberType(string _memberType)
        {
            switch (_memberType.ToLower())
            {
                case "bool":
                    return "Boolean";    // ⚠️ BinaryPrimitives에 ReadBooleanLittleEndian 없음 — 미래 정정 필요
                case "short":
                    return "Int16";
                case "ushort":
                    return "UInt16";
                case "int":
                    return "Int32";
                case "long":
                    return "Int64";
                case "float":
                    return "Single";
                case "double":
                    return "Double";
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

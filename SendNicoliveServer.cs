using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.IO;
using System.Net;
using System.Net.Sockets;

namespace SendNicolive
{
    class SendNicolive
    {
        static CookieContainer ccNico;
        static string ckfile = Path.Combine(UserDirectory(), ".cookie.dat");

        static void Main(string[] args)
        {
            if (args.Length == 2 && args[1] == "--continue")
                settingCookie("", "");
            else if (args.Length == 3 && args[1] == "--cookie")
                confirmAndSetCookie(args[2]);
            else if (args.Length == 4 && args[1] == "--login")
                settingCookie(args[2], args[3]);
            else
            {
                Console.WriteLine("usage: SendNicolive <prefix> <option>\n");
                Console.WriteLine("prefix, for example: http://localhost:8000/");
                Console.WriteLine("options are:");
                Console.WriteLine("\t--continue");
                Console.WriteLine("\t--login <email-address> <password>");
                Console.WriteLine("\t--cookie <cookie>");
                return;
            }
            if (ccNico == null)
                return;

            VimWebServer.Start(args[0], ccNico);
        }

        static void confirmAndSetCookie(string str)
        {
            var container = new CookieContainer();
            var cookie = new Cookie("user_session", str, "/", ".nicovideo.jp");
            container.Add(cookie);
            if (NicoliveAPI.IsLogin(container))
            {
                ccNico = container;
                writeCookie(ckfile, ccNico);
            }
            else
                Console.WriteLine("Cookie is invalid.");
        }

        static void settingCookie(string mail, string password)
        {
            try
            {
                if (File.Exists(ckfile))
                    ccNico = readCookie(ckfile);
                else
                    Console.WriteLine("Can not found '.cookie.dat'");
                if (!NicoliveAPI.IsLogin(ccNico))
                {
                    Console.WriteLine("Try to login...");
                    if (mail == "" || password == "")
                    {
                        Console.WriteLine("no email address or no password");
                        ccNico = null;
                        return;
                    }
                    ccNico = nicoLogin(mail, password, ckfile);
                    if (!NicoliveAPI.IsLogin(ccNico))
                    {
                        Console.WriteLine("Can not login to niconico.");
                        ccNico = null;
                        return;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public static string UserDirectory()
        {
            return (Environment.OSVersion.Platform == PlatformID.Unix || Environment.OSVersion.Platform == PlatformID.MacOSX)
                ? Environment.GetEnvironmentVariable("HOME")
                : Environment.ExpandEnvironmentVariables("%HOMEDRIVE%%HOMEPATH%");
        }

        static CookieContainer nicoLogin(string mail, string password, string file)
        {
            var account = new string[] { mail, password };
            var cc = NicoliveAPI.Login(account);
            writeCookie(file, cc);
            return cc;
        }

        static CookieContainer readCookie(string file)
        {
            var container = new CookieContainer();
            var data = File.ReadAllText(file).Trim();
            var cookie = new Cookie("user_session", data, "/", ".nicovideo.jp");
            container.Add(cookie);
            return container;
        }

        static void writeCookie(string file, CookieContainer cc)
        {
            var uri = new Uri("http://live.nicovideo.jp");
            string data = null;
            foreach (Cookie cookie in cc.GetCookies(uri))
            {
                if (cookie.Name == "user_session")
                    data = cookie.Value;
            }
            File.WriteAllText(file, data);
        }
    }

    class CommentClient
    {
        int cntBlock = 0;
        string liveID;
        string addr;
        string port;
        string thread;
        string base_time;
        string userID;
        string token;
        string postkey;
        CookieContainer ccNico;
        CommentSocket csock;

        public CommentClient(string lvID, CookieContainer cc)
        {
            liveID = lvID;
            ccNico = cc;
            var info = NicoliveAPI.GetPlayerStatus(liveID, cc);
            addr = info["addr"];
            port = info["port"];
            thread = info["thread"];
            base_time = info["base_time"];
            userID = info["user_id"];
            csock = new CommentSocket(addr, port, thread);
            token = NicoliveAPI.GetToken(lvID, cc);
        }

        public void Dispose()
        {
            csock.Disconnect();
        }

        public void Send(string comment, bool anonym)
        {
            if (comment == "")
                return;

            if (token == "")
                listenerComment(comment, anonym);
            else
                publisherComment(comment);
        }

        void listenerComment(string comment, bool anonym)
        {
            csock.Send("");
            var count = csock.Receive();
            var curCount = int.Parse(count);
            if (postkey == null || curCount - cntBlock * 100 > 100)
            {
                postkey = NicoliveAPI.GetPostKey(count, thread, ccNico);
                cntBlock = curCount / 100;
            }
            if (postkey == "")
            {
                Console.WriteLine("Can not get postkey.");
                return;
            }

            var anonymous = "";
            if (anonym)
            {
                anonymous = "mail=\"184\"";
            }
            
            Int64 serverTimeSpan = Int64.Parse(csock.SrvTime) - Int64.Parse(base_time);
            Int64 localTimeSpan = toUnixTime(DateTime.Now) - toUnixTime(csock.DateTimeStart);
            string vpos = ((serverTimeSpan + localTimeSpan) * 100).ToString();

            string param = String.Format("<chat thread=\"{0}\" ticket=\"{1}\" vpos=\"{2}\" postkey=\"{3}\" user_id=\"{4}\" premium=\"1\" {5}>{6}</chat>\0"
                , thread
                , csock.Ticket
                , vpos
                , postkey
                , userID
                , anonymous
                , comment);
            csock.Send(param);
        }

        static Int64 toUnixTime(DateTime targetTime)
        {
            TimeSpan elapsedTime = targetTime.ToUniversalTime() - new DateTime(1970, 1, 1, 0, 0, 0, 0);
            return (Int64)elapsedTime.TotalSeconds;
        }

        async void publisherComment(string comment)
        {
            var send = "http://watch.live.nicovideo.jp/api/broadcast/lv" + liveID;
            var body = Uri.EscapeDataString(WebUtility.HtmlEncode(comment));
            var sendurl = send + "?body=" + body + "&token=" + token;
            var req = (HttpWebRequest)WebRequest.Create(sendurl);
            req.CookieContainer = ccNico;
            var res = await req.GetResponseAsync();
            Console.WriteLine(new StreamReader(res.GetResponseStream()).ReadLine());
        }
    }

    static class NicoliveAPI
    {

        public static CookieContainer Login(string[] account)
        {
            var url = "https://secure.nicovideo.jp/secure/login";
            var cc = new CookieContainer();
            var param = new Dictionary<string, string>();
            param.Add("mail", account[0]);
            param.Add("password", account[1]);

            post(url, ref cc, param);
            return cc;
        }

        public static bool IsLogin(CookieContainer cc)
        {
            var url = "http://live.nicovideo.jp/notifybox";
            var notifybox = get(url, ref cc);
            var ret = notifybox.Length > 124;

            Console.WriteLine("Login: " + ret);
            return ret;
        }

        public static Dictionary<string, string> GetPlayerStatus(string liveID, CookieContainer cc)
        {
            var url = "http://live.nicovideo.jp/api/getplayerstatus?v=" + liveID;
            var xdoc = XDocument.Parse(get(url, ref cc));
            var ret = new Dictionary<string, string>();

            var status = xdoc.Element("getplayerstatus").Attribute("status").Value;
            if (status != "ok")
            {
                var code = xdoc.Descendants("code").Single().Value;
                throw new Exception("PlayerStatus is error on: " + code);
            }
            var ms = xdoc.Descendants("ms").Single();
            ret.Add("addr", ms.Element("addr").Value);
            ret.Add("port", ms.Element("port").Value);
            ret.Add("thread", ms.Element("thread").Value);
            ret.Add("base_time", xdoc.Descendants("base_time").Single().Value);
            ret.Add("user_id", xdoc.Descendants("user_id").Single().Value);
            ret.Add("comnID", xdoc.Descendants("default_community").Single().Value);
            ret.Add("room_label", xdoc.Descendants("room_label").Single().Value);
            return ret;
        }

        public static string GetToken(string liveID, CookieContainer cc)
        {
            try
            {
                var url = "http://live.nicovideo.jp/api/getpublishstatus?v=lv" + liveID;
                var xdoc = XDocument.Parse(get(url, ref cc));

                if (xdoc.Descendants("getpublishstatus").Single().Attribute("status").Value == "ok")
                    return xdoc.Descendants("token").Single().Value;
                else
                    return "";
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return "";
            }
        }

        public static string GetPostKey(string count, string thread, CookieContainer cc)
        {
            try
            {
                UInt32 block_no = UInt32.Parse(count) / 100;
                var url = String.Format("http://live.nicovideo.jp/api/getpostkey?thread={0}&block_no={1}",
                                thread, block_no);

                return get(url, ref cc).Replace("postkey=", "");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return "";
            }
        }

        static string get(string url, ref CookieContainer cc)
        {
            var req = (HttpWebRequest)WebRequest.Create(url);
            req.CookieContainer = cc;

            string ret;
            using (var res = req.GetResponse())
            using (var resStream = res.GetResponseStream())
            using (var sr = new StreamReader(resStream, Encoding.GetEncoding("UTF-8")))
            {
                ret = sr.ReadToEnd();
            }
            return ret;
        }

        static string post(string url, ref CookieContainer cc, Dictionary<string, string> param)
        {
            var postData = string.Join("&",
                param.Select(x => x.Key + "=" + System.Uri.EscapeUriString(x.Value)).ToArray());

            byte[] postDataBytes = Encoding.ASCII.GetBytes(postData);

            var req = (HttpWebRequest)WebRequest.Create(url);
            req.CookieContainer = cc;
            req.Method = "POST";
            req.ContentType = "application/x-www-form-urlencoded";
            req.ContentLength = postDataBytes.Length;

            using (var reqStream = req.GetRequestStream())
            {
                reqStream.Write(postDataBytes, 0, postDataBytes.Length);
            };

            string ret;
            using (var res = req.GetResponse())
            using (var resStream = res.GetResponseStream())
            using (var sr = new StreamReader(resStream, Encoding.GetEncoding("UTF-8")))
            {
                ret = sr.ReadToEnd();
            }
            return ret;
        }
    }

    class CommentSocket
    {
        Socket sock;
        public string Thread  { get; private set; }
        public string Ticket  { get; private set; }
        public string SrvTime { get; private set; }
        public DateTime DateTimeStart { get; private set; }

        public CommentSocket(string addr, string port, string thread)
        {
            try
            {
                Thread = thread;

                var hostaddr = Dns.GetHostEntry(addr).AddressList[0];
                var ephost = new IPEndPoint(hostaddr, int.Parse(port));
                sock = new Socket(AddressFamily.InterNetwork,
                                  SocketType.Stream, ProtocolType.Tcp);

                sock.Connect(ephost);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public void Disconnect()
        {
            if (sock != null)
                sock.Disconnect(false);
        }
        
        public string Receive()
        {
            try
            {
                var bufsize = 1024;
                var buf = new byte[bufsize];
                var res = "";
                while (res.Length == 0)
                {
                    sock.Receive(buf);
                    res = Encoding.UTF8.GetString(buf).Trim('\0');
                }
                var th = XElement.Parse(res.Split('\0')[0]);			
                Ticket = th.Attribute("ticket").Value;
                SrvTime = th.Attribute("server_time").Value;
                var no = th.Attribute("last_res");
                DateTimeStart = DateTime.Now;

                if (no != null)
                    return no.Value;
                else
                    return "0";
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return "-1";
            }
        }


        public void Send(string data)
        {
            if (data == "")
                data = String.Format("<thread thread=\"{0}\" version=\"20061206\" res_from=\"-1\"/>\0", Thread);

            var byteData = Encoding.UTF8.GetBytes(data);
            sock.Send(byteData, 0, byteData.Length, 0);
        }

    }

    class VimWebServer
    {
        static HttpListener listener;
        static CommentClient cli;
        static string noAnonymaousFile = Path.Combine(".nicolive_no_anonymous", SendNicolive.UserDirectory());
        static bool IsAnonymous = !File.Exists(noAnonymaousFile);

        public static void Start(string prefix, CookieContainer cc)
        {
            // var prefix = "http://localhost:8000/";
            listener = new HttpListener();
            listener.Prefixes.Add(prefix);
            listener.Start();
            try
            {
                while (true)
                {
                    var ctx = listener.GetContext();
                    var qry = ctx.Request.RawUrl.Substring(1).Split('?');
                    var res = ctx.Response;

                    if (qry[0] == "connect")
                    {
                        if (cli != null)
                            cli.Dispose();
                        cli = new CommentClient(qry[1], cc);
                        Console.WriteLine("Connect: " + qry[1]);
                    }
                    else if (qry[0] == "set")
                    {
                        if (qry[1] == "anonymous")
                        {
                            IsAnonymous = true;
                            Console.WriteLine("Setting is anonymous.");
                        }
                        if (qry[1] == "noanonymous")
                        {
                            IsAnonymous = false;
                            Console.WriteLine("Setting is not anonymous.");
                        }
                        if (qry[1] == "isanonymous")
                        {
                            Console.WriteLine("Anonymous: " + IsAnonymous);
                        }
                    }
                    else if (qry[0] == "send")
                    {
                        if (cli == null)
                            Console.WriteLine("no connect");
                        else
                            cli.Send(Uri.UnescapeDataString(qry[1]), IsAnonymous);
                    }
                    var result = Encoding.UTF8.GetBytes("ok");
                    res.OutputStream.Write(result, 0, result.Length);
                    res.Close();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public static void Stop()
        {
            listener.Abort();
        }
    }

}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using Test.VoiceClient;

namespace LiveJoinVoiceTest.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EventsController : ControllerBase
    {
        private static readonly string Prefix;
        private static int NextId = 0;
        private static readonly Dictionary<int, WebSocket> Sockets = new Dictionary<int, WebSocket>();
        private static readonly Dictionary<int, Guid> CallIds = new Dictionary<int, Guid>();
        private static string Directory;
        private static readonly object FileLock = new object();
        private static string VoiceUrl;
        private static string MyUrlBase;

        internal static void Configure(string directory, string voiceUrl, string myUrl)
        {
            Directory = directory;
            VoiceUrl = voiceUrl;
            MyUrlBase = myUrl;
        }

        [HttpGet("start/{id}")]
        public async Task Start(int id, int accountId, string accountName, string accountPhone, int agentId, string agentFirstName, string agentLastName, string agentPhone, bool? agentEnter, bool? agentLeave, bool? agentUnmute, bool? agentMute, string visitorFirstName, string visitorLastName, string visitorPhone, bool? visitorEnter, bool? visitorLeave, bool? visitorUnmute, bool? visitorMute)
        {
            var client = new swaggerClient(VoiceUrl, new HttpClient());
            var agentAnnouncements = new List<AnnouncementType>();
            if (agentEnter.HasValue && agentEnter.Value)
            {
                agentAnnouncements.Add(AnnouncementType.Join);
            }
            if (agentLeave.HasValue && agentLeave.Value)
            {
                agentAnnouncements.Add(AnnouncementType.Leave);
            }
            if (agentMute.HasValue && agentMute.Value)
            {
                agentAnnouncements.Add(AnnouncementType.Mute);
            }
            if (agentUnmute.HasValue && agentUnmute.Value)
            {
                agentAnnouncements.Add(AnnouncementType.Unmute);
            }
            var visitorAnnouncements = new List<AnnouncementType>();
            if (visitorEnter.HasValue && visitorEnter.Value)
            {
                visitorAnnouncements.Add(AnnouncementType.Join);
            }
            if (visitorLeave.HasValue && visitorLeave.Value)
            {
                visitorAnnouncements.Add(AnnouncementType.Leave);
            }
            if (visitorMute.HasValue && visitorMute.Value)
            {
                visitorAnnouncements.Add(AnnouncementType.Mute);
            }
            if (visitorUnmute.HasValue && visitorUnmute.Value)
            {
                visitorAnnouncements.Add(AnnouncementType.Unmute);
            }

            try
            {
                await client.CallsAsync(new NewVoiceCall
                {
                    Account = new Account
                    {
                        Id = accountId,
                        Name = accountName,
                        Phone = accountPhone
                    },
                    Agent = new Agent
                    {
                        FirstName = agentFirstName,
                        Id = agentId,
                        LastName = agentLastName,
                        Phone = agentPhone,
                        Announcements = agentAnnouncements
                    },
                    Callbacks = new CallbacksRecord
                    {
                        Types = new CallbackType[]
                        {
                            CallbackType.CallEnd,
                            CallbackType.RecordingAvailable,
                            CallbackType.CallerEnter,
                            CallbackType.CallerHangup,
                            CallbackType.CallerMute,
                            CallbackType.CallerUnmute
                        },
                        Url = $"{MyUrlBase}{id}"
/*                        AgentAnswer = true,
                        AgentHangup = true,
                        CallEnd = true,
                        CallRecordingAvailable = true,
                        JoinerEnter = true,
                        JoinerHangup = true,
                        JoinerPickup = true,
                        JoinerUnmute = true,
                        Url = $"MyUrlBase{id}",
                        VisitorHangup = true,
                        VisitorAnswer = true
*/                    },
                    System = "test",
                    SystemId = $"{Prefix}{id}",
                    Visitor = new Visitor
                    {
                        FirstName = visitorFirstName,
                        LastName = visitorLastName,
                        Phone = visitorPhone,
                        Announcements = visitorAnnouncements
                    }
                });

                var callId = Guid.NewGuid();
                _ = Log(id, "info", $"Call started for account ({accountId}/{accountName}/{accountPhone}) between agent ({agentId}/{agentFirstName} {agentLastName}/{agentPhone}) and visitor ({visitorFirstName} {visitorLastName}/{visitorPhone}) => {callId}");
                CallIds[id] = callId;
            }
            catch (Exception e)
            {
                _ = Log(id, "error", $"Call failed for account {accountId} between agent {agentPhone} and visitor {visitorPhone} due to error {e.GetType().Name}: {e.Message}");
                throw;
            }
        }

        [HttpGet("join/{id}")]
        public async Task AddJoiner(int id, int joinerId, string joinerName, string joinerPhone, bool joinerPositiveStart, bool joinerUnmute, bool? joinerAnnounceEnter, bool? joinerAnnounceLeave, bool? joinerAnnounceMute, bool? joinerAnnounceUnmute)
        {
            var client = new swaggerClient(VoiceUrl, new HttpClient());
            var joinerAnnouncements = new List<AnnouncementType>();
            if (joinerAnnounceEnter.HasValue && joinerAnnounceEnter.Value)
            {
                joinerAnnouncements.Add(AnnouncementType.Join);
            }
            if (joinerAnnounceLeave.HasValue && joinerAnnounceLeave.Value)
            {
                joinerAnnouncements.Add(AnnouncementType.Leave);
            }
            if (joinerAnnounceMute.HasValue && joinerAnnounceMute.Value)
            {
                joinerAnnouncements.Add(AnnouncementType.Mute);
            }
            if (joinerAnnounceUnmute.HasValue && joinerAnnounceUnmute.Value)
            {
                joinerAnnouncements.Add(AnnouncementType.Unmute);
            }
            try
            {
                await client.Joiners2Async($"{Prefix}{id}", new NewJoiner
                {
                    Name = joinerName,
                    Phone = joinerPhone, 
                    RequirePositiveStart = joinerPositiveStart,
                    RequireUnmute = joinerUnmute, 
                    SystemId = joinerId.ToString(),
                    Announcements = joinerAnnouncements
                });
                _ = Log(id, "info", $"Joiner ({joinerId}/{joinerName}/{joinerPhone}) added to call");
            }
            catch (Exception e)
            {
                _ = Log(id, "error", $"Failed to add joiner {joinerPhone} to call due to error {e.GetType().Name}: {e.Message}");
                throw;
            }
        }

        private static async Task Log(int id, string type, string text)
        {
            lock (FileLock)
            {
                System.IO.File.AppendAllText($"{Directory}{Prefix}{id}.log", $"{DateTime.UtcNow.ToString("MM/dd/yyyy HH:mm:ss.fff")} : {text}{Environment.NewLine}");
            }
            if (Sockets.ContainsKey(id))
            {
                try
                {
                    var evt = new JObject(
                        new JProperty("type", type),
                        new JProperty("data", text)
                    );
                    await Sockets[id].SendAsync(Encoding.UTF8.GetBytes(evt.ToString()), WebSocketMessageType.Text, true, CancellationToken.None);
                }
                catch
                {
                    _ = Sockets[id].CloseAsync(WebSocketCloseStatus.NormalClosure, "TX Failure", CancellationToken.None);
                    Sockets.Remove(id);
                }
            }
        }

        private static Random _rand = new Random();
        static EventsController()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            var s = new char[4];
            for (var i = 0; i < s.Length; ++i)
            {
                s[i] = chars[_rand.Next(chars.Length)];
            }
            Prefix = new string(s);
        }

        public static int GetNextId()
        {
            return _rand.Next(int.MaxValue);
        }

        [HttpPost("{id}")]
        public async void PostCallback(int id)
        {
            using (var reader = new StreamReader(Request.Body))
            {
                var jsonString = reader.ReadToEnd();
                lock (FileLock){
                    System.IO.File.AppendAllText($"{Directory}{Prefix}{id}.log", $"{DateTime.UtcNow.ToShortDateString()} {DateTime.UtcNow.ToShortTimeString()} : {jsonString}{Environment.NewLine}");
                }

                if (Sockets.ContainsKey(id))
                {
                    try
                    {
                        var json = JObject.Parse(jsonString);

                        //TODO: do stuff based on the json object
                        var rxEvent = new JObject(
                            new JProperty("type", "rx"),
                            new JProperty("data", jsonString)
                        );
                        await Sockets[id].SendAsync(Encoding.UTF8.GetBytes(rxEvent.ToString()), WebSocketMessageType.Text, true, CancellationToken.None);
                    }
                    catch
                    {
                        _ = Sockets[id].CloseAsync(WebSocketCloseStatus.NormalClosure, "TX Failure", CancellationToken.None);
                        Sockets.Remove(id);
                    }
                }
            }
        }

        [HttpGet("{id}")]
        public async Task Index(int id)
        {
            if (HttpContext.WebSockets.IsWebSocketRequest)
            {
                var socket = await HttpContext.WebSockets.AcceptWebSocketAsync();
                Sockets[id] = socket;
                while (true)
                {
                    try
                    {
                        await socket.ReceiveAsync(new byte[1024], CancellationToken.None);
                    }
                    catch
                    {
                        if (Sockets[id] == socket)
                        {
                            Sockets.Remove(id);
                        }
                        return;
                    }
                }
            }
        }
    }
}
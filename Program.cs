#region Imports
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TL;
using WTelegram;
#endregion


#region Classes


public class ConfigDatas
{
    public string api_id { get; set; }
    public string api_hash { get; set; }
    public string phone_number { get; set; }
    public string CrossTheLimitChannelId { get; set; }
    public string[] admin_ids { get; set; }

    public static ConfigDatas LoadConfig(string filePath)
    {
        string jsonText = File.ReadAllText(filePath);
        return JsonConvert.DeserializeObject<ConfigDatas>(jsonText) ?? new ConfigDatas();
    }
}
public class tempCount
{
    public string channel_id { get; set; }
    public int count { get; set; }
}
public class Count
{
    public string channel_id { get; set; }
    public int count { get; set; }
    public int percentage { get; set; }
    public List<int> lines { get; set; }

    public static void SaveAllToJson(List<Count> counts, string filePath)
    {
        try
        {
            string json = JsonConvert.SerializeObject(counts, Formatting.Indented);
            File.WriteAllText(filePath, json);
            Console.WriteLine($"counts saved to {filePath} successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving counts to {filePath}: {ex.Message}");
        }
    }
}

public class MessageData
{
    public string message { get; set; }
    public int count { get; set; }
    public ulong id { get; set; }
    public bool isCroosTheLimit { get; set; }

}

public class AllMessageData
{
    public string channel_id { get; set; }

    public List<MessageData> messages { get; set; }

    public AllMessageData()
    {
        messages = new List<MessageData>(); // Initialize the messages property
    }

    public static void SaveAllToJson(List<AllMessageData> allMessages, string filePath)
    {
        try
        {
            string json = JsonConvert.SerializeObject(allMessages, Formatting.Indented);
            File.WriteAllText(filePath, json);
            Console.WriteLine($"allMessageData saved to {filePath} successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving counts to {filePath}: {ex.Message}");
        }
    }


}

#endregion


namespace UserTelegramBot
{

    class Program
    {
        public static long ForwardTo;

        #region Variables

        public static Client client;

        public static List<Count> counts = LoadCountsFromJson(Environment.CurrentDirectory + "/counts.json");

        public static List<tempCount> tempCts = TempLoadCountsFromJson(Environment.CurrentDirectory + "/counts.json");


        public static List<AllMessageData> allMessages = LoadAllMessageDataFromJson(Environment.CurrentDirectory + "/all_messages.json");
        public static ConfigDatas config = ConfigDatas.LoadConfig(Environment.CurrentDirectory + "/config.json");
        public static long crossTheLimitChannelId = long.Parse(config.CrossTheLimitChannelId);

        static readonly Dictionary<long, User> Users = new Dictionary<long, User>();
        static readonly Dictionary<long, ChatBase> Chats = new Dictionary<long, ChatBase>();


        #endregion


        #region Main
        static async Task Main(string[] args)
        {

            if (counts == null)
                tempCts.ForEach(c =>
                {
                    Console.WriteLine("sa");
                    counts.Add(new Count { channel_id = c.channel_id, count = c.count, percentage = 100, lines = new List<int>() });
                });
            counts.FindAll(c => c.lines.Count == 0).ForEach(x => x.lines.Add(0));
            Console.WriteLine("benim count" + counts.Count());
            Count.SaveAllToJson(counts, "counts.json");

            Console.WriteLine("Api ID: " + config.api_id);
            Console.WriteLine("Api Hash: " + config.api_hash);
            Console.WriteLine("Phone Number: " + config.phone_number);
            Console.WriteLine("Cross The Limit Channel ID: " + config.CrossTheLimitChannelId);
            client = new Client(Config);
            var myself = await client.LoginUserIfNeeded();
            Console.WriteLine($"We are logged-in as {myself} (id {myself.id})");

            client.OnUpdate += OnUpdate;

            var dialogs = await client.Messages_GetAllDialogs();
            dialogs.CollectUsersChats(Users, Chats);

            await SaveSystem();

            await Task.Delay(-1);
        }
        #endregion


        #region Update
        static async Task OnUpdate(UpdatesBase arg)
        {
            if (!(arg is UpdatesBase updates))
            {
                return;
            }

            updates.CollectUsersChats(Users, Chats);

            try
            {
                foreach (var update in arg.UpdateList)
                {
                    if (update is UpdateNewMessage || update is UpdateNewChannelMessage)
                    {
                        Console.WriteLine("naber");
                        var admin_ids = config.admin_ids.ToList();
                        var unm = update as dynamic;


                        var xqc = unm.message as Message;
                        if (xqc == null)
                        {
                            return;
                        }

                        Console.WriteLine("\n\nPEEEER : " + xqc.peer_id.ID + "\n");
                        Console.WriteLine($"\n\nFROOOM : " + (xqc.from_id == null ? "null" : xqc.from_id.ID.ToString()) + "\n");

                        var id = xqc.peer_id.ID;

                        long sender_id = 0;

                        var textMessage = "";
                        bool skip = false;

                        if (xqc != null)
                        {
                            if (xqc.from_id != null)
                            {
                                sender_id = xqc.from_id.ID;
                                Console.WriteLine($" SENDER ID : {sender_id}");
                            }
                            else
                            {
                                Console.WriteLine("Sender ID not available (from_id is null)");
                                skip = true;
                            }
                        }

                        var messageContent = xqc.message;

                        var entities = xqc.entities;

                        var responseMessageContent = messageContent;
                        await Console.Out.WriteLineAsync(responseMessageContent);


                        if (responseMessageContent.StartsWith("/"))
                        {
                            if (config.admin_ids.Any(admin_id => admin_id == sender_id.ToString()))
                            {
                                await Console.Out.WriteLineAsync("naberrrrr");
                                await ProcessCommand(responseMessageContent, id);
                                return;
                            }
                            else
                            {
                                await client.SendMessageAsync(Chats[id], "You are not authorized to use this command.");
                            }
                        }

                        CheckMessageChatId(xqc, id);

                    }
                    else
                    {
                        return;
                    }


                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }


        }
        #endregion


        #region Funcs

        static async Task ProcessCommand(string command, long id)
        {
            if (command.StartsWith("/start"))
            {
                string response = "start";

                Console.WriteLine(response);
            }
            else if (command.StartsWith("/count"))
            {
       
                await HandleSetCount(command, id);
            }
            else if (command.StartsWith("/channels"))
            {
                Console.WriteLine(id);
                await GetChannelsAsync(client, id);
            }
            else if (command.StartsWith("/commands"))
            {
                var chats = await client.Messages_GetAllChats();
                var target = chats.chats[id];

                var st = "/channels => Shows all of the channels with their current attributes (ID,Name,Count,Percentage,Line)\n\n" +
                    "/count => /count {channelID} {count value} percentage:{percentage value} line: {linevalue1,linevalue2} OR {linevalue1}\n\n" +
                    "examples of /count => /count 9428272 6 , /count 87472248 25 percentage:5 line: 5,10 , /count 87472248 25 percentage:5 line: 5\n\n" +
                    "To delete a channel from the list use /count ChannelID";

                await client.SendMessageAsync(target, st);
            }
            else if (command.StartsWith("/set"))
            {
                var chats = await client.Messages_GetAllChats();
                var target = chats.chats[id];

                if (long.TryParse(command.Split(' ')[1], out long chID))
                {

                    await client.SendMessageAsync(target, $"Your new channel has set from {ForwardTo} to {chID}");
                    ForwardTo = chID;
                    return;

                }
                await client.SendMessageAsync(target, $"Channel ids must be a number");

            }
        }

        public static string Config(string what)
        {
            switch (what)
            {
                case "session_pathname": return Environment.CurrentDirectory + "/" + "WTelegram.session";
                case "api_id": return config.api_id;
                case "api_hash": return config.api_hash;
                case "phone_number": return config.phone_number;
                case "verification_code": Console.Write("Verification Code: "); return Console.ReadLine().Trim();
                case "first_name": return "Test";
                case "last_name": return "Test2";
                case "password": Console.Write("Password (If you enabled 2FA or no you can just make it blank): "); return System.Console.ReadLine();     // if user has enabled 2FA
                default: return null;
            }


        }

        public static async Task HandleSetCount(string command, long id)
        {
            try
            {
                var chats = await client.Messages_GetAllChats();
                var target = chats.chats[id];

                string[] parts = command.Split(' ');

                if (parts.Length == 2) // Sadece /count ve bir kanal kimliği girilirse
                {
                    var kanalId = parts[1];

                    // Kanal kimliğine sahip olan sayımı silelim
                    var existingCount = counts.FirstOrDefault(c => c.channel_id == kanalId);

                    if (existingCount != null)
                    {
                        counts.Remove(existingCount);
                        Count.SaveAllToJson(counts, "counts.json");
                        await client.SendMessageAsync(target, $"Count has been deleted for {kanalId}");
                    }
                    else
                    {
                        await client.SendMessageAsync(target, $"There is no count for {kanalId}");
                    }
                    return;
                }

                var channelId = parts[1];
                var messageCount = parts[2];
                var percentage = 100;
                List<int> lines = new List<int>() { 0 };
                var messageText = "";

                for (int i = 3; i < parts.Length; i++)
                {
                    if (parts[i].StartsWith("percentage:"))
                    {
                        int.TryParse(parts[i].Trim().Replace("percentage:", ""), out percentage);
                        if (percentage > 100 || percentage < 0)
                        {
                            await client.SendMessageAsync(target, "Percentage has to between 100 and 0");
                            return;
                        }
                    }
                    else if (parts[i].StartsWith("line:"))
                    {
                        string linePart = parts[i].Replace("line:", "");
                        if (linePart.Contains(","))
                        {
                            // line değeri bir dizi olarak verilmiş, virgülle ayırıyoruz
                            string[] lineValues = linePart.Split(',');
                            lineValues.ToList().ForEach(c => c = c.Trim());
                            int temp = -1;
                            for (int x = 0; x < 2; x++)
                            {
                                var myLineValue = lineValues[x];

                                if (!int.TryParse(myLineValue, out int parsedLine))
                                {
                                    await client.SendMessageAsync(target, "Line value must be number!");
                                    return;
                                }

                                if (parsedLine <= 0)
                                {
                                    await client.SendMessageAsync(target, "Numbers can not be lower than 0");
                                    return;
                                }

                                if (temp > parsedLine)
                                {
                                    await client.SendMessageAsync(target, "Second number can not be lower than First one");
                                    return;
                                }
                                if (lines[0] == 0)
                                {
                                    lines[0] = parsedLine;
                                    temp = parsedLine;
                                }
                                else
                                {
                                    temp = parsedLine;
                                    lines.Add(parsedLine);
                                }
                            }
                        }
                        else
                        {
                            // line değeri sadece tek bir sayı olarak verilmiş
                            if (int.TryParse(linePart, out int parsedLine))
                            {
                                lines.Clear(); // Diziyi temizle
                                lines.Add(parsedLine); // Tek elemanı ekle
                            }
                        }
                    }
                }

                var existingCount2 = counts.FirstOrDefault(c => c.channel_id == channelId);
                if (existingCount2 != null)
                {
                    existingCount2.count = int.Parse(messageCount);
                    existingCount2.percentage = int.Parse(percentage.ToString());
                    existingCount2.lines = lines; // lines dizisini güncelliyoruz
                    messageText = $"count has been updated for {channelId}";
                }
                else
                {
                    Count count = new Count
                    {
                        channel_id = channelId,
                        percentage = percentage,
                        lines = lines, // lines dizisini oluşturuyoruz
                        count = int.Parse(messageCount)
                    };

                    counts.Add(count);
                    messageText = $"count has been created for {channelId}";
                }

                lines.ForEach(l => Console.WriteLine(l));
                Count.SaveAllToJson(counts, "counts.json");

                await client.SendMessageAsync(target, messageText);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }




        public static async Task GetChannelsAsync(Client client, long id)
        {
            var dialogs = await client.Messages_GetAllDialogs();
            var channels = new Dictionary<long, string>();
            long acces_hash = 0;

            var chats = await client.Messages_GetAllChats();

            var target = chats.chats[id];

            foreach (Dialog dialog in dialogs.dialogs)
            {
                switch (dialogs.UserOrChat(dialog))
                {
                    case ChatBase chat when chat.IsActive && chat is TL.Channel:
                        var channelName = ((TL.Channel)chat).title;
                        var channelId = ((TL.Channel)chat).id;
                        if (id == channelId)
                        {
                            acces_hash = ((TL.Channel)chat).access_hash;
                        }
                        channels.Add(channelId, channelName);
                        break;
                }
            }

            foreach (var channel in channels)
            {
                Console.WriteLine($"Channel ID: {channel.Key}");
                Console.WriteLine($"Channel Name: {channel.Value}");

                var count = new Count();

                try
                {
                    count = counts.FirstOrDefault(c => long.Parse(c.channel_id) == channel.Key);

                }
                catch (Exception e)
                {
                    continue;
                }
                try
                {
                    if (count != null)
                    {
                        // Append count information to the message
                        var countInfo = $"Count: {count.count}, Percentage: %{count.percentage}, Line: {count.lines}";
                        var txt = $"{channel.Key} | {channel.Value}\n{countInfo}\n\n";
                        Console.WriteLine(txt);
                    }
                    else
                    {
                        var txt = $"{channel.Key} | {channel.Value}\n\n";
                        Console.WriteLine(txt);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }
            }


            var messageText = string.Join("\n\n", channels.Select(channel =>
            {

                var count = counts.FirstOrDefault(c => long.Parse(c.channel_id) == channel.Key);
                if (count != null) {
                    string lineRange = count.lines.Count > 1 ? count.lines[0] + "-" + count.lines[1] : count.lines[0].ToString();
                    return $"{channel.Key} | {channel.Value}\nCount: {count.count}, Percentage: {count.percentage}, Line: {lineRange}";
                }
                else
                    return $"{channel.Key} | {channel.Value}";
            }));

            messageText = messageText + $"\n\n Cross The Limit Groups : \n\n {(chats.chats[ForwardTo] == null ? " ": chats.chats[ForwardTo].Title)} \n\n {crossTheLimitChannelId} | {chats.chats[crossTheLimitChannelId].Title} ";

            await client.SendMessageAsync(target, messageText);
        }

        public static async Task CheckMessageChatId(Message msg, long id)
        {

            var fromGroup = Chats[id];
            var toGroup = Chats[crossTheLimitChannelId] ?? null;
            var toGroup2 = Chats.ToList().Exists(x => x.Key == ForwardTo) ? Chats[ForwardTo] : null;
            foreach (var item in counts)
            {
                string channelId = item.channel_id.Trim(); // Trim whitespace
                string msgChannelId = msg.ToString().Split('>')[0].Trim(); // Trim whitespace

                Console.WriteLine($"item.channel_id : {channelId} (Length: {channelId.Length})");
                Console.WriteLine($"msg.ToString().Split('>')[0] : {msgChannelId} (Length: {msgChannelId.Length})");

                if (channelId == msgChannelId)
                {
                    MessageData messageData = new MessageData();
                    messageData.message = TruncateStr(msg.ToString().Split('>')[1].Trim(), item.lines);
                    messageData.count = 1;
                    messageData.isCroosTheLimit = false;
                    messageData.id = (ulong)msg.id;

                    bool found = false;
                    foreach (var allMessageData in allMessages)
                    {

                        if (allMessageData.channel_id == item.channel_id)
                        {
                            bool messageFound = false;
                            foreach (var existingMessage in allMessageData.messages)
                            {
                                if (CheckStringMatch(existingMessage.message.ToString().ToLower(), messageData.message.ToString().ToLower(), item.percentage, item.lines))
                                {
                                    existingMessage.count++;
                                    if (existingMessage.count > item.count && !existingMessage.isCroosTheLimit)
                                    {
                                        var messageText = $"Message: The count for {existingMessage.message} exceeded {item.count}.";
                                        await client.Messages_ForwardMessages(fromGroup, new[] { (int)existingMessage.id }, new[] { WTelegram.Helpers.RandomLong() }, toGroup);
                                        if (toGroup2 != null)
                                        {
                                            await client.Messages_ForwardMessages(fromGroup, new[] { (int)existingMessage.id }, new[] { WTelegram.Helpers.RandomLong() }, toGroup2);
                                        }


                                        existingMessage.isCroosTheLimit = true;
                                    }
                                    messageFound = true;
                                    break;
                                }
                            }

                            if (!messageFound)
                            {
                                allMessageData.messages.Add(messageData);
                            }

                            found = true;
                            break;
                        }
                    }

                    if (!found)
                    {
                        AllMessageData newAllMessageData = new AllMessageData();
                        newAllMessageData.channel_id = item.channel_id;
                        newAllMessageData.messages.Add(messageData);
                        allMessages.Add(newAllMessageData);
                        Console.WriteLine(allMessages);
                        Console.WriteLine("jjjjjjjjjjjjjjjjjjjjjjjjj");
                    }
                }
            }
            Console.WriteLine(allMessages.Count);
        }

        static bool CheckStringMatch(string str1, string str2, int Percentage, List<int> lines)
        {

            str1 = str1.Replace("\n", "");

            string truncatedStr2 = TruncateStr(str2, lines);
            int minLength = Math.Min(str1.Length, truncatedStr2.Length);
            int maxLength = Math.Max(str1.Length, truncatedStr2.Length);


            int matchCount = 0;

            for (int i = 0; i < minLength; i++)
            {
                if (str1[i] == truncatedStr2[i])
                {
                    matchCount++;
                }
            }

            double matchPercentage = (double)matchCount / maxLength * 100;

            return matchPercentage >= Percentage;
        }

        static string TruncateStr(string str, List<int> line)
        {
            string truncatedStr = str;
            string[] lines = str.Split('\n');

            if (line.Count == 1)
            {
                int endLine = Math.Min(line[0], lines.Length);
                if (line[0] != 0) truncatedStr = string.Join("\n", lines.Take(endLine));
            }
            else if (line.Count == 2)
            {
                int startLine = Math.Min(Math.Max(line[0], 0), lines.Length);
                int endLine = Math.Min(Math.Max(line[1], startLine), lines.Length);
                truncatedStr = string.Join("\n", lines.Skip(startLine - 1).Take(endLine - startLine));
            }

            return truncatedStr;
        }





        #endregion



        #region Save and Load
        public static async Task SaveSystem()
        {
            for (; ; )
            {
                try
                {
                    File.WriteAllText(Environment.CurrentDirectory + "/" + "all_messages.json", JsonConvert.SerializeObject(allMessages, Formatting.Indented));

                }
                catch (Exception ex)
                {

                    Console.WriteLine(ex);
                }
                await Task.Delay(500);
            }
        }

        private static List<Count> LoadCountsFromJson(string filePath)
        {
            if (File.Exists(filePath))
            {

                string json = File.ReadAllText(filePath);
                return JsonConvert.DeserializeObject<List<Count>>(json) ?? new List<Count>();
            }
            else
            {
                Console.WriteLine("dosya bulunamadı");
            }
            return new List<Count>();
        }
        private static List<tempCount> TempLoadCountsFromJson(string filePath)
        {
            if (File.Exists(filePath))
            {

                string json = File.ReadAllText(filePath);
                return JsonConvert.DeserializeObject<List<tempCount>>(json) ?? new List<tempCount>();
            }
            else
            {
                Console.WriteLine("dosya bulunamadı");
            }
            return new List<tempCount>();
        }

        private static List<AllMessageData> LoadAllMessageDataFromJson(string filePath)
        {
            if (File.Exists(filePath))
            {
                try
                {
                    string json = File.ReadAllText(filePath);
                    return JsonConvert.DeserializeObject<List<AllMessageData>>(json) ?? new List<AllMessageData>();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }

                return new List<AllMessageData>();

            }
            else
            {

            }
            return new List<AllMessageData>();
        }
        #endregion


    }


}
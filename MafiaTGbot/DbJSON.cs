using MafiaTGbot.Constant;
using MafiaTGbot.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types.Passport;

namespace MafiaTGbot
{
    public static class DbJSON
    {
        public static List<GameSession> gameSessions = new List<GameSession>();

        public static async Task PutToDbAsync(GameSession gameSession)
        {
            //string json = JsonConvert.SerializeObject(gameSession);

            //using (StreamWriter writer = new StreamWriter(Constants.DBFileName + $"_{gameSession.Id}.txt", false))
            //{
            //    await writer.WriteLineAsync(json);
            //}

            if(!gameSessions.Any(a => a.Id == gameSession.Id))
            {
                gameSessions.Add(gameSession);
            }
        }

        public static async Task<GameSession> GetSessionFromDBAsync(int sessionId)
        {
            //string text;
            //using (StreamReader reader = new StreamReader(Constants.DBFileName + $"_{sessionId}.txt"))
            //{
            //    text = await reader.ReadToEndAsync();
            //}

            //var currentSession = JsonConvert.DeserializeObject<GameSession>(text);

            //return currentSession;

            return gameSessions.Where(a => a.Id == sessionId).FirstOrDefault();
        }
    }
}

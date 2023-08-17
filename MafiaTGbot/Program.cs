using MafiaTGbot.Models;
using Telegram.Bot.Types.ReplyMarkups;


namespace MafiaTGbot
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Hello, World!");
            
            var bot = new MafiaBot();

            var game = new GameLogic(bot);

            bot.GameCreationRequest += game.GameCreationHanler;
            bot.GameJoinRequest += game.GameJoinHanler;
            bot.GameStartRequest += game.LaunchGameHandler;
            bot.GetRoleRequest += game.GetRoleHandler;
            bot.GameActionRequest += game.GameActionHandler;

            await bot.Start();

            //GameSessionMember gameSessionMember1 = new GameSessionMember()
            //{
            //    botUser = new BotUser() { Id = 1, Name = "Vlad" }
            //};

            //GameSessionMember gameSessionMember2 = new GameSessionMember()
            //{
            //    botUser = new BotUser() { Id = 2, Name = "Den" }
            //};

            //GameSessionMember gameSessionMember3 = new GameSessionMember()
            //{
            //    botUser = new BotUser() { Id = 3, Name = "Nastia" }
            //};
            //GameSessionMember gameSessionMember4 = new GameSessionMember()
            //{
            //    botUser = new BotUser() { Id = 4, Name = "Taras" }
            //};
            //GameSessionMember gameSessionMember5 = new GameSessionMember()
            //{
            //    botUser = new BotUser() { Id = 5, Name = "Vika" }
            //};
            //GameSessionMember gameSessionMember6 = new GameSessionMember()
            //{
            //    botUser = new BotUser() { Id = 6, Name = "Vadim" }
            //};

            //GameSessionMember gameSessionMember7 = new GameSessionMember()
            //{
            //    botUser = new BotUser() { Id = 1, Name = "Slava" }
            //};

            //var gameSessionMembers = new List<GameSessionMember>
            //{
            //    gameSessionMember1,
            //    gameSessionMember2,
            //    gameSessionMember3,
            //    gameSessionMember4,
            //    gameSessionMember5,
            //    gameSessionMember6,
            //    gameSessionMember7
            //};

            //GameSession Session = new()
            //{
            //    Id = 1,
            //    GameMembers = gameSessionMembers,
            //    State = Enums.GameSessionState.Registration
            //};

            //GameLogic.ResolveRoles(Session);

            //Console.ReadKey();

        }
    }
}
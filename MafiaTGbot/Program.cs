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

        }
    }
}
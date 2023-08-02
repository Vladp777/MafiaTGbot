using System.Text;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Extensions.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using MafiaTGbot.Models;
using System.Collections.Concurrent;

namespace MafiaTGbot
{
    public class MafiaBot
    {
        private static readonly ConcurrentDictionary<string, TelegramVote> VoteRegistry =
    new ConcurrentDictionary<string, TelegramVote>();


        TelegramBotClient client = new TelegramBotClient(MafiaTGbot.Constant.Constants.TelegramToken);

        CancellationToken cancellationToken = new CancellationToken();
        ReceiverOptions receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = { },
            ThrowPendingUpdates = true,
        };

        public async Task Start()
        {
            client.StartReceiving(HandlerUpdateAsync, HandlerError, receiverOptions, cancellationToken);
            var botMe = await client.GetMeAsync();

            Console.WriteLine($"Бот {botMe.Username} почав працювати");
            Thread.Sleep(int.MaxValue);
            //Console.ReadKey();
        }

        private Task HandlerError(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            var ErrorMasage = exception switch
            {
                ApiRequestException apiRequestException => $"Помилка в телеграм бот АПІ:\n{apiRequestException.ErrorCode}" +
                $"\n{apiRequestException.Message}",
                _ => exception.ToString()
            };
            Console.WriteLine(ErrorMasage);

            return Task.CompletedTask;
        }

        private async Task HandlerUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            var handler = update.Type switch
            {
                UpdateType.Message => HandlerMessageAsync(botClient, update.Message!),
                UpdateType.EditedMessage => HandlerMessageAsync(botClient, update.EditedMessage!),
                UpdateType.CallbackQuery => BotOnCallbackQueryReceived(botClient, update.CallbackQuery!),
                //UpdateType.InlineQuery => BotOnInlineQueryReceived(botClient, update.InlineQuery!),
                //UpdateType.ChosenInlineResult => BotOnChosenInlineResultReceived(botClient, update.ChosenInlineResult!),
                //_ => UnknownUpdateHandlerAsync(botClient, update)
            };
        }

        private async Task BotOnCallbackQueryReceived(ITelegramBotClient botClient, CallbackQuery callbackQuery)
        {
            var user = new BotUser
            {
                Id = (int)callbackQuery.From.Id,
                Name = callbackQuery.From.FirstName + " " + callbackQuery.From.LastName
            };

            if (callbackQuery.Data == "join")
            {
                OnGameJoinRequest(callbackQuery, user);
            }
            else if(callbackQuery.Data == "role")
            {
                OnGetRoleRequest(callbackQuery, user);
            }
            else if (callbackQuery.Data.StartsWith("action"))
            {
                var victimId = Int32.Parse(callbackQuery.Data.Split('_')[1]);

                OnGameActionRequest(callbackQuery, victimId);
            }
            else if (callbackQuery.Data.StartsWith("vote"))
            {
                {
                    var roomId = callbackQuery.Message.Chat.Id.ToString();

                    var userId = (int)callbackQuery.From.Id;

                    var voice = Int32.Parse(callbackQuery.Data.Split('_')[1]);
                    var answer = await ProcessVoteAnswer(roomId, userId, voice);
                    await  client.AnswerCallbackQueryAsync(
                        callbackQuery.Id,
                        answer);
                    return;
                }
            }

            
        }

        private async Task<string> ProcessVoteAnswer(string roomId, int userId, int voice)
        {
            if (!IsActiveVote(roomId))
            {
                return "Не актуально!";
            }

            var voteInfo = GetVoteInfo(roomId);
            if (voteInfo == null)
                return "Голосування не знайдено";

            // check allowance
            if (!voteInfo.AllowedToPassVoteUsersIds.ContainsKey(userId))
                return "Заборонено!";
            if (voteInfo.VoteAllowedPredicate != null &&
                !voteInfo.VoteAllowedPredicate.Invoke((userId, voice)))
            {
                return "Відхилено!";
            }

            voteInfo.AddOrUpdateVote(userId.ToString(), voice.ToString());
            await UpdateVote(roomId, voteInfo);
            return "Голос прийнято";
        }
        public async void OnGamerJoined(GameSession session, int messageId)
        {
            var text = GenerateRegistrationMessage(session, out var buttons);
            await client.EditMessageTextAsync(session.Id, messageId, text, parseMode: ParseMode.Html, replyMarkup: new InlineKeyboardMarkup(buttons));

            //botClient.EditMessageTextAsync(callbackQuery.Message.Chat.Id, callbackQuery.Message.MessageId, "змінено");
        }

        private async Task HandlerMessageAsync(ITelegramBotClient botClient, Message message)
        {
            // skip old messages
            if (message.Type != MessageType.Text)
                return;

            // trying to parse bot commands
            var text = message.Text;

            try
            {
                if (text.StartsWith("/start"))
                    await StartCommand(botClient, message);
                else if (text == "/next")
                    await LaunchCommand(message);
                
            }
            catch (Exception e)
            {
                await botClient.SendTextMessageAsync(message.Chat.Id, e.Message,
                    message.MessageId);
                //Log.Error(e, "Error occured in message handling");
            }
        }

        private async Task LaunchCommand(Message message)
        {
            var sessionId = (int)message.Chat.Id;

            OnGameStartRequest(sessionId);
        }

        private async Task StartCommand(ITelegramBotClient bot, Message message)
        {
            var roomId = (int) message.Chat.Id;

            

            var user = new BotUser
            {
                Id = (int)message.From.Id,
                Name = message.From.FirstName + " " + message.From.LastName
            };
            var gamerAccountId = (int) message.From.Id;

            OnGameCreationRequest(roomId, user);

            //var gameses = GameLogic.CreateSession(message.Chat.Id, message.From.Id);

            //var text = GameLogic.GenerateRegistrationMessage(gameses, new TelegramFrontendSettings { BotUserName = "mafia_na_bani_bot" }, out var buttons);

            //Message message1 = await bot.SendTextMessageAsync(gameses.Id, text, replyMarkup: new InlineKeyboardMarkup(buttons));
        }

        public void OnGameSessionCreated(GameSession session) => CreateRegistrationMessage(session);

        public async Task OnGameStarted(int sessionId)
        {
            var buttons = new List<InlineKeyboardButton>()
            {
                InlineKeyboardButton.WithCallbackData
                (
                    "МОЯ РОЛЬ",
                    "role"
                )
            };

            using FileStream fsSource = new FileStream("D:\\MyProjects\\MafiaTGbot\\MafiaTGbot\\WhoAmI.png", FileMode.Open, FileAccess.Read);
            await client.SendPhotoAsync(sessionId,
            InputFile.FromStream(fsSource),
            parseMode: ParseMode.Html,
            replyMarkup: new InlineKeyboardMarkup(buttons));
            return;
        }

        public async Task<Message> SendNightMessageToRoom(GameSession session, int day)
        {
            var text = $"Місто засинає. Ніч #{day} 🌃";

            var buttons = new List<List<InlineKeyboardButton>>();

            foreach (var user in session.GameMembers)
            {
                if (!user.IsDead)
                    buttons.Add(new List<InlineKeyboardButton>
                    {
                        InlineKeyboardButton.WithCallbackData
                            (
                                $"{user.botUser.Name}",
                                $"action_{user.botUser.Id}"
                            )
                    }) ;
            }

            return await client.SendTextMessageAsync(session.Id, text, parseMode: ParseMode.Html, replyMarkup: new InlineKeyboardMarkup(buttons));
        }

        public async Task DeleteInlineKeyboard(Message message)
        {
            await client.EditMessageReplyMarkupAsync(message.Chat.Id, message.MessageId);
        }

        private string GenerateRegistrationMessage(GameSession session, out List<InlineKeyboardButton> buttons)
        {
            var text =
           $"<b>Зібрав нас: <i>{session.CreatedByGamerAccount.Name}</i></b>\n\n<b>Учасники:</b> \n\n";

            var index = 1;
            foreach (var member in session.GameMembers)
            {
                text += $"{index}. {member.botUser.Name} \n";
                index++;
            }

            buttons = new List<InlineKeyboardButton>()
            {
                InlineKeyboardButton.WithCallbackData
                (
                    "JOIN 🎮",
                    "join"
                )
            };

            return text ;
        }
        private async Task CreateRegistrationMessage(GameSession session)
        {
            var text = GenerateRegistrationMessage(session, out var buttons);

            await client.SendTextMessageAsync(session.Id, text, parseMode: ParseMode.Html, replyMarkup: new InlineKeyboardMarkup(buttons));
        }

        public async Task<VoteDescriptor[]> CreateLynchVoteAndReceiveResults(int sessionId,
            List<GameSessionMember> allowedMembers)
        {
            // creating vote
            var variants = allowedMembers.Select(m => (m.botUser.Id, m.botUser.Name)).ToArray();
            var allowedToVoteUserIds = new Dictionary<int, string>();
            foreach (var gameSessionMember in allowedMembers)
            {
                if (!allowedToVoteUserIds.ContainsKey(gameSessionMember.botUser.Id))
                {
                    allowedToVoteUserIds.Add(gameSessionMember.botUser.Id,
                        gameSessionMember.botUser.Name);
                }
            }

            var telegramVote = new TelegramVote(variants, "<i>Кого кікаємо?</i> 🎲", allowedToVoteUserIds,
                request =>
                    request.userId != request.voice);
            await CreateVote(telegramVote, sessionId.ToString());
            //const int maxTries = 6;
            var canVoteCount = allowedToVoteUserIds.Count;
            var voices = new TelegramVoiceItem[0];
            while (true)
            {
                await Task.Delay(10000);
                voices = telegramVote.GetVoices();
                if (voices.Length == canVoteCount)
                    break;
            }

            await FinishVote(telegramVote, sessionId.ToString());

            return voices.Select(v => new VoteDescriptor
            {
                VoiceOwner = allowedMembers.First(g => g.botUser.Id.ToString() == v.UserId),
                VoiceTarget = allowedMembers.First(g => g.botUser.Id.ToString() == v.Voice)
            }).ToArray();
        }

        private async Task FinishVote(TelegramVote telegramVote, string roomId)
        {
            DeleteVote(roomId);
            await UpdateVote(roomId, telegramVote, true);
        }

        # region Votes

        public static bool IsActiveVote(string roomId) => VoteRegistry.ContainsKey(roomId);
        public static void DeleteVote(string roomId) => VoteRegistry.TryRemove(roomId, out _);

        public static void AddVote(string roomId, TelegramVote vote) =>
            VoteRegistry.AddOrUpdate(roomId, vote, (key, _) => vote);

        public static TelegramVote? GetVoteInfo(string roomId) =>
            VoteRegistry.TryGetValue(roomId, out var voteInfo) ? voteInfo : null;

        # endregion
        private async Task CreateVote(TelegramVote telegramVote, string roomId)
        {
            AddVote(roomId, telegramVote);
            await UpdateVote(roomId, telegramVote);
        }

        private static readonly SemaphoreSlim BotLock = new SemaphoreSlim(1);
        public async Task LockAndDo(Func<Task> action)
        {
            try
            {
                await BotLock.WaitAsync();
                await Task.Delay(50);
                await action();
            }
            catch (Exception e)
            {
                await Console.Out.WriteLineAsync(e.Message);
            }

            finally
            {
                BotLock.Release();
            }
        }

        private async Task UpdateVote(string roomId, TelegramVote telegramVote, bool finish = false)
        {
            var voices = telegramVote.GetVoices();
            var voicesInfo = new StringBuilder();
            var usersAndVoices = (from voice in voices
                                  join user in telegramVote.AllowedToPassVoteUsersIds on voice.UserId.ToString() equals user.ToString()
                                  select new { voice = voice.Voice, userName = user.Value }).ToList();
            foreach (var (uiName, internalName) in telegramVote.Variants)
            {
                var voiceInfo = usersAndVoices.Where(u => u.voice == internalName).Select(u => u.userName).ToList();
                //voicesInfo.AppendLine(
                //    $"- <b>{internalName}</b>: {(voiceInfo.Any() ? string.Join(", ", voiceInfo) : "нет голосов")}.\n");
            }

            var messageId = telegramVote.MessageId;
            var finalText = $"<b>Голосування</b>\n{telegramVote.Text}\n\n{voicesInfo}";
            if (finish)
            {
                await LockAndDo(() => client.EditMessageTextAsync(roomId, messageId.Value,
                    $"{finalText}\n<b>Голосування закінчено!</b>", ParseMode.Html,
                    null));
                return;
            }

            var buttons = telegramVote
                .Variants
                .Select(variant => new List<InlineKeyboardButton>
                {
                    InlineKeyboardButton.WithCallbackData
                    (
                        $"{variant.internalName} ({voices.Count(v => v.Voice == variant.uiName.ToString())})",
                        $"vote_{variant.uiName}"
                    )
                })
                .ToArray();

            await LockAndDo(async () =>
            {
                if (messageId == null)
                {
                    var message = await client.SendTextMessageAsync(roomId, finalText, parseMode: ParseMode.Html,
                        replyMarkup: new InlineKeyboardMarkup(buttons)
                    );
                    await Task.Delay(100);
                    //await client.PinMessageIfAllowed(message, CancellationToken.None);
                    telegramVote.SetMessageId(message.MessageId);
                }
                else
                {
                    await client.EditMessageTextAsync(roomId, messageId.Value, finalText, parseMode: ParseMode.Html,
                         replyMarkup: new InlineKeyboardMarkup(buttons));
                }
            });
        }

        public event Action<(CallbackQuery callbackQuery, BotUser user)> GameJoinRequest;
        public event Action<(int roomId, BotUser user)> GameCreationRequest;
        public event Action<(int roomId, BotUser user)> GameStopRequest;
        public event Action<(int roomId, BotUser user)> GameLeaveRequest;
        public event Action<int> GameStartRequest;
        public event Action<(CallbackQuery callbackQuery, BotUser user)> GetRoleRequest;
        public event Action<(CallbackQuery callbackQuery, int victimId)> GameActionRequest;

        protected void OnGameActionRequest(CallbackQuery callbackQuery, int victimId) =>
            GameActionRequest?.Invoke((callbackQuery, victimId));
        protected  void OnGetRoleRequest(CallbackQuery callbackQuery, BotUser user) =>
            GetRoleRequest?.Invoke((callbackQuery, user));
        protected void OnGameJoinRequest(CallbackQuery callbackQuery, BotUser user) =>
            GameJoinRequest?.Invoke((callbackQuery, user));

        protected void OnGameCreationRequest(int gameRoomId, BotUser user) =>
            GameCreationRequest?.Invoke((gameRoomId, user));

        protected void OnGameStopRequest(int gameRoomId, BotUser user) =>
            GameStopRequest?.Invoke((gameRoomId, user));

        protected void OnGameStartRequest(int gameRoomId) =>
            GameStartRequest?.Invoke(gameRoomId);

        protected void OnGameLeaveRequest(int gameRoomId, BotUser user) =>
            GameLeaveRequest?.Invoke((gameRoomId, user));

        internal async Task SendAnswerToGamer(string callbackQueryId, string v, bool a = false)
        {
            await client.AnswerCallbackQueryAsync(callbackQueryId, v, showAlert: a);
        }

        internal async Task SendMessageToRoom(int sesionId, string v)
        {
            await client.SendTextMessageAsync(sesionId, v, parseMode: ParseMode.Html);
        }
    }
}

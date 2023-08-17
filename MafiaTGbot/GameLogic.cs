using MafiaTGbot.Models;
using System.Text;
using Telegram.Bot.Types;
using MafiaTGbot.Constant;
using Serilog;
using MafiaTGbot.Enums;
using System.Diagnostics;

namespace MafiaTGbot
{
    public  class GameLogic
    {
        private MafiaBot _bot;
        public GameLogic(MafiaBot bot) 
        {
            _bot = bot; 
        }

        private Dictionary<int, int> _healedRegistry = new Dictionary<int, int>();
        private Dictionary<int, int> _fuckedRegistry = new Dictionary<int, int>();
        private Dictionary<int, List<int>> _chekUpRegistry = new Dictionary<int, List<int>>();


        private Dictionary<int,  List<UserAction>> _actions = new Dictionary<int, List<UserAction>>();


        public void GameCreationHanler((int roomId, BotUser user) param)
        {

            //_healedRegistry.Clear();
            //_fuckedRegistry.Clear();
            //_chekUpRegistry.Clear();
            //_actions.Clear();
            if (DbJSON.gameSessions.Any(a => a.Id == param.roomId))
            {
                DbJSON.gameSessions.Remove(DbJSON.gameSessions.Where(a => a.Id == param.roomId).First());
            }

            if (_healedRegistry.ContainsKey(param.roomId))
            {
                _healedRegistry.Remove(param.roomId);
            }
            if (_fuckedRegistry.ContainsKey(param.roomId))
            {
                _fuckedRegistry.Remove(param.roomId);
            }
            if (_chekUpRegistry.ContainsKey(param.roomId))
            {
                _chekUpRegistry.Remove(param.roomId);
            }
            if (_actions.ContainsKey(param.roomId))
            {
                _actions.Remove(param.roomId);
            }

            var newSession = new GameSession
            {
                Id = param.roomId,
                StartedOn = DateTime.Now,
                CreatedByGamerAccount = param.user,
                State = GameSessionState.Registration
            };

            _healedRegistry.Add((int)newSession.Id, 0);
            _fuckedRegistry.Add((int)newSession.Id, 0);
            _chekUpRegistry.Add((int)newSession.Id, new List<int>());
            _actions.Add((int)newSession.Id, new List<UserAction>());

            _ = DbJSON.PutToDbAsync(newSession);

            //string json = JsonConvert.SerializeObject(newSession);

            //using (StreamWriter writer = new StreamWriter(Constants.DBFileName + $"_{newSession.Id}.txt", false))
            //{
            //    await writer.WriteLineAsync(json);
            //}
            _bot.OnGameSessionCreated(newSession);
        }
        public async void GameJoinHanler((CallbackQuery callbackQuery, BotUser user) param)
        {
            try
            {
                GameSession currentSession;
                BotUser joinedGamerAccount = param.user;
                int sessionId = (int)param.callbackQuery.Message.Chat.Id;

                currentSession = DbJSON.GetSessionFromDBAsync(sessionId).Result;

                //string text;
                //using (StreamReader reader = new StreamReader(Constants.DBFileName + $"_{sessionId}.txt"))
                //{
                //    text = await reader.ReadToEndAsync();
                //}
                //currentSession =  JsonConvert.DeserializeObject<GameSession>(text);

                
                if (currentSession == null)
                {
                    await _bot.SendAnswerToGamer(param.callbackQuery.Id ,
                        "Спочатку створи гру '/start'");
                    return;
                }

                if (currentSession.State != GameSessionState.Registration)
                {
                    await _bot.SendAnswerToGamer(param.callbackQuery.Id,
                        "Гра вже почалася, гуляй");
                    return;
                }
                if (currentSession.GameMembers.Any(gm => gm.botUser.Id == param.user.Id))
                {
                    await _bot.SendAnswerToGamer(param.callbackQuery.Id, "Даун чи шо, чекай!!!");
                    return;
                }

                currentSession.GameMembers.Add(new GameSessionMember
                {
                    botUser = param.user,
                    gameSession = currentSession,
                });

                _ = DbJSON.PutToDbAsync(currentSession);

                //string json = JsonConvert.SerializeObject(currentSession);

                //using (StreamWriter writer = new StreamWriter(Constants.DBFileName + $"_{currentSession.Id}.txt", false))
                //{
                //    await writer.WriteLineAsync(json);
                //}

                await _bot.SendAnswerToGamer(param.callbackQuery.Id, "Ти в грі");
                
                _bot.OnGamerJoined(currentSession, param.callbackQuery.Message.MessageId);
            }
            catch (Exception e)
            {
                Log.Error(e, "Error occured when user tried to join a game");
            }
        }

        public  GameSession CreateSession(long sessionId, long accountId)
        {
            GameSession gameSession = new GameSession
            {
                StartedOn = DateTime.Now,
                Id = sessionId,
                //CreatedByGamerAccountId = accountId
            };
                
            return gameSession;
        }

        public async void LaunchGameHandler(int sessionId)
        {
            try
            {
                GameSession session;

                session = DbJSON.GetSessionFromDBAsync(sessionId).Result;

                if (session == null)
                {
                    await _bot.SendMessageToRoom(sessionId, "Створи спочатку гру");
                    return;
                }

                if (session.State == GameSessionState.Playing)
                {
                    await _bot.SendMessageToRoom(sessionId, "Гра вже почалася");
                    return;
                }

                if ( session.GameMembers.Count < Constants.MinimalGamerCount)
                {
                    await _bot.SendMessageToRoom(sessionId,
                        $"Замало людей зібралося на бані.Мін. кількість: {Constants.MinimalGamerCount}");
                    return;
                }

                ResolveRoles(session);

                session.State = GameSessionState.Playing;

                _ = DbJSON.PutToDbAsync(session);

                await _bot.SendMessageToRoom(sessionId,
                    $"Гра почалася! Кількість мафійозників: {session.GameMembers.Count(m => m.Role == GameRoles.Mafia)}");

                await _bot.OnGameStarted((int)session.Id);

                await Task.Delay(5000);

                await RunGame((int)session.Id);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        public async void GetRoleHandler((CallbackQuery callbackQuery, BotUser user) param)
        {
            try
            {
                GameSession session;
                //BotUser joinedGamerAccount = param.user;

                int sessionId = (int)param.callbackQuery.Message.Chat.Id;

                session = DbJSON.GetSessionFromDBAsync(sessionId).Result;

                if (session == null)
                {
                    await _bot.SendAnswerToGamer(param.callbackQuery.Id,
                        "Спочатку створи гру '/start'");
                    return;
                }

                if (session.State != GameSessionState.Playing)
                {
                    await _bot.SendAnswerToGamer(param.callbackQuery.Id,
                        "Гра ще не почалася");
                    return;
                }

                
                var man = session.GameMembers.FirstOrDefault(m => m.botUser.Id == param.user.Id);

                if (man == null)
                {
                    await _bot.SendAnswerToGamer(param.callbackQuery.Id,
                        "Ти не береш участі у грі");
                    return;
                }


                var describedRole = man.Role switch
                {
                     GameRoles.Mafia => "Ти мафія",
                     GameRoles.Doctor => "Ти лікар",
                     GameRoles.Cop => "Ти Комісар",
                     GameRoles.Citizen => "Ти мирний житель",
                     GameRoles.Maniac => "Ти маніяк",
                     GameRoles.Whore => "Ти повія"
                };

                if (man.Role == GameRoles.Mafia)
                {
                    var listOfMafiaName = session.GameMembers.Where(a => a.Role == GameRoles.Mafia 
                                                                        && a.botUser.Id != param.user.Id).Select(a => a.botUser.Name);
                    if (listOfMafiaName.Count() > 0)
                    {
                        describedRole += "\nСоюзники: \n";
                        foreach (var a in listOfMafiaName)
                        {
                            describedRole += a + "\n ";
                        }
                    }
                }

                await _bot.SendAnswerToGamer(param.callbackQuery.Id, describedRole, true);
            }
            catch (Exception e)
            {
                await Console.Out.WriteLineAsync(e.Message);
            }
        }

        public async void GameActionHandler((CallbackQuery callbackQuery, int victimId) param)
        {
            try
            {
                var sessionId = (int)param.callbackQuery.Message.Chat.Id;
                var actionOwnerId = (int)param.callbackQuery.From.Id;
                var victimId = param.victimId;

                var session = DbJSON.GetSessionFromDBAsync(sessionId).Result;

                if (!session.GameMembers.Any(a => a.botUser.Id == actionOwnerId))
                {
                    await _bot.SendAnswerToGamer(param.callbackQuery.Id, "Ви не берете участь у грі",true);
                    return;
                }

                if (session.GameMembers.Where(a => a.botUser.Id == actionOwnerId).First().IsDead)
                {
                    await _bot.SendAnswerToGamer(param.callbackQuery.Id, "Трупи не мають права на дію", true);
                    return;
                }

                if (session.GameMembers.Where(a => a.botUser.Id == actionOwnerId).First().Role == GameRoles.Citizen)
                {
                    await _bot.SendAnswerToGamer(param.callbackQuery.Id, "Ти памоєму пєрєпутал", true);
                    return;
                }

                var actionOwnerRole = session.GameMembers.Where(a => a.botUser.Id == actionOwnerId).First().Role;

                Dictionary<GameRoles, GameAction> describedRole = new()
                {
                    { GameRoles.Mafia, GameAction.Killing},
                    { GameRoles.Doctor, GameAction.Healing},
                    { GameRoles.Cop, GameAction.Checkup},
                    { GameRoles.Maniac, GameAction.Murder},
                    { GameRoles.Whore, GameAction.Fucking}
                };

                var botUser = new BotUser
                {
                    Id = victimId,
                    Name = session.GameMembers.Where(a => a.botUser.Id == victimId).First().botUser.Name
                };

                UserAction userAction = new UserAction
                {
                    ActionFromID = actionOwnerId,
                    gameAction = describedRole[actionOwnerRole],
                    Victim = botUser
                };

                if (userAction.gameAction == GameAction.Killing && _actions[sessionId].Any(a => a.gameAction == GameAction.Killing))
                {
                    await _bot.SendAnswerToGamer(param.callbackQuery.Id, "Мафія уже зробила вибір", true);
                    return;
                }

                if (_actions[sessionId].Any(a => a.ActionFromID == userAction.ActionFromID))
                {
                    //_actions[sessionId].Where(a => a.ActionFromID == userAction.ActionFromID).First().Victim = botUser;
                    await _bot.SendAnswerToGamer(param.callbackQuery.Id, "Ви уже зробили вибір", true);
                    return;
                }

                if (userAction.gameAction == GameAction.Checkup)
                {
                    if(session.GameMembers.Where(a => a.botUser.Id == victimId).First().Role == GameRoles.Mafia)
                    {
                        await _bot.SendAnswerToGamer(param.callbackQuery.Id, $"{userAction.Victim.Name} - мафія", true);
                        _chekUpRegistry[sessionId].Add(victimId);
                    }
                    else
                        await _bot.SendAnswerToGamer(param.callbackQuery.Id, $"{userAction.Victim.Name} - НЕ мафія", true);
                }

                if (userAction.gameAction == GameAction.Healing)
                {
                    if (_healedRegistry[sessionId] == victimId)
                    {
                        await _bot.SendAnswerToGamer(param.callbackQuery.Id, $"Вибери іншого", true);
                        return;
                    }
                    else
                        _healedRegistry[sessionId] = victimId;
                } 

                if(userAction.gameAction == GameAction.Fucking)
                {
                    if (_fuckedRegistry[sessionId] == victimId || actionOwnerId == victimId)
                    {
                        await _bot.SendAnswerToGamer(param.callbackQuery.Id, $"Вибери іншого", true);
                        return;
                    }
                    else
                        _fuckedRegistry[sessionId] = victimId;
                }

                //if (_actions.ContainsKey(sessionId))
                //else
                //{
                //    var newlist = new List<UserAction>() { userAction };;

                //    _actions.Add(sessionId, newlist);
                //}
                _actions[sessionId].Add(userAction);

                await _bot.SendAnswerToGamer(param.callbackQuery.Id, "Вибір зараховано", true);
            }
            catch(Exception ex)
            {
                await Console.Out.WriteLineAsync(ex.Message);
            }
        }

        private async Task  RunGame(int sessionId)
        {
            try
            {
                // creating doctor healing registry for session
                //_healedRegistry.Add(sessionId, 0);
                //_fuckedRegistry.Add(sessionId, 0);
                //_chekUpRegistry.Add(sessionId, 0);
                //_healedRegistry[sessionId] = 0;
                //_fuckedRegistry[sessionId] = 0;

                var session = DbJSON.GetSessionFromDBAsync(sessionId).Result;

                var stopwatch = new Stopwatch();
                stopwatch.Start();

                //await SendIntroduceMessages(session.GameMembers);
                var dayNumber = 1;

                while (true)
                {
                    _actions[sessionId].Clear();
                    
                    var KilledPlayers = new List<GameSessionMember>();

                    var message = await _bot.SendNightMessageToRoom(session, dayNumber);

                    await Task.Delay(60000);

                    await _bot.DeleteInlineKeyboard(message);

                    // resolving actions
                    var doctorAction = _actions[sessionId].FirstOrDefault(a => a.gameAction == GameAction.Healing);
                    GameSessionMember? healingTarget = null;

                    if (doctorAction != null)
                        healingTarget = session.GameMembers.First(a => a.botUser.Id == doctorAction.Victim.Id);
                    else
                        _healedRegistry[sessionId] = 0; 

                    var whoreAction = _actions[sessionId].FirstOrDefault(a => a.gameAction == GameAction.Fucking);
                    GameSessionMember? whoreTarget = null;
                    if (whoreAction != null)
                    {
                        whoreTarget = session.GameMembers.First(a => a.botUser.Id == whoreAction.Victim.Id);

                        await _bot.SendMessageToRoom(sessionId, "Переспали з " + whoreTarget.botUser.Name);
                    }
                    else
                    {
                        _fuckedRegistry[sessionId] = 0;
                    }

                    var copAction = _actions[sessionId].FirstOrDefault(a => a.gameAction == GameAction.Checkup);
                    GameSessionMember? copTarget = null;
                    if (copAction != null)
                    {
                        copTarget = session.GameMembers.First(a => a.botUser.Id == copAction.Victim.Id);
                        if (_chekUpRegistry[sessionId].Where(a => a == copTarget.botUser.Id).Count() == 2)
                            KilledPlayers.Add(copTarget);
                    }

                    var mafiaAction = _actions[sessionId].FirstOrDefault(a => a.gameAction == GameAction.Killing);
                    GameSessionMember? mafiaTarget = null;
                    if (mafiaAction != null)
                    {
                        mafiaTarget = session.GameMembers.First(a => a.botUser.Id == mafiaAction.Victim.Id);

                        KilledPlayers.Add(mafiaTarget);

                        if (mafiaTarget.Role == GameRoles.Whore && whoreTarget != null) // && whoreTarget.botUser.Id != mafiaAction.ActionFromID) 
                            KilledPlayers.Add(whoreTarget);
                    }

                    var maniacAction = _actions[sessionId].FirstOrDefault(a => a.gameAction == GameAction.Murder);
                    GameSessionMember? maniacTarget = null;
                    if (maniacAction != null)
                    {
                        maniacTarget = session.GameMembers.First(a => a.botUser.Id == maniacAction.Victim.Id);

                        KilledPlayers.Add(maniacTarget);

                        if (maniacTarget.Role == GameRoles.Whore && whoreTarget != null) // && whoreTarget.botUser.Id != maniacAction.ActionFromID)
                            KilledPlayers.Add(whoreTarget);

                    }

                    // Whore action
                    if (whoreAction != null && !KilledPlayers.Any(a => a.botUser.Id == whoreAction.ActionFromID))
                    {
                        if (whoreTarget != null && KilledPlayers.Contains(whoreTarget))
                        {
                            KilledPlayers.Remove(whoreTarget);
                        }
                    }

                    // Doctor action
                    if(healingTarget != null)
                        KilledPlayers.Remove(healingTarget);

                    var newKilledPlayers = KilledPlayers.Distinct();

                    if (!newKilledPlayers.Any())
                        await _bot.SendMessageToRoom(sessionId, "Неймовірно. Усі живі");
                    else
                    {
                        foreach (var player in newKilledPlayers)
                            await KillGamer(sessionId, player);
                    }

                    await Task.Delay(5000);

                    // ensure this game is over
                    if (await IsGameOver(session, stopwatch))
                        break;

                        #region Day logic

                        await _bot.SendMessageToRoom(sessionId,
                            $@"День #{dayNumber} ☀️
<b>Гравці</b>: 
{GetMembersInfo(session, false, false)}

Час на обговорення: 60 сек.");

                    await Task.Delay(60000);
                    var gamerForLynch = await PublicLynchVote(session);

                    if (gamerForLynch != null)
                    {
                        await _bot.SendMessageToRoom(sessionId,
                            @$"Виганяємо {gamerForLynch.botUser.Name}...");
                        await KillGamer((int)session.Id, gamerForLynch);
                    }
                    else
                        await _bot.SendMessageToRoom((int)session.Id, "Сьогодні нікого не виганяємо");

                    #endregion

                    await Task.Delay(3000);
                    // ensure this game is over
                    if (await IsGameOver(session, stopwatch))
                        break;
                    dayNumber++;
                }
                
            }
            catch (Exception e)
            {
                await Console.Out.WriteLineAsync(e.Message); ;
            }

        }
        private List<GameSessionMember> GetAliveMembers(GameSession session) =>
            session.GameMembers.Where(gm => !gm.IsDead).ToList();
        private async Task<bool> IsGameOver(GameSession session, Stopwatch stopwatch)
        {
            var aliveMembers = GetAliveMembers(session);
            var citizensCount = aliveMembers.Count(g => g.Role != GameRoles.Mafia && g.Role != GameRoles.Maniac);
            var mafiaCount = aliveMembers.Count(g => g.Role == GameRoles.Mafia);
            var maniacCount = aliveMembers.Count(g => g.Role == GameRoles.Maniac);

            var isGameOver = false;
            var mafiaWins = false;
            var maniacWins = false;
            // case 1: mafia wins
            if (mafiaCount >= citizensCount && maniacCount == 0)
            {
                isGameOver = true;
                mafiaWins = true;
                foreach (var gameSessionMember in aliveMembers.Where(g => g.Role == GameRoles.Mafia))
                {
                    gameSessionMember.IsWin = true;
                }
            }
            // case 2: citizens wins
            else if (mafiaCount == 0 && maniacCount == 0)
            {
                isGameOver = true;
                foreach (var gameSessionMember in aliveMembers.Where(g => g.Role != GameRoles.Mafia && g.Role != GameRoles.Maniac))
                {
                    gameSessionMember.IsWin = true;
                }
            }
            else if(maniacCount >= citizensCount && mafiaCount == 0)
            {
                isGameOver = true;
                var gameSessionMember = aliveMembers.Where(g => g.Role != GameRoles.Mafia).First();
                gameSessionMember.IsWin = true;
                
            }
            else if (mafiaCount == 1  && maniacCount == 1 && citizensCount == 0)
            {
                isGameOver = true;

            }
            
            // game is not over
            if (!isGameOver) return false;

            stopwatch.Stop();
            var gameOverString =
                new StringBuilder(
                    $"<b>Гра закінчена!</b> 🏁\n\n");
            gameOverString.AppendLine($"<b>Переможці</b>: {(mafiaWins ? "Мафійозники 😈" : maniacWins? "Маніяк": "Мирні жителі 👤")}.");
            gameOverString.AppendLine(
                $"<b>Гра тривала</b>: {(int)Math.Round(stopwatch.Elapsed.TotalMinutes)} хвилин.\n");
            gameOverString.AppendLine("<b>Гравці:</b>");
            gameOverString.Append(GetMembersInfo(session, true, true));
            gameOverString.AppendLine("------");    
            gameOverString.AppendLine("Дякую всім за участь! :)");
            await _bot.SendMessageToRoom((int)session.Id, gameOverString.ToString());

            session.State = GameSessionState.GameOver;

            DbJSON.gameSessions.Remove(DbJSON.gameSessions.Where(a => a.Id == session.Id).First());

            return true;
        }
        public  string GetMembersInfo(GameSession session, bool roles = false, bool showRoleIfDead = false)
        {
            var sb = new StringBuilder();
            foreach (var member in session.GameMembers)
            {
                var infoStringBuilder = new StringBuilder();
                if (roles || (member.IsDead && showRoleIfDead))
                    infoStringBuilder.Append($":  <i>{GetRoleName(member.Role)}</i>");
                if (member.IsDead) infoStringBuilder.Append("  ☠️");
                if (member.IsWin) infoStringBuilder.Append("  🏆");

                sb.AppendLine($"- {member.botUser.Name}{infoStringBuilder}");
            }

            return sb.ToString();
        }

        public string GetRoleName(GameRoles role)
        {
            return role switch
            {
                GameRoles.Citizen => "Мирний житель",
                GameRoles.Doctor => "Доктор",
                GameRoles.Cop => "Комісар",
                GameRoles.Mafia => "Мафійозник",
                GameRoles.Maniac => "Маніяк",
                GameRoles.Whore => "Повія",
                _ => ""
            };
        }

        private async Task<GameSessionMember?> PublicLynchVote(GameSession session)
        {
            GameSessionMember skip = new GameSessionMember();
            skip.botUser = new BotUser { Id = -1, Name = "Skip" };
            //{
            //    botUser = new BotUser { Id = -1, Name = "Skip" }
            //};
            var aliveMembers = GetAliveMembers(session);
            aliveMembers.Add(skip);

            var votes = await _bot.CreateLynchVoteAndReceiveResults((int)session.Id, aliveMembers);
            // empty results
            if (!votes.Any())
                return null;

            var groupedVotes = (from vote in votes
                                group vote by vote.VoiceTarget.botUser.Id
                                into voteGroup
                                select new { key = voteGroup.Key, elements = voteGroup, voices = voteGroup.Count() }).ToList();
            var topVotes = (from voteItem in groupedVotes
                            where voteItem.voices == groupedVotes.Max(g => g.voices)
                            select voteItem.elements.First().VoiceTarget).ToList();
            // there are two top result, skip this Lynch... otherwise return it.
            return topVotes.Count > 1 ? null : topVotes[0].botUser.Id == -1 ? null: topVotes[0];
        }

        private async Task KillGamer(int sessionId, GameSessionMember actionTarget)
        {
            actionTarget.IsDead = true;
            await _bot.SendMessageToRoom(sessionId,
                $"Було вбито: <b>{actionTarget.botUser.Name}</b>");
        }


        public static void ResolveRoles(GameSession session)
        {
            var players = session.GameMembers;
            var playersCount = players.Count;

            //var enemyCount = (int)Math.Truncate(playersCount / (double)Constants.MinimalGamerCount);

            var listOfRoles = new List<GameRoles>
            {
                GameRoles.Mafia,
                GameRoles.Doctor
            };

            if (playersCount > 6)
            {
                listOfRoles.Add(GameRoles.Cop);
            }
            
            if(playersCount >= 7)
            {
                listOfRoles.Add(GameRoles.Mafia);
            }

            if(playersCount >= 9)
            {
                listOfRoles.Add(GameRoles.Maniac);
                listOfRoles.Add(GameRoles.Whore);
            }
            var countOfCitizen = playersCount - listOfRoles.Count;

            for (int i = 0; i < countOfCitizen; i++)
            {
                listOfRoles.Add(GameRoles.Citizen);
            }

            //var roles = Enumerable.Range(0, players.Count)
            //    .Select(CalculateRole)
            //    .ToList();

            var r = new Random();

            var reorderedPlayers = players.Select(x => new
            {
                Index = r.Next(),
                Item = x
            })
                .OrderBy(x => x.Index)
                .ToList();

            for (var i = 0; i < players.Count; i++)
                reorderedPlayers[i].Item.Role = listOfRoles[i];
        }
    }
}

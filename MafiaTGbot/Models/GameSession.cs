using MafiaTGbot.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MafiaTGbot.Models
{
    public class GameSession
    {
        public long Id { get; set; }
        public DateTime StartedOn { get; set; }
        public DateTime FinishedOn { get; set; }
        public List<GameSessionMember> GameMembers { get; set; } = new List<GameSessionMember>();
        public BotUser CreatedByGamerAccount { get; set; }
        public GameSessionState State { get; set; }

        //public int NumberOfNight { get; set; }

    }
}

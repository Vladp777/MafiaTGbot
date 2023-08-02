using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MafiaTGbot.Enums;
using Newtonsoft.Json;

namespace MafiaTGbot.Models
{
    public class GameSessionMember
    {
        public BotUser botUser { get; set; }

        [JsonIgnore]
        public GameSession gameSession { get; set; }
        public bool IsDead { get; set; }
        public bool IsWin { get; set; }
        public GameRoles Role { get; set; }


        public override bool Equals(object? obj)
        {
            if (obj == null) 
                throw new ArgumentNullException(nameof(obj));

            var a = obj as GameSessionMember;
            if (a == null)
                return false;
            else if(this.botUser.Id == a.botUser.Id)
                return true;
            else
                return false;
            
        }
    }
}

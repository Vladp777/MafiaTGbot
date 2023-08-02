using MafiaTGbot.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MafiaTGbot.Models
{
    internal class UserAction
    {
        public int ActionFromID { get; set; }
        public GameAction gameAction { get; set; }

        public BotUser Victim {  get; set; }
    }
}

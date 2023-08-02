using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MafiaTGbot.Models
{
    public struct VoteDescriptor
    {
        public GameSessionMember VoiceOwner { get; set; }
        public GameSessionMember VoiceTarget { get; set; }
    }
}

﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MafiaTGbot
{
    public class TelegramVote
    {
        public string Text { get; }

        /// <summary>
        /// Vote allowed predicate
        /// </summary>
        public Predicate<(int userId, int voice)>? VoteAllowedPredicate { get; }

        public (int uiName, string internalName)[] Variants { get; }
        public Dictionary<int, string> AllowedToPassVoteUsersIds { get; }
        private List<TelegramVoiceItem> _voices;


        public int? MessageId => _message;
        private int? _message;

        public TelegramVote((int uiName, string internalName)[] variants, string text,
            Dictionary<int, string> allowedToPassVoteUsersIds,
            Predicate<(int userId, int voice)>? voteAllowedPredicate = null)
        {
            Variants = variants;
            Text = text;
            AllowedToPassVoteUsersIds = allowedToPassVoteUsersIds;
            VoteAllowedPredicate = voteAllowedPredicate;
            _message = null;
            _voices = new List<TelegramVoiceItem>();
        }

        /// <summary>
        /// Set vote's message id
        /// </summary>
        /// <param name="messageId"></param>
        public void SetMessageId(int messageId)
        {
            _message = messageId;
        }

        public void AddOrUpdateVote(string userId, string voice)
        {
            if (userId == null) throw new ArgumentNullException(nameof(userId));
            if (voice == null) throw new ArgumentNullException(nameof(voice));
            lock (_voices)
            {
                var voiceItem = _voices.FirstOrDefault(v => v.UserId == userId);
                // not voice, create
                if (voiceItem.Voice == null && voiceItem.UserId == null)
                {
                    voiceItem = new TelegramVoiceItem { UserId = userId, Voice = voice };
                    _voices.Add(voiceItem);
                    return;
                }

                // nothing changed, just skip
                if (voiceItem.Voice == voice)
                    return;
                _voices.RemoveAll(p => p.Voice == voiceItem.Voice && p.UserId == voiceItem.UserId);
                voiceItem.Voice = voice;
                _voices.Add(voiceItem);
            }
        }

        public TelegramVoiceItem[] GetVoices()
        {
            lock (_voices)
            {
                return _voices.ToArray();
            }
        }
    }
}

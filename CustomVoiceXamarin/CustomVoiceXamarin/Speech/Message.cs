﻿namespace CustomVoiceXamarin
{
    public class Message
    {
        public string Text { get; set; }
        public MemberData MemberData{ get; set; }
        public bool BelongsToCurrentUser { get; set; }
    }
}
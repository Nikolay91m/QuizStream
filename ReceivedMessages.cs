using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QuizStream
{
    class ReceivedMessage
    {
        public string message;
        public string user;
        public string youtubeID;
        public ReceivedMessage(string Message, string YoutubeID, string User)
        {
            message = Message;
            user = User;
            youtubeID = YoutubeID;
            for (int i = 0; i<33; i++)
            {
                message = message.Replace(QuizStream.Game1.ruscaps[i], QuizStream.Game1.rusnoncaps[i]);
            }
        }
    }
}

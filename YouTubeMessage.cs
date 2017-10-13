using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace YoutubeTest
{
    public class YouTubeMessage
    {
        public YouTubeMessage(string id, string displayName, string message)
        {
            this.id = id;
            this.displayName = displayName;
            this.message = message;
        }
        public string id { get; set; }
        public string displayName { get; set; }
        public string message { get; set; }
    }
}

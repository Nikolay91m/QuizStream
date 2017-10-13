using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QuizStream
{
    public class Member
    {
        public string member;
        public string youtubeID;
        public int score;
        public int id;
        public int tries;
        public Member(string Member, string YoutubeID, int Id)
        {
            member = Member;
            score = 0;
            id = Id;
            tries = 0;
        }
        public Member(string Member, string YoutubeID, int Id, int Score)
        {
            member = Member;
            youtubeID = YoutubeID;
            score = Score;
            id = Id;
            tries = 0;
        }
        public void AddScore(int Score)
        {
            score += Score;
        }
        public override string ToString()
        {
            return "Member No " + id + ": " + member + ", score: " + score.ToString() + "; tries = " + tries.ToString();
        }
    }
}

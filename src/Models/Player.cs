using System.Drawing;

namespace Models
{
    public class Player
    {
        public Point Position { get; set; }
        public TeamType Team { get; set; }
        public bool HasBall { get; set; }
        public bool IsOffside { get; set; }
        public bool IsAnnotated { get; set; }

        public Player(Point position, TeamType team)
        {
            Position = position;
            Team = team;
            HasBall = false;
            IsOffside = false;
            IsAnnotated = false;
        }
    }
}
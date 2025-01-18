using System.Drawing;

namespace Models
{
    public class Player(Point position, TeamType team)
    {
        public Point Position { get; set; } = position;
        public TeamType Team { get; set; } = team;
        public bool HasBall { get; set; } = false;
        public bool IsOffside { get; set; } = false;
        public bool IsAnnotated { get; set; } = false;
    }
}
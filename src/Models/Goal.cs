using System.Drawing;

namespace Models
{
    public class Goal
    {
        public DateTime Timestamp { get; set; }
        public TeamType ScoringTeam { get; set; }
        public Position BallPosition { get; set; }

        public Goal(TeamType scoringTeam, Point ballPosition, DateTime timestamp)
        {
            Timestamp = timestamp;
            ScoringTeam = scoringTeam;
            BallPosition = new Position { X = ballPosition.X, Y = ballPosition.Y };
        }
    }

    public class Position
    {
        public int X { get; set; }
        public int Y { get; set; }
    }
}
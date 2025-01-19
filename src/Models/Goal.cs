using System.Drawing;

namespace Models
{
    public class Goal
    {
        public DateTime? Timestamp { get; set; }
        public TeamType? ScoringTeam { get; set; }
        public Position? BallPosition { get; set; }

        // Add parameterless constructor for JSON deserialization
        public Goal() { }

        // Keep the parameterized constructor for normal instantiation
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
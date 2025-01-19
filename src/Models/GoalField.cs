using System.Drawing;

namespace Models
{
    public class GoalField(Rectangle bounds, TeamType team)
    {
        public Rectangle Bounds { get; } = bounds;
        public TeamType Team { get; } = team;
        public Point Center => new(
            Bounds.X + Bounds.Width / 2,
            Bounds.Y + Bounds.Height / 2
        );
    }
}
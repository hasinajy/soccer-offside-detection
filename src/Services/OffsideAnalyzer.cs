using System.Drawing;
using Models;

namespace Services
{
    public class OffsideAnalyzer
    {
        public void AnalyzeOffside(List<Player> players, Point ballPosition)
        {
            // Find ball holder
            var ballHolder = FindBallHolder(players, ballPosition);
            if (ballHolder == null) return;

            // Find last defender (excluding goalkeeper)
            var defenders = players.Where(p => p.Team != ballHolder.Team).OrderBy(p => p.Position.Y).Skip(1).ToList();
            if (!defenders.Any()) return;

            var lastDefenderY = defenders.First().Position.Y;

            // Check offside for all attacking players
            foreach (var player in players.Where(p => p.Team == ballHolder.Team && p != ballHolder))
            {
                // Player is offside if they're closer to the goal line than the last defender
                player.IsOffside = player.Position.Y < lastDefenderY;
            }
        }

        private Player FindBallHolder(List<Player> players, Point ballPosition)
        {
            return players.OrderBy(p =>
                Math.Sqrt(
                    Math.Pow(p.Position.X - ballPosition.X, 2) +
                    Math.Pow(p.Position.Y - ballPosition.Y, 2)
                )
            ).FirstOrDefault();
        }
    }
}
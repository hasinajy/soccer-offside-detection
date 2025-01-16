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
            var defenders = players.Where(p => p.Team != ballHolder.Team).ToList();
            if (!defenders.Any()) return;

            // Sort defenders based on attacking direction
            var isAttackingDownward = ballHolder.Team == TeamType.TeamA; // Red team attacks downward
            defenders = !isAttackingDownward
                ? defenders.OrderBy(p => p.Position.Y).Skip(1).ToList()    // For red team (top to bottom)
                : defenders.OrderByDescending(p => p.Position.Y).Skip(1).ToList(); // For blue team (bottom to top)

            if (!defenders.Any()) return;

            var lastDefenderY = defenders.First().Position.Y;
            defenders.First().IsOffside = true;

            // Check offside for all attacking players
            foreach (var player in players.Where(p => p.Team == ballHolder.Team && p != ballHolder))
            {
                if (isAttackingDownward)
                {
                    // Red team (top) attacking downward: offside if player is BELOW last defender
                    player.IsOffside = player.Position.Y > lastDefenderY;
                }
                else
                {
                    // Blue team (bottom) attacking upward: offside if player is ABOVE last defender
                    player.IsOffside = player.Position.Y < lastDefenderY;
                }
            }

            foreach (var player in players.Where(p => p.Team == ballHolder.Team))
            {
                if (isAttackingDownward && player.Position.Y > ballHolder.Position.Y)
                {
                    // Red team (top) attacking downward
                    player.IsAnnotated = true;
                }
                else if (!isAttackingDownward && player.Position.Y < ballHolder.Position.Y)
                {
                    // Blue team (bottom) attacking upward
                    player.IsAnnotated = true;
                }
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
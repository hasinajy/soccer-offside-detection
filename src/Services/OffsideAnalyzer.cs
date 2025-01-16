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

            // Detect attacking direction
            // Assume that the team attacking downward has a greater average Y-coordinate for their players
            var attackingDirectionY = players
                .Where(p => p.Team == ballHolder.Team)
                .Select(p => p.Position.Y)
                .Average();
            var defendingDirectionY = players
                .Where(p => p.Team != ballHolder.Team)
                .Select(p => p.Position.Y)
                .Average();
            var isAttackingDownward = attackingDirectionY < defendingDirectionY;

            // Find last defender (excluding goalkeeper)
            var defenders = players.Where(p => p.Team != ballHolder.Team).ToList();
            if (!defenders.Any()) return;

            // Sort defenders based on attacking direction
            defenders = isAttackingDownward
                ? defenders.OrderByDescending(p => p.Position.Y).Skip(1).ToList() // Attacking downward: bottom to top
                : defenders.OrderBy(p => p.Position.Y).Skip(1).ToList();          // Attacking upward: top to bottom

            if (!defenders.Any()) return;

            var lastDefenderY = defenders.First().Position.Y;
            defenders.First().IsOffside = true;

            // Check offside for all attacking players
            foreach (var player in players.Where(p => p.Team == ballHolder.Team && p != ballHolder))
            {
                if (isAttackingDownward)
                {
                    // Attacking downward: offside if player is BELOW last defender
                    player.IsOffside = player.Position.Y > lastDefenderY;
                }
                else
                {
                    // Attacking upward: offside if player is ABOVE last defender
                    player.IsOffside = player.Position.Y < lastDefenderY;
                }
            }

            // Annotate attacking players (optional logic for highlighting them based on ball holder position)
            foreach (var player in players.Where(p => p.Team == ballHolder.Team))
            {
                if (isAttackingDownward && player.Position.Y > ballHolder.Position.Y)
                {
                    // Attacking downward
                    player.IsAnnotated = true;
                }
                else if (!isAttackingDownward && player.Position.Y < ballHolder.Position.Y)
                {
                    // Attacking upward
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
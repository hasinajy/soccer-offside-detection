using System.Drawing;
using Models;

namespace Services
{
    public class OffsideAnalyzer
    {
        // Move the enum inside the class and make it public
        public enum AttackDirection
        {
            Upward,
            Downward
        }

        public void AnalyzeOffside(List<Player> players, Point ballPosition)
        {
            var ballHolder = FindBallHolder(players, ballPosition);
            if (ballHolder == null) return;

            var attackingTeam = players.Where(p => p.Team == ballHolder.Team).ToList();
            var defendingTeam = players.Where(p => p.Team != ballHolder.Team).ToList();

            if (!defendingTeam.Any()) return;

            var attackDirection = DetermineAttackDirection(attackingTeam, defendingTeam);
            var lastDefender = FindLastDefender(defendingTeam, attackDirection);

            MarkOffsidePlayers(attackingTeam, lastDefender, ballHolder, attackDirection);
            AnnotateAttackingPlayers(attackingTeam, ballPosition, attackDirection);
        }

        private static Player? FindBallHolder(List<Player> players, Point ballPosition)
        {
            return players
                .OrderBy(p => CalculateDistance(p.Position, ballPosition))
                .FirstOrDefault();
        }

        private static double CalculateDistance(Point p1, Point p2)
        {
            return Math.Sqrt(
                Math.Pow(p1.X - p2.X, 2) +
                Math.Pow(p1.Y - p2.Y, 2)
            );
        }

        private static AttackDirection DetermineAttackDirection(List<Player> attackingTeam, List<Player> defendingTeam)
        {
            var attackingAvgY = attackingTeam.Average(p => p.Position.Y);
            var defendingAvgY = defendingTeam.Average(p => p.Position.Y);

            return attackingAvgY < defendingAvgY
                ? AttackDirection.Downward
                : AttackDirection.Upward;
        }

        private static Player FindLastDefender(List<Player> defenders, AttackDirection direction)
        {
            // Skip goalkeeper (first player in the sorted list)
            return direction == AttackDirection.Downward
                ? defenders.OrderByDescending(p => p.Position.Y).Skip(1).First()
                : defenders.OrderBy(p => p.Position.Y).Skip(1).First();
        }

        private static void MarkOffsidePlayers(
            List<Player> attackingTeam,
            Player lastDefender,
            Player ballHolder,
            AttackDirection direction)
        {
            foreach (var player in attackingTeam.Where(p => p != ballHolder))
            {
                player.IsOffside = direction == AttackDirection.Downward
                    ? player.Position.Y > lastDefender.Position.Y  // Attacking downward
                    : player.Position.Y < lastDefender.Position.Y; // Attacking upward
            }
        }

        private static void AnnotateAttackingPlayers(
            List<Player> attackingTeam,
            Point ballPosition,
            AttackDirection direction)
        {
            foreach (var player in attackingTeam)
            {
                player.IsAnnotated = direction == AttackDirection.Downward
                    ? player.Position.Y > ballPosition.Y  // Attacking downward
                    : player.Position.Y < ballPosition.Y; // Attacking upward
            }
        }
    }
}
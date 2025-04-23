using System;
using System.Collections.Generic;
using System.Linq;

namespace SoccerScheduler.Models
{
    public class ScheduleStatistics
    {
        public List<TeamStatistics> TeamStats { get; private set; }
        public List<RoundStatistics> RoundStats { get; private set; }
        public double TotalDistance { get; private set; }
        public int TopGamesCount { get; private set; }
        public bool IsBalanced { get; private set; }

        public ScheduleStatistics(List<Game> games, List<Team> teams)
        {
            TeamStats = new List<TeamStatistics>();
            RoundStats = new List<RoundStatistics>();

            CalculateStatistics(games, teams);
        }

        private void CalculateStatistics(List<Game> games, List<Team> teams)
        {
            foreach (var team in teams)
            {
                var homeGames = games.Count(g => g.HomeTeam == team);
                var awayGames = games.Count(g => g.AwayTeam == team);

                var teamGames = games.Where(g => g.HomeTeam == team || g.AwayTeam == team).OrderBy(g => g.Round).ToList();

                double totalDistance = 0;
                string lastLocation = team.City;

                foreach (var game in teamGames)
                {
                    string currentLocation;

                    if (game.HomeTeam == team)
                    {
                        currentLocation = team.City;
                    }
                    else
                    {
                        currentLocation = game.HomeTeam.City;
                        totalDistance += CalculateDistance(lastLocation, currentLocation);
                    }

                    lastLocation = currentLocation;
                }

                totalDistance += CalculateDistance(lastLocation, team.City);

                TeamStats.Add(new TeamStatistics
                {
                    TeamName = team.Name,
                    HomeGames = homeGames,
                    AwayGames = awayGames,
                    TotalDistance = totalDistance,
                    ConsecutiveHomeGames = CalculateMaxConsecutive(teamGames, team, true),
                    ConsecutiveAwayGames = CalculateMaxConsecutive(teamGames, team, false)
                });
            }

            foreach (var round in games.Select(g => g.Round).Distinct().OrderBy(r => r))
            {
                var roundGames = games.Where(g => g.Round == round).ToList();

                RoundStats.Add(new RoundStatistics
                {
                    RoundNumber = round,
                    GamesCount = roundGames.Count,
                    TopGamesCount = roundGames.Count(g => g.GameType == GameType.TopGame),
                    TotalDistance = roundGames.Sum(g => g.Distance)
                });
            }

            TotalDistance = games.Sum(g => g.Distance);
            TopGamesCount = games.Count(g => g.GameType == GameType.TopGame);

            bool homeAwayBalanced = TeamStats.All(t => Math.Abs(t.HomeGames - t.AwayGames) <= 1);
            IsBalanced = homeAwayBalanced;
        }

        private double CalculateDistance(string fromCity, string toCity)
        {
            if (fromCity == toCity)
                return 0;

            return 100; 
        }

        private int CalculateMaxConsecutive(List<Game> teamGames, Team team, bool isHome)
        {
            int maxConsecutive = 0;
            int currentConsecutive = 0;

            foreach (var game in teamGames)
            {
                if ((isHome && game.HomeTeam == team) || (!isHome && game.AwayTeam == team))
                {
                    currentConsecutive++;
                    maxConsecutive = Math.Max(maxConsecutive, currentConsecutive);
                }
                else
                {
                    currentConsecutive = 0;
                }
            }

            return maxConsecutive;
        }
    }

    public class TeamStatistics
    {
        public string TeamName { get; set; }
        public int HomeGames { get; set; }
        public int AwayGames { get; set; }
        public double TotalDistance { get; set; }
        public int ConsecutiveHomeGames { get; set; }
        public int ConsecutiveAwayGames { get; set; }
    }

    public class RoundStatistics
    {
        public int RoundNumber { get; set; }
        public int GamesCount { get; set; }
        public int TopGamesCount { get; set; }
        public double TotalDistance { get; set; }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SoccerScheduler.Models
{
    public class ScheduleGenerator
    {
        private List<Team> _teams;
        private DistanceMatrix _distanceMatrix;
        private int _maxHomeGamesInRow;
        private int _maxAwayGamesInRow;
        private int _topGamesPerRound;
        private bool _optimizeForDistance;
        private int _roundsToPlay;

        public ScheduleGenerator(
            List<Team> teams,
            DistanceMatrix distanceMatrix,
            int maxHomeGamesInRow,
            int maxAwayGamesInRow,
            int topGamesPerRound,
            bool optimizeForDistance,
            int roundsToPlay = 0)
        {
            _teams = teams;
            _distanceMatrix = distanceMatrix;
            _maxHomeGamesInRow = maxHomeGamesInRow;
            _maxAwayGamesInRow = maxAwayGamesInRow;
            _topGamesPerRound = topGamesPerRound;
            _optimizeForDistance = optimizeForDistance;
            _roundsToPlay = roundsToPlay <= 0 ? (teams.Count - 1) * 2 : roundsToPlay;
        }

        public async Task<List<Game>> GenerateScheduleAsync()
        {
            return await Task.Run(() => GenerateSchedule());
        }

        private List<Game> GenerateSchedule()
        {
            if (_teams.Count < 2)
                return new List<Game>();

            List<Team> teamsToSchedule = new List<Team>(_teams);
            if (teamsToSchedule.Count % 2 != 0)
            {
                teamsToSchedule.Add(new Team("Bye", "N/A", TeamLevel.Средний));
            }

            List<Game> schedule = new List<Game>();

            int numTeams = teamsToSchedule.Count;
            int numRounds = numTeams - 1;

            Team[] circle = new Team[numTeams];
            for (int i = 0; i < numTeams; i++)
            {
                circle[i] = teamsToSchedule[i];
            }

            for (int round = 0; round < numRounds * 2 && round < _roundsToPlay; round++)
            {
                List<Game> roundGames = new List<Game>();

                for (int i = 0; i < numTeams / 2; i++)
                {
                    Team home, away;

                    if (round < numRounds)
                    {
                        home = circle[i];
                        away = circle[numTeams - 1 - i];
                    }
                    else
                    {
                        away = circle[i];
                        home = circle[numTeams - 1 - i];
                    }

                    if (home.Name != "Bye" && away.Name != "Bye")
                    {
                        Game game = new Game(round + 1, home, away);
                        game.Location = home.City;

                        game.Distance = _distanceMatrix.GetDistance(away.City, home.City);

                        roundGames.Add(game);
                    }
                }

                Team temp = circle[1];
                for (int i = 1; i < numTeams - 1; i++)
                {
                    circle[i] = circle[i + 1];
                }
                circle[numTeams - 1] = temp;

                OptimizeRound(roundGames);

                schedule.AddRange(roundGames);
            }

            BalanceSchedule(schedule);

            return schedule;
        }

        private void OptimizeRound(List<Game> roundGames)
        {
            int topGamesCount = roundGames.Count(g => g.GameType == GameType.TopGame);

            if (topGamesCount > _topGamesPerRound)
            {
                roundGames.Sort((a, b) =>
                {
                    if (a.GameType == GameType.TopGame && b.GameType != GameType.TopGame)
                        return -1;
                    if (a.GameType != GameType.TopGame && b.GameType == GameType.TopGame)
                        return 1;
                    return 0;
                });

                for (int i = 0; i < topGamesCount - _topGamesPerRound; i++)
                {
                    if (i < roundGames.Count)
                    {
                        var game = roundGames[i];

                        var nonFavoriteTeam = _teams.FirstOrDefault(t => t.Level != TeamLevel.Фаворит);
                        if (nonFavoriteTeam != null)
                        {
                            var originalTeam = game.AwayTeam;
                            game.AwayTeam = nonFavoriteTeam;

                        }
                    }
                }
            }
        }

        private void BalanceSchedule(List<Game> schedule)
        {
            var teamGames = new Dictionary<Team, List<Game>>();

            foreach (var team in _teams)
            {
                teamGames[team] = new List<Game>();
            }

            foreach (var game in schedule)
            {
                if (teamGames.ContainsKey(game.HomeTeam))
                    teamGames[game.HomeTeam].Add(game);

                if (teamGames.ContainsKey(game.AwayTeam))
                    teamGames[game.AwayTeam].Add(game);
            }

            foreach (var team in _teams)
            {
                var games = teamGames[team].OrderBy(g => g.Round).ToList();

                for (int i = 0; i < games.Count - _maxHomeGamesInRow; i++)
                {
                    bool tooManyHomeGames = true;
                    for (int j = 0; j < _maxHomeGamesInRow + 1; j++)
                    {
                        if (i + j >= games.Count || games[i + j].HomeTeam != team)
                        {
                            tooManyHomeGames = false;
                            break;
                        }
                    }

                    if (tooManyHomeGames)
                    {
                        SwapHomeAway(games[i + _maxHomeGamesInRow]);
                    }

                    bool tooManyAwayGames = true;
                    for (int j = 0; j < _maxAwayGamesInRow + 1; j++)
                    {
                        if (i + j >= games.Count || games[i + j].AwayTeam != team)
                        {
                            tooManyAwayGames = false;
                            break;
                        }
                    }

                    if (tooManyAwayGames)
                    {
                        SwapHomeAway(games[i + _maxAwayGamesInRow]);
                    }
                }
            }

            if (_optimizeForDistance)
            {
                OptimizeForDistance(schedule);
            }
        }

        private void SwapHomeAway(Game game)
        {
            var temp = game.HomeTeam;
            game.HomeTeam = game.AwayTeam;
            game.AwayTeam = temp;
            game.Location = game.HomeTeam.City;
            game.Distance = _distanceMatrix.GetDistance(game.AwayTeam.City, game.HomeTeam.City);
        }

        private void OptimizeForDistance(List<Game> schedule)
        {
            // Group games by round
            var gamesByRound = schedule.GroupBy(g => g.Round).ToDictionary(g => g.Key, g => g.ToList());

            foreach (var round in gamesByRound.Keys.OrderBy(r => r))
            {
                var roundGames = gamesByRound[round];

                foreach (var team in _teams)
                {
                    var prevRoundGame = schedule.LastOrDefault(g =>
                        g.Round < round && (g.HomeTeam == team || g.AwayTeam == team));

                    var nextRoundGame = schedule.FirstOrDefault(g =>
                        g.Round > round && (g.HomeTeam == team || g.AwayTeam == team));

                    var currentGame = roundGames.FirstOrDefault(g =>
                        g.HomeTeam == team || g.AwayTeam == team);

                    if (currentGame != null)
                    {
                        bool isHome = currentGame.HomeTeam == team;
                        string prevLocation = prevRoundGame != null ?
                            (prevRoundGame.HomeTeam == team ? prevRoundGame.Location : prevRoundGame.AwayTeam.City) : team.City;

                        string nextLocation = nextRoundGame != null ?
                            (nextRoundGame.HomeTeam == team ? nextRoundGame.Location : nextRoundGame.AwayTeam.City) : team.City;

                        double currentDistance = _distanceMatrix.GetDistance(prevLocation, isHome ? team.City : currentGame.HomeTeam.City) +
                                                _distanceMatrix.GetDistance(isHome ? team.City : currentGame.HomeTeam.City, nextLocation);

                        double alternativeDistance = _distanceMatrix.GetDistance(prevLocation, isHome ? currentGame.AwayTeam.City : team.City) +
                                                    _distanceMatrix.GetDistance(isHome ? currentGame.AwayTeam.City : team.City, nextLocation);

                        if (alternativeDistance < currentDistance)
                        {
                            var otherTeam = isHome ? currentGame.AwayTeam : currentGame.HomeTeam;

                            var otherTeamPrevGame = schedule.LastOrDefault(g =>
                                g.Round < round && (g.HomeTeam == otherTeam || g.AwayTeam == otherTeam));

                            var otherTeamNextGame = schedule.FirstOrDefault(g =>
                                g.Round > round && (g.HomeTeam == otherTeam || g.AwayTeam == otherTeam));

                            string otherPrevLocation = otherTeamPrevGame != null ?
                                (otherTeamPrevGame.HomeTeam == otherTeam ? otherTeamPrevGame.Location : otherTeamPrevGame.AwayTeam.City) : otherTeam.City;

                            string otherNextLocation = otherTeamNextGame != null ?
                                (otherTeamNextGame.HomeTeam == otherTeam ? otherTeamNextGame.Location : otherTeamNextGame.AwayTeam.City) : otherTeam.City;

                            double otherCurrentDistance = _distanceMatrix.GetDistance(otherPrevLocation, !isHome ? otherTeam.City : currentGame.HomeTeam.City) +
                                                        _distanceMatrix.GetDistance(!isHome ? otherTeam.City : currentGame.HomeTeam.City, otherNextLocation);

                            double otherAlternativeDistance = _distanceMatrix.GetDistance(otherPrevLocation, !isHome ? currentGame.AwayTeam.City : otherTeam.City) +
                                                            _distanceMatrix.GetDistance(!isHome ? currentGame.AwayTeam.City : otherTeam.City, otherNextLocation);

                            if (alternativeDistance + otherAlternativeDistance < currentDistance + otherCurrentDistance)
                            {
                                SwapHomeAway(currentGame);
                            }
                        }
                    }
                }
            }
        }
    }
}
 

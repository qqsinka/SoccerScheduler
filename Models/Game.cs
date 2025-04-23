using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SoccerScheduler.Models
{
    public class Game : INotifyPropertyChanged
    {
        private int _round;
        private Team _homeTeam;
        private Team _awayTeam;
        private string _location;
        private GameType _gameType;
        private double _distance;

        public int Round
        {
            get => _round;
            set
            {
                if (_round != value)
                {
                    _round = value;
                    OnPropertyChanged();
                }
            }
        }

        public Team HomeTeam
        {
            get => _homeTeam;
            set
            {
                if (_homeTeam != value)
                {
                    _homeTeam = value;
                    UpdateGameType();
                    OnPropertyChanged();
                }
            }
        }

        public Team AwayTeam
        {
            get => _awayTeam;
            set
            {
                if (_awayTeam != value)
                {
                    _awayTeam = value;
                    UpdateGameType();
                    OnPropertyChanged();
                }
            }
        }

        public string Location
        {
            get => _location;
            set
            {
                if (_location != value)
                {
                    _location = value;
                    OnPropertyChanged();
                }
            }
        }

        public GameType GameType
        {
            get => _gameType;
            private set
            {
                if (_gameType != value)
                {
                    _gameType = value;
                    OnPropertyChanged();
                }
            }
        }

        public double Distance
        {
            get => _distance;
            set
            {
                if (_distance != value)
                {
                    _distance = value;
                    OnPropertyChanged();
                }
            }
        }

        public Game(int round, Team homeTeam, Team awayTeam)
        {
            _round = round;
            _homeTeam = homeTeam;
            _awayTeam = awayTeam;
            _location = homeTeam?.City ?? "";
            UpdateGameType();
            _distance = 0;
        }

        public Game() : this(0, null, null)
        {
        }

        private void UpdateGameType()
        {
            if (_homeTeam == null || _awayTeam == null)
            {
                GameType = GameType.Regular;
                return;
            }

            if (_homeTeam.Level == TeamLevel.Фаворит && _awayTeam.Level == TeamLevel.Фаворит)
                GameType = GameType.TopGame;
            else if (_homeTeam.Level == TeamLevel.Аутсайдер && _awayTeam.Level == TeamLevel.Аутсайдер)
                GameType = GameType.UnderdogGame;
            else
                GameType = GameType.Regular;
        }

        public override string ToString()
        {
            return $"Тур {Round}: {HomeTeam?.Name ?? "будет определено"} vs {AwayTeam?.Name ?? "будет определено"} В {Location}";
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public enum GameType
    {
        TopGame,
        Regular,
        UnderdogGame
    }
}
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SoccerScheduler.Models
{
    public class Team : INotifyPropertyChanged
    {
        private string _name;
        private string _city;
        private TeamLevel _level;

        public string Name
        {
            get => _name;
            set
            {
                if (_name != value)
                {
                    _name = value;
                    OnPropertyChanged();
                }
            }
        }

        public string City
        {
            get => _city;
            set
            {
                if (_city != value)
                {
                    _city = value;
                    OnPropertyChanged();
                }
            }
        }

        public TeamLevel Level
        {
            get => _level;
            set
            {
                if (_level != value)
                {
                    _level = value;
                    OnPropertyChanged();
                }
            }
        }

        public Team(string name, string city, TeamLevel level)
        {
            _name = name;
            _city = city;
            _level = level;
        }

        public Team() : this("", "", TeamLevel.Средний)
        {
        }

        public override string ToString()
        {
            return $"{Name} ({City})";
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public enum TeamLevel
    {
        Фаворит,
        Средний,
        Аутсайдер
    }
}
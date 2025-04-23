using System;
using System.Collections.Generic;
using System.Linq;

namespace SoccerScheduler.Models
{
    public class DistanceMatrix
    {
        private Dictionary<string, Dictionary<string, double>> _distances;

        public DistanceMatrix()
        {
            _distances = new Dictionary<string, Dictionary<string, double>>();
        }

        public void AddDistance(string cityA, string cityB, double distance)
        {
            if (!_distances.ContainsKey(cityA))
                _distances[cityA] = new Dictionary<string, double>();

            if (!_distances.ContainsKey(cityB))
                _distances[cityB] = new Dictionary<string, double>();

            _distances[cityA][cityB] = distance;
            _distances[cityB][cityA] = distance;

            _distances[cityA][cityA] = 0;
            _distances[cityB][cityB] = 0;
        }

        public double GetDistance(string cityA, string cityB)
        {
            if (string.IsNullOrEmpty(cityA) || string.IsNullOrEmpty(cityB))
                return 0;

            if (cityA == cityB)
                return 0;

            if (_distances.ContainsKey(cityA) && _distances[cityA].ContainsKey(cityB))
                return _distances[cityA][cityB];

            return 0; 
        }

        public bool HasDistance(string cityA, string cityB)
        {
            return _distances.ContainsKey(cityA) && _distances[cityA].ContainsKey(cityB);
        }

        public List<string> GetAllCities()
        {
            return _distances.Keys.ToList();
        }

        public void GenerateRandomDistances(List<string> cities)
        {
            Random random = new Random();

            foreach (var cityA in cities)
            {
                if (!_distances.ContainsKey(cityA))
                    _distances[cityA] = new Dictionary<string, double>();

                _distances[cityA][cityA] = 0;

                foreach (var cityB in cities)
                {
                    if (cityA != cityB)
                    {
                        if (!HasDistance(cityA, cityB))
                        {
                            double distance = random.Next(10, 501);
                            AddDistance(cityA, cityB, distance);
                        }
                    }
                }
            }
        }

        public Dictionary<string, Dictionary<string, double>> GetDistances()
        {
            return _distances;
        }

        public void SetDistances(Dictionary<string, Dictionary<string, double>> distances)
        {
            _distances = distances;
        }

        public void Clear()
        {
            _distances.Clear();
        }
    }
}
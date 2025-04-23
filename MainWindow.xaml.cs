using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SoccerScheduler.Models;
using SoccerScheduler.Services;

namespace SoccerScheduler
{
    public partial class MainWindow : Window
    {
        private readonly ObservableCollection<Team> _teams;
        private readonly ObservableCollection<Game> _schedule;
        private readonly ObservableCollection<TeamStatistics> _teamStats;
        private readonly ObservableCollection<RoundStatistics> _roundStats;
        private readonly DistanceMatrix _distanceMatrix;
        private readonly DataService _dataService;

        public MainWindow()
        {
            InitializeComponent();

            // Initialize collections
            _teams = new ObservableCollection<Team>();
            _schedule = new ObservableCollection<Game>();
            _teamStats = new ObservableCollection<TeamStatistics>();
            _roundStats = new ObservableCollection<RoundStatistics>();
            _distanceMatrix = new DistanceMatrix();
            _dataService = new DataService();

            // Set data contexts for data grids
            TeamsDataGrid.ItemsSource = _teams;
            ScheduleDataGrid.ItemsSource = _schedule;
            TeamStatsDataGrid.ItemsSource = _teamStats;
            RoundStatsDataGrid.ItemsSource = _roundStats;

            // Select default values
            TeamLevelComboBox.SelectedIndex = 0;
            OptimizationPriorityComboBox.SelectedIndex = 0;

            // Setup distance matrix grid
            InitializeDistanceMatrixGrid();
        }

        private void InitializeDistanceMatrixGrid()
        {
            RefreshDistanceMatrix();
        }

        private void RefreshDistanceMatrix()
        {
            DistanceMatrixDataGrid.Columns.Clear();
            DistanceMatrixDataGrid.Items.Clear();

            if (_teams.Count == 0)
                return;

            // Add city column
            var cityColumn = new DataGridTextColumn
            {
                Header = "Город",
                Binding = new System.Windows.Data.Binding("[0]"),
                IsReadOnly = true
            };
            DistanceMatrixDataGrid.Columns.Add(cityColumn);

            for (int i = 0; i < _teams.Count; i++)
            {
                var cityName = _teams[i].City;
                var column = new DataGridTextColumn
                {
                    Header = cityName,
                    Binding = new System.Windows.Data.Binding($"[{i + 1}]")
                };
                DistanceMatrixDataGrid.Columns.Add(column);
            }

            for (int i = 0; i < _teams.Count; i++)
            {
                var row = new object[_teams.Count + 1];
                row[0] = _teams[i].City;

                for (int j = 0; j < _teams.Count; j++)
                {
                    row[j + 1] = _distanceMatrix.GetDistance(_teams[i].City, _teams[j].City);
                }

                DistanceMatrixDataGrid.Items.Add(row);
            }
        }

        private void AddTeamButton_Click(object sender, RoutedEventArgs e)
        {
            string name = TeamNameTextBox.Text.Trim();
            string city = TeamCityTextBox.Text.Trim();

            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(city))
            {
                MessageBox.Show("Пожалуйста, введите название команды и город.", "Ошибка ввода", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (_teams.Any(t => t.Name == name))
            {
                MessageBox.Show("Команда с таким названием уже существует.", "Дублирующаяся команда", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            TeamLevel level = (TeamLevel)Enum.Parse(typeof(TeamLevel), ((ComboBoxItem)TeamLevelComboBox.SelectedItem).Content.ToString());

            _teams.Add(new Team(name, city, level));

            TeamNameTextBox.Clear();
            TeamCityTextBox.Clear();
            TeamLevelComboBox.SelectedIndex = 0;

            RefreshDistanceMatrix();
        }

        private void EditTeamButton_Click(object sender, RoutedEventArgs e)
        {
            if (TeamsDataGrid.SelectedItem is Team selectedTeam)
            {

                TeamNameTextBox.Text = selectedTeam.Name;
                TeamCityTextBox.Text = selectedTeam.City;

                switch (selectedTeam.Level)
                {
                    case TeamLevel.Фаворит:
                        TeamLevelComboBox.SelectedIndex = 0;
                        break;
                    case TeamLevel.Средний:
                        TeamLevelComboBox.SelectedIndex = 1;
                        break;
                    case TeamLevel.Аутсайдер:
                        TeamLevelComboBox.SelectedIndex = 2;
                        break;
                }

                _teams.Remove(selectedTeam);

                RefreshDistanceMatrix();
            }
        }

        private void RemoveTeamButton_Click(object sender, RoutedEventArgs e)
        {
            if (TeamsDataGrid.SelectedItem is Team selectedTeam)
            {
                if (MessageBox.Show($"Вы уверены, что хотите удалить {selectedTeam.Name}?",
                                  "Подтвердить удаление",
                                  MessageBoxButton.YesNo,
                                  MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    _teams.Remove(selectedTeam);

                    RefreshDistanceMatrix();
                }
            }
        }

        private async void ImportTeamsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var importedTeams = await _dataService.LoadDataAsync<List<Team>>(
                    "загрузить команды",
                    "JSON Files (*.json)|*.json|All Files (*.*)|*.*");

                if (importedTeams != null)
                {
                    _teams.Clear();
                    foreach (var team in importedTeams)
                    {
                        _teams.Add(team);
                    }

                    RefreshDistanceMatrix();

                    MessageBox.Show($"Успешно загружены {importedTeams.Count} команды.",
                                  "Импорт прошел успешно",
                                  MessageBoxButton.OK,
                                  MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error importing teams: {ex.Message}",
                              "Import Error",
                              MessageBoxButton.OK,
                              MessageBoxImage.Error);
            }
        }

        private async void ExportTeamsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await _dataService.SaveDataAsync(
                    _teams.ToList(),
                    "Export Teams",
                    "JSON Files (*.json)|*.json|All Files (*.*)|*.*");

                MessageBox.Show("Команды успешно экспортированы.",
                              "Экспорт успешен",
                              MessageBoxButton.OK,
                              MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при экспорте команд: {ex.Message}",
                              "Ошибка экспорта",
                              MessageBoxButton.OK,
                              MessageBoxImage.Error);
            }
        }

        private void GenerateDistanceMatrixButton_Click(object sender, RoutedEventArgs e)
        {
            if (_teams.Count < 2)
            {
                MessageBox.Show("Для составления матрицы расстояний необходимо как минимум две команды.",
                              "Недостаточно команд",
                              MessageBoxButton.OK,
                              MessageBoxImage.Warning);
                return;
            }

            _distanceMatrix.GenerateRandomDistances(_teams.Select(t => t.City).ToList());

            RefreshDistanceMatrix();

            MessageBox.Show("Случайные расстояния сгенерированы успешно.",
                          "Generation Complete",
                          MessageBoxButton.OK,
                          MessageBoxImage.Information);
        }

        private void EditDistanceMatrixButton_Click(object sender, RoutedEventArgs e)
        {
            if (_teams.Count < 2)
            {
                MessageBox.Show("Для редактирования матрицы расстояний необходимо как минимум две команды.",
                              "Недостаточно команд",
                              MessageBoxButton.OK,
                              MessageBoxImage.Warning);
                return;
            }

            foreach (var column in DistanceMatrixDataGrid.Columns)
            {
                if (column != DistanceMatrixDataGrid.Columns[0]) 
                {
                    column.IsReadOnly = false;
                }
            }

            MessageBox.Show("Теперь вы можете редактировать расстояния. Нажмите Enter, чтобы сохранить изменения.",
                          "Режим редактирования",
                          MessageBoxButton.OK,
                          MessageBoxImage.Information);

            DistanceMatrixDataGrid.CellEditEnding += DistanceMatrixDataGrid_CellEditEnding;
        }

        private void DistanceMatrixDataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.EditAction == DataGridEditAction.Commit)
            {
                int rowIndex = e.Row.GetIndex();
                int columnIndex = e.Column.DisplayIndex;

                if (columnIndex > 0 && rowIndex < _teams.Count && columnIndex <= _teams.Count)
                {
                    string cityA = _teams[rowIndex].City;
                    string cityB = _teams[columnIndex - 1].City;

                    if (e.EditingElement is TextBox textBox)
                    {
                        if (double.TryParse(textBox.Text, out double distance))
                        {
                            _distanceMatrix.AddDistance(cityA, cityB, distance);

                            // Update the symmetric cell as well
                            RefreshDistanceMatrix();
                        }
                        else
                        {
                            MessageBox.Show("Пожалуйста, введите действительное число для расстояния.",
                                          "Неверный ввод",
                                          MessageBoxButton.OK,
                                          MessageBoxImage.Error);

                            e.Cancel = true;
                        }
                    }
                }
            }
        }

        private async void ImportDistancesButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var distances = await _dataService.LoadDataAsync<Dictionary<string, Dictionary<string, double>>>(
                    "Импорт расстояний",
                    "JSON Files (*.json)|*.json|All Files (*.*)|*.*");

                if (distances != null)
                {
                    _distanceMatrix.SetDistances(distances);

                    // Refresh the grid
                    RefreshDistanceMatrix();

                    MessageBox.Show("Расстояния успешно импортированы.",
                                  "Импорт прошел успешно",
                                  MessageBoxButton.OK,
                                  MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при импорте расстояний: {ex.Message}",
                              "Ошибка импорта",
                              MessageBoxButton.OK,
                              MessageBoxImage.Error);
            }
        }

        private async void ExportDistancesButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await _dataService.SaveDataAsync(
                    _distanceMatrix.GetDistances(),
                    "Экспорт расстояний",
                    "JSON Files (*.json)|*.json|All Files (*.*)|*.*");

                MessageBox.Show("Расстояния успешно экспортированы.",
                              "Экспорт успешен",
                              MessageBoxButton.OK,
                              MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при экспорте расстояний: {ex.Message}",
                              "Ошибка экспорта",
                              MessageBoxButton.OK,
                              MessageBoxImage.Error);
            }
        }

        private async void GenerateScheduleButton_Click(object sender, RoutedEventArgs e)
        {
            if (_teams.Count < 2)
            {
                MessageBox.Show("Для составления расписания необходимо как минимум две команды.",
                              "Недостаточно команд",
                              MessageBoxButton.OK,
                              MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(MaxHomeGamesTextBox.Text, out int maxHomeGames) ||
                !int.TryParse(MaxAwayGamesTextBox.Text, out int maxAwayGames) ||
                !int.TryParse(TopGamesPerRoundTextBox.Text, out int topGamesPerRound) ||
                !int.TryParse(RoundsToPlayTextBox.Text, out int roundsToPlay))
            {
                MessageBox.Show("Пожалуйста, введите правильные числа для всех полей конфигурации.",
                              "Неверный ввод",
                              MessageBoxButton.OK,
                              MessageBoxImage.Error);
                return;
            }

            bool optimizeForDistance = ((ComboBoxItem)OptimizationPriorityComboBox.SelectedItem).Content.ToString() == "Расстояние";

            try
            {
                Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;

                var generator = new ScheduleGenerator(
                    _teams.ToList(),
                    _distanceMatrix,
                    maxHomeGames,
                    maxAwayGames,
                    topGamesPerRound,
                    optimizeForDistance,
                    roundsToPlay
                );

                var generatedSchedule = await generator.GenerateScheduleAsync();

                // Update UI
                _schedule.Clear();
                foreach (var game in generatedSchedule)
                {
                    _schedule.Add(game);
                }

                var statistics = new ScheduleStatistics(generatedSchedule, _teams.ToList());

                UpdateStatistics(statistics);

                Mouse.OverrideCursor = null;

                MessageBox.Show("Расписание составлено успешно!",
                              "Успешно!",
                              MessageBoxButton.OK,
                              MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                // Hide loading indicator
                Mouse.OverrideCursor = null;

                MessageBox.Show($"Ошибка составления расписания!: {ex.Message}",
                              "Ошибка!",
                              MessageBoxButton.OK,
                              MessageBoxImage.Error);
            }
        }

        private void UpdateStatistics(ScheduleStatistics statistics)
        {
            _teamStats.Clear();
            foreach (var stat in statistics.TeamStats)
            {
                _teamStats.Add(stat);
            }

            _roundStats.Clear();
            foreach (var stat in statistics.RoundStats)
            {
                _roundStats.Add(stat);
            }

            // Update summary statistics
            TotalDistanceTextBlock.Text = $"Общее расстояние поездки: {statistics.TotalDistance:N0} км";
            TopGamesTextBlock.Text = $"Всего лучших игр: {statistics.TopGamesCount}";
            HomeAwayBalanceTextBlock.Text = $"Дома/В гостях: {(statistics.IsBalanced ? "Хорошо" : "Требует улучшения")}";
        }
    }
}
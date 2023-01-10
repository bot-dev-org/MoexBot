using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using RuBot.Utils;
using RuBot.ViewModels.Strategies;

namespace RuBot.Views
{
    /// <summary>
    /// Interaction logic for Quoting.xaml
    /// </summary>
    public partial class Quoting
    {
        private readonly List<BaseStrategy> _strategies;
        private BaseStrategy _currentStrategy;
        public Quoting(List<BaseStrategy> strategies)
        {
            _strategies = strategies;
            InitializeComponent();
            _strategies.ForEach(s => StrategiesBox.Items.Add(s.Name));
        }
        private void StrategiesBoxSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (StrategiesBox.SelectedItem == null)
                return;
            _currentStrategy = _strategies[StrategiesBox.Items.IndexOf(StrategiesBox.SelectedItem)];
            ValueBox.Text = _currentStrategy.PartVolume.ToString();
        }
        private void Button1Click(object sender, RoutedEventArgs e)
        {
            var valueString = ValueBox.Text.Trim();
            if (_currentStrategy != null && !string.IsNullOrEmpty(valueString))
            {
                try
                {
                    Logger.LogDebug(
                        $"Start learn Quoting {_currentStrategy.Name + " " + _currentStrategy.GetType().Name.Substring(0, 2)} with PartVolume={valueString}");
                    _currentStrategy.PartVolume = int.Parse(valueString);
                    ThreadPool.QueueUserWorkItem(delegate{_currentStrategy.UpdateQuoteParams();});
                }
                catch (Exception exp)
                {
                    MessageBox.Show(exp.Message, "Error while setting");
                }
            }
            Close();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using RuBot.ViewModels.Strategies;

namespace RuBot.Views
{
    public partial class SetParameter
    {
        private readonly List<BaseStrategy> _strategies;
        private BaseStrategy _currentStrategy;
        private readonly List<FieldInfo> _fields = new List<FieldInfo>();
        private FieldInfo _currentField;

        public SetParameter(List<BaseStrategy> strategies)
        {
            _strategies = strategies;
            InitializeComponent();
            _strategies.ForEach(s => StrategiesBox.Items.Add(s.Name + " " +s.GetType().Name.ToString().Substring(0,2)));
            
        }

        private void Button1Click(object sender, RoutedEventArgs e)
        {
            var valueString = ValueBox.Text.Trim();
            if (StrategiesBox.SelectedItem != null && ParametersBox.SelectedItem != null && !string.IsNullOrEmpty(valueString))
            {
                try
                {
                    object value = null;
                    if (_currentField.FieldType == typeof (int))
                        value = int.Parse(ValueBox.Text.Trim());
                    else if (_currentField.FieldType == typeof (double))
                        value = double.Parse(ValueBox.Text.Trim());
                    else if (_currentField.FieldType == typeof(decimal))
                        value = decimal.Parse(ValueBox.Text.Trim());

                    _currentField.SetValue(_currentStrategy.Parameters, value);
                }
                catch (Exception exp)
                {
                    MessageBox.Show(exp.Message, "Error while setting");
                }
            }
            Close();
        }

        private void StrategiesBoxSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (StrategiesBox.SelectedItem == null)
                return;
            ParametersBox.Items.Clear();
            ParametersBox.SelectedItem = null;
            _fields.Clear();
            _currentStrategy = _strategies[StrategiesBox.Items.IndexOf(StrategiesBox.SelectedItem)];
            foreach (var fieldInfo in _currentStrategy.Parameters.GetType().GetFields().Where(fieldInfo => fieldInfo.FieldType == typeof(int) || fieldInfo.FieldType == typeof(double) || fieldInfo.FieldType == typeof(decimal)))
            {
                _fields.Add(fieldInfo);
                ParametersBox.Items.Add(fieldInfo.Name);
            }
        }

        private void ParametersBoxSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ParametersBox.SelectedItem != null)
            {
                _currentField = _fields[ParametersBox.Items.IndexOf(ParametersBox.SelectedItem)];
                ValueBox.Text = _currentField.GetValue(_currentStrategy.Parameters).ToString();
            }
            else
                ValueBox.Text = string.Empty;
        }
    }
}

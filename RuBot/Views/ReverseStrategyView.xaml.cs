using System.Windows.Threading;
using RuBot.ViewModels.Strategies;
using Candle = RuBot.Models.Candle;

namespace RuBot.Views.Strategy
{
    public partial class ReverseStrategyView
    {
        private readonly BaseStrategy _viewModel;
        private readonly Dispatcher _guiDispatcher;
        public ReverseStrategyView(BaseStrategy strategy)
        {
            _viewModel = strategy;

            _viewModel.OnDraw += Draw;
            DataContext = _viewModel;
            _guiDispatcher = Dispatcher;
            InitializeComponent();
        }
        public void Draw(Candle candle)
        {
        }
    }
}

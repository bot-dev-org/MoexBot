using System.Windows.Input;
using RuBot.ViewModels;

namespace RuBot.Views
{
    public partial class ControlView
    {
        private readonly ControlViewModel _viewModel;

        public ControlView(ControlViewModel viewModel)
        {
            _viewModel = viewModel;
            InitializeComponent();
            CommandBindings.Add(new CommandBinding(ControlViewModel.Connect, _viewModel.ConnectCommandHandler,
                                        _viewModel.CanConnectCommand));
            CommandBindings.Add(new CommandBinding(ControlViewModel.Start, _viewModel.StartCommandHandler,
                                        _viewModel.CanStartCommand));
            CommandBindings.Add(new CommandBinding(ControlViewModel.SetParameter, _viewModel.SetParameterCommandHandler,
                                        _viewModel.CanSetParameterCommand));
            CommandBindings.Add(new CommandBinding(ControlViewModel.Quoting, _viewModel.QuotingCommandHandler,
                                        _viewModel.CanQuotingCommand));
            DataContext = _viewModel;
        }
    }
}

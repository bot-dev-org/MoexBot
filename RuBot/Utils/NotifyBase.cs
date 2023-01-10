using System.ComponentModel;
using System.Windows;

namespace RuBot.Utils
{
    public abstract class NotifyBase : INotifyPropertyChanged
    {
        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        protected void RaisePropertyChanged(string propertyName)
        {
            if (PropertyChanged != null && Application.Current != null && Application.Current.Dispatcher != null)
            {
                Application.Current.Dispatcher.BeginInvoke(new PropertyChangedEventHandler(PropertyChanged), this,
                                                      new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
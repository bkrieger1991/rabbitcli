using System;
using System.Threading;
using Caliburn.Micro;

namespace RabbitMQ.Win.UI.ViewModels
{
    public class CounterViewModel : PropertyChangedBase
    {
        private readonly Timer _timer;

        private int _counter = 0;
        public int Counter
        {
            get => _counter;
            set
            {
                _counter = value;
                NotifyOfPropertyChange(nameof(Counter));
            }
        }

        public CounterViewModel()
        {
            _timer = new Timer(_ =>
            {
                Counter++;
            }, null, TimeSpan.Zero, TimeSpan.FromSeconds(1));
        }
    }
}
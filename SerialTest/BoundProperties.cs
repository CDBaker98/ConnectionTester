using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SerialTest {

    public class BoundProperties : INotifyPropertyChanged {

        #region Private Fields

        private string _SerialResponse;
        private string _SocketResponse;

        #endregion

        #region Public Properties

        public string SerialResponse { get => _SerialResponse; set => SetProperty(ref _SerialResponse, value); }
        public string SocketResponse { get => _SocketResponse; set => SetProperty(ref _SocketResponse, value); }

        #endregion

        public BoundProperties() {
            SerialResponse = SocketResponse = "";
        }

        #region SetProperty

        // Delegates can be used in event handling to pass values to the UI thread.
        // To implement the INotifyPropertyChanged interface, you must register the PropertyChangedEventHandler delegate as an event.
        public event PropertyChangedEventHandler PropertyChanged;

        // When the PropertyChanged event is raised, this method will instantiate an object containing the name of the property that was changed 
        // so the UI control can connect to the appropriate property.
        protected void OnPropertyChanged([CallerMemberName] string propertyName = "") {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected virtual void SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = "") {
            if (EqualityComparer<T>.Default.Equals(storage, value)) {
                return;
            }
            storage = value;
            OnPropertyChanged(propertyName);
        }
        #endregion
    }
}

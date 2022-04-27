using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SerialTest {

    public class BoundProperties : INotifyPropertyChanged {

        private string _SerialResponse;
        public string SerialResponse {
            get => _SerialResponse;
            set {
                _SerialResponse = value;

                OnPropertyChanged();
            }
        }

        private string _SocketResponse;
        public string SocketResponse {
            get => _SocketResponse;
            set {
                _SocketResponse = value;
                OnPropertyChanged();
            }
        }

        public BoundProperties() {
            SerialResponse = SocketResponse = "";
        }

        // Delegates can be used in event handling to pass values to the UI thread.
        // To implement the INotifyPropertyChanged interface, you must register the PropertyChangedEventHandler delegate as an event.
        public event PropertyChangedEventHandler PropertyChanged;

        // When the PropertyChanged event is raised, this method will instantiate an object containing the name of the property that was changed 
        // so the UI control can connect to the appropriate property.
        protected void OnPropertyChanged([CallerMemberName] string propertyName = "") {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

    }
}

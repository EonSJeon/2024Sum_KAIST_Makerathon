using com.atid.lib.atx88;
using com.atid.lib.module.barcode;
using com.atid.lib.module.barcode.events;
using com.atid.lib.module.barcode.types;
using com.atid.lib.module.rfid.uhf;
using com.atid.lib.module.rfid.uhf.events;
using com.atid.lib.reader;
using com.atid.lib.reader.events;
using com.atid.lib.reader.parameters;
using com.atid.lib.reader.types;
using com.atid.lib.transport;
using com.atid.lib.transport.types;
using com.atid.lib.types;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;


namespace testApp
{
    public enum TokenColor
    {
        RED,
        BLUE,
        YELLOW
    }

    public enum FoodType
    {
        MEAT,
        DAIRY,
        VEGETABLE,
        FISH,
        FRUIT
    }


    public class RemainingDaysToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is int remainingDays)
            {
                if (remainingDays < 0)
                {
                    return new SolidColorBrush(Windows.UI.Colors.Red);
                }
                else if (remainingDays <= 2)
                {
                    return new SolidColorBrush(Windows.UI.Colors.Yellow);
                }
                else
                {
                    return new SolidColorBrush(Windows.UI.Colors.Green);
                }
            }
            return new SolidColorBrush(Windows.UI.Colors.Transparent);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }


    public class TokenColorToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is TokenColor color)
            {
                switch (color)
                {
                    case TokenColor.RED:
                        return new SolidColorBrush(Windows.UI.Colors.Red);
                    case TokenColor.BLUE:
                        return new SolidColorBrush(Windows.UI.Colors.Blue);
                    case TokenColor.YELLOW:
                        return new SolidColorBrush(Windows.UI.Colors.Yellow);
                }
            }
            return new SolidColorBrush(Windows.UI.Colors.Transparent);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }


    //public class FoodTypeToImageConverter : IValueConverter
    //{
    //    public object Convert(object value, Type targetType, object parameter, string language)
    //    {
    //        if (value is FoodType foodType)
    //        {
    //            switch (foodType)
    //            {
    //                case FoodType.FRUIT:
    //                    return "FRUIT";//new Uri("ms-appx:///Assets/fruit.png");
    //                case FoodType.MEAT:
    //                    return "MEAT";//new Uri("ms-appx:///Assets/meat.png");
    //                case FoodType.VEGETABLE:
    //                    return "VEGETABLE";//new Uri("ms-appx:///Assets/vegetable.png");
    //                case FoodType.FISH:
    //                    return "FISH";//new Uri("ms-appx:///Assets/fish.png");
    //                case FoodType.DAIRY:
    //                    return "DAIRY"; //new Uri("ms-appx:///Assets/dairy.png");
    //                default:
    //                    return "UNKNOWN";// new Uri("ms-appx:///Assets/unknown.png");
    //            }
    //        }
    //        return "UNKNOWN"; //new Uri("ms-appx:///Assets/unknown.png");
    //    }

    //    public object ConvertBack(object value, Type targetType, object parameter, string language)
    //    {
    //        throw new NotImplementedException();
    //    }
    //}



    public class DateTimeToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is DateTime dateTime)
            {
                return dateTime.ToString("yyyy-MM-dd");
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }


    public class Container
    {
        static public string rfidPrefix = "4400000000000000000000002103031004";
        static public DateTime farPast = new DateTime(1900, 1, 1); // Corrected initialization

        public Container()
        {
            Rfid = rfidPrefix;
            PurchaseDate = DateTime.Now.Date;
        }

        public string Rfid { get; set; }

        // 표기됨
        public FoodType Type { get; set; }

        public int Days
        {
            get
            {
                switch (this.Type)
                {
                    case FoodType.FISH:
                        return 14;
                    case FoodType.VEGETABLE:
                        return 21;
                    case FoodType.FRUIT:
                        return 7;
                    case FoodType.MEAT:
                        return 4;
                    case FoodType.DAIRY:
                        return 7;
                    default:
                        return 10;
                }
            }
        }

        public TokenColor Color { get; set; }

        // 표기됨
        public DateTime PurchaseDate { get; set; }
        public DateTime DiscardDate => PurchaseDate.AddDays(Days);
        public int RemainingDays => (DiscardDate - DateTime.Today).Days;


        public DateTime OutTime { get; set; } =  farPast;

        public bool IsInUse { get; set; } = true;

        public bool IsInBox { get; set; } = false;
    }


    public class ReaderViewModel : INotifyPropertyChanged, IATEAReaderEventListener, IATRfidUhfEventListener, IATBarcodeEventListener
    {
        private ATEAReader _reader;
        private DispatcherTimer _timer;
        private DispatcherTimer _filterRefreshTimer;
        public event PropertyChangedEventHandler PropertyChanged;
        private ObservableCollection<Container> _records = new ObservableCollection<Container>();


        private ActionState _actionState;
        private ICommand _connectClickCommand;
        private ICommand _inventoryClickCommand;
        private ICommand _decodeClickCommand;
        private ICommand _clearClickCommand;
        private bool _canExecute;
        private string _portName;
        private int _deviceIndex;
        private ConnectState _connState;
        private string _battState;
        private string _version;
        private string _serialNumber;
        private TimeSpan _autoResetTime;

        public ObservableCollection<Container> FilteredContainers {get; set;}
        

        public ReaderViewModel()
        {
            _records = new ObservableCollection<Container>
        {

            new Container{Rfid = Container.rfidPrefix + "15", Color=TokenColor.RED, Type = FoodType.FRUIT, PurchaseDate=new DateTime(2024, 07, 10)},
            new Container{Rfid = Container.rfidPrefix + "10", Color=TokenColor.RED, Type = FoodType.DAIRY, PurchaseDate=new DateTime(2024, 06, 10)},
            new Container{Rfid = Container.rfidPrefix + "0F", Color=TokenColor.YELLOW, Type = FoodType.FRUIT},
            new Container{Rfid = Container.rfidPrefix + "11", Color=TokenColor.RED, Type = FoodType.MEAT, PurchaseDate=new DateTime(2024, 07, 3)},
            new Container{Rfid = Container.rfidPrefix + "18", Color=TokenColor.BLUE, Type = FoodType.DAIRY, PurchaseDate=new DateTime(2024, 07, 7)},
                
            new Container{Rfid = Container.rfidPrefix + "13", Color=TokenColor.BLUE, Type = FoodType.VEGETABLE},
            new Container{Rfid = Container.rfidPrefix + "16", Color=TokenColor.BLUE, Type = FoodType.FISH},
            new Container{Rfid = Container.rfidPrefix + "17",Color=TokenColor.YELLOW, Type = FoodType.MEAT},
            new Container{Rfid = Container.rfidPrefix + "0E", Color=TokenColor.RED, Type = FoodType.VEGETABLE, PurchaseDate=new DateTime(2024, 06, 30)},
            new Container{Rfid = Container.rfidPrefix + "0D", Color=TokenColor.YELLOW, Type = FoodType.VEGETABLE},

            new Container{Rfid = Container.rfidPrefix + "12", Color=TokenColor.BLUE, Type = FoodType.FRUIT, PurchaseDate=new DateTime(2024, 07, 3)},
            new Container{Rfid = Container.rfidPrefix + "1C",Color=TokenColor.BLUE, Type = FoodType.MEAT}
        };

            _reader = null;
            _canExecute = true;
            _portName = "COM6";
            _deviceIndex = 0;
            _connState = ConnectState.Disconnected;
            _battState = "";
            _version = "";
            _serialNumber = "";
            _actionState = ActionState.Stop;

            _autoResetTime = TimeSpan.FromSeconds(5);

            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += Timer_Tick;
            _timer.Start();

            // Initialize the filter refresh timer
            _filterRefreshTimer = new DispatcherTimer();
            _filterRefreshTimer.Interval = TimeSpan.FromSeconds(3); // CHANGE LATER !!!!!!: 5 min
            _filterRefreshTimer.Tick += FilterRefreshTimer_Tick;
            _filterRefreshTimer.Start();

            Debug.WriteLine("ReaderViewModel initialized");
            

            // Initialize the filtered view
            FilteredContainers = new ObservableCollection<Container>();
            RefreshDataView(true);
        }

        private void Timer_Tick(object sender, object e)
        {
            OnPropertyChanged("Today");
        }

        private void FilterRefreshTimer_Tick(object sender, object e)
        {
            RefreshDataView();
        }


        public string Today
        {
            get
            {
                return DateTime.Now.ToString("F"); // "F" gives full date and time
            }
        }



        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }


        public int TotalCount
        {
            get
            {
                int count = 0;
                foreach (Container container in _records)
                {
                    if (container.IsInUse == true)
                    {
                        count++;
                    }
                }
                return count;
            }
        }



        public async void onRfidUhfReadTag(ATRfidUhf uhf, string tag, object parameters)
        {
            await Windows.ApplicationModel.Core.CoreApplication.MainView.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                bool changed = false;

                var currentItem = _records.FirstOrDefault(c => c.Rfid == tag);

                foreach (Container item in _records)
                {
                    DateTime now = DateTime.Now;

                    if (currentItem == item)
                    {
                        item.IsInBox = true;
                        if (item.IsInUse)
                        {
                            item.IsInUse = false;
                            changed = true;
                        }
                    }
                    else // Not Detected
                    {
                        if (item.IsInBox)
                        {
                            item.IsInBox = false;
                            item.OutTime = now;
                        }
                    }
                }

                if (changed)
                {
                    Debug.WriteLine("onRfidUhfReadTag: Data changed");
                    RefreshDataView(changed);
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("TotalCount"));
                }
            });
        }


        private void RefreshDataView(bool changed=false)
        {
            DateTime now = DateTime.Now;

            foreach (var item in _records)
            {
                if (!item.IsInBox  && !item.IsInUse && (now - item.OutTime) > _autoResetTime)
                {
                    item.IsInUse = true;
                    item.PurchaseDate = now;
                    changed = true;
                }
            }

            var filteredItems = _records
                .Where(c => c.IsInUse)
                .OrderBy(c => c.RemainingDays)
                .ToList();

            if (changed)
            {
                Debug.WriteLine("RefreshDataView: FilteredItems count: " + filteredItems.Count);
                FilteredContainers= new ObservableCollection<Container>(filteredItems);

                OnPropertyChanged(nameof(FilteredContainers));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("TotalCount"));
            }

            foreach (var item in _records)
            {
                item.IsInBox = false;
            }
        }


        // Don't touch below =====================================================================================================================

        public async void Connect()
        {
            if (_reader == null)
            {
                await Task.Run(() =>
                {
                    _reader = new ATD100Reader(new ATransportVcp(DeviceType.ATD100, "ATID Reader", _portName));
                   

                    _reader.addListener(this);
                    _reader.connect();

                    NotifyMethod mute = new NotifyMethod(false, false, true);
                    _reader.setAlertNotify(mute);
                });
            }
            else
            {
                _reader.disconnect();
            }
        }

        public void Clear()
        {
            System.Diagnostics.Debug.WriteLine("+++ Clear");

            _records.Clear();
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("TotalCount"));
        }

        public string ConnectStateTextForButton
        {
            get
            {
                if (_connState == ConnectState.Connected)
                    return "Disconnect";
                else if (_connState == ConnectState.Disconnected)
                    return "Connect";
                else
                    return "...";
            }
        }
        public void Inventory()
        {
            System.Diagnostics.Debug.WriteLine("+++ Inventory");
            if (_reader == null)
                return;

            if (_reader.getAction() == ActionState.Stop)
            {
                _reader.getRfidUhf()?.inventory6c();
            }
            else
            {
                _reader.getRfidUhf()?.stop();
            }
        }
        public void Decode()
        {
            System.Diagnostics.Debug.WriteLine("+++ Decode");
            if (_reader == null)
                return;

            if (_reader.getAction() == ActionState.Stop)
            {
                _reader.getBarcode()?.startDecode();
            }
            else
            {
                _reader.getBarcode()?.stop();
            }
        }

        public string InventoryStateTextForButton
        {
            get
            {
                if (_actionState == ActionState.Stop)
                    return ActionState.Inventory6c.ToString();
                else if (_actionState == ActionState.Inventory6c)
                    return ActionState.Stop.ToString();

                return "";
            }
        }
        public string DecodeStateTextForButton
        {
            get
            {
                if (_actionState == ActionState.Stop)
                    return ActionState.Decoding.ToString();
                else if (_actionState == ActionState.Decoding)
                    return ActionState.Stop.ToString();

                return "";
            }
        }
        public string PortName
        {
            get
            {
                return _portName;
            }
            set
            {
                if (value != _portName)
                {
                    _portName = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("PortName"));
                }
            }
        }

        public int DeviceIndex
        {
            get
            {
                return _deviceIndex;
            }
            set
            {
                if (value != _deviceIndex)
                {
                    _deviceIndex = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("DeviceIndex"));
                }
            }
        }

        public async void onReaderStateChanged(ATEAReader reader, ConnectState state, object objs)
        {
            await Windows.ApplicationModel.Core.CoreApplication.MainView.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                if (state == ConnectState.Connecting)
                {
                }
                else if(state == ConnectState.Connected)
                {
                    _version = _reader.getVersion();
                    _serialNumber = _reader.getSerialNo();
                    _reader.getRfidUhf()?.addListener(this);
                    _reader.getBarcode()?.addListener(this);
                }
                else if(state == ConnectState.Disconnected)
                {
                    if (_reader != null)
                    {
                        _reader.getRfidUhf()?.removeListener(this);
                        _reader.getBarcode()?.removeListener(this);
                        _reader.removeListener(this);
                        _reader = null;
                    }

                    _version = "";
                    _serialNumber = "";
                }

                _connState = state;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("ConnState"));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("ConnectStateTextForButton"));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Version"));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("SerialNumber"));
            });
        }

        public async void onReaderActionChanged(ATEAReader reader, ResultCode code, ActionState action, object objs)
        {
            await Windows.ApplicationModel.Core.CoreApplication.MainView.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                _actionState = action;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("ActState"));
                if (action == ActionState.Stop)
                {
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("InventoryStateTextForButton"));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("DecodeStateTextForButton"));
                }
                else if(action == ActionState.Inventory6c)
                {
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("InventoryStateTextForButton"));
                }
                else if(action == ActionState.Decoding)
                {
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("DecodeStateTextForButton"));
                }
            });
        }

        public void onReaderOperationModeChanged(ATEAReader reader, OperationMode mode, object objs)
        {
            throw new NotImplementedException();
        }

        public async void onReaderBatteryState(ATEAReader reader, int batteryState, object objs)
        {
            await Windows.ApplicationModel.Core.CoreApplication.MainView.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                _battState = batteryState.ToString();
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("BattState"));
            });
        }

        public void onKeyEvent(ATEAReader reader, byte keyCode, byte keyState)
        {
            throw new NotImplementedException();
        }

        public void onRfidUhfAccessResult(ATRfidUhf uhf, ResultCode code, ActionState action, string epc, string data, object parameters)
        {
            throw new NotImplementedException();
        }

        public void onRfidUhfPowerGainChanged(ATRfidUhf uhf, int power, object parameters)
        {
            throw new NotImplementedException();
        }

        public async void onBarcodeReadData(ATBarcode barcode, BarcodeType type, string codeId, string data, object parameters)
        {
            await Windows.ApplicationModel.Core.CoreApplication.MainView.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                throw new NotImplementedException();
            });
        }

        public string ConnState
        {
            get
            {
                return _connState.ToString();
            }
        }
        public string BattState
        {
            get
            {
                return _battState;
            }
        }
        public string ActState
        {
            get
            {
                return _actionState.ToString();
            }
        }
        public string Version
        {
            get
            {
                return _version;
            }
        }
        public string SerialNumber
        {
            get
            {
                return _serialNumber;
            }
        }
        public ICommand ConnectClick
        {
            get
            {
                return _connectClickCommand ?? (_connectClickCommand = new CommandHandler(() => Connect(), _canExecute));
            }
        }
        public ICommand InventoryClick
        {
            get
            {
                return _inventoryClickCommand ?? (_inventoryClickCommand = new CommandHandler(() => Inventory(), _canExecute));
            }
        }
        public ICommand DecodeClick
        {
            get
            {
                return _decodeClickCommand ?? (_decodeClickCommand = new CommandHandler(() => Decode(), _canExecute));
            }
        }
        public ICommand ClearClick
        {
            get
            {
                return _clearClickCommand ?? (_clearClickCommand = new CommandHandler(() => Clear(), _canExecute));
            }
        }
    }

    public class CommandHandler : ICommand
    {
        private Action _action;
        private bool _canExecute;

        public CommandHandler(Action action, bool canExecute)
        {
            _action = action;
            _canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return _canExecute;
        }

        public void Execute(object parameter)
        {
            _action();
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

using com.atid.lib.diagnostics;
using com.atid.lib.types;
using com.atid.lib.transport;
using com.atid.lib.transport.types;
using com.atid.lib.util;
using com.atid.lib.util.diagnotics;
using com.atid.lib.util.wifi;
using com.atid.lib.reader;
using com.atid.lib.reader.parameters;
using com.atid.lib.reader.types;
using com.atid.lib.atx88;
using com.atid.lib.reader.events;
using com.atid.lib.module.barcode;
using com.atid.lib.module.barcode.events;
using com.atid.lib.module.barcode.parameters;
using com.atid.lib.module.barcode.types;
using com.atid.lib.module.rfid.uhf;
using com.atid.lib.module.rfid.uhf.events;
using com.atid.lib.module.rfid.uhf.parameters;
using com.atid.lib.module.rfid.uhf.types;
using System.Collections.ObjectModel;
using Windows.Devices.SerialCommunication;
using Windows.Devices.Enumeration;
using System.Threading.Tasks;

// 빈 페이지 항목 템플릿에 대한 설명은 https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x412에 나와 있습니다.

namespace testApp
{
    /// <summary>
    /// 자체적으로 사용하거나 프레임 내에서 탐색할 수 있는 빈 페이지입니다.
    /// </summary>
    /// 

    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
        }
    }


}

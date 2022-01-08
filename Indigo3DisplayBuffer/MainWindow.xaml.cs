using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.ComponentModel;

using System.Threading;
using System.IO;
using Microsoft.Win32;

namespace Indigo3DisplayBuffer
{
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainWindow : Window
    {
        private UserFrameProperty FetchDecode0;
        private UserFrameProperty FetchLayer0;
        private UserFrameProperty FetchLayer1;
        public MainWindow()
        {
            InitializeComponent();

            FetchDecode0 = new UserFrameProperty(UserFrameProperty.SourceType.SRC_FETCH_DECODE0);
            FetchLayer0 = new UserFrameProperty(UserFrameProperty.SourceType.SRC_FETCHLAYER0);
            FetchLayer1 = new UserFrameProperty(UserFrameProperty.SourceType.SRC_FETCHLAYER1);

            lblDispWidth.Content = 1920;
            lblDispHeight.Content = 720;
        }

        private void btnOpenPar_Click(object sender, RoutedEventArgs e)
        {
            displayBuffer_SizeChange();
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "Par File(*.par)|*.par";
            if (dialog.ShowDialog() == true)
            {
                updateProgressLog_UI(String.Format("config: {0}", dialog.SafeFileName), "Blue");
                if (LoadConfig(dialog.FileName))
                {
                    updateProgressLog_UI(String.Format("    upgrade: pre_process {0}, cmd {1}, files {2}",
                        listSshCommand_Pre.Count, listSshCommand.Count, listTxRxFile.Count), "Black");
                    updateProgressLog_UI(String.Format("    check: {0}", listSshCheck.Count), "Black");
                    StateActive();
                }
            }
        }

        private void btnOpenBin_Click(object sender, RoutedEventArgs e)
        {

        }

        private void mainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            displayBuffer_SizeChange();
        }

        private void StackPanel_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            displayBuffer_SizeChange();
        }


        /**************************/
        //     Panel
        /**************************/

        private void displayBuffer_SizeChange()
        {
            try
            {
                int real_width = Convert.ToInt32(lblDispWidth.Content);
                int real_height = Convert.ToInt32(lblDispHeight.Content);

                double width = (gridMain.ActualWidth) -
                    (stackControl.Margin.Left + stackControl.Margin.Right + stackControl.ActualWidth) - 5.0;
                double height = (gridMain.ActualHeight) -
                    (stackConnect.Margin.Bottom + stackConnect.Margin.Top + stackConnect.ActualHeight);

                double exp_height = width * real_height / real_width;
                if (exp_height <= height)
                {
                    cvsDispBuff.Width = width;
                    cvsDispBuff.Height = exp_height;
                    return;
                }

                double exp_width = height * real_width / real_height;
                if (exp_width <= width)
                {
                    cvsDispBuff.Width = exp_width;
                    cvsDispBuff.Height = height;
                    return;
                }
            }
            catch { }
        }

    }
}

public class UserDispInfo : INotifyPropertyChanged
{
    public UserDispInfo(double x, double y, string type = "Move")
    {
        Type = type;
        X = Convert.ToInt32(Math.Round(x)).ToString();
        Y = Convert.ToInt32(Math.Round(y)).ToString();
    }
    public string Type { get; }
    public string X { get; }
    public string Y { get; }
    public string Width { get; }
    public string Height { get; }

    public event PropertyChangedEventHandler PropertyChanged;

    // This method is called by the Set accessor of each property.
    // The CallerMemberName attribute that is applied to the optional propertyName
    // parameter causes the property name of the caller to be substituted as an argument.
    private void NotifyPropertyChanged(String propertyName = "")
    {
        if (PropertyChanged != null)
        {
            PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

public class UserFrameProperty
{
    public enum SourceType
    {
        SRC_NONE,
        SRC_FETCHLAYER0,
        SRC_FETCHLAYER1,
        SRC_FETCH_DECODE0,
        SRC_FETCH_DECODE1,
    };
    public enum ParType
    {
        PAR_TYPE_NONE,
        PAR_TYPE_FETCH_DECODE,
        PAR_TYPE_FETCH_DECODE_GLOBAL,
        PAR_TYPE_FETCH_DECODE_PALETTE,
        PAR_TYPE_FETCH_LAYER0,
        PAR_TYPE_FETCH_LAYER0_GLOBAL,
        PAR_TYPE_FETCH_LAYER0_PALETTE,
        PAR_TYPE_FETCH_LAYER1,
        PAR_TYPE_FETCH_LAYER1_GLOBAL,
        PAR_TYPE_FETCH_LAYER1_PALETTE,
    };

    static public UserImageSourceBuffer CreateObject(SourceType type, int base_idx)
    {
        if (type == SourceType.SRC_FETCHLAYER0)
        {
            int base_address = 0x00043014 + (base_idx * 0x28);
            UserImageSourceBuffer obj = new UserImageSourceBuffer("SRC_FETCHLAYER0-" + base_idx.ToString(), base_address, type);
            return obj;
        }
        if (type == SourceType.SRC_FETCHLAYER1)
        {
            int base_address = 0x00043814 + (base_idx * 0x28);
            UserImageSourceBuffer obj = new UserImageSourceBuffer("SRC_FETCHLAYER1-" + base_idx.ToString(), base_address, type);
            return obj;
        }
        if (type == SourceType.SRC_FETCH_DECODE0)
        {
            UserImageSourceBuffer obj = new UserImageSourceBuffer("FETCHDECODE0", 0x0004281C, type);
            return obj;
        }

        return null;
    }

    static public ParType getCmdType(string par_line, ref string addr, ref string value)
    {
        char[] token = { ' ' };
        string[] split = par_line.Split(token);

        if (!split[0].Equals("i3write"))
        {
            return ParType.PAR_TYPE_NONE;
        }
        if (!split[1].Equals("32"))
        {
            return ParType.PAR_TYPE_NONE;
        }

        addr = split[2];
        value = split[3];
        UInt32 address = Convert.ToUInt32(addr, 16);
        if (address % 4 > 0)
        {
            return ParType.PAR_TYPE_NONE;
        }

        if ((0x42800 <= address) && (address < 0x42878))
        {
            if ((0x00042844 <= address) && (address <= 0x00042854))
            {
                return ParType.PAR_TYPE_FETCH_DECODE_GLOBAL;
            }
            return ParType.PAR_TYPE_FETCH_DECODE;
        }
        if ((0x00042C00 <= address) && (address < 0x43000))
        {
            return ParType.PAR_TYPE_FETCH_DECODE_PALETTE;
        }
        if ((0x43000 <= address) && (address < 0x432F8))
        {
            if ((0x00043294 <= address) && (address <= 0x0004329C))
            {
                return ParType.PAR_TYPE_FETCH_LAYER0_GLOBAL;
            }
            return ParType.PAR_TYPE_FETCH_LAYER0;
        }
        if ((0x43400 <= address) && (address < 0x43800))
        {
            return ParType.PAR_TYPE_FETCH_LAYER0_PALETTE;
        }

        if ((0x43800 <= address) && (address < 0x43AF8))
        {
            if ((0x00043A94 <= address) && (address <= 0x00043A9C))
            {
                return ParType.PAR_TYPE_FETCH_LAYER1_GLOBAL;
            }
            return ParType.PAR_TYPE_FETCH_LAYER1;
        }
        if ((0x43C00 <= address) && (address < 0x44000))
        {
            return ParType.PAR_TYPE_FETCH_LAYER1_PALETTE;
        }

        return ParType.PAR_TYPE_NONE;
    }

    public UserFrameProperty(SourceType _type)
    {
        srcType = _type;

        colorPalette = new UInt32[256];
        ImageList = new List<UserImageSourceBuffer>();

        if ((_type == SourceType.SRC_FETCHLAYER0) || (_type == SourceType.SRC_FETCHLAYER1))
        {
            for(int idx=0; idx<16; idx++)
            {
                UserImageSourceBuffer obj = CreateObject(_type, idx);
                ImageList.Add(obj);
            }
        }
        if ((_type == SourceType.SRC_FETCH_DECODE0) || (_type == SourceType.SRC_FETCH_DECODE1))
        {
            UserImageSourceBuffer obj = CreateObject(_type, 0);
            ImageList.Add(obj);
        }
    }

    public bool setSrcProperty(ParType _type, string _address, string _value)
    {
        if(!isPareAvailable(_type))
        {
            return false;
        }

        foreach(UserImageSourceBuffer obj in ImageList)
        {
            if(obj.setProperty(_address, _value))
            {
                return true;
            }
        }

        return false;
    }

    public bool setProperty(SourceType _type, string _address, string _value)
    {
        if(srcType != _type)
        {
            return false;
        }

        UInt32 address = Convert.ToUInt32(_address, 16);
        UInt32 value = Convert.ToUInt32(_value, 16);
        if (address % 4 > 0)
        {
            return false;
        }

        switch (address)
        {
            // FRAMEDIMENSIONS
            case 0x00042844:    // fetch decode0
            case 0x00043294:    // fetch layer0
            case 0x00043A94:    // fetch layer1
                {
                    height = (int)((value >> 16) & 0x3FFF); // Height
                    width = (int)((value >> 0) & 0x3FFF); // Width
                    break;
                }
            // CONTROL
            case 0x00042854:    // fetch decode0
            case 0x0004329C:    // fetch layer0
            case 0x00043A9C:    // fetch layer1
                {
                    bitsPerPixelForPalette = (int)((value >> 8) & 0x07);
                    rawPixelMode = (((value >> 7) & 0x01) > 0);
                    break;
                }
            // FRAMERESAMPLING
            case 0x00042848:    // fetch decode0
            case 0x00043298:    // fetch layer0
            case 0x00043A98:    // fetch layer1
                {
                    scaley = (float)((value >> 18) & 0x3F)/4; // Y
                    scalex = (float)((value >> 12) & 0x3F)/4; // X
                    y = (int)((value >> 6) & 0x3F); // Y
                    x = (int)((value >> 0) & 0x3F); // X
                    break;
                }
            default: return false;
        }

        return true;
    }

    public bool setColorPalette(SourceType _type, string _address, string _value)
    {
        if (srcType != _type)
        {
            return false;
        }

        UInt32 address = Convert.ToUInt32(_address, 16);
        UInt32 value = Convert.ToUInt32(_value, 16);
        if (address % 4 > 0)
        {
            return false;
        }

        UInt32 palette_idx = (0x43400 - address) / 4;
        if(palette_idx >= 256)
        {
            return false;
        }

        colorPalette[palette_idx] = value;

        return true;
    }

    private bool isPareAvailable(ParType _type)
    {
        if(srcType == SourceType.SRC_FETCHLAYER0)
        {
            if (_type == ParType.PAR_TYPE_FETCH_LAYER0)
            {
                return true;
            }
        }
        if (srcType == SourceType.SRC_FETCHLAYER1)
        {
            if (_type == ParType.PAR_TYPE_FETCH_LAYER1)
            {
                return true;
            }
        }
        if (srcType == SourceType.SRC_FETCH_DECODE0)
        {
            if (_type == ParType.PAR_TYPE_FETCH_DECODE)
            {
                return true;
            }
        }
        return false;
    }

    public List<UserImageSourceBuffer> ImageList { get; }

    private SourceType srcType;
    private int bitsPerPixelForPalette;
    private bool rawPixelMode;
    private int x;
    private int y;
    private int width;
    private int height;
    private float scalex;
    private float scaley;
    private UInt32[] colorPalette;
}

public class UserImageSourceBuffer
{
    static public UserImageSourceBuffer CreateObject(string par_line)
    {
        char[] token = { ' ' };
        string[] split = par_line.Split(token);

        if(!split[0].Equals("i3write"))
        {
            return null;
        }
        if (!split[1].Equals("32"))
        {
            return null;
        }

        UserFrameProperty.SourceType type;
        string source_buffer;
        switch (split[2])
        {
            case "0x0004281C": source_buffer = "FETCHDECODE0"; type = UserFrameProperty.SourceType.SRC_FETCH_DECODE0; break;

            case "0x00043014": source_buffer = "FETCHLAYER0.0"; type = UserFrameProperty.SourceType.SRC_FETCHLAYER0; break;
            case "0x0004303C": source_buffer = "FETCHLAYER0.1"; type = UserFrameProperty.SourceType.SRC_FETCHLAYER0; break;
            case "0x00043064": source_buffer = "FETCHLAYER0.2"; type = UserFrameProperty.SourceType.SRC_FETCHLAYER0; break;
            case "0x0004308C": source_buffer = "FETCHLAYER0.3"; type = UserFrameProperty.SourceType.SRC_FETCHLAYER0; break;
            case "0x000430B4": source_buffer = "FETCHLAYER0.4"; type = UserFrameProperty.SourceType.SRC_FETCHLAYER0; break;
            case "0x000430DC": source_buffer = "FETCHLAYER0.5"; type = UserFrameProperty.SourceType.SRC_FETCHLAYER0; break;
            case "0x00043104": source_buffer = "FETCHLAYER0.6"; type = UserFrameProperty.SourceType.SRC_FETCHLAYER0; break;
            case "0x0004312C": source_buffer = "FETCHLAYER0.7"; type = UserFrameProperty.SourceType.SRC_FETCHLAYER0; break;
            case "0x00043154": source_buffer = "FETCHLAYER0.8"; type = UserFrameProperty.SourceType.SRC_FETCHLAYER0; break;
            case "0x0004317C": source_buffer = "FETCHLAYER0.9"; type = UserFrameProperty.SourceType.SRC_FETCHLAYER0; break;
            case "0x000431A4": source_buffer = "FETCHLAYER0.10"; type = UserFrameProperty.SourceType.SRC_FETCHLAYER0; break;
            case "0x000431CC": source_buffer = "FETCHLAYER0.11"; type = UserFrameProperty.SourceType.SRC_FETCHLAYER0; break;
            case "0x000431F4": source_buffer = "FETCHLAYER0.12"; type = UserFrameProperty.SourceType.SRC_FETCHLAYER0; break;
            case "0x0004321C": source_buffer = "FETCHLAYER0.13"; type = UserFrameProperty.SourceType.SRC_FETCHLAYER0; break;
            case "0x00043244": source_buffer = "FETCHLAYER0.14"; type = UserFrameProperty.SourceType.SRC_FETCHLAYER0; break;
            case "0x0004326C": source_buffer = "FETCHLAYER0.15"; type = UserFrameProperty.SourceType.SRC_FETCHLAYER0; break;

            case "0x00043814": source_buffer = "FETCHLAYER1.0"; type = UserFrameProperty.SourceType.SRC_FETCHLAYER1; break;
            case "0x0004383C": source_buffer = "FETCHLAYER1.1"; type = UserFrameProperty.SourceType.SRC_FETCHLAYER1; break;
            case "0x00043864": source_buffer = "FETCHLAYER1.2"; type = UserFrameProperty.SourceType.SRC_FETCHLAYER1; break;
            case "0x0004388C": source_buffer = "FETCHLAYER1.3"; type = UserFrameProperty.SourceType.SRC_FETCHLAYER1; break;
            case "0x000438B4": source_buffer = "FETCHLAYER1.4"; type = UserFrameProperty.SourceType.SRC_FETCHLAYER1; break;
            case "0x000438DC": source_buffer = "FETCHLAYER1.5"; type = UserFrameProperty.SourceType.SRC_FETCHLAYER1; break;
            case "0x00043904": source_buffer = "FETCHLAYER1.6"; type = UserFrameProperty.SourceType.SRC_FETCHLAYER1; break;
            case "0x0004392C": source_buffer = "FETCHLAYER1.7"; type = UserFrameProperty.SourceType.SRC_FETCHLAYER1; break;
            case "0x00043954": source_buffer = "FETCHLAYER1.8"; type = UserFrameProperty.SourceType.SRC_FETCHLAYER1; break;
            case "0x0004397C": source_buffer = "FETCHLAYER1.9"; type = UserFrameProperty.SourceType.SRC_FETCHLAYER1; break;
            case "0x000439A4": source_buffer = "FETCHLAYER1.10"; type = UserFrameProperty.SourceType.SRC_FETCHLAYER1; break;
            case "0x000439CC": source_buffer = "FETCHLAYER1.11"; type = UserFrameProperty.SourceType.SRC_FETCHLAYER1; break;
            case "0x000439F4": source_buffer = "FETCHLAYER1.12"; type = UserFrameProperty.SourceType.SRC_FETCHLAYER1; break;
            case "0x00043A1C": source_buffer = "FETCHLAYER1.13"; type = UserFrameProperty.SourceType.SRC_FETCHLAYER1; break;
            case "0x00043A44": source_buffer = "FETCHLAYER1.14"; type = UserFrameProperty.SourceType.SRC_FETCHLAYER1; break;
            case "0x00043A6C": source_buffer = "FETCHLAYER1.15"; type = UserFrameProperty.SourceType.SRC_FETCHLAYER1; break;
            default:    return null;
        }

        UserImageSourceBuffer obj = new UserImageSourceBuffer(source_buffer, split[2], type);

        return obj;
    }

    public UserImageSourceBuffer(string _source, string _address, UserFrameProperty.SourceType _type)
    {
        srcType = _type;
        Name = _source;
        Address = Convert.ToUInt32(_address, 16);
        colorBits = new int[4];
        colorShift = new int[4];
    }

    public UserImageSourceBuffer(string _source, int _address, UserFrameProperty.SourceType _type)
    {
        srcType = _type;
        Name = _source;
        Address = (UInt32)_address;
        colorBits = new int[4];
        colorShift = new int[4];
        sourceBufferEnable = false;
    }

    public string Name { get; }
    public UInt32 Address { get; }
    public string X { get { return x.ToString();  } }
    public string Y { get { return y.ToString(); } }
    public string Width { get { return width.ToString(); } }
    public string Height { get { return height.ToString(); } }
    public bool Enabled { get { return sourceBufferEnable; } }

    public bool setProperty(string addr, string value)
    {
        UInt32 address = Convert.ToUInt32(addr, 16);
        if (address % 4 > 0)
        {
            return false;
        }

        UInt32 offset = Address - address;
        if (offset > 36)
        {
            return false;
        }

        switch (offset)
        {
            case 4: // SOURCEBUFFERATTRIBUTES0
                {
                    setSourceAttribute(value);
                    break;
                }
            case 8: // SOURCEBUFFERDIMENSION0
                {
                    setSourceDimension(value);
                    break;
                }
            case 12:    // COLORCOMPONENTBITS0
                {
                    setColorBits(value);
                    break;
                }
            case 16:    // COLORCOMPONENTSHIFT0
                {
                    setColorShift(value);
                    break;
                }
            case 20:    // LAYEROFFSET0
                {
                    setLayerOffset(value);
                    break;
                }
            case 36:    // LAYERPROPERTY0
                {
                    setLayerProperty(value);
                    break;
                }
            default: return false;
        }

        return true;
    }

    private void setSourceAttribute(string value)
    {
        UInt32 Value = Convert.ToUInt32(value, 16);

        bitsPerPixel = (int)((Value >> 16) & 0x1F);
    }
    private void setSourceDimension(string value)
    {
        UInt32 Value = Convert.ToUInt32(value, 16);

        height = (int)((Value >> 16) & 0x3FFF); // Height
        width = (int)((Value >> 0) & 0x3FFF); // Width
    }
    private void setColorBits(string value)
    {
        UInt32 Value = Convert.ToUInt32(value, 16);

        colorBits[0] = (int)((Value >> 24) & 0x0F); // R
        colorBits[1] = (int)((Value >> 16) & 0x0F); // G
        colorBits[2] = (int)((Value >> 8) & 0x0F);  // B
        colorBits[3] = (int)((Value >> 0) & 0x0F);  // A
    }
    private void setColorShift(string value)
    {
        UInt32 Value = Convert.ToUInt32(value, 16);

        colorShift[0] = (int)((Value >> 24) & 0x0F); // R
        colorShift[1] = (int)((Value >> 16) & 0x0F); // G
        colorShift[2] = (int)((Value >> 8) & 0x0F);  // B
        colorShift[3] = (int)((Value >> 0) & 0x0F);  // A
    }
    private void setLayerOffset(string value)
    {
        UInt32 Value = Convert.ToUInt32(value, 16);

        y = (int)((Value >> 16) & 0x7FFF); // Y
        x = (int)((Value >> 0) & 0x7FFF); // X
    }
    private void setLayerProperty(string value)
    {
        UInt32 Value = Convert.ToUInt32(value, 16);

        sourceBufferEnable = (((Value >> 31) & 0x01) > 0);
        alpheEnable = (((Value >> 11) & 0x01) > 0);
        paletteEnable = (((Value >> 0) & 0x01) > 0);
    }

    private UserFrameProperty.SourceType srcType;
    private int bitsPerPixel;
    private int x;
    private int y;
    private int width;
    private int height;
    private int[] colorBits;
    private int[] colorShift;

    private bool sourceBufferEnable;
    private bool alpheEnable;
    private bool paletteEnable;
}

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
using System.Drawing;
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
        private byte[] SourceData;

        public MainWindow()
        {
            InitializeComponent();

            FetchDecode0 = new UserFrameProperty(UserFrameProperty.SourceType.SRC_FETCH_DECODE0);
            FetchLayer0 = new UserFrameProperty(UserFrameProperty.SourceType.SRC_FETCHLAYER0);
            FetchLayer1 = new UserFrameProperty(UserFrameProperty.SourceType.SRC_FETCHLAYER1);

            listFetchDecode0.ItemsSource = FetchDecode0.ImageList;
            listFetchLayer0.ItemsSource = FetchLayer0.ImageList;
            listFetchLayer1.ItemsSource = FetchLayer1.ImageList;

            listFetchDecode0.MouseLeftButtonUp += listFetchDecode0_CurrentChanged;
            listFetchDecode0.SelectionChanged += listFetchDecode0_CurrentChanged;
            listFetchLayer0.MouseLeftButtonUp += listFetchLayer0_CurrentChanged;
            listFetchLayer0.SelectionChanged += listFetchLayer0_CurrentChanged;
            listFetchLayer1.MouseLeftButtonUp += listFetchLayer1_CurrentChanged;
            listFetchLayer1.SelectionChanged += listFetchLayer1_CurrentChanged;

            SourceData = null;

            cvsDispBuff.Visibility = Visibility.Hidden;
        }

        private void btnOpenPar_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "Par File(*.par)|*.par";
            if (dialog.ShowDialog() == true)
            {
                StreamReader parReader = File.OpenText(dialog.FileName);
                lblParIndigo3.Text = dialog.SafeFileName;

                while (!parReader.EndOfStream)
                {
                    string line = parReader.ReadLine();
                    string address;
                    string value;

                    bool result = false;
                    UserFrameProperty.ParType type = UserFrameProperty.getCmdType(line, out address, out value);
                    // fetch decode 0
                    if(type == UserFrameProperty.ParType.PAR_TYPE_FETCH_DECODE_GLOBAL)
                    {
                        result = FetchDecode0.setProperty(UserFrameProperty.SourceType.SRC_FETCH_DECODE0, address, value);
                    }
                    else if(type == UserFrameProperty.ParType.PAR_TYPE_FETCH_DECODE_PALETTE)
                    {
                        result = FetchDecode0.setColorPalette(UserFrameProperty.SourceType.SRC_FETCH_DECODE0, address, value);
                    }
                    else if(type == UserFrameProperty.ParType.PAR_TYPE_FETCH_DECODE)
                    {
                        result = FetchDecode0.setSrcProperty(type, address, value);
                    }
                    // fetch layer 0
                    else if(type == UserFrameProperty.ParType.PAR_TYPE_FETCH_LAYER0_GLOBAL)
                    {
                        result = FetchLayer0.setProperty(UserFrameProperty.SourceType.SRC_FETCHLAYER0, address, value);
                    }
                    else if (type == UserFrameProperty.ParType.PAR_TYPE_FETCH_LAYER0_PALETTE)
                    {
                        result = FetchLayer0.setColorPalette(UserFrameProperty.SourceType.SRC_FETCHLAYER0, address, value);
                    }
                    else if (type == UserFrameProperty.ParType.PAR_TYPE_FETCH_LAYER0)
                    {
                        result = FetchLayer0.setSrcProperty(type, address, value);
                    }
                    // fetch layer 1
                    else if(type == UserFrameProperty.ParType.PAR_TYPE_FETCH_LAYER1_GLOBAL)
                    {
                        result = FetchLayer1.setProperty(UserFrameProperty.SourceType.SRC_FETCHLAYER1, address, value);
                    }
                    else if (type == UserFrameProperty.ParType.PAR_TYPE_FETCH_LAYER1_PALETTE)
                    {
                        result = FetchLayer1.setColorPalette(UserFrameProperty.SourceType.SRC_FETCHLAYER1, address, value);
                    }
                    else if (type == UserFrameProperty.ParType.PAR_TYPE_FETCH_LAYER1)
                    {
                        result = FetchLayer1.setSrcProperty(type, address, value);
                    }
                    else
                    {
                        continue;
                    }

                    if(!result)
                    {
                        Console.WriteLine("** error(parse): {0} {1}", type, line);
                    }
                }


                lblFetchDecode0Size.Content = String.Format("{0} x {1}", FetchDecode0.Width, FetchDecode0.Height);
                lblFetchDecode0PaletteWidth.Content = String.Format("{0}", FetchDecode0.BitsPerPixelForPalette);
                lblFetchDecode0Scale.Content = String.Format("x:{0}, y:{1}", FetchDecode0.ScaleX, FetchDecode0.ScaleY);
                listFetchDecode0.Items.Refresh();

                lblFetchLayer0Size.Content = String.Format("{0} x {1}", FetchLayer0.Width, FetchLayer0.Height);
                lblFetchLayer0PaletteWidth.Content = String.Format("{0}", FetchLayer0.BitsPerPixelForPalette);
                lblFetchLayer0Scale.Content = String.Format("x:{0}, y:{1}", FetchLayer0.ScaleX, FetchLayer0.ScaleY);
                listFetchLayer0.Items.Refresh();

                lblFetchLayer1Size.Content = String.Format("{0} x {1}", FetchLayer1.Width, FetchLayer1.Height);
                lblFetchLayer1PaletteWidth.Content = String.Format("{0}", FetchLayer1.BitsPerPixelForPalette);
                lblFetchLayer1Scale.Content = String.Format("x:{0}, y:{1}", FetchLayer1.ScaleX, FetchLayer1.ScaleY);
                listFetchLayer1.Items.Refresh();
            }
        }

        private void btnOpenBin_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "Bin File(*.bin)|*.bin";
            if (dialog.ShowDialog() == true)
            {
                try
                {
                    FileStream binReader = File.Open(dialog.FileName, FileMode.Open);
                    lblBinIndigo3.Text = dialog.SafeFileName;

                    SourceData = new byte[binReader.Length];

                    binReader.Read(SourceData, 0, (int)binReader.Length);

                    binReader.Close();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }

        private void mainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
        }

        private void StackPanel_SizeChanged(object sender, SizeChangedEventArgs e)
        {
        }


        /**************************/
        //     Panel
        /**************************/

        private void listFetchDecode0_CurrentChanged(object sender, EventArgs e)
        {
            UserImageSourceBuffer item = listFetchDecode0.SelectedItem as UserImageSourceBuffer;
            showImageSourceBufferInfo(item);
        }

        private void listFetchLayer0_CurrentChanged(object sender, EventArgs e)
        {

            UserImageSourceBuffer item = listFetchLayer0.SelectedItem as UserImageSourceBuffer;
            showImageSourceBufferInfo(item);
        }

        private void listFetchLayer1_CurrentChanged(object sender, EventArgs e)
        {

            UserImageSourceBuffer item = listFetchLayer1.SelectedItem as UserImageSourceBuffer;
            showImageSourceBufferInfo(item);
        }
        
        /**************************/
        //     User Function
        /**************************/

        private void showImageSourceBufferInfo(UserImageSourceBuffer item)
        {
            if (item == null)
            {
                return;
            }

            lblSource.Content = String.Format("{0} ({1})", item.Name, item.Enabled?"O":"X");
            lblSourceBufferBaseAddress.Content = String.Format("0x{0:X8}", item.SourceBufferAddress);
            lblDispSize.Content = String.Format("{0} x {1}", item.Width, item.Height);
            lblDispBitsPerPixel.Content = String.Format("{0}", item.BitsPerPixel);

            if (item.Enabled)
            {
                cvsDispBuff.Width = item.Width;
                cvsDispBuff.Height = item.Height;
                cvsDispBuff.Visibility = Visibility.Visible;
            }
            else
            {
                cvsDispBuff.Visibility = Visibility.Hidden;
            }

            drawImage(item);
        }

        private void drawImage(UserImageSourceBuffer item)
        {
            if(SourceData == null)
            {
                lblDrawStatus.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 0, 0));
                lblDrawStatus.Content = "null data";
                return;
            }
            if(!cvsDispBuff.IsVisible)
            {
                lblDrawStatus.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 0, 0));
                lblDrawStatus.Content = "No Info";
                return;
            }

            lblDrawStatus.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 0, 0));
            lblDrawStatus.Content = "Loading";

            int offset = Convert.ToInt32(txtBinOffset.Text, 16);
            int startPos = item.SourceBufferAddress - 0x017C0000 - offset;
            UInt32[] palette;
            int palette_bits;
            int palette_offset;

            if (item.SrcType == UserFrameProperty.SourceType.SRC_FETCH_DECODE0)
            {
                palette = FetchDecode0.ColorPalette;
                palette_bits = FetchDecode0.BitsPerPixelForPalette;
                palette_offset = 0;
            }
            else if (item.SrcType == UserFrameProperty.SourceType.SRC_FETCHLAYER0)
            {
                palette = FetchLayer0.ColorPalette;
                palette_bits = FetchLayer0.BitsPerPixelForPalette;
                palette_offset = (1 << palette_bits) * item.BaseIdx;
            }
            else if (item.SrcType == UserFrameProperty.SourceType.SRC_FETCHLAYER1)
            {
                palette = FetchLayer1.ColorPalette;
                palette_bits = FetchLayer1.BitsPerPixelForPalette;
                palette_offset = (1 << palette_bits) * item.BaseIdx;
            }
            else
            {
                return;
            }

            cvsDispBuff.Children.Clear();
            Bitmap image = new Bitmap(item.Width, item.Height);
            int pixel_mask = (1 << item.BitsPerPixel) - 1;
            
            try
            {
                bool stop = false;
                for (int h = 0; (h < item.Height) && !stop; h++)
                {
                    for (int w = 0; (w < item.Height) && !stop; w++)
                    {
                        int bit_pos = (item.Width * h * palette_bits) + w * palette_bits;
                        int byte_pos = startPos + (bit_pos / 8);
                        int bit_pos_ind = bit_pos % 8;

                        if(byte_pos >= SourceData.Length)
                        {
                            stop = true;
                            break;
                        }

                        byte pix_byte = SourceData[byte_pos];
                        int index = (pix_byte >> bit_pos_ind) & pixel_mask;

                        System.Drawing.Color color = item.getColorFromPalette(palette[palette_offset + index]);

                        image.SetPixel(w, h, color);
                    }
                }

                // Bitmap 담을 메모리스트림 준비
                MemoryStream ms = new MemoryStream();   // 초기화
                image.Save(ms, System.Drawing.Imaging.ImageFormat.Bmp);

                // BitmapImage 로 변환
                var bi = new BitmapImage();
                bi.BeginInit();
                bi.StreamSource = ms;
                bi.CacheOption = BitmapCacheOption.OnLoad;
                bi.EndInit();

                ImageBrush ib = new ImageBrush();
                ib.ImageSource = bi;
                cvsDispBuff.Background = ib;

                if(stop)
                {
                    lblDrawStatus.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 0, 0));
                    lblDrawStatus.Content = "buffer out of range";
                    Console.WriteLine("** error(drawImage): out of range");
                }
                else
                {
                    lblDrawStatus.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 0, 255));
                    lblDrawStatus.Content = "Complete";
                }
            }
            catch (Exception ex)
            {
                lblDrawStatus.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 0, 0));
                lblDrawStatus.Content = ex.Message;
                Console.WriteLine("** error(drawImage): " + ex.Message);
            }
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
            UserImageSourceBuffer obj = new UserImageSourceBuffer(base_idx, base_address, type);
            return obj;
        }
        if (type == SourceType.SRC_FETCHLAYER1)
        {
            int base_address = 0x00043814 + (base_idx * 0x28);
            UserImageSourceBuffer obj = new UserImageSourceBuffer(base_idx, base_address, type);
            return obj;
        }
        if (type == SourceType.SRC_FETCH_DECODE0)
        {
            UserImageSourceBuffer obj = new UserImageSourceBuffer(0, 0x0004281C, type);
            return obj;
        }

        return null;
    }

    static public ParType getCmdType(string par_line, out string addr, out string value)
    {
        char[] token = { ' ' };
        string[] split = par_line.Split(token);

        addr = "";
        value = "";

        if (split.Length < 4)
        {
            return ParType.PAR_TYPE_NONE;
        }

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

    public List<UserImageSourceBuffer> ImageList { get; }
    public int Width { get { return width; } }
    public int Height { get { return height; } }
    public float ScaleX { get { return scalex; } }
    public float ScaleY { get { return scaley; } }
    public int BitsPerPixelForPalette { get { return bitsPerPixelForPalette; } }
    public UInt32[] ColorPalette { get { return colorPalette; } }

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
        if(!isParAvailable(_type))
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
                    height = (int)((value >> 16) & 0x3FFF)+1; // Height
                    width = (int)((value >> 0) & 0x3FFF)+1; // Width
                    break;
                }
            // CONTROL
            case 0x00042854:    // fetch decode0
            case 0x0004329C:    // fetch layer0
            case 0x00043A9C:    // fetch layer1
                {
                    bitsPerPixelForPalette = (int)((value >> 8) & 0x07)+1;
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
            default:
                {
                    return false;
                }
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

        UInt32 base_addr = 0;
        if(srcType == SourceType.SRC_FETCHLAYER0)
        {
            base_addr = 0x00043400;
        }
        if(srcType == SourceType.SRC_FETCHLAYER1)
        {
            base_addr = 0x00043C00;
        }
        if(srcType == SourceType.SRC_FETCH_DECODE0)
        {
            base_addr = 0x00042C00;
        }
        UInt32 palette_idx = (address - base_addr) / 4;
        if(palette_idx >= 256)
        {
            return false;
        }

        colorPalette[palette_idx] = value;

        return true;
    }

    private bool isParAvailable(ParType _type)
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
    public string Name { get; }
    public string strX { get { return x.ToString(); } }
    public string strY { get { return y.ToString(); } }
    public string strWidth { get { return width.ToString(); } }
    public string strHeight { get { return height.ToString(); } }

    public UserFrameProperty.SourceType SrcType { get { return srcType; } }
    public int BaseIdx { get; }
    public int Address { get; }
    public int SourceBufferAddress { get { return baseSourceBufferAddress; } }
    public int BitsPerPixel { get { return bitsPerPixel; } }
    public int X { get { return x; } }
    public int Y { get { return y; } }
    public int Width { get { return width; } }
    public int Height { get { return height; } }
    public bool Enabled { get { return sourceBufferEnable; } }
    
    public UserImageSourceBuffer(int base_idx, int _address, UserFrameProperty.SourceType _type)
    {
        BaseIdx = base_idx;
        srcType = _type;
        Address = _address;
        colorMask = new int[4];
        colorShift = new int[4];
        sourceBufferEnable = false;

        if (_type == UserFrameProperty.SourceType.SRC_FETCHLAYER0)
        {
            Name = "FETCHLAYER0-" + base_idx;
        }
        else if (_type == UserFrameProperty.SourceType.SRC_FETCHLAYER1)
        {
            Name = "FETCHLAYER1-" + base_idx;
        }
        else if (_type == UserFrameProperty.SourceType.SRC_FETCH_DECODE0)
        {
            Name = "FETCHDECODE0";
        }
        else
        {
            Name = "Unknown";
        }
    }

    public bool setProperty(string addr, string value)
    {
        int address = Convert.ToInt32(addr, 16);
        if (address % 4 > 0)
        {
            return false;
        }

        int offset = address - Address;
        if (offset > 36)
        {
            return false;
        }

        switch (offset)
        {
            case 0: // BASEADDRESS0
                {
                    setSourceBuffer(value);
                    break;
                }
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
            default:
                {
                    return false;
                }
        }

        return true;
    }

    public System.Drawing.Color getColorFromPalette(UInt32 palette)
    {
        int R = (byte)((palette >> colorShift[0]) & colorMask[0]); // R
        int G = (byte)((palette >> colorShift[1]) & colorMask[1]); // G
        int B = (byte)((palette >> colorShift[2]) & colorMask[2]);  // B
        int A = (byte)((palette >> colorShift[3]) & colorMask[3]);  // A

        return System.Drawing.Color.FromArgb(A, R, G, B);
    }

    private void setSourceBuffer(string value)
    {
        int Value = Convert.ToInt32(value, 16);

        baseSourceBufferAddress = Value;
    }
    private void setSourceAttribute(string value)
    {
        int Value = Convert.ToInt32(value, 16);

        bitsPerPixel = (int)((Value >> 16) & 0x1F);
    }
    private void setSourceDimension(string value)
    {
        int Value = Convert.ToInt32(value, 16);

        height = (int)((Value >> 16) & 0x3FFF)+1; // Height
        width = (int)((Value >> 0) & 0x3FFF)+1; // Width
    }
    private void setColorBits(string value)
    {
        int Value = Convert.ToInt32(value, 16);
        int[] colorBits;

        colorBits = new int[4];

        colorBits[0] = (int)((Value >> 24) & 0x0F); // R
        colorMask[0] = (1 << colorBits[0]) - 1;
        colorBits[1] = (int)((Value >> 16) & 0x0F); // G
        colorMask[1] = (1 << colorBits[1]) - 1;
        colorBits[2] = (int)((Value >> 8) & 0x0F);  // B
        colorMask[2] = (1 << colorBits[2]) - 1;
        colorBits[3] = (int)((Value >> 0) & 0x0F);  // A
        colorMask[3] = (1 << colorBits[3]) - 1;
    }
    private void setColorShift(string value)
    {
        int Value = Convert.ToInt32(value, 16);

        colorShift[0] = (int)((Value >> 24) & 0x1F); // R
        colorShift[1] = (int)((Value >> 16) & 0x1F); // G
        colorShift[2] = (int)((Value >> 8) & 0x1F);  // B
        colorShift[3] = (int)((Value >> 0) & 0x1F);  // A
    }
    private void setLayerOffset(string value)
    {
        int Value = Convert.ToInt32(value, 16);

        y = (int)((Value >> 16) & 0x7FFF); // Y
        x = (int)((Value >> 0) & 0x7FFF); // X
    }
    private void setLayerProperty(string value)
    {
        int Value = Convert.ToInt32(value, 16);

        sourceBufferEnable = (((Value >> 31) & 0x01) > 0);
        alpheEnable = (((Value >> 11) & 0x01) > 0);
        paletteEnable = (((Value >> 0) & 0x01) > 0);
    }

    private UserFrameProperty.SourceType srcType;
    private int baseSourceBufferAddress;
    private int bitsPerPixel;
    private int x;
    private int y;
    private int width;
    private int height;
    private int[] colorMask;
    private int[] colorShift;

    private bool sourceBufferEnable;
    private bool alpheEnable;
    private bool paletteEnable;
}

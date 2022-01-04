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

namespace Indigo3DisplayBuffer
{
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            lblDispWidth.Content = 1920;
            lblDispHeight.Content = 720;
        }

        private void btnOpenPar_Click(object sender, RoutedEventArgs e)
        {
            displayBuffer_SizeChange();
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

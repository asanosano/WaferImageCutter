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
using OpenCvSharp;
using OpenCvSharp.WpfExtensions;

namespace DkUtil
{
    /// <summary>
    /// DkImageList.xaml の相互作用ロジック
    /// </summary>
    public partial class DkImageList : UserControl
    {
        public DkImageList()
        {
            InitializeComponent();

            numDisplayMagnificationRate.Value = 1.0;
            numDisplayMagnificationRate.Maximum = 8.0;
            numDisplayMagnificationRate.Minimum = 0.1;
        }

        public void SetMatList(List<Mat> listMat)
        {
            MainWrapPanel.Children.Clear();

            foreach (Mat mat in listMat)
            {
                DkImageViewer viewer = new DkImageViewer();
                
                viewer.Margin = new Thickness(5);
                viewer.BaseToolPanelVisibility = Visibility.Hidden;
                viewer.SetDisplayMagnificationRate((double)numDisplayMagnificationRate.Value);
               numDisplayMagnificationRate.Value = viewer.DisplayMagnificationRate.Value;
                viewer.SetSourceImage(mat);
                viewer.Draw();

                MainWrapPanel.Children.Add(viewer);
            }
        }

        private void ButtonRedraw_Click(object sender, RoutedEventArgs e)
        {

            foreach(var t in MainWrapPanel.Children)
            {
                DkImageViewer viewer = t as DkImageViewer;
                if(viewer != null)
                {
                    viewer.SetDisplayMagnificationRate((double)numDisplayMagnificationRate.Value);
                    numDisplayMagnificationRate.Value = viewer.DisplayMagnificationRate.Value;

                }
            }
        }
    }
}

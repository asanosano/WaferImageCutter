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

namespace DkUtil
{
    /// <summary>
    /// DkFolderSelectControl.xaml の相互作用ロジック
    /// </summary>
  
    public partial class DkFolderSelectControl : UserControl
    {

        public string Label
        {
            get { return textLabel.Text; }
            set { textLabel.Text = value; }
        }
        public int LabelSize
        {
            get { return (int)textLabel.Width; }
            set { grid.ColumnDefinitions[0] = new ColumnDefinition { Width = new GridLength(value) }; }
        }

        public bool AutoLabel { get; set; } = false;

        public string TargetDir
        {
            get { return textTargetDir.Text; }
            set
            {
                textTargetDir.Text = value;
                textTargetDir.ToolTip = value;
                if (AutoLabel)
                {
                    Label = System.IO.Path.GetFileName(value);
                }
            }
        }
        public bool IsChecked
        {
            get { return cbEnable.IsChecked == true; }
            set { cbEnable.IsChecked = value; }
        }
        public Visibility CheckboxVisibility
        {
            get { return cbEnable.Visibility; }
            set { cbEnable.Visibility = value; }
        }


        public event EventHandler TargetChanged;
        protected virtual void OnTargetChanged(EventArgs e)
        {
            if (TargetChanged != null)
                TargetChanged(this, e);
        }
        public event EventHandler CheckChanged;
        protected virtual void OnCheckChanged(EventArgs e)
        {
            if (CheckChanged != null)
                CheckChanged(this, e);
        }
        public DkFolderSelectControl()
        {
            InitializeComponent();
        }

        private void BtnSelect_Click(object sender, RoutedEventArgs e)
        {
            if ((System.Windows.Input.Keyboard.Modifiers & (ModifierKeys.Control | ModifierKeys.Shift)) != ModifierKeys.None)
            {
                System.Diagnostics.Process.Start("EXPLORER.EXE", textTargetDir.Text);
                return;
            }
            System.Windows.Forms.FolderBrowserDialog d = new System.Windows.Forms.FolderBrowserDialog();
            d.SelectedPath = textTargetDir.Text;

            if (d.ShowDialog() != System.Windows.Forms.DialogResult.OK)
            {
                return;
            }
            TargetDir = d.SelectedPath;
            OnTargetChanged(EventArgs.Empty);
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            //   this.Height = FontSize + 10;
        }

        private void cbEnable_CheckChanged(object sender, RoutedEventArgs e)
        {
            OnCheckChanged(EventArgs.Empty);
        }

      
    }
}

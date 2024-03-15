using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.IO;
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
    /// DkFileSelect.xaml の相互作用ロジック
    /// </summary>
    public partial class DkFileSelect : UserControl
    {
        //public System.IO.SearchOption SearchOption { get; set; } = System.IO.SearchOption.TopDirectoryOnly;
        //  string FilePattern { get { return TextTargetPattern.Text + ComboBoxTargetExtPattern.SelectedItem.ToString(); } }


        public String TargetExtPattern
        {
            set
            {
                foreach (var t in ComboBoxTargetExtPattern.Items)
                {
                    var textBlock = (TextBlock)t;
                    var ext = textBlock.Text;

                    if (ext == value)
                    {
                        ComboBoxTargetExtPattern.SelectedItem = textBlock;
                    }
                }
            }
        }
        public string TargetFilePath
        {
            get
            {
                if (ListBoxTargetFiles.SelectedItem == null)
                {
                    return null;

                }
                return System.IO.Path.Combine(TextTargetDir.Text, (string)((ListBoxItem)ListBoxTargetFiles.SelectedItem).Content);
            }
        }
        public string TargetDir
        {
            get
            {
                return TextTargetDir.Text;
            }
        }

        public DkFileSelect()
        {
            InitializeComponent();
            ListBoxTargetFiles.AllowDrop = true;
            ComboBoxTargetExtPattern.SelectedIndex = 1;
        }



        private void BtnSelectTargetDir_Click(object sender, RoutedEventArgs e)
        {
            using CommonOpenFileDialog dialog = new CommonOpenFileDialog()
            {
                Title = "フォルダを選択してください。",
                InitialDirectory = TextTargetDir.Text,
                IsFolderPicker = true,
            };

            if (dialog.ShowDialog() != CommonFileDialogResult.Ok)
            {
                return;
            }

            SetTargetDir(dialog.FileName);

        }
        public void SetTargetDir(string dir)
        {
            if (dir == null || !System.IO.Directory.Exists(dir))
            {
                return;
            }
            if (ComboBoxTargetExtPattern.SelectedItem == null)
            {
                return;
            }
            TextTargetDir.Text = dir;
            TextTargetDir.ToolTip = dir;
            string s1 = TextTargetPattern.Text;
            string s2 = ((TextBlock)ComboBoxTargetExtPattern.SelectedItem).Text;
            string s3 = s1 + s2;
            var searchOp = (bool)this.ChkSubDir.IsChecked ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            string[] files = System.IO.Directory.GetFiles(TextTargetDir.Text, s3, searchOp);

            int ln = TextTargetDir.Text.Length + 1;
            for (int i = 0; i < files.Length; i++)
            {
                files[i] = files[i].Remove(0, ln);
            }

            ListBoxTargetFiles.Items.Clear();
            foreach (string file in files)
            {
                ListBoxItem item = new ListBoxItem();
                item.Content = file;
                item.ToolTip = file;

                ListBoxTargetFiles.Items.Add(item);
            }
            if (ListBoxTargetFiles.Items.Count > 0)
            {
                ListBoxTargetFiles.SelectedIndex = 0;
            }
        }


        //void SearchFiles(string? topDir, string[] pathArray)
        //{
        //    if (pathArray == null || pathArray.Length == 0)
        //    {
        //        return;
        //    }
        //    if (topDir == null || topDir.Length == 0)
        //    {
        //        string tmp = pathArray[0];
        //        if (System.IO.File.GetAttributes(pathArray[0]).HasFlag(System.IO.FileAttributes.Directory))
        //        {
        //            topDir = pathArray[0];
        //        }
        //        else
        //        {
        //            topDir = System.IO.Path.GetDirectoryName(pathArray[0]);
        //        }
        //        // System.IO.Directory.GetFiles()
        //    }
        //    Dictionary<string, string> fileList = new();
        //    foreach (string path in pathArray)
        //    {

        //    }
        //    return;

        //}
        public event EventHandler TargetChanged;
        protected virtual void OnTargetChanged(EventArgs e)
        {
            //if (TargetChanged != null)
            //    TargetChanged(this, e);

            BtnLoad.IsEnabled = false;
            BtnNext.IsEnabled = false;
            BtnPrev.IsEnabled = false;

            if (TargetChanged != null)
                TargetChanged(this, e);

            BtnLoad.IsEnabled = true;
            BtnNext.IsEnabled = true;
            BtnPrev.IsEnabled = true;
        }
        private void BtnLoad_Click(object sender, RoutedEventArgs e)
        {
            OnTargetChanged(EventArgs.Empty);
        }


        public List<string> GetAllFiles()
        {
            List<string> fileList = new List<string>();

            foreach (var t in ListBoxTargetFiles.Items)
            {
                //  string file = System.IO.Path.Combine(TextTargetDir.Text, (string)((ListBoxItem)ListBoxTargetFiles.SelectedItem).Content);
                string file = System.IO.Path.Combine(TextTargetDir.Text, (string)((ListBoxItem)t).Content);
                fileList.Add(file);
            }
            return fileList;
        }


        private void BtnPrev_Click(object sender, RoutedEventArgs e)
        {
            if (ListBoxTargetFiles.SelectedIndex == 0 || ListBoxTargetFiles.Items.Count == 0)
            {
                return;
            }
            ListBoxTargetFiles.SelectedIndex = ListBoxTargetFiles.SelectedIndex - 1;
            ListBoxTargetFiles.ScrollIntoView(ListBoxTargetFiles.SelectedItem);
            OnTargetChanged(EventArgs.Empty);
        }

        private void BtnNext_Click(object sender, RoutedEventArgs e)
        {
            if (ListBoxTargetFiles.SelectedIndex == ListBoxTargetFiles.Items.Count - 1 || ListBoxTargetFiles.Items.Count == 0)
            {
                return;
            }
            ListBoxTargetFiles.SelectedIndex = ListBoxTargetFiles.SelectedIndex + 1;
            ListBoxTargetFiles.ScrollIntoView(ListBoxTargetFiles.SelectedItem);
            OnTargetChanged(EventArgs.Empty);
        }

        private void ComboBoxTargetExtPattern_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SetTargetDir(TextTargetDir.Text);
        }

        private void TextTargetPattern_TextChanged(object sender, TextChangedEventArgs e)
        {
            SetTargetDir(TextTargetDir.Text);
        }

        private void ListBoxTargetFiles_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ListBoxTargetFiles.SelectedIndex < 0)
            {
                return;
            }
            if (ListBoxTargetFiles.InputHitTest(e.GetPosition((IInputElement)ListBoxTargetFiles.SelectedItem)) == null)
            {
                return;
            }

            OnTargetChanged(EventArgs.Empty);
        }

        private void ListBoxTargetFiles_Drop(object sender, DragEventArgs e)
        {
            string[] inputItems = e.Data.GetData(DataFormats.FileDrop) as string[];
            if (inputItems == null)
            {
                return;
            }
            if (inputItems.Length == 1)
            {
                if (System.IO.Directory.Exists(inputItems[0]))
                {
                    SetTargetDir(inputItems[0]);
                    return;

                }
            }
            TextTargetDir.Text = System.IO.Path.GetDirectoryName(inputItems[0]);


            string s1 = TextTargetPattern.Text;
            string s2 = ((TextBlock)ComboBoxTargetExtPattern.SelectedItem).Text;
            string s3 = s1 + s2;

            List<string> fileList = new List<string>();
            foreach (string item in inputItems)
            {
                if (System.IO.File.GetAttributes(item).HasFlag(System.IO.FileAttributes.Directory))
                {
                    var searchOp = (bool)this.ChkSubDir.IsChecked ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
                    string[] files = System.IO.Directory.GetFiles(item, s3, searchOp);
                    fileList.AddRange(files);
                }
                else
                {
                    fileList.Add(item);
                }
            }
            ListBoxTargetFiles.Items.Clear();

            int ln = TextTargetDir.Text.Length + 1;
            foreach (string file in fileList)
            {
                string rPath = file.Remove(0, ln);
                ListBoxItem item = new ListBoxItem();
                item.Content = rPath;
                item.ToolTip = rPath;

                ListBoxTargetFiles.Items.Add(item);
            }
            if (ListBoxTargetFiles.Items.Count > 0)
            {
                ListBoxTargetFiles.SelectedIndex = 0;
            }
        }

        private void ListBoxTargetFiles_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetData(DataFormats.FileDrop) != null)
            {
                e.Effects = DragDropEffects.Copy;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }

            e.Handled = true;
        }

        private void ChkSubDir_Checked(object sender, RoutedEventArgs e)
        {
            SetTargetDir(TextTargetDir.Text);
        }

        private void ChkSubDir_Unchecked(object sender, RoutedEventArgs e)
        {
            SetTargetDir(TextTargetDir.Text);
        }
    }
}


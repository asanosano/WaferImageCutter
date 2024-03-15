using IjhCommonUtility;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RICommonWinUtility
{
    public partial class DataContractPropertyGridForm : Form
    {

        IEnumerable<Type> knownTypes = null;

        public DataContractPropertyGridForm()
        {
            InitializeComponent();

            this.ToolStripMenuItem_file.Text = "File";
            this.ToolStripMenuItem_read.Text = "Read";
            this.ToolStripMenuItem_write.Text = "Write";
        }

        public DataContractPropertyGridForm(string formName)
            : this()
        {
            this.Text = formName;
        }

        public DataContractPropertyGridForm(string formName, object dataContractObject, PropertySort sort = PropertySort.Categorized)
            :this(formName)
        {
            this.SetObjectToPropertyGrid(dataContractObject, sort);
        }

        public void SetObjectToPropertyGrid(object dataContractObject, PropertySort sort = PropertySort.Categorized)
        {
            this.propertyGrid1.SelectedObject = dataContractObject;
            this.propertyGrid1.PropertySort = sort;
        }

        public void SetKnownTypes(IEnumerable<Type> knownTypes)
        {
            this.knownTypes = knownTypes;
        }

        public void SetInitDirectory(string path, string name)
        {
            this.openFileDialog1.InitialDirectory = path;
            this.openFileDialog1.FileName = name;
            this.saveFileDialog1.InitialDirectory = path;
            this.saveFileDialog1.FileName = name;
        }

        public override void Refresh()
        {
            base.Refresh();
            this.propertyGrid1.Refresh();
        }

        private void ToolStripMenuItem_read_Click(object sender, EventArgs e)
        {
            this.openFileDialog1.Filter = "XML(*.xml)|*.xml";
            if (this.openFileDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                DataContractReaderWriter.ReadXml_WithoutException(this.propertyGrid1.SelectedObject, this.openFileDialog1.FileName, this.knownTypes);
            }
            this.propertyGrid1.Refresh();
        }

        private void ToolStripMenuItem_write_Click(object sender, EventArgs e)
        {
            this.saveFileDialog1.Filter = "XML(*.xml)|*.xml";
            if (this.saveFileDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                DataContractReaderWriter.WriteXml_WithoutException(this.propertyGrid1.SelectedObject, this.saveFileDialog1.FileName, this.knownTypes);
            }
        }
    }
}

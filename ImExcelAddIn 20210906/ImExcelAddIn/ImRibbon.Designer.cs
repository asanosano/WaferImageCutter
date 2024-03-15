
namespace ImExcelAddIn
{
    partial class ImRibbon : Microsoft.Office.Tools.Ribbon.RibbonBase
    {
        /// <summary>
        /// 必要なデザイナー変数です。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        public ImRibbon()
            : base(Globals.Factory.GetRibbonFactory())
        {
            InitializeComponent();
        }

        /// <summary> 
        /// 使用中のリソースをすべてクリーンアップします。
        /// </summary>
        /// <param name="disposing">マネージド リソースを破棄する場合は true を指定し、その他の場合は false を指定します。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region コンポーネント デザイナーで生成されたコード

        /// <summary>
        /// デザイナー サポートに必要なメソッドです。このメソッドの内容を
        /// コード エディターで変更しないでください。
        /// </summary>
        private void InitializeComponent()
        {
            this.tab1 = this.Factory.CreateRibbonTab();
            this.group = this.Factory.CreateRibbonGroup();
            this.btnInsertImage = this.Factory.CreateRibbonButton();
            this.txtDstColumn = this.Factory.CreateRibbonEditBox();
            this.txtImageZoom = this.Factory.CreateRibbonEditBox();
            this.cbRotation = this.Factory.CreateRibbonCheckBox();
            this.btnRayout = this.Factory.CreateRibbonButton();
            this.button1 = this.Factory.CreateRibbonButton();
            this.tab1.SuspendLayout();
            this.group.SuspendLayout();
            this.SuspendLayout();
            // 
            // tab1
            // 
            this.tab1.ControlId.ControlIdType = Microsoft.Office.Tools.Ribbon.RibbonControlIdType.Office;
            this.tab1.Groups.Add(this.group);
            this.tab1.Label = "TabAddIns";
            this.tab1.Name = "tab1";
            // 
            // group
            // 
            this.group.Items.Add(this.btnInsertImage);
            this.group.Items.Add(this.txtDstColumn);
            this.group.Items.Add(this.txtImageZoom);
            this.group.Items.Add(this.cbRotation);
            this.group.Items.Add(this.btnRayout);
            this.group.Items.Add(this.button1);
            this.group.Label = "画像貼り付け";
            this.group.Name = "group";
            // 
            // btnInsertImage
            // 
            this.btnInsertImage.Label = "画像貼り付け";
            this.btnInsertImage.Name = "btnInsertImage";
            this.btnInsertImage.Click += new Microsoft.Office.Tools.Ribbon.RibbonControlEventHandler(this.btnInsertImage_Click);
            // 
            // txtDstColumn
            // 
            this.txtDstColumn.Label = "配置先 (+)1-9 or A-Z";
            this.txtDstColumn.MaxLength = 3;
            this.txtDstColumn.Name = "txtDstColumn";
            this.txtDstColumn.Text = "1";
            // 
            // txtImageZoom
            // 
            this.txtImageZoom.Label = "画像倍率";
            this.txtImageZoom.Name = "txtImageZoom";
            this.txtImageZoom.Text = "0.5";
            // 
            // cbRotation
            // 
            this.cbRotation.Label = "回転";
            this.cbRotation.Name = "cbRotation";
            // 
            // btnRayout
            // 
            this.btnRayout.Label = "Z再配置";
            this.btnRayout.Name = "btnRayout";
            this.btnRayout.Click += new Microsoft.Office.Tools.Ribbon.RibbonControlEventHandler(this.btnRayout_Click);
            // 
            // button1
            // 
            this.button1.Label = "";
            this.button1.Name = "button1";
            // 
            // ImRibbon
            // 
            this.Name = "ImRibbon";
            this.RibbonType = "Microsoft.Excel.Workbook";
            this.Tabs.Add(this.tab1);
            this.Load += new Microsoft.Office.Tools.Ribbon.RibbonUIEventHandler(this.ImRibbon_Load);
            this.tab1.ResumeLayout(false);
            this.tab1.PerformLayout();
            this.group.ResumeLayout(false);
            this.group.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        internal Microsoft.Office.Tools.Ribbon.RibbonTab tab1;
        internal Microsoft.Office.Tools.Ribbon.RibbonGroup group;
        internal Microsoft.Office.Tools.Ribbon.RibbonEditBox txtDstColumn;
        internal Microsoft.Office.Tools.Ribbon.RibbonEditBox txtImageZoom;
        internal Microsoft.Office.Tools.Ribbon.RibbonButton btnInsertImage;
        internal Microsoft.Office.Tools.Ribbon.RibbonButton btnRayout;
        internal Microsoft.Office.Tools.Ribbon.RibbonButton button1;
        internal Microsoft.Office.Tools.Ribbon.RibbonCheckBox cbRotation;
    }

    partial class ThisRibbonCollection
    {
        internal ImRibbon ImRibbon
        {
            get { return this.GetRibbon<ImRibbon>(); }
        }
    }
}


using Patagames.Ocr;
using Patagames.Ocr.Enums;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace OCRExtractTable
{
    public partial class Default : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
            }
        }

        protected void btnOCRReader_Click(object sender, EventArgs e)
        {
            string filePath = Server.MapPath("~/uploads/" + Path.GetFileName(hdnUploadedImage.Value));

            // Crop Image Here & Save
            string fileName = Path.GetFileName(filePath);
            string cropFileName = "";
            string cropFilePath = "";
            if (File.Exists(filePath))
            {
                System.Drawing.Image orgImg = System.Drawing.Image.FromFile(filePath);
                Rectangle CropArea = new Rectangle(
                    Convert.ToInt32(X.Value),
                    Convert.ToInt32(Y.Value),
                    Convert.ToInt32(W.Value),
                    Convert.ToInt32(H.Value));
                try
                {
                    Bitmap bitMap = new Bitmap(CropArea.Width, CropArea.Height);
                    using (Graphics g = Graphics.FromImage(bitMap))
                    {
                        g.DrawImage(orgImg, new Rectangle(0, 0, bitMap.Width, bitMap.Height), CropArea, GraphicsUnit.Pixel);
                    }
                    cropFileName = "crop_" + fileName;
                    cropFilePath = Path.Combine(Server.MapPath("~/uploads"), cropFileName);
                    bitMap.Save(cropFilePath);
                    //Response.Redirect("~/UploadImages/" + cropFileName, false);
                }
                catch (Exception ex)
                {
                    //throw;
                }
            }
            string extractText = this.ExtractTextFromImage(cropFilePath);
            lblText.Text = extractText.Replace(Environment.NewLine, "<br />");
        }

        private string ExtractTextFromImage(string filePath)
        {
            string plainText = "";
            string datapath = Server.MapPath("~");
            List<System.Drawing.Image> cropimages = new List<System.Drawing.Image>();
            System.Drawing.Image img = System.Drawing.Image.FromFile(filePath, true);

            int totalColumns = 0;
            int.TryParse(txtColumns.Text, out totalColumns);
            int totalRows = 0;
            int.TryParse(txtRows.Text, out totalRows);
            cropimages = MultiCrop(filePath, img, totalRows, totalColumns);

            string directorypath = Server.MapPath("~/uploads/") + Path.GetFileNameWithoutExtension(filePath);
            if (!Directory.Exists(directorypath)) ;
            {
                Directory.CreateDirectory(directorypath);
            }

            DataTable dt = new DataTable();
            for (int i = 1; i <= totalColumns; i++)
            {
                dt.Columns.Add("Column" + i);
            }
            List<string> lstdata = new List<string>();

            for (int i = 0; i < cropimages.Count; i++)
            {
                {
                    string temp_crop_file = directorypath + "\\" + i + ".bmp";
                    {
                        cropimages[i].Save(temp_crop_file);

                    }
                    try
                    {
                        using (var api = OcrApi.Create())
                        {
                            api.Init(Languages.English, datapath);
                            using (var bmp = Bitmap.FromFile(temp_crop_file) as Bitmap)
                            {
                                lstdata.Add(api.GetTextFromImage(bmp));
                            }
                        }
                    }
                    catch
                    {
                        lstdata.Add("");
                    }
                }
            }

            for (int i = 0; i < lstdata.Count; i++)
            {
                DataRow dr = dt.NewRow();
                for (int j = 0; j < totalColumns; j++)
                {
                    dr[j] = lstdata[i];
                    i++;

                    if (i >= lstdata.Count)
                    {
                        break;
                    }
                }
                i--;
                dt.Rows.Add(dr);
            }

            GenerateReport(dt);
            string extractedText = "";
            //string extractedText = modiImage.Layout.Text;
            //modiDocument.Close();
            return extractedText;
        }

        public List<System.Drawing.Image> MultiCrop(string filepath, System.Drawing.Image img, int row, int col)
        {
            List<System.Drawing.Image> list = new List<System.Drawing.Image>();
            Graphics g = Graphics.FromImage(img);
            Brush redBrush = new SolidBrush(Color.Red);
            Pen pen = new Pen(redBrush, 3);
            for (int i = 0; i < row; i++)
            {
                for (int y = 0; y < col; y++)
                {
                    System.Drawing.Image temp = System.Drawing.Image.FromFile(filepath, true);


                    Rectangle r = new Rectangle(y * (img.Width / col),
                                                i * (img.Height / row),
                                                img.Width / col,
                                                img.Height / row);

                    g.DrawRectangle(pen, r);

                    list.Add(cropImage(temp, r));
                }
            }
            return list;
        }

        private System.Drawing.Image cropImage(System.Drawing.Image img, Rectangle cropArea)
        {
            Bitmap bmpImage = new Bitmap(img);
            Bitmap bmpCrop = bmpImage.Clone(cropArea, System.Drawing.Imaging.PixelFormat.DontCare);
            img.Dispose();
            return (System.Drawing.Image)(bmpCrop);

        }


        #region--Generate Excel--
        protected void GenerateReport(DataTable dt)
        {
            Response.ClearContent();
            Response.Buffer = true;
            Response.AddHeader("content-disposition", string.Format("attachment; filename={0}", "ToDaysOfferAddedReport" + DateTime.Now.ToString("dd_MM_yyyy_hh_mm_ss") + ".xls"));
            Response.ContentType = "application/ms-excel";

            string tab = string.Empty;

            foreach (DataColumn dc in dt.Columns)
            {
                Response.Write(tab + dc.ColumnName);
                tab = "\t";
            }
            Response.Write("\n");
            int i;
            foreach (DataRow dr in dt.Rows)
            {
                tab = "";
                for (i = 0; i < dt.Columns.Count; i++)
                {
                    Response.Write(tab + dr[i].ToString());
                    tab = "\t";
                }
                Response.Write("\n");
            }


            Response.End();
        }
        #endregion


    }
}
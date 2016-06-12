﻿
using Patagames.Ocr;
using Patagames.Ocr.Enums;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace OCRExtractTable
{
    public partial class Default : System.Web.UI.Page
    {
        #region--Page Load--
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
            }
        }
        #endregion

        #region--btnOCRReader_Click--
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
            this.ExtractTextFromImage(cropFilePath);

        }
        #endregion

        private string ExtractTextFromImage(string filePath)
        {
            string datapath = Server.MapPath("~");
            List<System.Drawing.Image> cropimages = new List<System.Drawing.Image>();
            System.Drawing.Image img = System.Drawing.Image.FromFile(filePath, true);

            int totalColumns = 0;
            int.TryParse(txtColumns.Text, out totalColumns);
            int totalRows = 0;
            int.TryParse(txtRows.Text, out totalRows);
            cropimages = MultiCrop(filePath, img, totalRows, totalColumns);

            string directorypath = Server.MapPath("~/uploads/") + Path.GetFileNameWithoutExtension(filePath);
            if (!Directory.Exists(directorypath))
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
                    string temp_crop_file = directorypath + "\\" + i + Path.GetExtension(filePath);
                    {
                        cropimages[i].Save(temp_crop_file);

                    }
                    try
                    {
                        string[] configs = { "config.cfg" };
                        using (var api = OcrApi.Create())
                        {
                            api.Init(Languages.English, datapath, OcrEngineMode.OEM_DEFAULT, configs);
                            using (var bmp = Bitmap.FromFile(temp_crop_file) as Bitmap)
                            {
                                string extracedText = api.GetTextFromImage(bmp);
                                lstdata.Add(extracedText); 
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
            System.Drawing.Image orgImage = (System.Drawing.Image)(bmpCrop);
            System.Drawing.Image newImage = resizeImage(orgImage, 400, 400);

            return newImage;

        }

        #region--Resize Image--
        public System.Drawing.Image resizeImage(System.Drawing.Image image, int maxWidth, int maxHeight)
        {
            var ratioX = (double)maxWidth / image.Width;
            var ratioY = (double)maxHeight / image.Height;
            var ratio = Math.Min(ratioX, ratioY);

            var newWidth = (int)(image.Width * ratio);
            var newHeight = (int)(image.Height * ratio);

            var newImage = new Bitmap(newWidth, newHeight);
            Graphics.FromImage(newImage).DrawImage(image, 0, 0, newWidth, newHeight);
            Bitmap bmp = new Bitmap(newImage);

            return (System.Drawing.Image)bmp;
        }
        #endregion

        #region--Generate Excel--
        private void GenerateReport(DataTable table)
        {
            HttpContext.Current.Response.Clear();
            HttpContext.Current.Response.ClearContent();
            HttpContext.Current.Response.ClearHeaders();
            HttpContext.Current.Response.Buffer = true;
            HttpContext.Current.Response.ContentType = "application/ms-excel";
            HttpContext.Current.Response.Write(@"<!DOCTYPE HTML PUBLIC ""-//W3C//DTD HTML 4.0 Transitional//EN"">");
            HttpContext.Current.Response.AddHeader("Content-Disposition", "attachment;filename=Reports.xls");

            HttpContext.Current.Response.Charset = "utf-8";
            HttpContext.Current.Response.ContentEncoding = System.Text.Encoding.GetEncoding("windows-1250");
            //sets font
            HttpContext.Current.Response.Write("<font style='font-size:10.0pt; font-family:Calibri;'>");
            HttpContext.Current.Response.Write("<BR><BR><BR>");
            //sets the table border, cell spacing, border color, font of the text, background, foreground, font height
            HttpContext.Current.Response.Write("<Table border='1' bgColor='#ffffff' " +
              "borderColor='#000000' cellSpacing='0' cellPadding='0' " +
              "style='font-size:10.0pt; font-family:Calibri; background:white;'> <TR>");
            //am getting my grid's column headers
            //    foreach (DataColumn dc in dt.Columns)
            //    {
            //        Response.Write(tab + dc.ColumnName);
            //        tab = "\t";
            //    }
            int columnscount = table.Columns.Count;

            for (int j = 0; j < columnscount; j++)
            {      //write in new column
                HttpContext.Current.Response.Write("<Td>");
                //Get column headers  and make it as bold in excel columns
                HttpContext.Current.Response.Write("<B>");
                HttpContext.Current.Response.Write(table.Columns[j].ToString());
                HttpContext.Current.Response.Write("</B>");
                HttpContext.Current.Response.Write("</Td>");
            }
            HttpContext.Current.Response.Write("</TR>");
            foreach (DataRow row in table.Rows)
            {//write in new row
                HttpContext.Current.Response.Write("<TR>");
                for (int i = 0; i < table.Columns.Count; i++)
                {
                    HttpContext.Current.Response.Write("<Td>");
                    HttpContext.Current.Response.Write(row[i].ToString());
                    HttpContext.Current.Response.Write("</Td>");
                }

                HttpContext.Current.Response.Write("</TR>");
            }
            HttpContext.Current.Response.Write("</Table>");
            HttpContext.Current.Response.Write("</font>");
            HttpContext.Current.Response.Flush();
            HttpContext.Current.Response.End();
        }
        #endregion


    }
}
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using PdfSharp;
using PdfSharp.Drawing;
using PdfSharp.Pdf;

namespace HBScore
{
    public static class PDFScoreWriter
    {
        public static void GeneratePDF(IEnumerable<Image> images, string outPath,
            string title, string composer, string info, string noteList)
        {
            if (images == null) // || !images.Any())
                throw new ArgumentException("No images to put in PDF file");
            if (!Directory.Exists(Path.GetDirectoryName(outPath)))
                throw new ArgumentException("Output folder does not exist");

            PdfDocument pdfDoc = new PdfDocument(outPath);

            // Title page

            foreach (Image img in images)
            {
                PdfPage page = pdfDoc.AddPage();
                page.Size = PageSize.A4;
                page.Orientation = PageOrientation.Landscape;
                using (MemoryStream ms = new MemoryStream())
                {
                    img.Save(ms, ImageFormat.Png);
                    using (XGraphics gfx = XGraphics.FromPdfPage(page))
                    {
                        ms.Seek(0, SeekOrigin.Begin);
                        XImage image = XImage.FromStream(ms);
                        double ptWidth = image.PixelWidth * 72 / 300.0;
                        double ptHeight = image.PixelHeight * 72 / 300.0;
                        gfx.DrawImage(image, 36, 36, ptWidth, ptHeight);
                    }
                }
            }
            pdfDoc.Close();
        }
    }
}

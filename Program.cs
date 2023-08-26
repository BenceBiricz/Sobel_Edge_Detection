using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Sobel_Kisebb_Matrixok
{
    class Program
    {

        static void Main(string[] args)
        {
            string path = AppDomain.CurrentDomain.BaseDirectory;
            string pictureName = null;
            Console.Write("Picture name: ");
            pictureName = Console.ReadLine().ToString();

            Bitmap bmp;
            bmp = (Bitmap)Image.FromFile(path + pictureName);

            bmp = edgeDetection(bmp, xSobel_first, xSobel_second, ySobel_first, ySobel_second);

            bmp.Save(path + "proba1.jpg");

            Console.WriteLine("Picture created!");
            Console.ReadLine();
        }

        private static double[,] xSobel_second
        {
            get
            {
                return new double[,]
                {
                    { -1, 0, 1 }
            };
            }
        }

        private static double[,] xSobel_first
        {
            get
            {
                return new double[,]
                {
                    { 1 },
                    { 2 },
                    { 1 },
            };
            }
        }

        private static double[,] ySobel_second
        {
            get
            {
                return new double[,]
                {
                    { 1 },
                    { 0 },
                    { -1 },
            };
            }
        }

        private static double[,] ySobel_first
        {
            get
            {
                return new double[,]
                {
                     { 1, 2, 1 }
            };
            }
        }

        private static Bitmap edgeDetection(Bitmap sourceImage, double[,] xkernel_first, double[,] xkernel_second, double[,] ykernel_first, double[,] ykernel_second)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();

            int width = sourceImage.Width;
            int height = sourceImage.Height;

            //a kep bitjeit atteszi a memoriaba
            BitmapData source = sourceImage.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

            // a kep byte-je osszesen - 4 byte *szelesseg * magassag
            int bytes = source.Stride * source.Height;

            // byte vektorok a pixel informaciokra
            byte[] startingPixels = new byte[bytes];
            byte[] result = new byte[bytes];

            // megadja az elso pixel helyet
            IntPtr firstPixel = source.Scan0;

            // a kep informaciojat a startingPixel tömbbe menti
            Marshal.Copy(firstPixel, startingPixels, 0, bytes);

            //felszabadul a memória
            sourceImage.UnlockBits(source);

            //szürkeárnyalatos
            float rgb = 0;
            for (int i = 0; i < startingPixels.Length; i += 4)
            {
                rgb = startingPixels[i] * .21f;
                rgb += startingPixels[i + 1] * .71f;
                rgb += startingPixels[i + 2] * .071f;
                startingPixels[i] = (byte)rgb;
                startingPixels[i + 1] = startingPixels[i];
                startingPixels[i + 2] = startingPixels[i];
                startingPixels[i + 3] = 255;
            }

            //valtozok a pixel ertekenek minden kernelre
            double xr = 0.0;
            double xg = 0.0;
            double xb = 0.0;
            double yr = 0.0;
            double yg = 0.0;
            double yb = 0.0;
            double rt = 0.0;
            double gt = 0.0;
            double bt = 0.0;

            //köztes tárolók
            double[] storeX = new double[bytes];
            double[] storeY = new double[bytes];

            int calcOffset = 0;
            int byteOffset = 0;
            object lockObject = new object();

            Thread t = new Thread(
           () =>
           {
               for (int Yheight = 1; Yheight < height - 1; Yheight++)
               {
                   for (int Xwidth = 1; Xwidth < width - 1; Xwidth++)
                   {
                       //result tömbbe a megfelelő helyre emnteni az adatokat
                       byteOffset = Yheight * source.Stride + Xwidth * 4;

                       lock (lockObject)
                       {
                           for (int kernelX = -1; kernelX <= 1; kernelX++)
                           {
                               //startingPixels megfelelő adatainak kinyerése, 4esével növekszik 
                               calcOffset = byteOffset + kernelX * 4 + kernelX * source.Stride;

                               storeX[calcOffset] = (double)(startingPixels[calcOffset]) * xkernel_first[kernelX + 1, 0];
                               storeX[calcOffset + 1] = (double)(startingPixels[calcOffset + 1]) * xkernel_first[kernelX + 1, 0];
                               storeX[calcOffset + 2] = (double)(startingPixels[calcOffset + 2]) * xkernel_first[kernelX + 1, 0];

                               storeY[calcOffset] = (double)(startingPixels[calcOffset]) * ykernel_first[0, kernelX + 1];
                               storeY[calcOffset + 1] = (double)(startingPixels[calcOffset + 1]) * ykernel_first[0, kernelX + 1];
                               storeY[calcOffset + 2] = (double)(startingPixels[calcOffset + 2]) * ykernel_first[0, kernelX + 1];

                           }

                       }


                   }

               }

           });

            t.Start();

            Console.WriteLine(t.ThreadState);

            //főszál
            for (int Yheight = 1; Yheight < height - 1; Yheight++)
            {

                for (int Xwidth = 1; Xwidth < width - 1; Xwidth++)
                {

                    //szinek nullazasa                  
                    xr = xg = xb = 0;
                    yr = yg = yb = 0;

                    byteOffset = Yheight * source.Stride + Xwidth * 4;

                    lock (lockObject)
                    {

                        for (int kernelX = -1; kernelX <= 1; kernelX++)
                        {

                            calcOffset = byteOffset + kernelX * 4 + kernelX * source.Stride;

                            xr += storeX[calcOffset] * xkernel_second[0, kernelX + 1];
                            xg += storeX[calcOffset + 1] * xkernel_second[0, kernelX + 1];
                            xb += storeX[calcOffset + 2] * xkernel_second[0, kernelX + 1];

                            yr += storeY[calcOffset] * ykernel_second[kernelX + 1, 0];
                            yg += storeY[calcOffset + 1] * ykernel_second[kernelX + 1, 0];
                            yb += storeY[calcOffset + 2] * ykernel_second[kernelX + 1, 0];
                        }

                    }

                    //képlet alkalmazása
                    bt = 3 * Math.Sqrt((xb * xb) + (yb * yb));
                    gt = 3 * Math.Sqrt((xg * xg) + (yg * yg));
                    rt = 3 * Math.Sqrt((xr * xr) + (yr * yr));

                    //skálázás
                    if (bt > 255) bt = 255;
                    else if (bt < 0) bt = 0;
                    if (gt > 255) gt = 255;
                    else if (gt < 0) gt = 0;
                    if (rt > 255) rt = 255;
                    else if (rt < 0) rt = 0;

                    //mentés a result tömbbe
                    result[byteOffset] = (byte)(rt);
                    result[byteOffset + 1] = (byte)(gt);
                    result[byteOffset + 2] = (byte)(bt);
                    result[byteOffset + 3] = 255;

                }
            }

            Console.WriteLine(t.ThreadState);

            //uj bitmap a kesz adatokra
            Bitmap resultImage = new Bitmap(width, height);

            BitmapData resultData = resultImage.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

            Marshal.Copy(result, 0, resultData.Scan0, result.Length);

            resultImage.UnlockBits(resultData);

            sw.Stop();
            Console.WriteLine("Ellapsed time: {0} milliseconds.", sw.ElapsedMilliseconds);

            //visszaadni a kesz kepet
            return resultImage;
        }
    }
}

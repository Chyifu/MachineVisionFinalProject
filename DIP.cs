﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;


using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.Util;
using Emgu.CV.UI;
using Emgu.CV.Structure;
namespace Final_Project
{
    public partial class DIP : Form
    {
        Double[] HuM=new double[]{0,0,0,0,0,0};
        Double[] RM = new double[] { 0,0,0,0,0,0,0,0,0,0};
        Double[] SpM = new double[] {0,0,0,0,0,0,0,0,0,0,0,0 };
        Double[] CenM = new double[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
        Double[] NorM = new double[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
        int i,j;
        string[] files;
        Image<Bgr, byte> orignalImage;
        Image<Bgr, byte> delete_lineImg;
        Image<Gray, byte> GrayImg;
        Image<Gray, float> LaplaImg;
        Image<Gray, byte> DCImg;
        Image<Bgr, byte> DCImg2;
        int center_X, center_Y, circleR;
        public DIP()
        {
            InitializeComponent();
        }

        private void LoadFile_Click(object sender, EventArgs e)
        {
            LisView_pic.Items.Clear();
            imageList_ori.Images.Clear();
            OpenFileDialog OpenF = new OpenFileDialog();
            OpenF.Multiselect = true;
            OpenF.InitialDirectory = @"\Image";
            OpenF.Filter = "圖片文件(*.jpg *.jpeg *.gif *.png)|*.jpg;*.jpeg;*.gif;*.png";

            if (OpenF.ShowDialog() == DialogResult.OK && OpenF.FileNames != null)
            {
                files = OpenF.FileNames;
            }
            LisView_pic.View = View.LargeIcon;
            LisView_pic.MultiSelect = false;
            if (files != null)
            {
                for (i = 0; i < files.Length; i++)
                {
                    imageList_ori.Images.Add(files[i],Image.FromFile(files[i]));
                    imageList_ori.ImageSize = new Size(50, 50);
                }

                LisView_pic.LargeImageList = new ImageList();
                LisView_pic.LargeImageList.ImageSize = new Size(50, 50);
               
                for (i = 0; i < imageList_ori.Images.Count; i++)
                {
                    LisView_pic.LargeImageList.Images.Add(imageList_ori.Images.Keys[i], imageList_ori.Images[i]);
                }

                for (i = 0; i < files.Length; i++)
                {
                    FileInfo info = new FileInfo(files[i]);
                    LisView_pic.Items.Add(files[i], info.Name, i);                  
                }
            }
        }

        private void LisView_pic_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (LisView_pic.SelectedItems.Count == 1)
            {
                orignalImage = new Image<Bgr, byte>(LisView_pic.SelectedItems[0].Name);
                imgBox_orignal.Image = orignalImage;
            }

            DeleteLine(orignalImage, ref delete_lineImg);                            //先刪去圖片左右的黑邊直線
            ToGray(delete_lineImg, ref GrayImg);                                     //將圖片轉為灰階
            ToLaplacian(GrayImg, ref LaplaImg);                                      //去除雜訊 強調邊緣 (用以找出周圍的圓)
            FindCircle(LaplaImg,ref center_X,ref  center_Y,ref  circleR);            //尋找圓的圓心還有半徑
            DeleteCircle(delete_lineImg, ref DCImg, center_X, center_Y, circleR, ref DCImg2);    //刪去圓
            /*show 每一步驟的圖片 確認用*/
            imageBox1.Image = delete_lineImg;                   
            imageBox2.Image = GrayImg;
            imageBox3.Image = LaplaImg;
            imageBox4.Image = DCImg;

            
            /*計算Hu*/
            Hu(DCImg,ref HuM,ref RM);
            /*其他Moment*/
           // richTextBox4.Text = "";
            Moments(DCImg,ref SpM,ref CenM,ref NorM);
            /*直方圖*/
            histoG(DCImg,DCImg2);
            richTextBox2.Text = "";
            richTextBox3.Text = "";
            
            for (int i = 0; i < 6; i++)
            {
                richTextBox2.AppendText("HuM"+(i+1)+"="+HuM[i]+"\n");
            }

            for (int i = 0; i < 10; i++)
            {
                richTextBox3.AppendText("RM" + (i + 1) + "=" + RM[i]+"\n");
            }

        }

        void ToGray(Image<Bgr,byte> img, ref Image<Gray,byte> imgG)
        {
            imgG = new Image<Gray, Byte>(img.Size);
            for (int i = 0; i < img.Height; i++)
            {
                for (int j = 0; j < img.Width; j++)
                {
                    imgG.Data[i, j, 0] = (Byte)(img.Data[i, j, 0] * 0.1140 + img.Data[i, j, 1] * 0.5870 + img.Data[i, j, 2] * 0.2989);
                }
            }
        }

        void DeleteLine(Image<Bgr, byte> img, ref Image<Bgr, byte> imgD)
        {
            int imgwid = img.Width - 6;
            int imghei = img.Height;
            imgD = new Image<Bgr, Byte>(imgwid,imghei);
            for (int i = 0; i < imghei; i++)
            {
                for (int j = 0; j < imgwid; j++)
                {
                    imgD.Data[i, j, 0] = (Byte)(img.Data[i, j+2, 0]);
                    imgD.Data[i, j, 1] = (Byte)(img.Data[i, j+2, 1]);
                    imgD.Data[i, j, 2] = (Byte)(img.Data[i, j+2, 2]);
                }
            } 
        }

        void ToLaplacian(Image<Gray, byte> img, ref Image<Gray,float> imgL)
        {
            imgL = new Image<Gray, float>(img.Size);
            imgL = img.Laplace(5);
            imgL = imgL.ThresholdBinary(new Gray(60), new Gray(255));
        }

        void FindCircle(Image<Gray, float> img, ref int X,ref int Y,ref int R)
        {
            int[] Top = new int[2];
            int[] Left = new int[2];
            int[] Right = new int[2];
            int[] Bottom = new int[2];
            /*find Top*/
            for (int i = 0; i < img.Height; i++)
            {
                int check = 0;
                for (int j = 0; j < img.Width - 1; j++)
                {
                    if (img.Data[i, j, 0] < img.Data[i, j + 1, 0] || img.Data[i, j + 1, 0]==255)
                    {
                        check = 1;
                        Top[0] = i;
                        Top[1] = j + 1;
                        break;
                    }                    
                }
                if (check == 1)
                    break;
            }

            /*find Bottom*/
            for (int i = img.Height-1; i >1 ; i--)
            {
                if (img.Data[i, Top[1], 0] < img.Data[i - 1, Top[1], 0] || img.Data[i - 1, Top[1], 0]==255)
                {
                    Bottom[0] = i-1+2;
                    Bottom[1] = Top[1];
                    break;
                }
            }

            /*find Left*/
            for (int i = 0; i < img.Width; i++)
            {
                int check = 0;
                for (int j = 0; j < img.Height - 1; j++)
                {
                    if (img.Data[j, i, 0] < img.Data[j + 1, i, 0] || img.Data[j + 1, i, 0]==255)
                    {
                        check = 1;
                        Left[0] = j + 1;
                        Left[1] = i;
                        break;
                    }
                }
                if (check == 1)
                    break;
            }

            /*find Right*/
            for (int i = img.Width-1; i > 1; i--)
            {
                if (img.Data[Left[0], i, 0] < img.Data[Left[0], i - 1, 0] || img.Data[Left[0], i - 1, 0]==255)
                {
                    Right[0] = Left[0];
                    Right[1] =i-1;
                    break;
                }
            }

            Y = Top[0] + (Bottom[0] - Top[0]) / 2;
            X = Left[1] + (Right[1] - Left[1]) / 2;
            if ((Bottom[0] - Top[0]) < (Right[1] - Left[1]))
            {
                if (img.Height < 900)
                {
                    R = (Bottom[0] - Top[0]) / 2 - 13;
                }
                else 
                {
                    R = (Bottom[0] - Top[0]) / 2 - 18;
                }
                 
            }
            else
            {
                if (img.Height < 900)
                {
                    R = (Right[1] - Left[1]) / 2 - 13;
                }
                else 
                {
                    R = (Right[1] - Left[1]) / 2 - 18;
                }
            }

            richTextBox1.Text = "w:"+img.Width+"  H:"+img.Height+"\n"+"R:" + R + "   Center=(" + X + " , " + Y + ")";
        }

        void DeleteCircle(Image<Bgr, byte> img, ref Image<Gray, byte> imgDC, int X, int Y, int R,ref Image<Bgr, byte> imgDC2)
        {
            imgDC = new Image<Gray, Byte>(img.Size);
            imgDC2 = new Image<Bgr, Byte>(img.Size);
            for (int i = 0; i < img.Height; i++)
            {
                for (int j = 0; j < img.Width; j++)
                {
                    double a = Math.Pow((j - X), 2) + Math.Pow((i - Y), 2);
                    double check=Math.Sqrt(a);
                    if (check < R)
                    {
                        imgDC.Data[i, j, 0] = (Byte)(img.Data[i, j, 0] * 0.1140 + img.Data[i, j, 1] * 0.5870 + img.Data[i, j, 2] * 0.2989);
                        imgDC2.Data[i, j, 0] = img.Data[i, j, 0];
                        imgDC2.Data[i, j, 1] = img.Data[i, j, 1];
                        imgDC2.Data[i, j, 2] = img.Data[i, j, 2];
                    }
                    else 
                    {
                        imgDC.Data[i, j, 0] =255;
                        imgDC2.Data[i, j, 0] = 255;
                        imgDC2.Data[i, j, 1] = 255;
                        imgDC2.Data[i, j, 2] = 255;
                    }
                }
            }            
        }

        void Hu(Image<Gray, byte> img, ref Double[] MomentHu, ref Double[] MomentR)
        {
            double r1 = 0, r2 = 0, r3 = 0, r4 = 0, r5 = 0, r6 = 0, r7 = 0, r8 = 0, r9 = 0, r10 = 0;
            Image<Gray, byte> imgA = new Image<Gray, byte>(img.Size);
            imgA = img.ThresholdBinary(new Gray(60), new Gray(255));
            Contour<Point> contourA = imgA.FindContours();
            MCvMoments momentsA = contourA.GetMoments();
            MCvHuMoments HuMomentsA = momentsA.GetHuMoment();

            MomentHu[0] = HuMomentsA.hu1;
            MomentHu[1] = HuMomentsA.hu2;
            MomentHu[2] = HuMomentsA.hu3;
            MomentHu[3] = HuMomentsA.hu4;
            MomentHu[4] = HuMomentsA.hu5;
            MomentHu[5] = HuMomentsA.hu6;

            Rmoment(HuMomentsA.hu1, HuMomentsA.hu2, HuMomentsA.hu3, HuMomentsA.hu4, HuMomentsA.hu5, HuMomentsA.hu6, ref r1, ref r2, ref r3, ref r4, ref r5, ref r6, ref r7, ref r8, ref r9, ref r10);

            MomentR[0] = r1;
            MomentR[1] = r2;
            MomentR[2] = r3;
            MomentR[3] = r4;
            MomentR[4] = r5;
            MomentR[5] = r6;
            MomentR[6] = r7;
            MomentR[7] = r8;
            MomentR[8] = r9;
            MomentR[9] = r10;
        }

        void Rmoment(double h1, double h2, double h3, double h4, double h5, double h6, ref double r1, ref double r2, ref double r3, ref double r4, ref double r5, ref double r6, ref double r7, ref double r8, ref double r9, ref double r10)
        {
            r1 = Math.Sqrt(h2) / h1;
            r2 = (h1 + Math.Sqrt(h2)) / (h1 - Math.Sqrt(h2));
            r3 = Math.Sqrt(h3) / Math.Sqrt(h4);
            r4 = h3 / Math.Sqrt(Math.Abs(h5));
            r5 = Math.Sqrt(h4) / Math.Sqrt(Math.Abs(h5));
            r6 = Math.Abs(h6) / (h1 * h3);
            r7 = Math.Abs(h6) / (h1 * Math.Sqrt(Math.Abs(h5)));
            r8 = Math.Abs(h6) / (h3 * Math.Sqrt(h2));
            r9 = Math.Abs(h6) / (h2 * Math.Sqrt(Math.Abs(h5)));
            r10 = Math.Abs(h5) / (h3 * h4);
            if (h1 == 0)
            {
                r1 = 0;
                r6 = 0;
                r7 = 0;
            }
            if (h2 == 0)
            {
                r8 = 0;
                r9 = 0;
            }
            if (h3 == 0)
            {
                r6 = 0;
                r8 = 0;
                r10 = 0;
            }
            if (h4 == 0)
            {
                r3 = 0;
                r10 = 0;
            }
            if (h5 == 0)
            {
                r4 = 0;
                r5 = 0;
                r7 = 0;
                r9 = 0;
            }
            if (h1 - Math.Sqrt(Math.Abs(h2)) == 0)
                r2 = 0;            
        }

        void Moments(Image<Gray, byte> img,ref Double[] MomentSp,ref Double[] MomentCen,ref Double[] MomentNor)
        {
            Image<Gray, byte> imgA = new Image<Gray, byte>(img.Size);
            imgA = img.ThresholdBinary(new Gray(60), new Gray(255));
            Contour<Point> contourA = imgA.FindContours();
            MCvMoments momentsA = contourA.GetMoments();
            for (int xOrder = 0; xOrder <= 3; xOrder++)
            {
                for (int yOrder = 0; yOrder <= 3; yOrder++)
                {
                    if (xOrder + yOrder <= 3)
                    {
                        MomentSp[3*xOrder+yOrder] = momentsA.GetSpatialMoment(xOrder, yOrder);
                        MomentCen[3 * xOrder + yOrder] = momentsA.GetCentralMoment(xOrder, yOrder);
                        MomentNor[3*xOrder+yOrder] = momentsA.GetNormalizedCentralMoment(xOrder, yOrder);
                    }
                }
            }
       /*     for (int m = 0; m < 12; m++)
            {
                richTextBox4.AppendText("SP:"+MomentSp[m]+"  CEN:"+MomentCen[m]+"   Nor: "+MomentNor[m]+"\n");
            }*/
        }

        void histoG(Image<Gray, byte> img, Image<Bgr, byte> img2)
        {

            DenseHistogram Histo = new DenseHistogram ( 256, new RangeF(0, 255) );
            float[] grayHist = new float[256];
            Histo.Calculate(new Image<Gray,byte>[]{img},
                            true,
                            null);
            Histo.MatND.ManagedArray.CopyTo(grayHist, 0);

            DenseHistogram Histo_temp = new DenseHistogram(256, new RangeF(0, 255));
            float[,] colorHist = new float[3, 256];
            float[] tempHist = new float[256];
            Image<Gray, Byte>[] images = img2.Split();
            Histo_temp.Calculate(new Image<Gray,byte>[]{images[0]},
                            true,
                            null);
            Histo_temp.MatND.ManagedArray.CopyTo(tempHist, 0);
            for (int m = 0; m < 256; m++)
            {
                colorHist[0, m] = tempHist[m];
            }

            Histo_temp.Calculate(new Image<Gray, byte>[] { images[1] },
                true,
                null);
            Histo_temp.MatND.ManagedArray.CopyTo(tempHist, 0);
            for (int m = 0; m < 256; m++)
            {
                colorHist[1, m] = tempHist[m];
            }

            Histo_temp.Calculate(new Image<Gray, byte>[] { images[2] },
                true,
                null);
            Histo_temp.MatND.ManagedArray.CopyTo(tempHist, 0);
            for (int m = 0; m < 256; m++)
            {
                colorHist[2, m] = tempHist[m];
            }
            
            
            

            HistogramViewer.Show(Histo,"histo");
            HistogramViewer.Show(img2,256);
            float meangray=0,meanR=0,meanG=0,meanB=0;
            int totalgray = 0,totalR=0,totalG=0,totalB=0;
            for (int m=0; m < 255; m++)
            {
                meangray = meangray + grayHist[m] * m;
                totalgray = totalgray + (int)grayHist[m];
                meanB = meanB + colorHist[0, m] * m;
                totalB = totalB + (int)colorHist[0, m];
                meanG = meanG + colorHist[1, m] * m;
                totalG = totalG + (int)colorHist[1, m];
                meanR = meanR + colorHist[2, m] * m;
                totalR = totalR + (int)colorHist[2, m];
            }
            meangray = meangray / totalgray;
            meanB = meanB / totalB;
            meanG = meanG / totalG;
            meanR = meanR / totalR;
            richTextBox5.Text = "mean gray=" + meangray + "\nmean blue="+meanB+"\nmean Green="+meanG+"\nmean Red="+meanR;


        }
    }
}

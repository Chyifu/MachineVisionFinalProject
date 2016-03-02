using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Data.OleDb;
using System.IO;
using System.Web;

using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.Util;
using Emgu.CV.UI;
using Emgu.CV.Structure;
using Emgu.CV.ML;
using Emgu.CV.ML.Structure;
using System.Windows.Forms.DataVisualization.Charting;

namespace Final_Project
{
    public partial class train : Form
    {
        int trainSampleCount = 10590;
        int testSampleCount = 198; 
        int featureNum = 10;
        Matrix<float> trainData; 
        Matrix<float> trainClasses;
        Matrix<float> sample ;
        Matrix<float> sampleClasses;
        
        public train()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            trainData = new Matrix<float>(trainSampleCount , featureNum);


            OpenFileDialog openDialog = new OpenFileDialog();
            openDialog.InitialDirectory = Application.StartupPath + @"\Image";
            if (openDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
               richTextBox1.Text = openDialog.FileName;
               List<string> list = new List<string>();
               using (StreamReader sr=new StreamReader(openDialog.FileName,Encoding.Default))
               {
                   int row=0;
                   while (sr.Peek() != -1)
                   {
                       float Num;
                       string str = sr.ReadLine().Trim();
                       string[] str2 = str.Split('\t');
                       for (int i = 0; i < featureNum; i++)
                       {
                           string S = str2[i];
                           if (float.TryParse(S, out Num) == true)
                           {
                                   trainData[row, i] = Num;
                           }
                           else 
                           {
                                   trainData[row, i] = 0;
                           }
                       }
                       row++;   
                   }
               }

//               PrintMatrix(trainData,richTextBox4);
            }
            
            
        }

        private void PrintMatrix(Matrix<float> Mat, RichTextBox richbox)
        {

            for (int i = 0; i < Mat.Rows; i++)
            {
                string s = "";
                for (int j = 0; j < Mat.Cols; j++)
                {
                    s = s +"("+i+")" + Mat[i, j] + "\t";          //設定印出矩陣的格式
                }
                s = s + "\n";
                richbox.AppendText(s);
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            trainClasses = new Matrix<float>(trainSampleCount, 1);


            OpenFileDialog openDialog = new OpenFileDialog();
            openDialog.InitialDirectory = Application.StartupPath + @"\Image";
            if (openDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                richTextBox2.Text = openDialog.FileName;
                List<string> list = new List<string>();
                using (StreamReader sr = new StreamReader(openDialog.FileName, Encoding.Default))
                {
                    int row = 0;
                    while (sr.Peek() != -1)
                    {
                        float Num;
                        string str = sr.ReadLine().Trim();
                        string[] str2 = str.Split('\t');
                        for (int i = 0; i < str2.Length; i++)
                        {
                            string S = str2[i];
                            if (float.TryParse(S, out Num) == true)
                            {
                                    trainClasses[row, i] = Num;
                            }
                            else
                            {
                                    trainClasses[row, i] = 0;
                            }
                        }
                        row++;
                    }
                }
          //      PrintMatrix(trainClasses, richTextBox4);
          //      PrintMatrix(sampleClasses, richTextBox5);
            }
        }

        void TrainSVM(int times,ref float accuracy)
        {
            using (SVM model = new SVM())
            {
                SVMParams p = new SVMParams();
                p.KernelType = Emgu.CV.ML.MlEnum.SVM_KERNEL_TYPE.RBF;
                p.SVMType = Emgu.CV.ML.MlEnum.SVM_TYPE.C_SVC;
                p.C = 1;
                p.Gamma = 0.1;
                p.TermCrit = new MCvTermCriteria(50, 0.00001);
                bool trained = model.TrainAuto(trainData, trainClasses, null, null, p.MCvSVMParams, 5);

                Matrix<float> test = new Matrix<float>(1, featureNum);

               float count = 0;
                for (int i = 0; i < trainSampleCount; i++)
                {
                    for (int j = 0; j < featureNum; j++)
                    {
                        test.Data[0, j] = trainData.Data[i, j];
                    }
                    float response = model.Predict(test);
                    float classSam = trainClasses[i, 0];

                    if (response == 10)
                    {
                        if (classSam == 10)
                        {
                            count++;
                        }
                    }
                    else
                    {
                        if (classSam != 10)
                        {
                            count++;
                        }
                    }
                }

                float acc = count / trainSampleCount;

                chart1.Series["Series1"].ChartType = SeriesChartType.Spline;
                chart1.Series["Series1"].Points.AddXY(times,acc);

                if (acc > accuracy)
                {
                    accuracy = acc;
                    model.Save("SVM_NOTE.txt");
                }

                Application.DoEvents();
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            using (SVM model = new SVM())
            {

                float train_SVM_accuracy = 0;
                int train_time = 0;
                while (train_SVM_accuracy <0.98 && train_time<20)
                {
                    TrainSVM(train_time,ref train_SVM_accuracy);
                    train_time++;
                }
                
                model.Load("SVM_NOTE.txt");
                
                Matrix<float> test = new Matrix<float>(1, featureNum);
                
                float count = 0,count2=0;
                float[,] AC = new float[10,10];    
                for (int i = 0; i < testSampleCount; i++)
                {
                    for (int j = 0; j < featureNum; j++)
                    {    
                        test.Data[0,j] = sample.Data[i,j];
                        
                    }
                    float response = model.Predict(test);
                    float classSam = sampleClasses[i, 0];

                   /**********************10x10********************/
                    AC[(int)classSam-1, (int)response-1]++;
                    richTextBox6.AppendText("("+i+")"+"R=" + response+"  C="+classSam+"\n");
                    if (response==classSam)
                    {
                        count2++;
                    }
                    if (response == 10)
                    {
                        if (classSam == 10)
                        {
                            count++;
                        }
                    }
                    else
                    {
                        if (classSam != 10)
                        {
                            count++;
                        }
                    }
                }

                for (int a = 0; a < 10; a++)
                {
                    for (int b = 0; b < 10; b++)
                    {
                        richTextBox7.AppendText(AC[a, b] + "\t");
                    }
                    richTextBox7.AppendText("\n");
                }

               // model.Save("SVM_NOTE.txt");

                float accuracy = count / testSampleCount;
                float accuracy2 = count2 / testSampleCount;
                //richTextBox3.Text = "count=" + count;
                richTextBox3.Text = "分兩類準確率=" + accuracy+"   各類別準確率=" + accuracy2;
            }
        }

        private void button5_Click(object sender, EventArgs e)
        {
            sample = new Matrix<float>(testSampleCount, featureNum);

            OpenFileDialog openDialog = new OpenFileDialog();
            openDialog.InitialDirectory = Application.StartupPath + @"\Image";
            if (openDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                richTextBox5.Text = openDialog.FileName;
                List<string> list = new List<string>();
                using (StreamReader sr = new StreamReader(openDialog.FileName, Encoding.Default))
                {
                    int row = 0;
                    while (sr.Peek() != -1)
                    {
                        float Num;
                        string str = sr.ReadLine().Trim();
                        string[] str2 = str.Split('\t');
                        for (int i = 0; i < featureNum; i++)
                        {
                            string S = str2[i];
                            if (float.TryParse(S, out Num) == true)
                            {
                                sample[row, i] = Num;
                            }
                            else
                            {
                              sample[row , i] = 0;
                            }
                        }
                        row++;
                    }
                }

                //               PrintMatrix(trainData,richTextBox4);
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            sampleClasses = new Matrix<float>(testSampleCount, 1);

            OpenFileDialog openDialog = new OpenFileDialog();
            openDialog.InitialDirectory = Application.StartupPath + @"\Image";
            if (openDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                richTextBox4.Text = openDialog.FileName;
                List<string> list = new List<string>();
                using (StreamReader sr = new StreamReader(openDialog.FileName, Encoding.Default))
                {
                    int row = 0;
                    while (sr.Peek() != -1)
                    {
                        float Num;
                        string str = sr.ReadLine().Trim();
                        string[] str2 = str.Split('\n');
                        for (int i = 0; i < str2.Length; i++)
                        {
                            string S = str2[i];
                            if (float.TryParse(S, out Num) == true)
                            {
                                sampleClasses[row, i] = Num;
                            }
                        }
                        row++;
                    }
                }
                //      PrintMatrix(trainClasses, richTextBox4);
                //      PrintMatrix(sampleClasses, richTextBox5);
            }
        }



    }
}

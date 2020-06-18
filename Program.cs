using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace ConsoleApp2
{
    class Program
    {
        static void Main(string[] args)
        {
            RealKML();
        }
        public static void RealKML()
        {
          
            string MKLDirctory = @"E:\kml文件读取";
            List<string> ContentList = new List<string>();
            string[] AllFiles = Directory.GetFiles(MKLDirctory);
            Dictionary<string, List<zPointXY>> PointsDic = new Dictionary<string, List<zPointXY>>();

            foreach (string tmp in AllFiles)
            {
                if (tmp.Contains(".kml"))
                {
                    Console.WriteLine(tmp);
                    XmlDocument xmlD = new XmlDocument();
                    xmlD.Load(tmp);
                    XmlNode node = xmlD.DocumentElement; //SelectSingleNode("Document/name");
                    string keyDic = null;
                    foreach (XmlNode a in node.ChildNodes[0].ChildNodes)
                    {

                        if (a.Name == "name")
                        {
                            keyDic = tmp;
                        }
                        if (a.Name == "Folder")
                        {
                            //Console.WriteLine(a.ChildNodes[0]);

                            XmlNodeList nodeList = a.ChildNodes;
                            foreach (XmlNode i in nodeList)
                            {
                                if (i.ChildNodes[0].InnerText == "线路追踪路径")
                                {
                                    string zb = i.ChildNodes[4].ChildNodes[1].InnerText;
                                    string[] zbArr = zb.Trim().Split('\n');
                                    List<zPointXY> pointList = new List<zPointXY>();
                                    //  ed.WriteMessage(zb + "\n");
                                    foreach (string str in zbArr)
                                    {
                                        string[] points = str.Trim().Split(',');
                                        zPointXY point = new zPointXY(Convert.ToDouble(points[0]), Convert.ToDouble(points[1]));
                                        pointList.Add(point);
                                    }
                                    if (keyDic.Contains("反"))
                                    {
                                        pointList.Reverse();
                                    }
                                    if (!PointsDic.Keys.Contains(keyDic))
                                    {
                                        PointsDic.Add(keyDic, pointList);
                                    }

                                }
                            }


                        }
                    }

                }
            }
          


            int ii = 1;
            foreach (var tmp in PointsDic)
            {
                 
                List<string> xyTobl_list = new List<string>();
                List<zPointXY> bl_to_xy = WGS84_BLToXY(tmp.Value);
                List<zPointXY> xytobl = WGS84_XYToBL(bl_to_xy);

                foreach (var tmp1 in bl_to_xy)
                {
                    xyTobl_list.Add(Math.Round(tmp1.x, 15).ToString() + "\t" + Math.Round(tmp1.y, 15).ToString());
                }
                string path_1 = @"d:\zhy_" + ii.ToString() + ".bltoxy";
                File.WriteAllLines(path_1, xyTobl_list.ToArray());


                xyTobl_list.Clear();
                foreach (var tmp1 in xytobl)
                {
                    xyTobl_list.Add(tmp1.x.ToString() + "\t" + Math.Round(tmp1.y, 15).ToString());
                }
               path_1 = @"d:\zhy_" + (ii++).ToString() + ".xytobl";
                File.WriteAllLines(path_1, xyTobl_list.ToArray());


            }

            //List<zPointXY> liststrBL =PointsDic.Values;
            //List<string> xyTobl_list = new List<string>();

            //List<zPointXY> xytobl = WGS84_XYToBL()


            //   Console.ReadLine();
        }

        //WGS84经纬度坐标 转为   高斯投影,可以用======
        //与专业做地形图的图，与通过经纬度经换算后的坐标相同。


        public static zPointXY WGS84_BLToXY(zPointXY zBL, double centureL = 111)
        {
            //centureL 为中央子午线经度
            //  Editor ed = cadSer.Application.DocumentManager.MdiActiveDocument.Editor;
            //纬度
            double BB = zBL.y;
            //经纬度-中央子午线经度
            double LL = zBL.x - centureL;
            double pi = Math.PI;
            double BBr = BB * pi / 180;
            double LLr = LL * pi / 180;
            double sinB = Math.Sin(BBr);
            double cosB = Math.Cos(BBr);
            double t = Math.Tan(BBr);
            //ed.WriteMessage("BB："+BB.ToString()+"\n");
            //ed.WriteMessage("LL："+LL.ToString()+"\n");
            //ed.WriteMessage("sinB："+sinB.ToString()+"\n");
            double a = 6378137.0;
            double b = 6356752.3142;
            double e2 = 1 - (b * b) / (a * a);
            double yita2 = ((a * a) / (b * b) - 1) * cosB * cosB;
            //ed.WriteMessage("e2："+e2.ToString()+"\n");
            //ed.WriteMessage("yita2："+yita2.ToString()+"\n");
            //double pi = Math.PI;

            double W = Math.Sqrt(1 - e2 * sinB * sinB);
            //ed.WriteMessage("WWWWWW："+W.ToString()+"\n");
            double N = a / W;
            //ed.WriteMessage("NNNNNNNN："+N.ToString()+"\n");
            double m0 = a * (1 - e2);
            double m2 = 3.0 / 2.0 * e2 * m0;
            double m4 = 5.0 / 4.0 * e2 * m2;
            double m6 = 7.0 / 6.0 * e2 * m4;
            double m8 = 9.0 / 8.0 * e2 * m6;
            double a0 = m0 + m2 / 2 + 3.0 / 8 * m4 + 5.0 / 16 * m6 + 35.0 / 128 * m8;
            double a2 = m2 / 2 + m4 / 2 + 15.0 / 32 * m6 + 7.0 / 16 * m8;
            double a4 = m4 / 8 + 3.0 / 16 * m6 + 7.0 / 32 * m8;
            double a6 = m6 / 32 + m8 / 16;
            double a8 = m8 / 128;
            //中央子午线经度在纬度为0的坐标，相对于0经度
            double zY = a * centureL * pi / 180.0;
            //X向为纬度方向
            //ed.WriteMessage("a0a0a0a0a0a0："+a0.ToString()+"\n");
            double X = a0 * BBr - sinB * cosB * ((a2 - a4 + a6) + (2 * a4 - 16.0 / 3 * a6) * sinB * sinB + 16.0 / 3 * a6 * sinB * sinB * sinB * sinB);
            //ed.WriteMessage("XXXX坐标："+X.ToString()+"\n");

            double x = X + N / 2 * t * cosB * cosB * LLr * LLr;
            x += N / 24 * t * (5.0 - t * t + 9 * yita2 + 4 * yita2 * yita2) * Math.Pow(cosB, 4) * Math.Pow(LLr, 4);
            x += N / 720 * t * (61.0 - 58.0 * t * t + Math.Pow(t, 4)) * Math.Pow(cosB, 6) * Math.Pow(LLr, 6);

            //Y向为经度方向
            double y = N * cosB * LLr + N / 6 * (1 - t * t + yita2) * cosB * cosB * cosB * LLr * LLr * LLr;
            y += N / 120 * (5 - 18 * t * t + Math.Pow(t, 4) + 14 * yita2 - 58 * yita2 * t * t) * Math.Pow(cosB, 5) * Math.Pow(LLr, 5);
            y += 500000;
            //ed.WriteMessage("y坐标："+x.ToString()+"\n");
            //ed.WriteMessage("x坐标："+y.ToString()+"\n");
            return new zPointXY(y, x);

        }


        public static zPointXY WGS84_XYToBL(zPointXY zBL, double centureL = 111)
        {
            
            //xx为测量的坐标系
            double xx = zBL.y;
            double yy = zBL.x - 500000.0;

            double a = 6378137.0;
            double b = 6356752.3142;
            double e2 = 1 - (b * b) / (a * a);
            //double yita2 = ((a * a) / (b * b) - 1) * cosB * cosB;

            double m0 = a * (1 - e2);
            double m2 = 3.0 / 2.0 * e2 * m0;
            double m4 = 5.0 / 4.0 * e2 * m2;
            double m6 = 7.0 / 6.0 * e2 * m4;
            double m8 = 9.0 / 8.0 * e2 * m6;
            double a0 = m0 + m2 / 2 + 3.0 / 8 * m4 + 5.0 / 16 * m6 + 35.0 / 128 * m8;
            double a2 = m2 / 2 + m4 / 2 + 15.0 / 32 * m6 + 7.0 / 16 * m8;
            double a4 = m4 / 8 + 3.0 / 16 * m6 + 7.0 / 32 * m8;
            double a6 = m6 / 32 + m8 / 16;
            double a8 = m8 / 128;

            //double sinB;// = Math.Sin(BBr);
            //double cosB;// = Math.Cos(BBr);

            double Bi = 0;
            double Bi_1 = xx / a0;
            while (Math.Abs(Bi_1*180/Math.PI - Bi * 180 / Math.PI) > Math.Pow(10,-20))
            {
                Bi = Bi_1;
                //double  = Bi;
                //sinB = Math.Sin(Bi);
                // cosB = Math.Cos(Bi);

                //double fBi = - sinB * cosB * ((a2 - a4 + a6) + (2 * a4 - 16.0 / 3 * a6) * sinB * sinB + 16.0 / 3 * a6 * sinB * sinB * sinB * sinB);
                //double fBi = a0 * Bi - 0.5 * a2 * Math.Sin(6.0 * Bi) + a4 / 4 * Math.Sin(4 * Bi) - a6 / 6 * Math.Sin(6 * Bi) + a8 / 8 * Math.Sin(8 * Bi);
                double fBi = -0.5 * a2 * Math.Sin(2.0 * Bi) + a4 / 4 * Math.Sin(4 * Bi) - a6 / 6 * Math.Sin(6 * Bi) + a8 / 8 * Math.Sin(8 * Bi);

                Bi_1 = (xx - fBi) / a0;
               
            }
            double BBr = Bi_1;
            double t = Math.Tan(BBr);
            //sinB = Math.Sin(BBr);
            //cosB = Math.Cos(BBr);
            double sinB=Math.Sin(BBr);
            double cosB=Math.Cos(BBr);


            double M = a * (1 - e2) * Math.Pow((1 - e2 * sinB*sinB), -1.5);

            double N = a * Math.Pow((1 - e2 * sinB*sinB), -0.5);

            double yita2 = ((a * a) / (b * b) - 1) * cosB * cosB;


            double B = BBr - t / (2 * M * N) * yy * yy;
            B += t / (24 * M * N * N * N) * (5 + 3 * t * t + yita2 - 9 * yita2 * t * t) * Math.Pow(yy, 4);
            B -= t / (750 * M * N * N * N * N * N) * (61 + 90 * t * t + 45 * t * t * t * t) * Math.Pow(yy, 6);
            B = B * 180.0 / Math.PI;
            double L = 1 / (N * cosB) * yy - 1 / (6 * N * N * N * cosB) * (1 + 2 * t * t + yita2) * yy * yy * yy;
            L += 1 / (120 * N * N * N * N * N * cosB) * (5 + 28 * t * t + 24 * t * t * t * t + 6 * yita2 + 8 * yita2 * t * t) * Math.Pow(yy, 4);
            L = L * 180.0 / Math.PI;
            L += centureL;
            return new zPointXY(L, B);

        }
        public static List<zPointXY> WGS84_BLToXY(List<zPointXY> jwList, double centureL = 111)
        {
            List<zPointXY> zPList = new List<zPointXY>();
            foreach (zPointXY i in jwList)
            {
                zPList.Add(WGS84_BLToXY(i, centureL));
            }
            return zPList;
        }

        public static List<zPointXY> WGS84_XYToBL(List<zPointXY> jwList, double centureL = 111)
        {
            List<zPointXY> zPList = new List<zPointXY>();
            foreach (zPointXY i in jwList)
            {
                zPList.Add(WGS84_XYToBL(i, centureL));
            }
            return zPList;
        }


        //WGS84经纬度坐标 转为   弧长 坐标,类似于墨卡托,非平面类型
        //直接采用弧长表示坐标系，角度是发生了变化的。
        public static zPointXY WGS84_BLToXY_1(zPointXY zBL)
        {
            //centureL 为中央子午线经度
            //  Editor ed = cadSer.Application.DocumentManager.MdiActiveDocument.Editor;
            //纬度
            double BB = zBL.y;
            //经纬度-中央子午线经度
            double LL = zBL.x;
            double pi = Math.PI;
            double BBr = BB * pi / 180;
            //double LLr = LL*pi/180;
            double sinB = Math.Sin(BBr);
            double cosB = Math.Cos(BBr);
            //double t = Math.Tan(BBr);
            //ed.WriteMessage("BB："+BB.ToString()+"\n");
            //ed.WriteMessage("LL："+LL.ToString()+"\n");
            //ed.WriteMessage("sinB："+sinB.ToString()+"\n");
            double a = 6378137.0;
            double b = 6356752.3142;
            double e2 = 1 - (b * b) / (a * a);
            //double yita2 = ((a*a)/(b*b)-1)*cosB*cosB;
            //ed.WriteMessage("e2："+e2.ToString()+"\n");
            //ed.WriteMessage("yita2："+yita2.ToString()+"\n");
            //double pi = Math.PI;

            double W = Math.Sqrt(1 - e2 * sinB * sinB);
            //ed.WriteMessage("WWWWWW："+W.ToString()+"\n");
            //double N = a/W;
            //ed.WriteMessage("NNNNNNNN："+N.ToString()+"\n");
            double m0 = a * (1 - e2);
            double m2 = 3.0 / 2.0 * e2 * m0;
            double m4 = 5.0 / 4.0 * e2 * m2;
            double m6 = 7.0 / 6.0 * e2 * m4;
            double m8 = 9.0 / 8.0 * e2 * m6;
            double a0 = m0 + m2 / 2 + 3.0 / 8 * m4 + 5.0 / 16 * m6 + 35.0 / 128 * m8;
            double a2 = m2 / 2 + m4 / 2 + 15.0 / 32 * m6 + 7.0 / 16 * m8;
            double a4 = m4 / 8 + 3.0 / 16 * m6 + 7.0 / 32 * m8;
            double a6 = m6 / 32 + m8 / 16;
            double a8 = m8 / 128;
            //中央子午线经度在纬度为0的坐标，相对于0经度
            double zY = a * LL * pi / 180.0;
            //X向为纬度方向
            //ed.WriteMessage("a0a0a0a0a0a0："+a0.ToString()+"\n");
            double X = a0 * BBr - sinB * cosB * ((a2 - a4 + a6) + (2 * a4 - 16.0 / 3 * a6) * sinB * sinB + 16.0 / 3 * a6 * sinB * sinB * sinB * sinB);
            //ed.WriteMessage("XXXX坐标："+X.ToString()+"\n");

            double x = X;//+N/2*t*cosB*cosB*LLr*LLr;
            //x+=N/24*t*(5.0-t*t+9*yita2+4*yita2*yita2)*Math.Pow(cosB,4)*Math.Pow(LLr,4);
            //x+=N/720*t*(61.0-58.0*t*t+Math.Pow(t,4))*Math.Pow(cosB,6)*Math.Pow(LLr,6);

            //Y向为经度方向
            //double y = N*cosB*LLr+N/6*(1-t*t+yita2)*cosB*cosB*cosB*LLr*LLr*LLr;
            //y+=N/120*(5-18*t*t+Math.Pow(t,4)+14*yita2-58*yita2*t*t)*Math.Pow(cosB,5)*Math.Pow(LLr,5);
            double y = zY;
            //ed.WriteMessage("y坐标："+x.ToString()+"\n");
            //ed.WriteMessage("x坐标："+y.ToString()+"\n");
            return new zPointXY(y, x);

        }

        public static List<zPointXY> WGS84_BLToXY_1(List<zPointXY> jwList)
        {
            List<zPointXY> zPList = new List<zPointXY>();
            foreach (zPointXY i in jwList)
            {
                zPList.Add(WGS84_BLToXY_1(i));
            }
            return zPList;
        }


      
       




        public static zPointXY JW84ToWebMercatorXY(zPointXY jw)
        {
            zPointXY point = new zPointXY();
            double r = 6378137.0;
            point.x = r * jw.x / 180.0 * Math.PI;
            point.y = r * Math.Log(Math.Tan(Math.PI / 4 + jw.y / 180.0 / 2 * Math.PI));
            return point;
        }
        public static List<zPointXY> JW84ToWebMercatorXY(List<zPointXY> jwList)
        {
            List<zPointXY> zPList = new List<zPointXY>();
            foreach (zPointXY i in jwList)
            {
                zPList.Add(JW84ToWebMercatorXY(i));
            }
            return zPList;
        }


     


    }
    public class zPointXY
    {
        public zPointXY(double xx, double yy)
        {
            x = xx; y = yy;
        }
        public zPointXY() { }
        public double x { set; get; }
        public double y { set; get; }

    }

}

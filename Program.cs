using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

using ClassZjyRoadInfo;

namespace ConsoleApp2
{
    class Program
    {
        static void Main(string[] args)
        {
           
            string pmPatth = @"G:\新建文件夹\Desktop\夏县二级路\夏县二级路.pm";
            //zjy_cad_roadinfo road = new zjy_cad_roadinfo(pmPatth, 20);
            SortedList<double, ClassZjyRoadInfo.Vector2D> xyList = zjy_cad_roadinfo.GetRoadXYList(pmPatth, 20);

            List<string> xmlhead = new List<string>() {
            "<?xml version=\"1.0\" encoding=\"UTF-8\"?>",
         //  "<kml xmlns=\"http://www.opengis.net/kml/2.2\" xmlns:gx=\"http://www.google.com/kml/ext/2.2\" xmlns:kml=\"http://www.opengis.net/kml/2.2\" xmlns:atom=\"http://www.w3.org/2005/Atom\">",
          " <kml xmlns=\"http://earth.google.com/kml/2.1\">",
            "<Document>",
            "<name>zjy</name>",
           // "<open>1</open>",
           // "<description>zjy生成</description>",
            "<Style id=\"yellowLineGreenPoly\" >",
            "   <LineStyle>",
            "       <color>7f00ffff</color>",
            "       <width>4</width>",
            "   </LineStyle>",
            "   <PolyStyle>",
            "       <color>7f00ff00</color>",
            "   </PolyStyle>",
            "   </Style>",
            "<Folder>",
          "<name>zjy</name>",
            "       <visibility>1</visibility>",};

            List<string> xmlend = new List<string>()
            {
                "</Folder>",
                "</Document>",
                "</kml>"
            };

            List<Vector2D> zxBLList = new List<Vector2D>();
            //中央子午线经度
            double cenL = 111.0;
            foreach (var tmp in xyList)
            {
                zxBLList.Add(WGS84_XYToBL(tmp.Value, cenL));
            }

            List<Vector2D> zhFuList = new List<Vector2D>();
            SortedList<double, ClassZjyRoadInfo.Vector2D> bqList = new SortedList<double, Vector2D>();
            Matrix2D rotate_rev90 = new Matrix2D(-Math.PI / 2);
            for(int i=0;i<xyList.Values.Count-1;i++)
            {
                Vector2D zxP1 = xyList.Values[i];
                Vector2D zxP2 = xyList.Values[i+1];
                zhFuList.Add(WGS84_XYToBL(zxP1, cenL));

                Vector2D direction = (zxP2 - zxP1);
                direction = direction * rotate_rev90;
                direction = direction.Normal();

                Vector2D zxP3 = zxP1 + direction * 5;
                zhFuList.Add(WGS84_XYToBL(zxP3, cenL));

                Vector2D zxP4 = zxP3 + direction * 5;
                bqList.Add(xyList.Keys[i], WGS84_XYToBL(zxP4, cenL));
            }
            //i=xyList.Count-1
            {
                rotate_rev90 = new Matrix2D(Math.PI / 2);
                Vector2D zxP1 = xyList.Values[xyList.Count-1];
                Vector2D zxP2 = xyList.Values[xyList.Count - 2];
                zhFuList.Add(WGS84_XYToBL(zxP1, cenL));

                Vector2D direction = (zxP2 - zxP1);
                direction = direction * rotate_rev90;
                direction = direction.Normal();

                Vector2D zxP3 = zxP1 + direction * 5;
                zhFuList.Add(WGS84_XYToBL(zxP3, cenL));

                Vector2D zxP4 = zxP3 + direction * 5;
                bqList.Add(xyList.Keys[xyList.Count - 1], WGS84_XYToBL(zxP4, cenL));
            }

            //生成路线信息图像
            List<string> roadList = new List<string>()
            {
                "   <Placemark>",
               // "       <name>线路追踪路径</name>",
               // "       <visibility>1</visibility>",
               // "       <description>路线信息</description>",
                "       <styleUrl>#yellowLineGreenPoly</styleUrl>",
                "       <LineString>",
                "       <tessellate>1</tessellate>",
                "       <coordinates>"
            };
           
            foreach (var tmp in zxBLList)
            {
                roadList.Add("      " + tmp.X.ToString() + "," + tmp.Y.ToString() + ",0.0000");
            }
            roadList.Add("      </coordinates>");
            roadList.Add("      </LineString>");
            roadList.Add("  </Placemark>");



            List<string> roadStackLine = new List<string>();
           
          
            for (int i=0;i< zhFuList.Count-1;i+=2)
            {
                List<string> roadList_tmp = new List<string>()
                    {
                        "   <Placemark>",
                       // "       <name>线路追踪路径</name>",
                        //"       <visibility>1</visibility>",
                       // "       <description>路线信息</description>",
                        "       <styleUrl>#yellowLineGreenPoly</styleUrl>",
                        "       <LineString>",
                        "       <tessellate>1</tessellate>",
                        "       <coordinates>"
                    };
            
                roadList_tmp.Add("      " + zhFuList[i].X.ToString() + "," + zhFuList[i].Y.ToString() + ",0.000");
                roadList_tmp.Add("      " + zhFuList[i + 1].X.ToString() + "," + zhFuList[i + 1].Y.ToString() + ",0.000");

                roadList_tmp.Add("      </coordinates>");
                roadList_tmp.Add("      </LineString>");
                roadList_tmp.Add("  </Placemark>");              

                //int index = i / 2;

                //roadList_tmp.Add("    <Placemark>");
                //roadList_tmp.Add("        <name>" + bqList.Keys[index].ToString() + "</name>");
                //roadList_tmp.Add("        <Point>");
                //roadList_tmp.Add("        <coordinates>" + bqList.Values[index].X.ToString() + "," + bqList.Values[index].Y.ToString() + ",0.0" + "</coordinates>");
                //roadList_tmp.Add("        </Point>");
                //roadList_tmp.Add("        <markerStyle>-2</markerStyle>");
                //roadList_tmp.Add("     </Placemark>");

                roadStackLine.AddRange(roadList_tmp);

            }

            //生成标签信息
            List<string> roadBQList = new List<string>();
            
            foreach (var tmp in bqList)
            {
                roadBQList.Add("    <Placemark>");
                roadBQList.Add("        <name>" + tmp.Key.ToString() + "</name>");
                roadBQList.Add("        <Point>");
                roadBQList.Add("        <coordinates>" + tmp.Value.X.ToString() + "," + tmp.Value.Y.ToString() + ",0.0000" + "</coordinates>");
                roadBQList.Add("        </Point>");
                roadBQList.Add("        <markerStyle>-2</markerStyle>");
                roadBQList.Add("     </Placemark>");
            }



            #region
            //SortedList<double, ClassZjyRoadInfo.Vector2D> blList = new SortedList<double, Vector2D>();
            ////中央子午线经度
            ////  double cenL = 111.0;
            //foreach (var tmp in xyList)
            //{
            //    blList.Add(tmp.Key, WGS84_XYToBL(tmp.Value, cenL));
            //}
            ////生成路线信息图像
            //List<string> roadList = new List<string>()
            //{
            //    "   <Placemark>",
            //   // "       <name>线路追踪路径</name>",
            //    "       <visibility>1</visibility>",
            //   // "       <description>路线信息</description>",
            //    "       <styleUrl>#yellowLineGreenPoly</styleUrl>",
            //    "       <LineString>",
            //    "       <tessellate>1</tessellate>",
            //    "       <coordinates>"
            //};   
            //foreach(var tmp in blList.Values)
            //{
            //    roadList.Add("      "+tmp.X.ToString()+","+ tmp.Y.ToString()+",0");
            //}
            //roadList.Add("      </coordinates>");
            //roadList.Add("      </LineString>");
            //roadList.Add("  </Placemark>");

            //生成标签信息
            //List<string> roadBQList = new List<string>();
            //foreach (var tmp in blList)
            //{
            //    roadBQList.Add("    <Placemark>");
            //    roadBQList.Add("        <name>" + tmp.Key.ToString() + "</name>");
            //    roadBQList.Add("        <Point>");
            //    roadBQList.Add("        <coordinates>" + tmp.Value.X.ToString() + "," + tmp.Value.Y.ToString() + ",0.0" + "</coordinates>");
            //    roadBQList.Add("        </Point>");
            //    roadBQList.Add("        <markerStyle>-1</markerStyle>");
            //    roadBQList.Add("     </Placemark>");
            //}

            //roadBQList.Add("      </coordinates>");
            //roadBQList.Add("      </LineString>");
            //roadBQList.Add("  <Placemark>");
            #endregion
            List<string> kmlFile = new List<string>();
            kmlFile.AddRange(xmlhead);
            kmlFile.AddRange(roadList);
            kmlFile.AddRange(roadStackLine);
            kmlFile.AddRange(roadBQList);

            kmlFile.AddRange(xmlend);

          

            File.WriteAllLines(@"d:\zjyzjy.kml",kmlFile.ToArray());

        }
        public static void RealKML()
        {
          
            string MKLDirctory = @"E:\kml文件读取";
            List<string> ContentList = new List<string>();
            string[] AllFiles = Directory.GetFiles(MKLDirctory);
            Dictionary<string, List<Vector2D>> PointsDic = new Dictionary<string, List<Vector2D>>();

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
                                    List<Vector2D> pointList = new List<Vector2D>();
                                    //  ed.WriteMessage(zb + "\n");
                                    foreach (string str in zbArr)
                                    {
                                        string[] points = str.Trim().Split(',');
                                        Vector2D point = new Vector2D(Convert.ToDouble(points[0]), Convert.ToDouble(points[1]));
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
                List<Vector2D> bl_to_xy = WGS84_BLToXY(tmp.Value);
                List<Vector2D> xytobl = WGS84_XYToBL(bl_to_xy);

                foreach (var tmp1 in bl_to_xy)
                {
                    xyTobl_list.Add(Math.Round(tmp1.X, 15).ToString() + "\t" + Math.Round(tmp1.Y, 15).ToString());
                }
                string path_1 = @"d:\zhy_" + ii.ToString() + ".bltoxy";
                File.WriteAllLines(path_1, xyTobl_list.ToArray());


                xyTobl_list.Clear();
                foreach (var tmp1 in xytobl)
                {
                    xyTobl_list.Add(tmp1.X.ToString() + "\t" + Math.Round(tmp1.Y, 15).ToString());
                }
               path_1 = @"d:\zhy_" + (ii++).ToString() + ".xytobl";
                File.WriteAllLines(path_1, xyTobl_list.ToArray());


            }

        }

        //WGS84经纬度坐标 转为   高斯投影,可以用======
        //与专业做地形图的图，与通过经纬度经换算后的坐标相同。


        public static Vector2D WGS84_BLToXY(Vector2D zBL, double centureL = 111)
        {
            //centureL 为中央子午线经度
            //  Editor ed = cadSer.Application.DocumentManager.MdiActiveDocument.Editor;
            //纬度
            double BB = zBL.Y;
            //经纬度-中央子午线经度
            double LL = zBL.X - centureL;
            double pi = Math.PI;
            double BBr = BB * pi / 180;
            double LLr = LL * pi / 180;
            double sinB = Math.Sin(BBr);
            double cosB = Math.Cos(BBr);
            double t = Math.Tan(BBr);
        
            double a = 6378137.0;
            double b = 6356752.3142;
            double e2 = 1 - (b * b) / (a * a);
            double yita2 = ((a * a) / (b * b) - 1) * cosB * cosB;
           
            double W = Math.Sqrt(1 - e2 * sinB * sinB);
           
            double N = a / W;
            
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
            return new Vector2D(y, x);

        }


        public static Vector2D WGS84_XYToBL(Vector2D zBL, double centureL = 111)
        {
            
            //xx为测量的坐标系
            double xx = zBL.Y;
            double yy = zBL.X - 500000.0;

            double a = 6378137.0;// *1000000;
            double b = 6356752.3142;// * 1000000;
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



            double Bi = 0;
            double Bi_1 = xx / a0;
        
                while (Math.Abs(Bi_1  - Bi ) *Math.Pow(10,20)> 1)
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
       
            double sinB=Math.Sin(BBr);
            double cosB=Math.Cos(BBr);


            double M = a * (1 - e2) * Math.Pow((1 - e2 * sinB*sinB), -1.5);

            double N = a * Math.Pow((1 - e2 * sinB*sinB), -0.5);

            double yita2 = ((a * a) / (b * b) - 1) * cosB * cosB;

            double muti = 1000000;
            double B = BBr* muti - t*muti / (2 * M * N) * yy * yy;
            B += t * muti / (24 * M * N * N * N) * (5 + 3 * t * t + yita2 - 9 * yita2 * t * t) * Math.Pow(yy, 4);
            B -= t * muti / (750 * M * N * N * N * N * N) * (61 + 90 * t * t + 45 * t * t * t * t) * Math.Pow(yy, 6);
            B = B * 180.0 / Math.PI/muti;

            double L = 1*muti / (N * cosB) * yy - 1 *muti/ (6 * N * N * N * cosB) * (1 + 2 * t * t + yita2) * yy * yy * yy;
            L += 1*muti / (120 * N * N * N * N * N * cosB) * (5 + 28 * t * t + 24 * t * t * t * t + 6 * yita2 + 8 * yita2 * t * t) * Math.Pow(yy, 4);
            L = L * 180.0 / Math.PI/muti;
            L += centureL;
            return new Vector2D(L, B);

        }
        public static List<Vector2D> WGS84_BLToXY(List<Vector2D> jwList, double centureL = 111)
        {
            List<Vector2D> zPList = new List<Vector2D>();
            foreach (Vector2D i in jwList)
            {
                zPList.Add(WGS84_BLToXY(i, centureL));
            }
            return zPList;
        }

        public static List<Vector2D> WGS84_XYToBL(List<Vector2D> jwList, double centureL = 111)
        {
            List<Vector2D> zPList = new List<Vector2D>();
            foreach (Vector2D i in jwList)
            {
                zPList.Add(WGS84_XYToBL(i, centureL));
            }
            return zPList;
        }

        //WGS84经纬度坐标 转为   弧长 坐标,类似于墨卡托,非平面类型
        //直接采用弧长表示坐标系，角度是发生了变化的。
        public static Vector2D WGS84_BLToXY_1(Vector2D zBL)
        {
            //centureL 为中央子午线经度
            //  Editor ed = cadSer.Application.DocumentManager.MdiActiveDocument.Editor;
            //纬度
            double BB = zBL.Y;
            //经纬度-中央子午线经度
            double LL = zBL.X;
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
            return new Vector2D(y, x);

        }

        public static List<Vector2D> WGS84_BLToXY_1(List<Vector2D> jwList)
        {
            List<Vector2D> zPList = new List<Vector2D>();
            foreach (Vector2D i in jwList)
            {
                zPList.Add(WGS84_BLToXY_1(i));
            }
            return zPList;
        }


      
       




        public static Vector2D JW84ToWebMercatorXY(Vector2D jw)
        {
            Vector2D point = new Vector2D();
            double r = 6378137.0;
            point.X = r * jw.X / 180.0 * Math.PI;
            point.Y = r * Math.Log(Math.Tan(Math.PI / 4 + jw.Y / 180.0 / 2 * Math.PI));
            return point;
        }
        public static List<Vector2D> JW84ToWebMercatorXY(List<Vector2D> jwList)
        {
            List<Vector2D> zPList = new List<Vector2D>();
            foreach (Vector2D i in jwList)
            {
                zPList.Add(JW84ToWebMercatorXY(i));
            }
            return zPList;
        }


     


    }
    //public class Vector2D
    //{
    //    public Vector2D(double xx, double yy)
    //    {
    //        X = xx; Y = yy;
    //    }
    //    public Vector2D() { }
    //    public double X { set; get; }
    //    public double Y { set; get; }

    //}

}

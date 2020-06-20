using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Markup;

namespace ClassZjyRoadInfo
{
   public static  class zjy_cad_roadinfo
    {
        //SortedList<double, Vector2D> abc=new SortedList<double, Vector2D>();
        public static SortedList<double, Vector2D> GetRoadXYList(string pmPath,double jk)
       {
            Road road = CreatRoadFromPM(pmPath);

            List<LineTypeDetail> lineTypeDetails = road.roadDetailList;


            return road.GetXYList(jk, false);

        }

        
        
        
   static Road CreatRoadFromPM(string pathPM)
        {
            string pmPath = pathPM;
            string[] dataTmp = File.ReadAllLines(pmPath);
           
            Road road = new Road();

            string[] strLine3 = dataTmp[2].Trim().Split(new char[] { '\t', ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);

            if (strLine3[0] != "1") { /*OutputShow("左右偏信息出错")*/ return null; }
            road.startsStack = Convert.ToDouble(strLine3[1]);
            road.dirction = Convert.ToDouble(strLine3[2]);

            road.roadStartXY = new Vector2D(Convert.ToDouble(strLine3[5]), Convert.ToDouble(strLine3[4]));//以cad XY为基准

            for (int tmp = 3; tmp < dataTmp.Length; tmp += 3)
            {
                string[] strLineI = dataTmp[tmp].Trim().Split(new char[] { '\t', ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);

                if (dataTmp[tmp] == "")//忽略最后一行，因最后一行的前一行是空字符串
                {
                    //Output("左右偏信息出错")
                    break;
                }
                bool isLeft = false;
                if (strLineI[0] == "-1") isLeft = true;
                //只按弧长计算，其它不考虑，需自行检查
                double length = Convert.ToDouble(strLineI[2]);

                road.AddLine(new LineType(isLeft, strLineI[6], Math.Abs(length), Convert.ToDouble(strLineI[4]), Convert.ToDouble(strLineI[5])));

            }

             road.CreateRoadDetail();
             //road.GetXYList(20, true);
            return road;
        }
    static  void OutputShow(string str)
        {
            Console.WriteLine(str);
        }
    static  double CadxyToXY(double rad)
        {
            double tmp = Math.PI / 2 - rad;
            if (tmp < 0) tmp = Math.PI * 2 + tmp;
            return tmp;
        }

    }

    public class Road
    {
        public double startsStack { set; get; }
        public double dirction { set; get; }
        public Vector2D roadStartXY ;
        List<LineType> roadList = new List<LineType>();
        private List<LineTypeDetail> roadDetailLIst = new List<LineTypeDetail>();
        public List<LineTypeDetail> roadDetailList
        {
            get
            {
                return roadDetailLIst;
            }
        }
        public Road() { }
        public void AddLine(LineType lineType)
        {
            roadList.Add(lineType);
        }
        //已通过测试 2020 0604
        public List<LineTypeDetail> CreateRoadDetail()
        {
            double nowStack = startsStack;
            double nowDirection = dirction;
            Vector2D pointStart =roadStartXY;
            foreach (LineType tmp in roadList)
            {
                LineTypeDetail tmpLineDetail = new LineTypeDetail(tmp);
                tmpLineDetail.stackStart = nowStack;
                nowStack = nowStack + tmp.length;

                tmpLineDetail.startDir = nowDirection;
                nowDirection = nowDirection + tmp.GetDeltaA();

                tmpLineDetail.startXY = pointStart;

                Vector2D pGlobal = new Vector2D();
                switch (tmp.type)
                {
                    case "1":
                        {
                            pGlobal.X = tmpLineDetail.length * Math.Cos(tmpLineDetail.GetStartDirXY());
                            pGlobal.Y = tmpLineDetail.length * Math.Sin(tmpLineDetail.GetStartDirXY());
                            pGlobal += tmpLineDetail.startXY;
                        }
                        break;
                    case "3":
                        {
                            Vector2D pLocal = new Vector2D();
                            double angel = tmpLineDetail.length / tmpLineDetail.ro;
                            pLocal.X = tmpLineDetail.ro * Math.Sin(angel);
                            pLocal.Y = tmpLineDetail.ro * (1-Math.Cos(angel));
                            if (tmpLineDetail.isLeft == false)
                            {
                                pLocal.Y *= -1;
                            }
                            Matrix2D childToParent = new Matrix2D(tmpLineDetail.startXY, tmpLineDetail.GetStartDirXY());
                            pGlobal = pLocal * childToParent;
                        }
                        break;
                    case "21":
                        {
                            Vector2D pLocal = new Vector2D();
                            double angel = 0.5 * tmpLineDetail.length / tmpLineDetail.rd;
                            pLocal.X = tmpLineDetail.length * (1 - angel*angel/10+ Math.Pow( angel,4)/216);
                            pLocal.Y = tmpLineDetail.length *angel/3.0*(1 - angel * angel / 14 + Math.Pow(angel, 4) / 440);
                            if (tmpLineDetail.isLeft == false)
                            {
                                pLocal.Y *= -1;
                            }
                            Matrix2D childToParent = new Matrix2D(tmpLineDetail.startXY,tmpLineDetail.GetStartDirXY());
                            pGlobal = pLocal * childToParent;
                        }
                        break;
                    case "22":
                        {
                            Vector2D pLocal = new Vector2D();
                            double angel = 0.5 * tmpLineDetail.length / tmpLineDetail.ro;
                            pLocal.X = tmpLineDetail.length * (1 - angel * angel / 10 + Math.Pow(angel, 4) / 216);
                            pLocal.Y = tmpLineDetail.length * angel / 3.0 * (1 - angel * angel / 14 + Math.Pow(angel, 4) / 440);
                           
                            Matrix2D parentToChild = new Matrix2D(pLocal, angel);
                            parentToChild = parentToChild.ParentToChild();

                            Vector2D pLocal_end = new Vector2D(0,0);
                            pLocal_end = pLocal_end * parentToChild;
                            //镜像
                            pLocal_end.X *= -1.0;

                            if (tmpLineDetail.isLeft == false)
                            {
                                pLocal_end.Y *= -1;
                            }
                            Matrix2D childToParent = new Matrix2D(tmpLineDetail.startXY, tmpLineDetail.GetStartDirXY());
                            pGlobal = pLocal_end * childToParent;
                        }
                        break;
                    case "23":
                        {
                            double lo = tmpLineDetail.rd * tmpLineDetail.length / (tmpLineDetail.ro - tmpLineDetail.rd);

                            Vector2D pLocal_start = new Vector2D();
                            double angel = 0.5 * lo / tmpLineDetail.ro;
                            pLocal_start.X = lo * (1 - angel * angel / 10 + Math.Pow(angel, 4) / 216);
                            pLocal_start.Y = lo * angel / 3.0 * (1 - angel * angel / 14 + Math.Pow(angel, 4) / 440);

                            Matrix2D parentToChild = new Matrix2D(pLocal_start, angel);
                            parentToChild = parentToChild.ParentToChild();

                            Vector2D pLocal_end = new Vector2D();
                            double len = tmpLineDetail.length + lo;
                            angel = 0.5 * len / tmpLineDetail.rd;
                            pLocal_end.X = len * (1 - angel * angel / 10 + Math.Pow(angel, 4) / 216);
                            pLocal_end.Y = len * angel / 3.0 * (1 - angel * angel / 14 + Math.Pow(angel, 4) / 440);
                            

                            pLocal_end = pLocal_end * parentToChild;


                            if (tmpLineDetail.isLeft == false)
                            {
                                pLocal_end.Y *= -1;
                            }

                            Matrix2D childToParent = new Matrix2D(tmpLineDetail.startXY, tmpLineDetail.GetStartDirXY());
                            pGlobal = pLocal_end * childToParent;
                        }
                        break;
                    case "24":
                        {
                            double lo = tmpLineDetail.ro * tmpLineDetail.length / (tmpLineDetail.rd - tmpLineDetail.ro);

                            Vector2D pLocal_start = new Vector2D();
                            double len = tmpLineDetail.length + lo;
                            double angel = 0.5 * len / tmpLineDetail.ro;
                            pLocal_start.X = len * (1 - angel * angel / 10 + Math.Pow(angel, 4) / 216);
                            pLocal_start.Y = len * angel / 3.0 * (1 - angel * angel / 14 + Math.Pow(angel, 4) / 440);

                         

                            Matrix2D parentToChild = new Matrix2D(pLocal_start, angel);
                            parentToChild = parentToChild.ParentToChild();

                            Vector2D pLocal_end = new Vector2D();
                            angel = 0.5 * lo / tmpLineDetail.rd;
                            pLocal_end.X = lo * (1 - angel * angel / 10 + Math.Pow(angel, 4) / 216);
                            pLocal_end.Y = lo * angel / 3.0 * (1 - angel * angel / 14 + Math.Pow(angel, 4) / 440);


                           
                            pLocal_end = pLocal_end * parentToChild;

                            //镜像
                            pLocal_end.X *= -1.0;

                            if (tmpLineDetail.isLeft == false)
                            {
                                pLocal_end.Y *= -1;
                            }

                            Matrix2D childToParent = new Matrix2D(tmpLineDetail.startXY, tmpLineDetail.GetStartDirXY());
                            pGlobal = pLocal_end * childToParent;
                        }
                        break;
                }
                pointStart = pGlobal;
                roadDetailLIst.Add(tmpLineDetail);
            }
            return roadDetailLIst;
        }  
        //已通过测试 2020 0604
        public SortedList<double,Vector2D> GetXYList(double jianGe,bool isYaoSu)
        {
            SortedList<double, Vector2D> tmpSortList = new SortedList<double, Vector2D>();
            
            //CreateRoadDetail();
            
            double currentStack = (Math.Truncate( startsStack/jianGe)+1)*jianGe;
            
            double endStack = roadDetailLIst.Last().stackStart + roadDetailLIst.Last().length;
   
            //生成桩号序列  
            List<double> stackList = new List<double>();
            if(currentStack>0)
            {
                stackList.Add(startsStack);
            }
            while(currentStack<= endStack)
            {
                stackList.Add(currentStack);
                currentStack += jianGe;
            }
            if (isYaoSu == true)
            {
                foreach (var tmp in roadDetailLIst)
                {
                    double tmpStack = tmp.stackStart;
                    if (!stackList.Contains (tmpStack))
                    {
                        stackList.Add(tmpStack);
                    }
                }
                LineTypeDetail lastLine = roadDetailLIst[roadDetailLIst.Count() - 1];
                double lastStack = lastLine.stackStart + lastLine.length;
                if (!stackList.Contains(lastStack))
                {
                    stackList.Add(lastStack);
                }
            }
            stackList.Sort();

            foreach(double tmpStack in stackList)
            {
                int i =0;
                for (i = 0; i < roadDetailLIst.Count-1; i++)
                {
                    if (roadDetailLIst[i].stackStart <= tmpStack && roadDetailLIst[i + 1].stackStart > tmpStack)
                    {
                        tmpSortList.Add(tmpStack, roadDetailLIst[i].GetXY(tmpStack));
                        break;
                    }
                }
                
                if(i==roadDetailLIst.Count - 1)
                {

                    if (roadDetailLIst[i].stackStart <= tmpStack )
                    {
                        tmpSortList.Add(tmpStack, roadDetailLIst[i].GetXY(tmpStack));
                       
                    }
                }
                
            }
        
            return tmpSortList;
        }

        public SortedList<double, Hp_HDM> GetHp_HDM(Wid_HDmSingle wid_hdm_,Hp_HDM hp_hdm_,CG_W_Fuzhu cg_w_fuzhu_)
        {
            //List<Hp_HDM> hp_hdmList = new List<Hp_HDM>();
            //List<Wid_HDm> wid_hdmList = new List<Wid_HDm>();
            SortedList<double, Hp_HDM> hp_hdmSortedList = new SortedList<double, Hp_HDM>();

            SortedList<double, Wid_HDmSingle> wid_hdmSortedLeft = new SortedList<double, Wid_HDmSingle>();
            SortedList<double, Wid_HDmSingle> wid_hdmSortedRight = new SortedList<double, Wid_HDmSingle>();

            double wXCD = wid_hdm_.xingCheDaoWidth;
            double wYLJ = wid_hdm_.yingLuJianWidth;
            double wTLJ = wid_hdm_.tuLuJianWidth;

            double hXCD = hp_hdm_.h_left_XCD;
            double hYLJ = hp_hdm_.h_left_YLJ;
            double hTLJ = hp_hdm_.h_right_TLJ;

            bool isCG = cg_w_fuzhu_.isCG;
            double cgmax = cg_w_fuzhu_.maxh;
            string cgRotateBase = cg_w_fuzhu_.rotateBase;

            bool isJK = cg_w_fuzhu_.isJK;
            string addWidthType = cg_w_fuzhu_.addWidthType;

            double vDesign = cg_w_fuzhu_.vDesign;

            //首先将曲线要素处设置 未超前的路拱横坡
            LineTypeDetail lineLast = roadDetailLIst.Last();
            double lastStack = lineLast.stackStart + lineLast.length;
            foreach (LineTypeDetail roadtmp in roadDetailLIst)
            {
                Hp_HDM hp_hdm_tmp = new Hp_HDM(roadtmp.stackStart, hTLJ, hYLJ, hXCD);
                hp_hdmSortedList.Add(roadtmp.stackStart, hp_hdm_tmp);
            }
            hp_hdmSortedList.Add(lastStack, new Hp_HDM(roadDetailLIst.Last().stackStart, hTLJ, hYLJ, hXCD));

            //首先将曲线要素处设置 未加宽的路幅宽度
            foreach (LineTypeDetail roadtmp in roadDetailLIst)
            {
                Wid_HDmSingle wid_hdm_tmp = new Wid_HDmSingle(roadtmp.stackStart, wXCD, wYLJ, wTLJ);
                wid_hdmSortedLeft.Add(roadtmp.stackStart, wid_hdm_tmp);
            }
            wid_hdmSortedLeft.Add(lastStack, new Wid_HDmSingle(roadDetailLIst.Last().stackStart, wXCD, wYLJ, wTLJ));

            foreach (LineTypeDetail roadtmp in roadDetailLIst)
            {
                Wid_HDmSingle wid_hdm_tmp = new Wid_HDmSingle(roadtmp.stackStart, wXCD, wYLJ, wTLJ);
                wid_hdmSortedRight.Add(roadtmp.stackStart, wid_hdm_tmp);
            }
            wid_hdmSortedRight.Add(lastStack, new Wid_HDmSingle(roadDetailLIst.Last().stackStart, wXCD, wYLJ, wTLJ));

            //获取圆曲线索引列表
            List<int> yIndex_list = new List<int>();
            for (int i = 0; i < roadDetailLIst.Count; i++)
            {
                if (roadDetailList[i].type == "3")
                {
                    yIndex_list.Add(i);
                }
            }
            

            if (isJK==true&&isCG==false)  //只加宽
            {
                //起始
                {

                    double ys = 0, hs = 0, zLen = 0, he = 0, ye = 0;

                    int[] zhyhz_SecIndex = { -1, -1, -1, -1, -1 };
                    zhyhz_SecIndex[4] = yIndex_list.First();
                    bool isR_H = true;
                    for (int tmp_f = yIndex_list.First() - 1; tmp_f > 0; tmp_f--)
                    {
                        LineTypeDetail tmpLine = roadDetailLIst[tmp_f];
                        if (tmpLine.type.Contains("2") && isR_H == true)
                        {
                            zhyhz_SecIndex[3] = tmp_f;
                            isR_H = false;
                        }
                        else if (tmpLine.type.Contains("2") && isR_H == false)
                        {
                            zhyhz_SecIndex[1] = tmp_f;
                            isR_H = true;
                        }
                        else if (tmpLine.type.Contains("1"))
                        {
                            zhyhz_SecIndex[2] = tmp_f;
                        }
                    }

                    int ys_index = zhyhz_SecIndex[0];
                    int hs_index = zhyhz_SecIndex[1];
                    int z_index = zhyhz_SecIndex[2];
                    int he_index = zhyhz_SecIndex[3];
                    int ye_index = zhyhz_SecIndex[4];

                    LineTypeDetail ye_line = roadDetailLIst[ye_index];
                   
                    if (GetIsJK(ye_line.ro) == true)
                    {
                        double addWidth = GetAddWidth(ye_line.ro, addWidthType);
                        double jk_jb = GetJBLen_Round(Math.Max(addWidth * 15, 10));

                        // HY点处的加宽默认全加宽，当渐变段深入了圆曲线内时，此处的加宽值会修正
                        if (ye_line.isLeft == true)
                        {
                            wid_hdmSortedLeft[ye_line.stackStart].xingCheDaoWidth += addWidth;
                        }
                        else
                        {
                            wid_hdmSortedRight[ye_line.stackStart].xingCheDaoWidth += addWidth;
                        }
                        if (zhyhz_SecIndex[3] > -1 && zhyhz_SecIndex[2] > -1) //ZHY
                        {
                            ys = 0; hs = 0; zLen = 0; he = 0; ye = 0;
                            zLen = roadDetailLIst[z_index].length;
                            he = roadDetailLIst[he_index].length;
                            ye = roadDetailLIst[ye_index].length;

                            if (he >= jk_jb) { }
                            else//(he<jk_jb)
                            {
                                if (he + zLen >= jk_jb)
                                {
                                    double zhStack = roadDetailLIst[he_index].stackStart;
                                    Wid_HDmSingle zhWID_HDM = wid_hdm_;
                                    double zhAddWidth = GetAddWidth_Round(addWidth / jk_jb * (jk_jb - he));
                                    zhWID_HDM.xingCheDaoWidth += zhAddWidth;

                                    double jbsStack = ye_line.stackStart - jk_jb;
                                    Wid_HDmSingle jbsWID_HDM = wid_hdm_;

                                    AddOrEdit_widSort(ye_line.isLeft, zhStack, zhWID_HDM, ref wid_hdmSortedLeft, ref wid_hdmSortedRight);
                                    AddOrEdit_widSort(ye_line.isLeft, jbsStack, jbsWID_HDM, ref wid_hdmSortedLeft, ref wid_hdmSortedRight);

                                    #region
                                    //if (ye_line.isLeft == true)
                                    //{
                                    //    AddOrEdit_widSort(wid_hdmSortedLeft, zhStack, zhWID_HDM);

                                    //    if (wid_hdmSortedLeft.ContainsKey(jbsStack) == false)
                                    //    {
                                    //        wid_hdmSortedLeft.Add(jbsStack, jbsWID_HDM);
                                    //    }
                                    //    else
                                    //    {
                                    //        wid_hdmSortedLeft[jbsStack] = jbsWID_HDM;
                                    //    }
                                    //}
                                    //else
                                    //{
                                    //    if (wid_hdmSortedRight.ContainsKey(zhStack) == false)
                                    //    {
                                    //        wid_hdmSortedRight.Add(zhStack, zhWID_HDM);

                                    //    }
                                    //    else
                                    //    {
                                    //        wid_hdmSortedRight[zhStack] = zhWID_HDM;
                                    //    }

                                    //    if (wid_hdmSortedRight.ContainsKey(jbsStack) == false)
                                    //    {
                                    //        wid_hdmSortedRight.Add(jbsStack, jbsWID_HDM);
                                    //    }
                                    //    else
                                    //    {
                                    //        wid_hdmSortedRight[jbsStack] = jbsWID_HDM;
                                    //    }
                                    //}
                                    #endregion

                                }
                                else
                                {
                                    double zhStack = roadDetailLIst[he_index].stackStart;
                                    Wid_HDmSingle zhWID_HDM = wid_hdm_;
                                    double zhAddWidth = GetAddWidth_Round(addWidth / jk_jb * (zLen));
                                    zhWID_HDM.xingCheDaoWidth += zhAddWidth;

                                    double yhStack = roadDetailLIst[ye_index].stackStart;
                                    Wid_HDmSingle yjWID_HDM = wid_hdm_;
                                    double yhAddWidth = GetAddWidth_Round(addWidth / jk_jb * (zLen + he));
                                    yjWID_HDM.xingCheDaoWidth += yhAddWidth;

                                    double tmp_jk_JB = 0;
                                    if (he + zLen + ye / 2 >= jk_jb) {

                                        tmp_jk_JB = jk_jb;
                                    }
                                    else
                                    {
                                        tmp_jk_JB = he + zLen + ye / 2;
                                    }

                                    double jbeStack = roadDetailLIst[z_index].stackStart + tmp_jk_JB;
                                    Wid_HDmSingle jbeWID_HDM = wid_hdm_;
                                    double jbeAddWidth = GetAddWidth_Round(addWidth);
                                    jbeWID_HDM.xingCheDaoWidth += jbeAddWidth;

                                    AddOrEdit_widSort(ye_line.isLeft, zhStack, zhWID_HDM, ref wid_hdmSortedLeft, ref wid_hdmSortedRight);
                                    AddOrEdit_widSort(ye_line.isLeft, yhStack, yjWID_HDM, ref wid_hdmSortedLeft, ref wid_hdmSortedRight);
                                    AddOrEdit_widSort(ye_line.isLeft, jbeStack, jbeWID_HDM, ref wid_hdmSortedLeft, ref wid_hdmSortedRight);
                                }
                            }
                        }
                        else if (zhyhz_SecIndex[3] > -1 && zhyhz_SecIndex[2] == -1) //HY
                        {
                            ys = 0; hs = 0; zLen = 0; he = 0; ye = 0;

                            he = roadDetailLIst[he_index].length;
                            ye = roadDetailLIst[ye_index].length;

                            if (he >= jk_jb) { }
                            else//(he<jk_jb)
                            {


                                double yhStack = roadDetailLIst[ye_index].stackStart;
                                Wid_HDmSingle yjWID_HDM = wid_hdm_;
                                double yhAddWidth = GetAddWidth_Round(addWidth / jk_jb * (zLen + he));
                                yjWID_HDM.xingCheDaoWidth += yhAddWidth;

                                if (he + zLen + ye / 2 >= jk_jb) { }
                                else
                                {
                                    jk_jb = he + zLen + ye / 2;
                                }

                                double jbeStack = roadDetailLIst[he_index].stackStart + jk_jb;
                                Wid_HDmSingle jbeWID_HDM = wid_hdm_;
                                double jbeAddWidth = GetAddWidth_Round(addWidth / jk_jb * (jk_jb));
                                jbeWID_HDM.xingCheDaoWidth += jbeAddWidth;

                                AddOrEdit_widSort(ye_line.isLeft, yhStack, yjWID_HDM, ref wid_hdmSortedLeft, ref wid_hdmSortedRight);
                                AddOrEdit_widSort(ye_line.isLeft, jbeStack, jbeWID_HDM, ref wid_hdmSortedLeft, ref wid_hdmSortedRight);

                            }
                        }
                        else if (zhyhz_SecIndex[3] == -1 && zhyhz_SecIndex[2] > -1) //ZY
                        {
                            ys = 0; hs = 0; zLen = 0; he = 0; ye = 0;
                            zLen = roadDetailLIst[z_index].length;

                            ye = roadDetailLIst[ye_index].length;



                            if (zLen >= jk_jb)
                            {
                            }
                            else
                            {

                                double zyStack = roadDetailLIst[ye_index].stackStart;
                                Wid_HDmSingle yjWID_HDM = wid_hdm_;
                                double zyAddWidth = GetAddWidth_Round(addWidth / jk_jb * (zLen));
                                yjWID_HDM.xingCheDaoWidth += zyAddWidth;

                                if (zLen + ye / 2 >= jk_jb) { }
                                else
                                {
                                    jk_jb = zLen + ye / 2;
                                }
                                double jbeStack = roadDetailLIst[z_index].stackStart + jk_jb;
                                Wid_HDmSingle jbeWID_HDM = wid_hdm_;
                                double jbeAddWidth = GetAddWidth_Round(addWidth / jk_jb * (jk_jb));
                                jbeWID_HDM.xingCheDaoWidth += jbeAddWidth;


                                AddOrEdit_widSort(ye_line.isLeft, zyStack, yjWID_HDM, ref wid_hdmSortedLeft, ref wid_hdmSortedRight);
                                AddOrEdit_widSort(ye_line.isLeft, jbeStack, jbeWID_HDM, ref wid_hdmSortedLeft, ref wid_hdmSortedRight);
                            }

                        }
                        else if (zhyhz_SecIndex[3] == -1 && zhyhz_SecIndex[2] == -1)//Y
                        {

                            if (ye / 2 >= jk_jb) { }
                            else
                            {
                                jk_jb = ye / 2;
                            }
                            double jbeStack = roadDetailLIst[ye_index].stackStart + jk_jb;
                            Wid_HDmSingle jbeWID_HDM = wid_hdm_;
                            double jbeAddWidth = GetAddWidth_Round(addWidth / jk_jb * (jk_jb));
                            jbeWID_HDM.xingCheDaoWidth += jbeAddWidth;

                            AddOrEdit_widSort(ye_line.isLeft, jbeStack, jbeWID_HDM, ref wid_hdmSortedLeft, ref wid_hdmSortedRight);
                        }
                    }
                }
                //中部
                {
                    for(int i_pre=0; i_pre<yIndex_list.Count-1;i_pre++)
                    {
                        double ys = 0, hs = 0, zLen = 0, he = 0, ye = 0;

                        int[] zhyhz_SecIndex = { -1, -1, -1, -1, -1 };
                        zhyhz_SecIndex[0] = yIndex_list[i_pre];
                        zhyhz_SecIndex[4] = yIndex_list[i_pre+1];
                      
                        bool isR_H = false;
                        for (int i_mid=yIndex_list[i_pre];i_mid<yIndex_list[i_pre]+1;i_mid++)
                        {
                            LineTypeDetail tmpLine = roadDetailLIst[i_mid];
                            if (tmpLine.type.Contains("2") && isR_H == false)
                            {
                                zhyhz_SecIndex[3] = i_mid;
                                isR_H = true;
                            }
                            else if (tmpLine.type.Contains("2") && isR_H == true)
                            {
                                zhyhz_SecIndex[1] = i_mid;
                                isR_H = true;
                            }
                            else if (tmpLine.type.Contains("1"))
                            {
                                zhyhz_SecIndex[2] = i_mid;
                                isR_H = true;
                            }
                        }

                        int ys_index = zhyhz_SecIndex[0];
                        int hs_index = zhyhz_SecIndex[1];
                        int z_index = zhyhz_SecIndex[2];
                        int he_index = zhyhz_SecIndex[3];
                        int ye_index = zhyhz_SecIndex[4];

                        LineTypeDetail ys_line = roadDetailLIst[ys_index];
                        LineTypeDetail ye_line = roadDetailLIst[ye_index];
                        if (GetIsJK(ye_line.ro) == true)
                        {
                            double addWidth_Start = GetAddWidth(ys_line.ro, addWidthType);
                            double jk_jb_Start = GetJBLen_Round(Math.Max(addWidth_Start * 15, 10));
                            double addWidth_End = GetAddWidth(ye_line.ro, addWidthType);
                            double jk_jb_End = GetJBLen_Round(Math.Max(addWidth_End * 15, 10));

                            //前YH 后HY点处的加宽默认全加宽，当渐变段深入了圆曲线内时，此处的加宽值会修正

                            //if (ye_line.isLeft == true)
                            //{
                            //    wid_hdmSortedLeft[ye_line.stackStart].xingCheDaoWidth += addWidth_End;
                            //}
                            //else
                            //{
                            //    wid_hdmSortedRight[ye_line.stackStart].xingCheDaoWidth += addWidth_End;
                            //}

                            //Y H1 Z H2 Y
                            if (zhyhz_SecIndex[1] > -1&& zhyhz_SecIndex[2] > -1&& zhyhz_SecIndex[3] > -1) //YHZHY
                            {
                                if (ye_line.isLeft == true)
                                {
                                    wid_hdmSortedLeft[ye_line.stackStart].xingCheDaoWidth += addWidth_End;
                                }
                                else
                                {
                                    wid_hdmSortedRight[ye_line.stackStart].xingCheDaoWidth += addWidth_End;
                                }

                                if (ys_line.isLeft == true)
                                {
                                    wid_hdmSortedLeft[roadDetailLIst[hs_index].stackStart].xingCheDaoWidth += addWidth_End;
                                }
                                else
                                {
                                    wid_hdmSortedRight[roadDetailLIst[hs_index].stackStart].xingCheDaoWidth += addWidth_End;
                                }
                                ys = 0; hs = 0; zLen = 0; he = 0; ye = 0;
                               
                                ys = roadDetailLIst[ys_index].length;
                                hs = roadDetailLIst[hs_index].length;
                                zLen = roadDetailLIst[z_index].length;
                                he = roadDetailLIst[he_index].length;
                                ye = roadDetailLIst[ye_index].length;


                                if (hs >= jk_jb_Start && he >= jk_jb_End)
                                {

                                }
                                else if (hs < jk_jb_Start && he >= jk_jb_End)
                                {
                                    if (zLen + hs >= jk_jb_Start)
                                    {
                                        double hzStack = roadDetailLIst[z_index].stackStart;
                                        Wid_HDmSingle hzWID_HDM = wid_hdm_;
                                        double hzAddWidth = GetAddWidth_Round(addWidth_Start / jk_jb_Start * (jk_jb_Start-roadDetailLIst[hs_index].length));
                                        hzWID_HDM.xingCheDaoWidth += hzAddWidth;
                                        AddOrEdit_widSort(ys_line.isLeft, hzStack, hzWID_HDM, ref wid_hdmSortedLeft, ref wid_hdmSortedRight);

                                        double jbsStack =roadDetailLIst[hs_index].stackStart +jk_jb_Start;
                                        Wid_HDmSingle jbsWID_HDM = wid_hdm_;
                                        AddOrEdit_widSort(ys_line.isLeft, jbsStack, jbsWID_HDM, ref wid_hdmSortedLeft, ref wid_hdmSortedRight);
                                    }
                                    else
                                    {
                                        double hzStack = roadDetailLIst[z_index].stackStart;
                                        Wid_HDmSingle hzWID_HDM = wid_hdm_;
                                        double hzAddWidth = GetAddWidth_Round(addWidth_Start / jk_jb_Start * (roadDetailLIst[z_index].length));
                                        hzWID_HDM.xingCheDaoWidth += hzAddWidth;
                                        AddOrEdit_widSort(ys_line.isLeft, hzStack, hzWID_HDM, ref wid_hdmSortedLeft, ref wid_hdmSortedRight);

                                        double yhStack = roadDetailLIst[hs_index].stackStart;
                                        Wid_HDmSingle yjWID_HDM = wid_hdm_;
                                        double yhAddWidth = GetAddWidth_Round(addWidth_Start / jk_jb_Start * (roadDetailLIst[z_index].length + roadDetailLIst[hs_index].length));
                                        yjWID_HDM.xingCheDaoWidth += yhAddWidth;
                                        AddOrEdit_widSort(ys_line.isLeft, yhStack, yjWID_HDM, ref wid_hdmSortedLeft, ref wid_hdmSortedRight);

                                        double tmp_jk_JB = 0;

                                        if (zLen + he + ys / 2 >= jk_jb_Start)
                                        {
                                            tmp_jk_JB = jk_jb_Start;
                                        }
                                        else
                                        {
                                            tmp_jk_JB = hs + zLen + ys / 2;
                                        }

                                        double jbeStack = roadDetailLIst[z_index].stackStart - tmp_jk_JB;
                                        Wid_HDmSingle jbeWID_HDM = wid_hdm_;
                                        double jbeAddWidth = GetAddWidth_Round(addWidth_Start);
                                        jbeWID_HDM.xingCheDaoWidth += jbeAddWidth;

                                        AddOrEdit_widSort(ys_line.isLeft, jbeStack, jbeWID_HDM, ref wid_hdmSortedLeft, ref wid_hdmSortedRight);

                                    }
                                }
                                else if (hs >= jk_jb_Start && he < jk_jb_End)
                                {
                                    if(zLen+he>=jk_jb_End)
                                    {
                                        double zhStack = roadDetailLIst[hs_index].stackStart;
                                        Wid_HDmSingle zhWID_HDM = wid_hdm_;
                                        double zhAddWidth = GetAddWidth_Round(addWidth_End / jk_jb_End * (addWidth_End - roadDetailLIst[hs_index].length));
                                        zhWID_HDM.xingCheDaoWidth += zhAddWidth;
                                        AddOrEdit_widSort(ye_line.isLeft, zhStack, zhWID_HDM, ref wid_hdmSortedLeft, ref wid_hdmSortedRight);

                                        double jbsStack = roadDetailLIst[ys_index].stackStart - jk_jb_End;
                                        Wid_HDmSingle jbsWID_HDM = wid_hdm_;
                                        AddOrEdit_widSort(ye_line.isLeft, jbsStack, jbsWID_HDM, ref wid_hdmSortedLeft, ref wid_hdmSortedRight);
                                    }
                                    else
                                    {
                                        double zhStack = roadDetailLIst[hs_index].stackStart;
                                        Wid_HDmSingle zhWID_HDM = wid_hdm_;
                                        double hzAddWidth = GetAddWidth_Round(addWidth_End / jk_jb_End * (roadDetailLIst[z_index].length));
                                        zhWID_HDM.xingCheDaoWidth += hzAddWidth;
                                        AddOrEdit_widSort(ye_line.isLeft, zhStack, zhWID_HDM, ref wid_hdmSortedLeft, ref wid_hdmSortedRight);

                                        double yhStack = roadDetailLIst[ys_index].stackStart;
                                        Wid_HDmSingle yjWID_HDM = wid_hdm_;
                                        double yhAddWidth = GetAddWidth_Round(addWidth_End / jk_jb_End * (roadDetailLIst[z_index].length + roadDetailLIst[hs_index].length));
                                        yjWID_HDM.xingCheDaoWidth += yhAddWidth;
                                        AddOrEdit_widSort(ye_line.isLeft, yhStack, yjWID_HDM, ref wid_hdmSortedLeft, ref wid_hdmSortedRight);

                                        double tmp_jk_JB = 0;

                                        if (zLen + he + ye / 2 >= jk_jb_End)
                                        {
                                            tmp_jk_JB = jk_jb_End;
                                        }
                                        else
                                        {
                                            tmp_jk_JB = hs + zLen + ys / 2;
                                        }
                                       

                                        double jbeStack = roadDetailLIst[z_index].stackStart + tmp_jk_JB;
                                        Wid_HDmSingle jbeWID_HDM = wid_hdm_;
                                        double jbeAddWidth = GetAddWidth_Round(addWidth_End);
                                        jbeWID_HDM.xingCheDaoWidth += jbeAddWidth;

                                        AddOrEdit_widSort(ye_line.isLeft, jbeStack, jbeWID_HDM, ref wid_hdmSortedLeft, ref wid_hdmSortedRight); 
                                    }
                                }     
                                else if(hs< jk_jb_Start && he<jk_jb_End)
                                {
                                    //if (hs + zLen + he >= jk_jb_Start + jk_jb_End)
                                    //if (hs + zLen >= jk_jb_Start && zLen + he >= jk_jb_End)
                                     if (hs + zLen + he >= jk_jb_Start + jk_jb_End &&hs + zLen >= jk_jb_Start && zLen + he >= jk_jb_End)
                                    {
                                        double hzStack = roadDetailLIst[z_index].stackStart;
                                        Wid_HDmSingle hzWID_HDM = wid_hdm_;
                                        double hzAddWidth = GetAddWidth_Round(addWidth_Start / jk_jb_Start * (jk_jb_Start - roadDetailLIst[hs_index].length));
                                        hzWID_HDM.xingCheDaoWidth += hzAddWidth;
                                        AddOrEdit_widSort(ys_line.isLeft, hzStack, hzWID_HDM, ref wid_hdmSortedLeft, ref wid_hdmSortedRight);

                                        double jbssdStack = roadDetailLIst[hs_index].stackStart + jk_jb_Start;
                                        Wid_HDmSingle jbssWID_HDM = wid_hdm_;
                                        AddOrEdit_widSort(ys_line.isLeft, jbssdStack, jbssWID_HDM, ref wid_hdmSortedLeft, ref wid_hdmSortedRight);

                                        double zhStack = roadDetailLIst[hs_index].stackStart;
                                        Wid_HDmSingle zhWID_HDM = wid_hdm_;
                                        double zhAddWidth = GetAddWidth_Round(addWidth_End / jk_jb_End * (addWidth_End - roadDetailLIst[hs_index].length));
                                        zhWID_HDM.xingCheDaoWidth += zhAddWidth;
                                        AddOrEdit_widSort(ye_line.isLeft, zhStack, zhWID_HDM, ref wid_hdmSortedLeft, ref wid_hdmSortedRight);

                                        double jbeStack = roadDetailLIst[ys_index].stackStart - jk_jb_End;
                                        Wid_HDmSingle jbeWID_HDM = wid_hdm_;
                                        AddOrEdit_widSort(ye_line.isLeft, jbeStack, jbeWID_HDM, ref wid_hdmSortedLeft, ref wid_hdmSortedRight);

                                    }
                                    else if (hs + zLen >= jk_jb_Start)
                                    {
                                        double hzStack = roadDetailLIst[z_index].stackStart;
                                        Wid_HDmSingle hzWID_HDM = wid_hdm_;
                                        double hzAddWidth = GetAddWidth_Round(addWidth_Start / jk_jb_Start * (jk_jb_Start - roadDetailLIst[hs_index].length));
                                        hzWID_HDM.xingCheDaoWidth += hzAddWidth;
                                        AddOrEdit_widSort(ys_line.isLeft, hzStack, hzWID_HDM, ref wid_hdmSortedLeft, ref wid_hdmSortedRight);

                                        double jbsStack = roadDetailLIst[hs_index].stackStart + jk_jb_Start;
                                        Wid_HDmSingle jbsWID_HDM = wid_hdm_;
                                        AddOrEdit_widSort(ys_line.isLeft, jbsStack, jbsWID_HDM, ref wid_hdmSortedLeft, ref wid_hdmSortedRight);

                                        if (hs + zLen == jk_jb_Start)
                                        {
                                            double yhStack = roadDetailLIst[ys_index].stackStart;
                                            Wid_HDmSingle yhWID_HDM = wid_hdm_;
                                            double yhAddWidth = GetAddWidth_Round(addWidth_End / jk_jb_End * (roadDetailLIst[hs_index].length));
                                            yhWID_HDM.xingCheDaoWidth += yhAddWidth;
                                            AddOrEdit_widSort(ye_line.isLeft, yhStack, yhWID_HDM, ref wid_hdmSortedLeft, ref wid_hdmSortedRight);

                                            double tmp_jk_JB = 0;

                                            if (he + ye / 2 >= jk_jb_End)
                                            {
                                                tmp_jk_JB = jk_jb_End;
                                            }
                                            else
                                            {
                                                tmp_jk_JB = hs + ys / 2;
                                            }

                                            double jbeStack = roadDetailLIst[he_index].stackStart + tmp_jk_JB;
                                            Wid_HDmSingle jbeWID_HDM = wid_hdm_;
                                            double jbeAddWidth = GetAddWidth_Round(addWidth_End);
                                            jbeWID_HDM.xingCheDaoWidth += jbeAddWidth;

                                            AddOrEdit_widSort(ye_line.isLeft, jbeStack, jbeWID_HDM, ref wid_hdmSortedLeft, ref wid_hdmSortedRight);
                                        }
                                        else
                                        {
                                            double deltaStack = hs + zLen - jk_jb_Start;
                                            double zhStack = roadDetailLIst[hs_index].stackStart;
                                            Wid_HDmSingle zhWID_HDM = wid_hdm_;
                                            double zhAddWidth = GetAddWidth_Round(addWidth_End / jk_jb_End * (deltaStack));
                                            zhWID_HDM.xingCheDaoWidth += zhAddWidth;
                                            AddOrEdit_widSort(ye_line.isLeft, zhStack, zhWID_HDM, ref wid_hdmSortedLeft, ref wid_hdmSortedRight);

                                            double yhStack = roadDetailLIst[ys_index].stackStart;
                                            Wid_HDmSingle yhWID_HDM = wid_hdm_;
                                            double yhAddWidth = GetAddWidth_Round(addWidth_End / jk_jb_End * (deltaStack + roadDetailLIst[hs_index].length));
                                            yhWID_HDM.xingCheDaoWidth += yhAddWidth;
                                            AddOrEdit_widSort(ye_line.isLeft, yhStack, yhWID_HDM, ref wid_hdmSortedLeft, ref wid_hdmSortedRight);

                                            double tmp_jk_JB = 0;

                                            if (he + ye / 2 + deltaStack >= jk_jb_End)
                                            {
                                                tmp_jk_JB = jk_jb_End;
                                            }
                                            else
                                            {
                                                tmp_jk_JB = deltaStack + hs + ys / 2;
                                            }


                                            double jbeStack = roadDetailLIst[hs_index].stackStart + tmp_jk_JB - (deltaStack);
                                            Wid_HDmSingle jbeWID_HDM = wid_hdm_;
                                            double jbeAddWidth = GetAddWidth_Round(addWidth_End);
                                            jbeWID_HDM.xingCheDaoWidth += jbeAddWidth;

                                            AddOrEdit_widSort(ye_line.isLeft, jbeStack, jbeWID_HDM, ref wid_hdmSortedLeft, ref wid_hdmSortedRight);
                                        }
                                    }
                                    else if (zLen + he >= jk_jb_End)
                                    {
                                        double zhStack = roadDetailLIst[hs_index].stackStart;
                                        Wid_HDmSingle zhWID_HDM = wid_hdm_;
                                        double zhAddWidth = GetAddWidth_Round(addWidth_End / jk_jb_End * (addWidth_End - roadDetailLIst[hs_index].length));
                                        zhWID_HDM.xingCheDaoWidth += zhAddWidth;
                                        AddOrEdit_widSort(ye_line.isLeft, zhStack, zhWID_HDM, ref wid_hdmSortedLeft, ref wid_hdmSortedRight);

                                        double jbsStack = roadDetailLIst[ys_index].stackStart - jk_jb_End;
                                        Wid_HDmSingle jbsWID_HDM = wid_hdm_;
                                        AddOrEdit_widSort(ye_line.isLeft, jbsStack, jbsWID_HDM, ref wid_hdmSortedLeft, ref wid_hdmSortedRight);


                                        if (zLen + he == jk_jb_End)
                                        {
                                            //double hzStack = roadDetailLIst[z_index].stackStart;
                                            //Wid_HDmSingle hzWID_HDM = wid_hdm_;
                                            //double hzAddWidth = GetAddWidth_Round(addWidth_Start / jk_jb_Start * (roadDetailLIst[z_index].length));
                                            //hzWID_HDM.xingCheDaoWidth += hzAddWidth;
                                            //AddOrEdit_widSort(ys_line.isLeft, hzStack, hzWID_HDM, ref wid_hdmSortedLeft, ref wid_hdmSortedRight);

                                            double yhStack = roadDetailLIst[hs_index].stackStart;
                                            Wid_HDmSingle yjWID_HDM = wid_hdm_;
                                            double yhAddWidth = GetAddWidth_Round(addWidth_Start / jk_jb_Start * (roadDetailLIst[z_index].length + roadDetailLIst[hs_index].length));
                                            yjWID_HDM.xingCheDaoWidth += yhAddWidth;
                                            AddOrEdit_widSort(ys_line.isLeft, yhStack, yjWID_HDM, ref wid_hdmSortedLeft, ref wid_hdmSortedRight);

                                            double tmp_jk_JB = 0;

                                            if (he + ys / 2 >= jk_jb_Start)
                                            {
                                                tmp_jk_JB = jk_jb_Start;
                                            }
                                            else
                                            {
                                                tmp_jk_JB = hs + ys / 2;
                                            }

                                            double jbeStack = roadDetailLIst[z_index].stackStart - tmp_jk_JB;
                                            Wid_HDmSingle jbeWID_HDM = wid_hdm_;
                                            double jbeAddWidth = GetAddWidth_Round(addWidth_Start);
                                            jbeWID_HDM.xingCheDaoWidth += jbeAddWidth;

                                            AddOrEdit_widSort(ys_line.isLeft, jbeStack, jbeWID_HDM, ref wid_hdmSortedLeft, ref wid_hdmSortedRight);
                                        }
                                        else
                                        {
                                            double deltaStack = hs + zLen - jk_jb_End;
                                            double hzStack = roadDetailLIst[z_index].stackStart;
                                            Wid_HDmSingle hzWID_HDM = wid_hdm_;
                                            double hzAddWidth = GetAddWidth_Round(addWidth_Start / jk_jb_Start * (deltaStack));
                                            hzWID_HDM.xingCheDaoWidth += hzAddWidth;
                                            AddOrEdit_widSort(ys_line.isLeft, hzStack, hzWID_HDM, ref wid_hdmSortedLeft, ref wid_hdmSortedRight);

                                            double yhStack = roadDetailLIst[hs_index].stackStart;
                                            Wid_HDmSingle yjWID_HDM = wid_hdm_;
                                            double yhAddWidth = GetAddWidth_Round(addWidth_Start / jk_jb_Start * (deltaStack + roadDetailLIst[hs_index].length));
                                            yjWID_HDM.xingCheDaoWidth += yhAddWidth;
                                            AddOrEdit_widSort(ys_line.isLeft, yhStack, yjWID_HDM, ref wid_hdmSortedLeft, ref wid_hdmSortedRight);

                                            double tmp_jk_JB = 0;

                                            if (hs + ys / 2 + startsStack >= jk_jb_Start)
                                            {
                                                tmp_jk_JB = jk_jb_Start;
                                            }
                                            else
                                            {
                                                tmp_jk_JB = hs + ys / 2 + startsStack;
                                            }

                                            double jbeStack = roadDetailLIst[z_index].stackStart + deltaStack - tmp_jk_JB;
                                            Wid_HDmSingle jbeWID_HDM = wid_hdm_;
                                            double jbeAddWidth = GetAddWidth_Round(addWidth_Start);
                                            jbeWID_HDM.xingCheDaoWidth += jbeAddWidth;

                                            AddOrEdit_widSort(ys_line.isLeft, jbeStack, jbeWID_HDM, ref wid_hdmSortedLeft, ref wid_hdmSortedRight);
                                        }
                                    }
                                    else// 剩下的部分直接将直线均分
                                    {
                                        double zzStack = roadDetailLIst[z_index].stackStart + zLen / 2;
                                        Wid_HDmSingle zzWID_HDM = wid_hdm_;
                                        AddOrEdit_widSort(true, zzStack, zzWID_HDM, ref wid_hdmSortedLeft, ref wid_hdmSortedRight);
                                        AddOrEdit_widSort(false, zzStack, zzWID_HDM, ref wid_hdmSortedLeft, ref wid_hdmSortedRight);
                                        //起始端
                                        {
                                            double deltaStack = zLen / 2;
                                            double hzStack = roadDetailLIst[z_index].stackStart;
                                            Wid_HDmSingle hzWID_HDM = wid_hdm_;
                                            double hzAddWidth = GetAddWidth_Round(addWidth_Start / jk_jb_Start * (deltaStack));
                                            hzWID_HDM.xingCheDaoWidth += hzAddWidth;
                                            AddOrEdit_widSort(ys_line.isLeft, hzStack, hzWID_HDM, ref wid_hdmSortedLeft, ref wid_hdmSortedRight);

                                            double yhStack = roadDetailLIst[hs_index].stackStart;
                                            Wid_HDmSingle yjWID_HDM = wid_hdm_;
                                            double yhAddWidth = GetAddWidth_Round(addWidth_Start / jk_jb_Start * (deltaStack + roadDetailLIst[hs_index].length));
                                            yjWID_HDM.xingCheDaoWidth += yhAddWidth;
                                            AddOrEdit_widSort(ys_line.isLeft, yhStack, yjWID_HDM, ref wid_hdmSortedLeft, ref wid_hdmSortedRight);


                                            double tmp_jk_JB = 0;
                                            if (zLen / 2 + hs + ys / 2 >= jk_jb_Start)
                                            {
                                                tmp_jk_JB = jk_jb_Start;
                                            }
                                            else
                                            {
                                                tmp_jk_JB = zLen / 2 + hs + ys / 2;
                                            }

                                            double jbeStack = roadDetailLIst[z_index].stackStart + deltaStack - tmp_jk_JB;
                                            Wid_HDmSingle jbeWID_HDM = wid_hdm_;
                                            double jbeAddWidth = GetAddWidth_Round(addWidth_Start);
                                            jbeWID_HDM.xingCheDaoWidth += jbeAddWidth;

                                            AddOrEdit_widSort(ys_line.isLeft, jbeStack, jbeWID_HDM, ref wid_hdmSortedLeft, ref wid_hdmSortedRight);

                                        }
                                        //末端
                                        {
                                            double deltaStack = zLen / 2;
                                            double zhStack = roadDetailLIst[hs_index].stackStart;
                                            Wid_HDmSingle zhWID_HDM = wid_hdm_;
                                            double zhAddWidth = GetAddWidth_Round(addWidth_End / jk_jb_End * (deltaStack));
                                            zhWID_HDM.xingCheDaoWidth += zhAddWidth;
                                            AddOrEdit_widSort(ye_line.isLeft, zhStack, zhWID_HDM, ref wid_hdmSortedLeft, ref wid_hdmSortedRight);

                                            double yhStack = roadDetailLIst[ys_index].stackStart;
                                            Wid_HDmSingle yhWID_HDM = wid_hdm_;
                                            double yhAddWidth = GetAddWidth_Round(addWidth_End / jk_jb_End * (deltaStack + roadDetailLIst[hs_index].length));
                                            yhWID_HDM.xingCheDaoWidth += yhAddWidth;
                                            AddOrEdit_widSort(ye_line.isLeft, yhStack, yhWID_HDM, ref wid_hdmSortedLeft, ref wid_hdmSortedRight);

                                            double tmp_jk_JB = 0;

                                            if (he + ye / 2 + deltaStack >= jk_jb_End)
                                            {
                                                tmp_jk_JB = jk_jb_End;
                                            }
                                            else
                                            {
                                                tmp_jk_JB = deltaStack + hs + ys / 2;
                                            }


                                            double jbeStack = roadDetailLIst[hs_index].stackStart + tmp_jk_JB - (deltaStack);
                                            Wid_HDmSingle jbeWID_HDM = wid_hdm_;
                                            double jbeAddWidth = GetAddWidth_Round(addWidth_End);
                                            jbeWID_HDM.xingCheDaoWidth += jbeAddWidth;

                                            AddOrEdit_widSort(ye_line.isLeft, jbeStack, jbeWID_HDM, ref wid_hdmSortedLeft, ref wid_hdmSortedRight);
                                        }
                                    }                                
                                }
                            }
                            //Y H1 H2 Y
                            else if (zhyhz_SecIndex[1] > -1 && zhyhz_SecIndex[2] == -1 && zhyhz_SecIndex[3] > -1) //HY
                            {
                                if (ye_line.isLeft == true)
                                {
                                    wid_hdmSortedLeft[ye_line.stackStart].xingCheDaoWidth += addWidth_End;
                                }
                                else
                                {
                                    wid_hdmSortedRight[ye_line.stackStart].xingCheDaoWidth += addWidth_End;
                                }

                                if (ys_line.isLeft == true)
                                {
                                    wid_hdmSortedLeft[roadDetailLIst[hs_index].stackStart].xingCheDaoWidth += addWidth_End;
                                }
                                else
                                {
                                    wid_hdmSortedRight[roadDetailLIst[hs_index].stackStart].xingCheDaoWidth += addWidth_End;
                                }

                                ys = 0; hs = 0; zLen = 0; he = 0; ye = 0;

                                ys = roadDetailLIst[ys_index].length;
                                hs = roadDetailLIst[hs_index].length;
                                //zLen = roadDetailLIst[z_index].length;
                                he = roadDetailLIst[he_index].length;
                                ye = roadDetailLIst[ye_index].length;


                                if (hs >= jk_jb_Start && he >= jk_jb_End)
                                {

                                }
                                else if (hs < jk_jb_Start && he >= jk_jb_End)
                                {
                                    if (zLen + hs >= jk_jb_Start)
                                    {
                                       
                                    }
                                    else
                                    {
                                       
                                        double yhStack = roadDetailLIst[hs_index].stackStart;
                                        Wid_HDmSingle yjWID_HDM = wid_hdm_;
                                        double yhAddWidth = GetAddWidth_Round(addWidth_Start / jk_jb_Start * (roadDetailLIst[hs_index].length));//(roadDetailLIst[z_index].length + roadDetailLIst[hs_index].length));
                                        yjWID_HDM.xingCheDaoWidth += yhAddWidth;
                                        AddOrEdit_widSort(ys_line.isLeft, yhStack, yjWID_HDM, ref wid_hdmSortedLeft, ref wid_hdmSortedRight);

                                        double tmp_jk_JB = 0;

                                        if (zLen + he + ys / 2 >= jk_jb_Start)
                                        {
                                            tmp_jk_JB = jk_jb_Start;
                                        }
                                        else
                                        {
                                            tmp_jk_JB = hs + zLen + ys / 2;
                                        }

                                        double jbeStack = roadDetailLIst[hs_index].stackStart +hs- tmp_jk_JB;
                                        Wid_HDmSingle jbeWID_HDM = wid_hdm_;
                                        double jbeAddWidth = GetAddWidth_Round(addWidth_Start);
                                        jbeWID_HDM.xingCheDaoWidth += jbeAddWidth;

                                        AddOrEdit_widSort(ys_line.isLeft, jbeStack, jbeWID_HDM, ref wid_hdmSortedLeft, ref wid_hdmSortedRight);

                                    }
                                }
                                else if (hs >= jk_jb_Start && he < jk_jb_End)
                                {
                                    if (zLen + he >= jk_jb_End)
                                    {
                                     
                                    }
                                    else
                                    {
                                        

                                        double yhStack = roadDetailLIst[ys_index].stackStart;
                                        Wid_HDmSingle yjWID_HDM = wid_hdm_;
                                        double yhAddWidth = GetAddWidth_Round(addWidth_End / jk_jb_End * ( roadDetailLIst[hs_index].length));
                                        yjWID_HDM.xingCheDaoWidth += yhAddWidth;
                                        AddOrEdit_widSort(ye_line.isLeft, yhStack, yjWID_HDM, ref wid_hdmSortedLeft, ref wid_hdmSortedRight);

                                        double tmp_jk_JB = 0;

                                        if (zLen + he + ye / 2 >= jk_jb_End)
                                        {
                                            tmp_jk_JB = jk_jb_End;
                                        }
                                        else
                                        {
                                            tmp_jk_JB = hs + zLen + ys / 2;
                                        }

                                        double jbeStack = roadDetailLIst[hs_index].stackStart + tmp_jk_JB;
                                        Wid_HDmSingle jbeWID_HDM = wid_hdm_;
                                        double jbeAddWidth = GetAddWidth_Round(addWidth_End);
                                        jbeWID_HDM.xingCheDaoWidth += jbeAddWidth;

                                        AddOrEdit_widSort(ye_line.isLeft, jbeStack, jbeWID_HDM, ref wid_hdmSortedLeft, ref wid_hdmSortedRight);
                                    }
                                }
                                else if (hs < jk_jb_Start && he < jk_jb_End)
                                {
                                    
                                     
                                    //起始端
                                        {
                                            double deltaStack = 0;
                                           
                                            double yhStack = roadDetailLIst[hs_index].stackStart;
                                            Wid_HDmSingle yjWID_HDM = wid_hdm_;
                                            double yhAddWidth = GetAddWidth_Round(addWidth_Start / jk_jb_Start * (deltaStack + roadDetailLIst[hs_index].length));
                                            yjWID_HDM.xingCheDaoWidth += yhAddWidth;
                                            AddOrEdit_widSort(ys_line.isLeft, yhStack, yjWID_HDM, ref wid_hdmSortedLeft, ref wid_hdmSortedRight);


                                            double tmp_jk_JB = 0;
                                            if (zLen / 2 + hs + ys / 2 >= jk_jb_Start)
                                            {
                                                tmp_jk_JB = jk_jb_Start;
                                            }
                                            else
                                            {
                                                tmp_jk_JB = zLen / 2 + hs + ys / 2;
                                            }

                                            double jbeStack = roadDetailLIst[z_index].stackStart + deltaStack - tmp_jk_JB;
                                            Wid_HDmSingle jbeWID_HDM = wid_hdm_;
                                            double jbeAddWidth = GetAddWidth_Round(addWidth_Start);
                                            jbeWID_HDM.xingCheDaoWidth += jbeAddWidth;

                                            AddOrEdit_widSort(ys_line.isLeft, jbeStack, jbeWID_HDM, ref wid_hdmSortedLeft, ref wid_hdmSortedRight);

                                        }
                                      
                                    //末端
                                        {
                                            double deltaStack = 0;
                                            double zhStack = roadDetailLIst[hs_index].stackStart;
                                            Wid_HDmSingle zhWID_HDM = wid_hdm_;
                                            double zhAddWidth = GetAddWidth_Round(addWidth_End / jk_jb_End * (deltaStack));
                                            zhWID_HDM.xingCheDaoWidth += zhAddWidth;
                                            AddOrEdit_widSort(ye_line.isLeft, zhStack, zhWID_HDM, ref wid_hdmSortedLeft, ref wid_hdmSortedRight);

                                            double yhStack = roadDetailLIst[ys_index].stackStart;
                                            Wid_HDmSingle yhWID_HDM = wid_hdm_;
                                            double yhAddWidth = GetAddWidth_Round(addWidth_End / jk_jb_End * (deltaStack + roadDetailLIst[hs_index].length));
                                            yhWID_HDM.xingCheDaoWidth += yhAddWidth;
                                            AddOrEdit_widSort(ye_line.isLeft, yhStack, yhWID_HDM, ref wid_hdmSortedLeft, ref wid_hdmSortedRight);

                                            double tmp_jk_JB = 0;

                                            if (he + ye / 2 + deltaStack >= jk_jb_End)
                                            {
                                                tmp_jk_JB = jk_jb_End;
                                            }
                                            else
                                            {
                                                tmp_jk_JB = deltaStack + hs + ys / 2;
                                            }


                                            double jbeStack = roadDetailLIst[hs_index].stackStart + tmp_jk_JB - (deltaStack);
                                            Wid_HDmSingle jbeWID_HDM = wid_hdm_;
                                            double jbeAddWidth = GetAddWidth_Round(addWidth_End);
                                            jbeWID_HDM.xingCheDaoWidth += jbeAddWidth;

                                            AddOrEdit_widSort(ye_line.isLeft, jbeStack, jbeWID_HDM, ref wid_hdmSortedLeft, ref wid_hdmSortedRight);
                                        } 
                                    //}
                                }
                            }
                            //YH1ZY
                            else if (zhyhz_SecIndex[1] > -1 && zhyhz_SecIndex[2] > -1 && zhyhz_SecIndex[3] == -1) //ZY
                            {
                                if (ye_line.isLeft == true)
                                {
                                    wid_hdmSortedLeft[ye_line.stackStart].xingCheDaoWidth += addWidth_End;
                                }
                                else
                                {
                                    wid_hdmSortedRight[ye_line.stackStart].xingCheDaoWidth += addWidth_End;
                                }

                                if (ys_line.isLeft == true)
                                {
                                    wid_hdmSortedLeft[roadDetailLIst[hs_index].stackStart].xingCheDaoWidth += addWidth_End;
                                }
                                else
                                {
                                    wid_hdmSortedRight[roadDetailLIst[hs_index].stackStart].xingCheDaoWidth += addWidth_End;
                                }
                                ys = 0; hs = 0; zLen = 0; he = 0; ye = 0;

                                ys = roadDetailLIst[ys_index].length;
                                hs = roadDetailLIst[hs_index].length;
                                zLen = roadDetailLIst[z_index].length;
                                //he = roadDetailLIst[he_index].length;
                                ye = roadDetailLIst[ye_index].length;


                                if (hs >= jk_jb_Start && he >= jk_jb_End)//不会执行的
                                {

                                }
                                else if (hs < jk_jb_Start && he >= jk_jb_End)//不会执行的
                                {
                                    if (zLen + hs >= jk_jb_Start)
                                    {
                                        double hzStack = roadDetailLIst[z_index].stackStart;
                                        Wid_HDmSingle hzWID_HDM = wid_hdm_;
                                        double hzAddWidth = GetAddWidth_Round(addWidth_Start / jk_jb_Start * (jk_jb_Start - roadDetailLIst[hs_index].length));
                                        hzWID_HDM.xingCheDaoWidth += hzAddWidth;
                                        AddOrEdit_widSort(ys_line.isLeft, hzStack, hzWID_HDM, ref wid_hdmSortedLeft, ref wid_hdmSortedRight);

                                        double jbsStack = roadDetailLIst[hs_index].stackStart + jk_jb_Start;
                                        Wid_HDmSingle jbsWID_HDM = wid_hdm_;
                                        AddOrEdit_widSort(ys_line.isLeft, jbsStack, jbsWID_HDM, ref wid_hdmSortedLeft, ref wid_hdmSortedRight);
                                    }
                                    else
                                    {
                                        double hzStack = roadDetailLIst[z_index].stackStart;
                                        Wid_HDmSingle hzWID_HDM = wid_hdm_;
                                        double hzAddWidth = GetAddWidth_Round(addWidth_Start / jk_jb_Start * (roadDetailLIst[z_index].length));
                                        hzWID_HDM.xingCheDaoWidth += hzAddWidth;
                                        AddOrEdit_widSort(ys_line.isLeft, hzStack, hzWID_HDM, ref wid_hdmSortedLeft, ref wid_hdmSortedRight);

                                        double yhStack = roadDetailLIst[hs_index].stackStart;
                                        Wid_HDmSingle yjWID_HDM = wid_hdm_;
                                        double yhAddWidth = GetAddWidth_Round(addWidth_Start / jk_jb_Start * (roadDetailLIst[z_index].length + roadDetailLIst[hs_index].length));
                                        yjWID_HDM.xingCheDaoWidth += yhAddWidth;
                                        AddOrEdit_widSort(ys_line.isLeft, yhStack, yjWID_HDM, ref wid_hdmSortedLeft, ref wid_hdmSortedRight);

                                        double tmp_jk_JB = 0;

                                        if (zLen + he + ys / 2 >= jk_jb_Start)
                                        {
                                            tmp_jk_JB = jk_jb_Start;
                                        }
                                        else
                                        {
                                            tmp_jk_JB = hs + zLen + ys / 2;
                                        }

                                        double jbeStack = roadDetailLIst[z_index].stackStart - tmp_jk_JB;
                                        Wid_HDmSingle jbeWID_HDM = wid_hdm_;
                                        double jbeAddWidth = GetAddWidth_Round(addWidth_Start);
                                        jbeWID_HDM.xingCheDaoWidth += jbeAddWidth;

                                        AddOrEdit_widSort(ys_line.isLeft, jbeStack, jbeWID_HDM, ref wid_hdmSortedLeft, ref wid_hdmSortedRight);

                                    }
                                }
                                else if (hs >= jk_jb_Start && he < jk_jb_End)
                                {
                                    if (zLen + he >= jk_jb_End)
                                    {
                                        double zhStack = roadDetailLIst[hs_index].stackStart;
                                        Wid_HDmSingle zhWID_HDM = wid_hdm_;
                                        double zhAddWidth = GetAddWidth_Round(addWidth_End / jk_jb_End * (addWidth_End - roadDetailLIst[hs_index].length));
                                        zhWID_HDM.xingCheDaoWidth += zhAddWidth;
                                        AddOrEdit_widSort(ye_line.isLeft, zhStack, zhWID_HDM, ref wid_hdmSortedLeft, ref wid_hdmSortedRight);

                                        double jbsStack = roadDetailLIst[ys_index].stackStart - jk_jb_End;
                                        Wid_HDmSingle jbsWID_HDM = wid_hdm_;
                                        AddOrEdit_widSort(ye_line.isLeft, jbsStack, jbsWID_HDM, ref wid_hdmSortedLeft, ref wid_hdmSortedRight);
                                    }
                                    else
                                    {
                                        double zyStack = roadDetailLIst[ye_index].stackStart;
                                        Wid_HDmSingle zyWID_HDM = wid_hdm_;
                                        double zyAddWidth = GetAddWidth_Round(addWidth_End / jk_jb_End * (roadDetailLIst[z_index].length));
                                        zyWID_HDM.xingCheDaoWidth += zyAddWidth;
                                        AddOrEdit_widSort(ye_line.isLeft, zyStack, zyWID_HDM, ref wid_hdmSortedLeft, ref wid_hdmSortedRight);

                                        //double yhStack = roadDetailLIst[ys_index].stackStart;
                                        //Wid_HDmSingle yjWID_HDM = wid_hdm_;
                                        //double yhAddWidth = GetAddWidth_Round(addWidth_End / jk_jb_End * (roadDetailLIst[z_index].length + roadDetailLIst[hs_index].length));
                                        //yjWID_HDM.xingCheDaoWidth += yhAddWidth;
                                        //AddOrEdit_widSort(ye_line.isLeft, yhStack, yjWID_HDM, ref wid_hdmSortedLeft, ref wid_hdmSortedRight);

                                        double tmp_jk_JB = 0;

                                        if (zLen + he + ye / 2 >= jk_jb_End)
                                        {
                                            tmp_jk_JB = jk_jb_End;
                                        }
                                        else
                                        {
                                            tmp_jk_JB = hs + zLen + ys / 2;
                                        }


                                        double jbeStack = roadDetailLIst[z_index].stackStart + tmp_jk_JB;
                                        Wid_HDmSingle jbeWID_HDM = wid_hdm_;
                                        double jbeAddWidth = GetAddWidth_Round(addWidth_End);
                                        jbeWID_HDM.xingCheDaoWidth += jbeAddWidth;

                                        AddOrEdit_widSort(ye_line.isLeft, jbeStack, jbeWID_HDM, ref wid_hdmSortedLeft, ref wid_hdmSortedRight);
                                    }
                                }
                                else if (hs < jk_jb_Start && he < jk_jb_End)
                                {
                                    

                                    if (hs + zLen + he >= jk_jb_Start + jk_jb_End && hs + zLen >= jk_jb_Start && zLen + he >= jk_jb_End)
                                    {
                                        double hzStack = roadDetailLIst[z_index].stackStart;
                                        Wid_HDmSingle hzWID_HDM = wid_hdm_;
                                        double hzAddWidth = GetAddWidth_Round(addWidth_Start / jk_jb_Start * (jk_jb_Start - roadDetailLIst[hs_index].length));
                                        hzWID_HDM.xingCheDaoWidth += hzAddWidth;
                                        AddOrEdit_widSort(ys_line.isLeft, hzStack, hzWID_HDM, ref wid_hdmSortedLeft, ref wid_hdmSortedRight);

                                        double jbssdStack = roadDetailLIst[hs_index].stackStart + jk_jb_Start;
                                        Wid_HDmSingle jbssWID_HDM = wid_hdm_;
                                        AddOrEdit_widSort(ys_line.isLeft, jbssdStack, jbssWID_HDM, ref wid_hdmSortedLeft, ref wid_hdmSortedRight);

                                     
                                        double jbeStack = roadDetailLIst[ye_index].stackStart - jk_jb_End;
                                        Wid_HDmSingle jbeWID_HDM = wid_hdm_;
                                        AddOrEdit_widSort(ye_line.isLeft, jbeStack, jbeWID_HDM, ref wid_hdmSortedLeft, ref wid_hdmSortedRight);

                                    }
                                    else if (hs + zLen >= jk_jb_Start)
                                    {
                                        double hzStack = roadDetailLIst[z_index].stackStart;
                                        Wid_HDmSingle hzWID_HDM = wid_hdm_;
                                        double hzAddWidth = GetAddWidth_Round(addWidth_Start / jk_jb_Start * (jk_jb_Start - roadDetailLIst[hs_index].length));
                                        hzWID_HDM.xingCheDaoWidth += hzAddWidth;
                                        AddOrEdit_widSort(ys_line.isLeft, hzStack, hzWID_HDM, ref wid_hdmSortedLeft, ref wid_hdmSortedRight);

                                        double jbsStack = roadDetailLIst[hs_index].stackStart + jk_jb_Start;
                                        Wid_HDmSingle jbsWID_HDM = wid_hdm_;
                                        AddOrEdit_widSort(ys_line.isLeft, jbsStack, jbsWID_HDM, ref wid_hdmSortedLeft, ref wid_hdmSortedRight);

                                        if (hs + zLen == jk_jb_Start)
                                        {
                                            //double yhStack = roadDetailLIst[ys_index].stackStart;
                                            //Wid_HDmSingle yhWID_HDM = wid_hdm_;
                                            //double yhAddWidth = GetAddWidth_Round(addWidth_End / jk_jb_End * (roadDetailLIst[hs_index].length));
                                            //yhWID_HDM.xingCheDaoWidth += yhAddWidth;
                                            //AddOrEdit_widSort(ye_line.isLeft, yhStack, yhWID_HDM, ref wid_hdmSortedLeft, ref wid_hdmSortedRight);

                                            double tmp_jk_JB = 0;

                                            if (he + ye / 2 >= jk_jb_End)
                                            {
                                                tmp_jk_JB = jk_jb_End;
                                            }
                                            else
                                            {
                                                tmp_jk_JB = hs + ys / 2;
                                            }

                                            double jbeStack = roadDetailLIst[ye_index].stackStart + tmp_jk_JB;
                                            Wid_HDmSingle jbeWID_HDM = wid_hdm_;
                                            double jbeAddWidth = GetAddWidth_Round(addWidth_End);
                                            jbeWID_HDM.xingCheDaoWidth += jbeAddWidth;

                                            AddOrEdit_widSort(ye_line.isLeft, jbeStack, jbeWID_HDM, ref wid_hdmSortedLeft, ref wid_hdmSortedRight);
                                        }
                                        else
                                        {
                                            double deltaStack = hs + zLen - jk_jb_Start;
                                            double zyStack = roadDetailLIst[ye_index].stackStart;
                                            Wid_HDmSingle zyWID_HDM = wid_hdm_;
                                            double zyAddWidth = GetAddWidth_Round(addWidth_End / jk_jb_End * (deltaStack));
                                            zyWID_HDM.xingCheDaoWidth += zyAddWidth;
                                            AddOrEdit_widSort(ye_line.isLeft, zyStack, zyWID_HDM, ref wid_hdmSortedLeft, ref wid_hdmSortedRight);

                                            double tmp_jk_JB = 0;

                                            if (he + ye / 2 + deltaStack >= jk_jb_End)
                                            {
                                                tmp_jk_JB = jk_jb_End;
                                            }
                                            else
                                            {
                                                tmp_jk_JB = deltaStack + hs + ys / 2;
                                            }


                                            double jbeStack = roadDetailLIst[ye_index].stackStart + tmp_jk_JB - (deltaStack);
                                            Wid_HDmSingle jbeWID_HDM = wid_hdm_;
                                            double jbeAddWidth = GetAddWidth_Round(addWidth_End);
                                            jbeWID_HDM.xingCheDaoWidth += jbeAddWidth;

                                            AddOrEdit_widSort(ye_line.isLeft, jbeStack, jbeWID_HDM, ref wid_hdmSortedLeft, ref wid_hdmSortedRight);
                                        }
                                    }
                                    else if (zLen + he >= jk_jb_End)
                                    {

                                        double jbsStack = roadDetailLIst[ys_index].stackStart - jk_jb_End;
                                        Wid_HDmSingle jbsWID_HDM = wid_hdm_;
                                        AddOrEdit_widSort(ye_line.isLeft, jbsStack, jbsWID_HDM, ref wid_hdmSortedLeft, ref wid_hdmSortedRight);


                                        if (zLen + he == jk_jb_End)
                                        {
                                            
                                            double yhStack = roadDetailLIst[hs_index].stackStart;
                                            Wid_HDmSingle yjWID_HDM = wid_hdm_;
                                            double yhAddWidth = GetAddWidth_Round(addWidth_Start / jk_jb_Start * (roadDetailLIst[z_index].length + roadDetailLIst[hs_index].length));
                                            yjWID_HDM.xingCheDaoWidth += yhAddWidth;
                                            AddOrEdit_widSort(ys_line.isLeft, yhStack, yjWID_HDM, ref wid_hdmSortedLeft, ref wid_hdmSortedRight);

                                            double tmp_jk_JB = 0;

                                            if (he + ys / 2 >= jk_jb_Start)
                                            {
                                                tmp_jk_JB = jk_jb_Start;
                                            }
                                            else
                                            {
                                                tmp_jk_JB = hs + ys / 2;
                                            }

                                            double jbeStack = roadDetailLIst[z_index].stackStart - tmp_jk_JB;
                                            Wid_HDmSingle jbeWID_HDM = wid_hdm_;
                                            double jbeAddWidth = GetAddWidth_Round(addWidth_Start);
                                            jbeWID_HDM.xingCheDaoWidth += jbeAddWidth;

                                            AddOrEdit_widSort(ys_line.isLeft, jbeStack, jbeWID_HDM, ref wid_hdmSortedLeft, ref wid_hdmSortedRight);
                                        }
                                        else
                                        {
                                            double deltaStack = hs + zLen - jk_jb_End;
                                            double hzStack = roadDetailLIst[z_index].stackStart;
                                            Wid_HDmSingle hzWID_HDM = wid_hdm_;
                                            double hzAddWidth = GetAddWidth_Round(addWidth_Start / jk_jb_Start * (deltaStack));
                                            hzWID_HDM.xingCheDaoWidth += hzAddWidth;
                                            AddOrEdit_widSort(ys_line.isLeft, hzStack, hzWID_HDM, ref wid_hdmSortedLeft, ref wid_hdmSortedRight);

                                            double yhStack = roadDetailLIst[hs_index].stackStart;
                                            Wid_HDmSingle yjWID_HDM = wid_hdm_;
                                            double yhAddWidth = GetAddWidth_Round(addWidth_Start / jk_jb_Start * (deltaStack + roadDetailLIst[hs_index].length));
                                            yjWID_HDM.xingCheDaoWidth += yhAddWidth;
                                            AddOrEdit_widSort(ys_line.isLeft, yhStack, yjWID_HDM, ref wid_hdmSortedLeft, ref wid_hdmSortedRight);

                                            double tmp_jk_JB = 0;

                                            if (hs + ys / 2 + startsStack >= jk_jb_Start)
                                            {
                                                tmp_jk_JB = jk_jb_Start;
                                            }
                                            else
                                            {
                                                tmp_jk_JB = hs + ys / 2 + startsStack;
                                            }

                                            double jbeStack = roadDetailLIst[z_index].stackStart + deltaStack - tmp_jk_JB;
                                            Wid_HDmSingle jbeWID_HDM = wid_hdm_;
                                            double jbeAddWidth = GetAddWidth_Round(addWidth_Start);
                                            jbeWID_HDM.xingCheDaoWidth += jbeAddWidth;

                                            AddOrEdit_widSort(ys_line.isLeft, jbeStack, jbeWID_HDM, ref wid_hdmSortedLeft, ref wid_hdmSortedRight);
                                        }
                                    }
                                    else// 剩下的部分直接将直线均分
                                    {
                                        double zzStack = roadDetailLIst[z_index].stackStart + zLen / 2;
                                        Wid_HDmSingle zzWID_HDM = wid_hdm_;
                                        AddOrEdit_widSort(true, zzStack, zzWID_HDM, ref wid_hdmSortedLeft, ref wid_hdmSortedRight);
                                        AddOrEdit_widSort(false, zzStack, zzWID_HDM, ref wid_hdmSortedLeft, ref wid_hdmSortedRight);
                                        //起始端
                                        {
                                            double deltaStack = zLen / 2;
                                            double hzStack = roadDetailLIst[z_index].stackStart;
                                            Wid_HDmSingle hzWID_HDM = wid_hdm_;
                                            double hzAddWidth = GetAddWidth_Round(addWidth_Start / jk_jb_Start * (deltaStack));
                                            hzWID_HDM.xingCheDaoWidth += hzAddWidth;
                                            AddOrEdit_widSort(ys_line.isLeft, hzStack, hzWID_HDM, ref wid_hdmSortedLeft, ref wid_hdmSortedRight);

                                            double yhStack = roadDetailLIst[hs_index].stackStart;
                                            Wid_HDmSingle yjWID_HDM = wid_hdm_;
                                            double yhAddWidth = GetAddWidth_Round(addWidth_Start / jk_jb_Start * (deltaStack + roadDetailLIst[hs_index].length));
                                            yjWID_HDM.xingCheDaoWidth += yhAddWidth;
                                            AddOrEdit_widSort(ys_line.isLeft, yhStack, yjWID_HDM, ref wid_hdmSortedLeft, ref wid_hdmSortedRight);


                                            double tmp_jk_JB = 0;
                                            if (zLen / 2 + hs + ys / 2 >= jk_jb_Start)
                                            {
                                                tmp_jk_JB = jk_jb_Start;
                                            }
                                            else
                                            {
                                                tmp_jk_JB = zLen / 2 + hs + ys / 2;
                                            }

                                            double jbeStack = roadDetailLIst[z_index].stackStart + deltaStack - tmp_jk_JB;
                                            Wid_HDmSingle jbeWID_HDM = wid_hdm_;
                                            double jbeAddWidth = GetAddWidth_Round(addWidth_Start);
                                            jbeWID_HDM.xingCheDaoWidth += jbeAddWidth;

                                            AddOrEdit_widSort(ys_line.isLeft, jbeStack, jbeWID_HDM, ref wid_hdmSortedLeft, ref wid_hdmSortedRight);

                                        }
                                        //末端
                                        {
                                            double deltaStack = zLen / 2;
                                        

                                            double zyStack = roadDetailLIst[ys_index].stackStart;
                                            Wid_HDmSingle zyWID_HDM = wid_hdm_;
                                            double zyAddWidth = GetAddWidth_Round(addWidth_End / jk_jb_End * (deltaStack));
                                            zyWID_HDM.xingCheDaoWidth += zyAddWidth;
                                            AddOrEdit_widSort(ye_line.isLeft, zyStack, zyWID_HDM, ref wid_hdmSortedLeft, ref wid_hdmSortedRight);

                                            double tmp_jk_JB = 0;

                                            if (he + ye / 2 + deltaStack >= jk_jb_End)
                                            {
                                                tmp_jk_JB = jk_jb_End;
                                            }
                                            else
                                            {
                                                tmp_jk_JB = deltaStack + hs + ys / 2;
                                            }


                                            double jbeStack = roadDetailLIst[ye_index].stackStart + tmp_jk_JB - (deltaStack);
                                            Wid_HDmSingle jbeWID_HDM = wid_hdm_;
                                            double jbeAddWidth = GetAddWidth_Round(addWidth_End);
                                            jbeWID_HDM.xingCheDaoWidth += jbeAddWidth;

                                            AddOrEdit_widSort(ye_line.isLeft, jbeStack, jbeWID_HDM, ref wid_hdmSortedLeft, ref wid_hdmSortedRight);
                                        }
                                    }
                                }

                            }
                            //YZH2Y
                            else if (zhyhz_SecIndex[1] ==-1 && zhyhz_SecIndex[2] > -1 && zhyhz_SecIndex[3]> -1)//Y
                            {
                                if (ye_line.isLeft == true)
                                {
                                    wid_hdmSortedLeft[ye_line.stackStart].xingCheDaoWidth += addWidth_End;
                                }
                                else
                                {
                                    wid_hdmSortedRight[ye_line.stackStart].xingCheDaoWidth += addWidth_End;
                                }

                                if (ys_line.isLeft == true)
                                {
                                    wid_hdmSortedLeft[roadDetailLIst[z_index].stackStart].xingCheDaoWidth += addWidth_End;
                                }
                                else
                                {
                                    wid_hdmSortedRight[roadDetailLIst[hs_index].stackStart].xingCheDaoWidth += addWidth_End;
                                }
                                ys = 0; hs = 0; zLen = 0; he = 0; ye = 0;

                                ys = roadDetailLIst[ys_index].length;
                                //hs = roadDetailLIst[hs_index].length;
                                zLen = roadDetailLIst[z_index].length;
                                he = roadDetailLIst[he_index].length;
                                ye = roadDetailLIst[ye_index].length;


                                if (hs >= jk_jb_Start && he >= jk_jb_End)//不执行
                                {

                                }
                                else if (hs < jk_jb_Start && he >= jk_jb_End)
                                {
                                    if (zLen + hs >= jk_jb_Start)
                                    {
                                        double yzStack = roadDetailLIst[z_index].stackStart;
                                        Wid_HDmSingle yzWID_HDM = wid_hdm_;
                                        double yzAddWidth = GetAddWidth_Round(addWidth_Start / jk_jb_Start * (jk_jb_Start - roadDetailLIst[hs_index].length));
                                        yzWID_HDM.xingCheDaoWidth += yzAddWidth;
                                        AddOrEdit_widSort(ys_line.isLeft, yzStack, yzWID_HDM, ref wid_hdmSortedLeft, ref wid_hdmSortedRight);

                                        double jbsStack = roadDetailLIst[z_index].stackStart + jk_jb_Start;
                                        Wid_HDmSingle jbsWID_HDM = wid_hdm_;
                                        AddOrEdit_widSort(ys_line.isLeft, jbsStack, jbsWID_HDM, ref wid_hdmSortedLeft, ref wid_hdmSortedRight);
                                    }
                                    else
                                    {
                                        double yzStack = roadDetailLIst[z_index].stackStart;
                                        Wid_HDmSingle yzWID_HDM = wid_hdm_;
                                        double yzAddWidth = GetAddWidth_Round(addWidth_Start / jk_jb_Start * (roadDetailLIst[z_index].length));
                                        yzWID_HDM.xingCheDaoWidth += yzAddWidth;
                                        AddOrEdit_widSort(ys_line.isLeft, yzStack, yzWID_HDM, ref wid_hdmSortedLeft, ref wid_hdmSortedRight);


                                        double tmp_jk_JB = 0;

                                        if (zLen + he + ys / 2 >= jk_jb_Start)
                                        {
                                            tmp_jk_JB = jk_jb_Start;
                                        }
                                        else
                                        {
                                            tmp_jk_JB = hs + zLen + ys / 2;
                                        }

                                        double jbeStack = roadDetailLIst[z_index].stackStart+zLen - tmp_jk_JB;
                                        Wid_HDmSingle jbeWID_HDM = wid_hdm_;
                                        double jbeAddWidth = GetAddWidth_Round(addWidth_Start);
                                        jbeWID_HDM.xingCheDaoWidth += jbeAddWidth;

                                        AddOrEdit_widSort(ys_line.isLeft, jbeStack, jbeWID_HDM, ref wid_hdmSortedLeft, ref wid_hdmSortedRight);

                                    }
                                }
                                else if (hs >= jk_jb_Start && he < jk_jb_End)//不执行
                                {
                                    if (zLen + he >= jk_jb_End)
                                    {
                                        double zhStack = roadDetailLIst[hs_index].stackStart;
                                        Wid_HDmSingle zhWID_HDM = wid_hdm_;
                                        double zhAddWidth = GetAddWidth_Round(addWidth_End / jk_jb_End * (addWidth_End - roadDetailLIst[hs_index].length));
                                        zhWID_HDM.xingCheDaoWidth += zhAddWidth;
                                        AddOrEdit_widSort(ye_line.isLeft, zhStack, zhWID_HDM, ref wid_hdmSortedLeft, ref wid_hdmSortedRight);

                                        double jbsStack = roadDetailLIst[ys_index].stackStart - jk_jb_End;
                                        Wid_HDmSingle jbsWID_HDM = wid_hdm_;
                                        AddOrEdit_widSort(ye_line.isLeft, jbsStack, jbsWID_HDM, ref wid_hdmSortedLeft, ref wid_hdmSortedRight);
                                    }
                                    else
                                    {
                                        double zhStack = roadDetailLIst[hs_index].stackStart;
                                        Wid_HDmSingle zhWID_HDM = wid_hdm_;
                                        double hzAddWidth = GetAddWidth_Round(addWidth_End / jk_jb_End * (roadDetailLIst[z_index].length));
                                        zhWID_HDM.xingCheDaoWidth += hzAddWidth;
                                        AddOrEdit_widSort(ye_line.isLeft, zhStack, zhWID_HDM, ref wid_hdmSortedLeft, ref wid_hdmSortedRight);

                                        double yhStack = roadDetailLIst[ys_index].stackStart;
                                        Wid_HDmSingle yjWID_HDM = wid_hdm_;
                                        double yhAddWidth = GetAddWidth_Round(addWidth_End / jk_jb_End * (roadDetailLIst[z_index].length + roadDetailLIst[hs_index].length));
                                        yjWID_HDM.xingCheDaoWidth += yhAddWidth;
                                        AddOrEdit_widSort(ye_line.isLeft, yhStack, yjWID_HDM, ref wid_hdmSortedLeft, ref wid_hdmSortedRight);

                                        double tmp_jk_JB = 0;

                                        if (zLen + he + ye / 2 >= jk_jb_End)
                                        {
                                            tmp_jk_JB = jk_jb_End;
                                        }
                                        else
                                        {
                                            tmp_jk_JB = hs + zLen + ys / 2;
                                        }


                                        double jbeStack = roadDetailLIst[z_index].stackStart + tmp_jk_JB;
                                        Wid_HDmSingle jbeWID_HDM = wid_hdm_;
                                        double jbeAddWidth = GetAddWidth_Round(addWidth_End);
                                        jbeWID_HDM.xingCheDaoWidth += jbeAddWidth;

                                        AddOrEdit_widSort(ye_line.isLeft, jbeStack, jbeWID_HDM, ref wid_hdmSortedLeft, ref wid_hdmSortedRight);
                                    }
                                }
                                else if (hs < jk_jb_Start && he < jk_jb_End)
                                {
                                 
                                    if (hs + zLen + he >= jk_jb_Start + jk_jb_End && hs + zLen >= jk_jb_Start && zLen + he >= jk_jb_End)
                                    {
                                        //double hzStack = roadDetailLIst[z_index].stackStart;
                                        //Wid_HDmSingle hzWID_HDM = wid_hdm_;
                                        //double hzAddWidth = GetAddWidth_Round(addWidth_Start / jk_jb_Start * (jk_jb_Start - roadDetailLIst[hs_index].length));
                                        //hzWID_HDM.xingCheDaoWidth += hzAddWidth;
                                        //AddOrEdit_widSort(ys_line.isLeft, hzStack, hzWID_HDM, ref wid_hdmSortedLeft, ref wid_hdmSortedRight);

                                        double jbssdStack = roadDetailLIst[hs_index].stackStart + jk_jb_Start;
                                        Wid_HDmSingle jbssWID_HDM = wid_hdm_;
                                        AddOrEdit_widSort(ys_line.isLeft, jbssdStack, jbssWID_HDM, ref wid_hdmSortedLeft, ref wid_hdmSortedRight);

                                        double zhStack = roadDetailLIst[hs_index].stackStart;
                                        Wid_HDmSingle zhWID_HDM = wid_hdm_;
                                        double zhAddWidth = GetAddWidth_Round(addWidth_End / jk_jb_End * (addWidth_End - roadDetailLIst[hs_index].length));
                                        zhWID_HDM.xingCheDaoWidth += zhAddWidth;
                                        AddOrEdit_widSort(ye_line.isLeft, zhStack, zhWID_HDM, ref wid_hdmSortedLeft, ref wid_hdmSortedRight);

                                        double jbeStack = roadDetailLIst[ys_index].stackStart - jk_jb_End;
                                        Wid_HDmSingle jbeWID_HDM = wid_hdm_;
                                        AddOrEdit_widSort(ye_line.isLeft, jbeStack, jbeWID_HDM, ref wid_hdmSortedLeft, ref wid_hdmSortedRight);

                                    }
                                    else if (hs + zLen >= jk_jb_Start)
                                    {
                                        //double hzStack = roadDetailLIst[z_index].stackStart;
                                        //Wid_HDmSingle hzWID_HDM = wid_hdm_;
                                        //double hzAddWidth = GetAddWidth_Round(addWidth_Start / jk_jb_Start * (jk_jb_Start - roadDetailLIst[hs_index].length));
                                        //hzWID_HDM.xingCheDaoWidth += hzAddWidth;
                                        //AddOrEdit_widSort(ys_line.isLeft, hzStack, hzWID_HDM, ref wid_hdmSortedLeft, ref wid_hdmSortedRight);

                                        double jbsStack = roadDetailLIst[z_index].stackStart + jk_jb_Start;
                                        Wid_HDmSingle jbsWID_HDM = wid_hdm_;
                                        AddOrEdit_widSort(ys_line.isLeft, jbsStack, jbsWID_HDM, ref wid_hdmSortedLeft, ref wid_hdmSortedRight);

                                        if (hs + zLen == jk_jb_Start)
                                        {
                                            double yhStack = roadDetailLIst[ys_index].stackStart;
                                            Wid_HDmSingle yhWID_HDM = wid_hdm_;
                                            double yhAddWidth = GetAddWidth_Round(addWidth_End / jk_jb_End * (roadDetailLIst[hs_index].length));
                                            yhWID_HDM.xingCheDaoWidth += yhAddWidth;
                                            AddOrEdit_widSort(ye_line.isLeft, yhStack, yhWID_HDM, ref wid_hdmSortedLeft, ref wid_hdmSortedRight);

                                            double tmp_jk_JB = 0;

                                            if (he + ye / 2 >= jk_jb_End)
                                            {
                                                tmp_jk_JB = jk_jb_End;
                                            }
                                            else
                                            {
                                                tmp_jk_JB = hs + ys / 2;
                                            }

                                            double jbeStack = roadDetailLIst[he_index].stackStart + tmp_jk_JB;
                                            Wid_HDmSingle jbeWID_HDM = wid_hdm_;
                                            double jbeAddWidth = GetAddWidth_Round(addWidth_End);
                                            jbeWID_HDM.xingCheDaoWidth += jbeAddWidth;

                                            AddOrEdit_widSort(ye_line.isLeft, jbeStack, jbeWID_HDM, ref wid_hdmSortedLeft, ref wid_hdmSortedRight);
                                        }
                                        else
                                        {
                                            double deltaStack = hs + zLen - jk_jb_Start;
                                            double zhStack = roadDetailLIst[hs_index].stackStart;
                                            Wid_HDmSingle zhWID_HDM = wid_hdm_;
                                            double zhAddWidth = GetAddWidth_Round(addWidth_End / jk_jb_End * (deltaStack));
                                            zhWID_HDM.xingCheDaoWidth += zhAddWidth;
                                            AddOrEdit_widSort(ye_line.isLeft, zhStack, zhWID_HDM, ref wid_hdmSortedLeft, ref wid_hdmSortedRight);

                                            double yhStack = roadDetailLIst[ys_index].stackStart;
                                            Wid_HDmSingle yhWID_HDM = wid_hdm_;
                                            double yhAddWidth = GetAddWidth_Round(addWidth_End / jk_jb_End * (deltaStack + roadDetailLIst[hs_index].length));
                                            yhWID_HDM.xingCheDaoWidth += yhAddWidth;
                                            AddOrEdit_widSort(ye_line.isLeft, yhStack, yhWID_HDM, ref wid_hdmSortedLeft, ref wid_hdmSortedRight);

                                            double tmp_jk_JB = 0;

                                            if (he + ye / 2 + deltaStack >= jk_jb_End)
                                            {
                                                tmp_jk_JB = jk_jb_End;
                                            }
                                            else
                                            {
                                                tmp_jk_JB = deltaStack + hs + ys / 2;
                                            }


                                            double jbeStack = roadDetailLIst[hs_index].stackStart + tmp_jk_JB - (deltaStack);
                                            Wid_HDmSingle jbeWID_HDM = wid_hdm_;
                                            double jbeAddWidth = GetAddWidth_Round(addWidth_End);
                                            jbeWID_HDM.xingCheDaoWidth += jbeAddWidth;

                                            AddOrEdit_widSort(ye_line.isLeft, jbeStack, jbeWID_HDM, ref wid_hdmSortedLeft, ref wid_hdmSortedRight);
                                        }
                                    }
                                    else if (zLen + he >= jk_jb_End)
                                    {
                                        double zhStack = roadDetailLIst[hs_index].stackStart;
                                        Wid_HDmSingle zhWID_HDM = wid_hdm_;
                                        double zhAddWidth = GetAddWidth_Round(addWidth_End / jk_jb_End * (addWidth_End - roadDetailLIst[hs_index].length));
                                        zhWID_HDM.xingCheDaoWidth += zhAddWidth;
                                        AddOrEdit_widSort(ye_line.isLeft, zhStack, zhWID_HDM, ref wid_hdmSortedLeft, ref wid_hdmSortedRight);

                                        double jbsStack = roadDetailLIst[ys_index].stackStart - jk_jb_End;
                                        Wid_HDmSingle jbsWID_HDM = wid_hdm_;
                                        AddOrEdit_widSort(ye_line.isLeft, jbsStack, jbsWID_HDM, ref wid_hdmSortedLeft, ref wid_hdmSortedRight);


                                        if (zLen + he == jk_jb_End)
                                        {
                                            //double hzStack = roadDetailLIst[z_index].stackStart;
                                            //Wid_HDmSingle hzWID_HDM = wid_hdm_;
                                            //double hzAddWidth = GetAddWidth_Round(addWidth_Start / jk_jb_Start * (roadDetailLIst[z_index].length));
                                            //hzWID_HDM.xingCheDaoWidth += hzAddWidth;
                                            //AddOrEdit_widSort(ys_line.isLeft, hzStack, hzWID_HDM, ref wid_hdmSortedLeft, ref wid_hdmSortedRight);

                                            //double yzStack = roadDetailLIst[z_index].stackStart;
                                            //Wid_HDmSingle yzWID_HDM = wid_hdm_;
                                            //double yzAddWidth = GetAddWidth_Round(addWidth_Start / jk_jb_Start * (roadDetailLIst[z_index].length ));
                                            //yzWID_HDM.xingCheDaoWidth += yzAddWidth;
                                            //AddOrEdit_widSort(ys_line.isLeft, yzStack, yzWID_HDM, ref wid_hdmSortedLeft, ref wid_hdmSortedRight);

                                            double tmp_jk_JB = 0;

                                            if (he + ys / 2 >= jk_jb_Start)
                                            {
                                                tmp_jk_JB = jk_jb_Start;
                                            }
                                            else
                                            {
                                                tmp_jk_JB = hs + ys / 2;
                                            }

                                            double jbeStack = roadDetailLIst[z_index].stackStart - tmp_jk_JB;
                                            Wid_HDmSingle jbeWID_HDM = wid_hdm_;
                                            double jbeAddWidth = GetAddWidth_Round(addWidth_Start);
                                            jbeWID_HDM.xingCheDaoWidth += jbeAddWidth;

                                            AddOrEdit_widSort(ys_line.isLeft, jbeStack, jbeWID_HDM, ref wid_hdmSortedLeft, ref wid_hdmSortedRight);
                                        }
                                        else
                                        {
                                            double deltaStack = hs + zLen - jk_jb_End;
                                            //double hzStack = roadDetailLIst[z_index].stackStart;
                                            //Wid_HDmSingle hzWID_HDM = wid_hdm_;
                                            //double hzAddWidth = GetAddWidth_Round(addWidth_Start / jk_jb_Start * (deltaStack));
                                            //hzWID_HDM.xingCheDaoWidth += hzAddWidth;
                                            //AddOrEdit_widSort(ys_line.isLeft, hzStack, hzWID_HDM, ref wid_hdmSortedLeft, ref wid_hdmSortedRight);

                                            double yzStack = roadDetailLIst[z_index].stackStart;
                                            Wid_HDmSingle yzWID_HDM = wid_hdm_;
                                            double yzAddWidth = GetAddWidth_Round(addWidth_Start / jk_jb_Start * (deltaStack ));
                                            yzWID_HDM.xingCheDaoWidth += yzAddWidth;
                                            AddOrEdit_widSort(ys_line.isLeft, yzStack, yzWID_HDM, ref wid_hdmSortedLeft, ref wid_hdmSortedRight);

                                            double tmp_jk_JB = 0;

                                            if (hs + ys / 2 + startsStack >= jk_jb_Start)
                                            {
                                                tmp_jk_JB = jk_jb_Start;
                                            }
                                            else
                                            {
                                                tmp_jk_JB = hs + ys / 2 + startsStack;
                                            }

                                            double jbeStack = roadDetailLIst[z_index].stackStart + deltaStack - tmp_jk_JB;
                                            Wid_HDmSingle jbeWID_HDM = wid_hdm_;
                                            double jbeAddWidth = GetAddWidth_Round(addWidth_Start);
                                            jbeWID_HDM.xingCheDaoWidth += jbeAddWidth;

                                            AddOrEdit_widSort(ys_line.isLeft, jbeStack, jbeWID_HDM, ref wid_hdmSortedLeft, ref wid_hdmSortedRight);
                                        }
                                    }
                                    else// 剩下的部分直接将直线均分
                                    {
                                        double zzStack = roadDetailLIst[z_index].stackStart + zLen / 2;
                                        Wid_HDmSingle zzWID_HDM = wid_hdm_;
                                        AddOrEdit_widSort(true, zzStack, zzWID_HDM, ref wid_hdmSortedLeft, ref wid_hdmSortedRight);
                                        AddOrEdit_widSort(false, zzStack, zzWID_HDM, ref wid_hdmSortedLeft, ref wid_hdmSortedRight);
                                        //起始端
                                        {
                                            double deltaStack = zLen / 2;

                                            double yzStack = roadDetailLIst[z_index].stackStart;
                                            Wid_HDmSingle yzWID_HDM = wid_hdm_;
                                            double yzAddWidth = GetAddWidth_Round(addWidth_Start / jk_jb_Start * (deltaStack));
                                            yzWID_HDM.xingCheDaoWidth += yzAddWidth;
                                            AddOrEdit_widSort(ys_line.isLeft, yzStack, yzWID_HDM, ref wid_hdmSortedLeft, ref wid_hdmSortedRight);


                                            double tmp_jk_JB = 0;
                                            if (zLen / 2 + hs + ys / 2 >= jk_jb_Start)
                                            {
                                                tmp_jk_JB = jk_jb_Start;
                                            }
                                            else
                                            {
                                                tmp_jk_JB = zLen / 2 + hs + ys / 2;
                                            }

                                            double jbeStack = roadDetailLIst[z_index].stackStart + deltaStack - tmp_jk_JB;
                                            Wid_HDmSingle jbeWID_HDM = wid_hdm_;
                                            double jbeAddWidth = GetAddWidth_Round(addWidth_Start);
                                            jbeWID_HDM.xingCheDaoWidth += jbeAddWidth;

                                            AddOrEdit_widSort(ys_line.isLeft, jbeStack, jbeWID_HDM, ref wid_hdmSortedLeft, ref wid_hdmSortedRight);

                                        }
                                        //末端
                                        {
                                            double deltaStack = zLen / 2;
                                            double zhStack = roadDetailLIst[hs_index].stackStart;
                                            Wid_HDmSingle zhWID_HDM = wid_hdm_;
                                            double zhAddWidth = GetAddWidth_Round(addWidth_End / jk_jb_End * (deltaStack));
                                            zhWID_HDM.xingCheDaoWidth += zhAddWidth;
                                            AddOrEdit_widSort(ye_line.isLeft, zhStack, zhWID_HDM, ref wid_hdmSortedLeft, ref wid_hdmSortedRight);

                                            double yhStack = roadDetailLIst[ys_index].stackStart;
                                            Wid_HDmSingle yhWID_HDM = wid_hdm_;
                                            double yhAddWidth = GetAddWidth_Round(addWidth_End / jk_jb_End * (deltaStack + roadDetailLIst[hs_index].length));
                                            yhWID_HDM.xingCheDaoWidth += yhAddWidth;
                                            AddOrEdit_widSort(ye_line.isLeft, yhStack, yhWID_HDM, ref wid_hdmSortedLeft, ref wid_hdmSortedRight);

                                            double tmp_jk_JB = 0;

                                            if (he + ye / 2 + deltaStack >= jk_jb_End)
                                            {
                                                tmp_jk_JB = jk_jb_End;
                                            }
                                            else
                                            {
                                                tmp_jk_JB = deltaStack + hs + ys / 2;
                                            }


                                            double jbeStack = roadDetailLIst[hs_index].stackStart + tmp_jk_JB - (deltaStack);
                                            Wid_HDmSingle jbeWID_HDM = wid_hdm_;
                                            double jbeAddWidth = GetAddWidth_Round(addWidth_End);
                                            jbeWID_HDM.xingCheDaoWidth += jbeAddWidth;

                                            AddOrEdit_widSort(ye_line.isLeft, jbeStack, jbeWID_HDM, ref wid_hdmSortedLeft, ref wid_hdmSortedRight);
                                        }
                                    }
                                }
                            }
                            //YZY
                            else if (zhyhz_SecIndex[1] == -1 && zhyhz_SecIndex[2] > -1 && zhyhz_SecIndex[3] == -1)
                            {
                                if (ye_line.isLeft == true)
                                {
                                    wid_hdmSortedLeft[ye_line.stackStart].xingCheDaoWidth += addWidth_End;
                                }
                                else
                                {
                                    wid_hdmSortedRight[ye_line.stackStart].xingCheDaoWidth += addWidth_End;
                                }

                                if (ys_line.isLeft == true)
                                {
                                    wid_hdmSortedLeft[roadDetailLIst[hs_index].stackStart].xingCheDaoWidth += addWidth_End;
                                }
                                else
                                {
                                    wid_hdmSortedRight[roadDetailLIst[hs_index].stackStart].xingCheDaoWidth += addWidth_End;
                                }
                                ys = 0; hs = 0; zLen = 0; he = 0; ye = 0;

                                ys = roadDetailLIst[ys_index].length;
                                zLen = roadDetailLIst[z_index].length;
                                ye = roadDetailLIst[ye_index].length;

                                //不执行
                                if (hs >= jk_jb_Start && he >= jk_jb_End)//不执行
                                {

                                }
                                //不执行
                                else if (hs < jk_jb_Start && he >= jk_jb_End)//不执行
                                {
                                    if (zLen + hs >= jk_jb_Start)
                                    {
                                        double hzStack = roadDetailLIst[z_index].stackStart;
                                        Wid_HDmSingle hzWID_HDM = wid_hdm_;
                                        double hzAddWidth = GetAddWidth_Round(addWidth_Start / jk_jb_Start * (jk_jb_Start - roadDetailLIst[hs_index].length));
                                        hzWID_HDM.xingCheDaoWidth += hzAddWidth;
                                        AddOrEdit_widSort(ys_line.isLeft, hzStack, hzWID_HDM, ref wid_hdmSortedLeft, ref wid_hdmSortedRight);

                                        double jbsStack = roadDetailLIst[hs_index].stackStart + jk_jb_Start;
                                        Wid_HDmSingle jbsWID_HDM = wid_hdm_;
                                        AddOrEdit_widSort(ys_line.isLeft, jbsStack, jbsWID_HDM, ref wid_hdmSortedLeft, ref wid_hdmSortedRight);
                                    }
                                    else
                                    {
                                        double hzStack = roadDetailLIst[z_index].stackStart;
                                        Wid_HDmSingle hzWID_HDM = wid_hdm_;
                                        double hzAddWidth = GetAddWidth_Round(addWidth_Start / jk_jb_Start * (roadDetailLIst[z_index].length));
                                        hzWID_HDM.xingCheDaoWidth += hzAddWidth;
                                        AddOrEdit_widSort(ys_line.isLeft, hzStack, hzWID_HDM, ref wid_hdmSortedLeft, ref wid_hdmSortedRight);

                                        double yhStack = roadDetailLIst[hs_index].stackStart;
                                        Wid_HDmSingle yjWID_HDM = wid_hdm_;
                                        double yhAddWidth = GetAddWidth_Round(addWidth_Start / jk_jb_Start * (roadDetailLIst[z_index].length + roadDetailLIst[hs_index].length));
                                        yjWID_HDM.xingCheDaoWidth += yhAddWidth;
                                        AddOrEdit_widSort(ys_line.isLeft, yhStack, yjWID_HDM, ref wid_hdmSortedLeft, ref wid_hdmSortedRight);

                                        double tmp_jk_JB = 0;

                                        if (zLen + he + ys / 2 >= jk_jb_Start)
                                        {
                                            tmp_jk_JB = jk_jb_Start;
                                        }
                                        else
                                        {
                                            tmp_jk_JB = hs + zLen + ys / 2;
                                        }

                                        double jbeStack = roadDetailLIst[z_index].stackStart - tmp_jk_JB;
                                        Wid_HDmSingle jbeWID_HDM = wid_hdm_;
                                        double jbeAddWidth = GetAddWidth_Round(addWidth_Start);
                                        jbeWID_HDM.xingCheDaoWidth += jbeAddWidth;

                                        AddOrEdit_widSort(ys_line.isLeft, jbeStack, jbeWID_HDM, ref wid_hdmSortedLeft, ref wid_hdmSortedRight);

                                    }
                                }
                                //不执行
                                else if (hs >= jk_jb_Start && he < jk_jb_End)
                                {
                                    if (zLen + he >= jk_jb_End)
                                    {
                                        double zhStack = roadDetailLIst[hs_index].stackStart;
                                        Wid_HDmSingle zhWID_HDM = wid_hdm_;
                                        double zhAddWidth = GetAddWidth_Round(addWidth_End / jk_jb_End * (addWidth_End - roadDetailLIst[hs_index].length));
                                        zhWID_HDM.xingCheDaoWidth += zhAddWidth;
                                        AddOrEdit_widSort(ye_line.isLeft, zhStack, zhWID_HDM, ref wid_hdmSortedLeft, ref wid_hdmSortedRight);

                                        double jbsStack = roadDetailLIst[ys_index].stackStart - jk_jb_End;
                                        Wid_HDmSingle jbsWID_HDM = wid_hdm_;
                                        AddOrEdit_widSort(ye_line.isLeft, jbsStack, jbsWID_HDM, ref wid_hdmSortedLeft, ref wid_hdmSortedRight);
                                    }
                                    else
                                    {
                                        double zhStack = roadDetailLIst[hs_index].stackStart;
                                        Wid_HDmSingle zhWID_HDM = wid_hdm_;
                                        double hzAddWidth = GetAddWidth_Round(addWidth_End / jk_jb_End * (roadDetailLIst[z_index].length));
                                        zhWID_HDM.xingCheDaoWidth += hzAddWidth;
                                        AddOrEdit_widSort(ye_line.isLeft, zhStack, zhWID_HDM, ref wid_hdmSortedLeft, ref wid_hdmSortedRight);

                                        double yhStack = roadDetailLIst[ys_index].stackStart;
                                        Wid_HDmSingle yjWID_HDM = wid_hdm_;
                                        double yhAddWidth = GetAddWidth_Round(addWidth_End / jk_jb_End * (roadDetailLIst[z_index].length + roadDetailLIst[hs_index].length));
                                        yjWID_HDM.xingCheDaoWidth += yhAddWidth;
                                        AddOrEdit_widSort(ye_line.isLeft, yhStack, yjWID_HDM, ref wid_hdmSortedLeft, ref wid_hdmSortedRight);

                                        double tmp_jk_JB = 0;

                                        if (zLen + he + ye / 2 >= jk_jb_End)
                                        {
                                            tmp_jk_JB = jk_jb_End;
                                        }
                                        else
                                        {
                                            tmp_jk_JB = hs + zLen + ys / 2;
                                        }


                                        double jbeStack = roadDetailLIst[z_index].stackStart + tmp_jk_JB;
                                        Wid_HDmSingle jbeWID_HDM = wid_hdm_;
                                        double jbeAddWidth = GetAddWidth_Round(addWidth_End);
                                        jbeWID_HDM.xingCheDaoWidth += jbeAddWidth;

                                        AddOrEdit_widSort(ye_line.isLeft, jbeStack, jbeWID_HDM, ref wid_hdmSortedLeft, ref wid_hdmSortedRight);
                                    }
                                }
                                else if (hs < jk_jb_Start && he < jk_jb_End)
                                {
                                    
                                    double zzStack = roadDetailLIst[z_index].stackStart + zLen / 2;
                                        Wid_HDmSingle zzWID_HDM = wid_hdm_;
                                        AddOrEdit_widSort(true, zzStack, zzWID_HDM, ref wid_hdmSortedLeft, ref wid_hdmSortedRight);
                                        AddOrEdit_widSort(false, zzStack, zzWID_HDM, ref wid_hdmSortedLeft, ref wid_hdmSortedRight);
                                        //起始端
                                        {
                                            double deltaStack = zLen / 2;
                                         

                                            double yzStack = roadDetailLIst[z_index].stackStart;
                                            Wid_HDmSingle yzWID_HDM = wid_hdm_;
                                            double yzAddWidth = GetAddWidth_Round(addWidth_Start / jk_jb_Start * (deltaStack ));
                                            yzWID_HDM.xingCheDaoWidth += yzAddWidth;
                                            AddOrEdit_widSort(ys_line.isLeft, yzStack, yzWID_HDM, ref wid_hdmSortedLeft, ref wid_hdmSortedRight);


                                            double tmp_jk_JB = 0;
                                            if (zLen / 2 + hs + ys / 2 >= jk_jb_Start)
                                            {
                                                tmp_jk_JB = jk_jb_Start;
                                            }
                                            else
                                            {
                                                tmp_jk_JB = zLen / 2 + hs + ys / 2;
                                            }

                                            double jbeStack = roadDetailLIst[z_index].stackStart + deltaStack - tmp_jk_JB;
                                            Wid_HDmSingle jbeWID_HDM = wid_hdm_;
                                            double jbeAddWidth = GetAddWidth_Round(addWidth_Start);
                                            jbeWID_HDM.xingCheDaoWidth += jbeAddWidth;

                                            AddOrEdit_widSort(ys_line.isLeft, jbeStack, jbeWID_HDM, ref wid_hdmSortedLeft, ref wid_hdmSortedRight);

                                        }
                                        //末端
                                        {
                                            double deltaStack = zLen / 2;
                                           

                                            double zyStack = roadDetailLIst[ys_index].stackStart;
                                            Wid_HDmSingle zyWID_HDM = wid_hdm_;
                                            double zyAddWidth = GetAddWidth_Round(addWidth_End / jk_jb_End * (deltaStack ));
                                            zyWID_HDM.xingCheDaoWidth += zyAddWidth;
                                            AddOrEdit_widSort(ye_line.isLeft, zyStack, zyWID_HDM, ref wid_hdmSortedLeft, ref wid_hdmSortedRight);

                                            double tmp_jk_JB = 0;

                                            if (he + ye / 2 + deltaStack >= jk_jb_End)
                                            {
                                                tmp_jk_JB = jk_jb_End;
                                            }
                                            else
                                            {
                                                tmp_jk_JB = deltaStack + hs + ys / 2;
                                            }


                                            double jbeStack =roadDetailLIst[hs_index].stackStart+ tmp_jk_JB - (deltaStack);
                                            Wid_HDmSingle jbeWID_HDM = wid_hdm_;
                                            double jbeAddWidth = GetAddWidth_Round(addWidth_End);
                                            jbeWID_HDM.xingCheDaoWidth += jbeAddWidth;

                                            AddOrEdit_widSort(ye_line.isLeft, jbeStack, jbeWID_HDM, ref wid_hdmSortedLeft, ref wid_hdmSortedRight);
                                        }
                                    //}
                                }
                            }
                            //YY
                            else if (zhyhz_SecIndex[1] == -1 && zhyhz_SecIndex[2] == -1 && zhyhz_SecIndex[3] == -1)
                            {
                                if (ye_line.isLeft == true)
                                {
                                    wid_hdmSortedLeft[ye_line.stackStart].xingCheDaoWidth += addWidth_End;
                                }
                                else
                                {
                                    wid_hdmSortedRight[ye_line.stackStart].xingCheDaoWidth += addWidth_End;
                                }

                                if (ys_line.isLeft == true)
                                {
                                    wid_hdmSortedLeft[roadDetailLIst[hs_index].stackStart].xingCheDaoWidth += addWidth_End;
                                }
                                else
                                {
                                    wid_hdmSortedRight[roadDetailLIst[hs_index].stackStart].xingCheDaoWidth += addWidth_End;
                                }
                                ys = 0; hs = 0; zLen = 0; he = 0; ye = 0;

                                ys = roadDetailLIst[ys_index].length;
                             
                                ye = roadDetailLIst[ye_index].length;

                             
                                //起始端
                                {
                                    double deltaStack = 0;
                                    

                                    double tmp_jk_JB = 0;
                                    if (zLen / 2 + hs + ys / 2 >= jk_jb_Start)
                                    {
                                        tmp_jk_JB = jk_jb_Start;
                                    }
                                    else
                                    {
                                        tmp_jk_JB = zLen / 2 + hs + ys / 2;
                                    }

                                    double jbeStack = roadDetailLIst[ye_index].stackStart + deltaStack - tmp_jk_JB;
                                    Wid_HDmSingle jbeWID_HDM = wid_hdm_;
                                    double jbeAddWidth = GetAddWidth_Round(addWidth_Start);
                                    jbeWID_HDM.xingCheDaoWidth += jbeAddWidth;

                                    AddOrEdit_widSort(ys_line.isLeft, jbeStack, jbeWID_HDM, ref wid_hdmSortedLeft, ref wid_hdmSortedRight);

                                }
                                //末端
                                {
                                    double deltaStack = 0;
                                   
                                    double tmp_jk_JB = 0;

                                    if (he + ye / 2 + deltaStack >= jk_jb_End)
                                    {
                                        tmp_jk_JB = jk_jb_End;
                                    }
                                    else
                                    {
                                        tmp_jk_JB = deltaStack + hs + ys / 2;
                                    }


                                    double jbeStack =roadDetailLIst[ye_index].stackStart+ tmp_jk_JB - (deltaStack);
                                    Wid_HDmSingle jbeWID_HDM = wid_hdm_;
                                    double jbeAddWidth = GetAddWidth_Round(addWidth_End);
                                    jbeWID_HDM.xingCheDaoWidth += jbeAddWidth;

                                    AddOrEdit_widSort(ye_line.isLeft, jbeStack, jbeWID_HDM, ref wid_hdmSortedLeft, ref wid_hdmSortedRight);
                                }
                            }
                        }
                    } 
                }
                //尾部
                {

                    double ys = 0, hs = 0, zLen = 0, he = 0, ye = 0;

                    int[] zhyhz_SecIndex = { -1, -1, -1, -1, -1 };
                    zhyhz_SecIndex[0] = yIndex_list.Last();
                    bool isL_H = true;
                    for (int tmp_f = yIndex_list.Last() +1; tmp_f < roadDetailLIst.Count(); tmp_f++)
                    {
                        LineTypeDetail tmpLine = roadDetailLIst[tmp_f];
                        if (tmpLine.type.Contains("2") && isL_H == true)
                        {
                            zhyhz_SecIndex[1] = tmp_f;
                            isL_H = false;
                        }
                        else if (tmpLine.type.Contains("2") && isL_H == false)
                        {
                            zhyhz_SecIndex[3] = tmp_f;
                            isL_H = true;
                        }
                        else if (tmpLine.type.Contains("1"))
                        {
                            zhyhz_SecIndex[2] = tmp_f;
                        }
                    }

                    int ys_index = zhyhz_SecIndex[0];
                    int hs_index = zhyhz_SecIndex[1];
                    int z_index = zhyhz_SecIndex[2];
                    int he_index = zhyhz_SecIndex[3];
                    int ye_index = zhyhz_SecIndex[4];

                    LineTypeDetail ys_line = roadDetailLIst[ys_index];

                    if (GetIsJK(ys_line.ro) == true)
                    {
                        double addWidth = GetAddWidth(ys_line.ro, addWidthType);
                        double jk_jb = GetJBLen_Round(Math.Max(addWidth * 15, 10));

                        // HY点处的加宽默认全加宽，当渐变段深入了圆曲线内时，此处的加宽值会修正
                        if (ys_line.isLeft == true)
                        {
                            wid_hdmSortedLeft[roadDetailLIst[hs_index].stackStart].xingCheDaoWidth += addWidth;
                        }
                        else
                        {
                            wid_hdmSortedRight[roadDetailLIst[hs_index].stackStart].xingCheDaoWidth += addWidth;
                        }

                        if (zhyhz_SecIndex[1] > -1 && zhyhz_SecIndex[2] > -1) //ZHY
                        {
                            ys = 0; hs = 0; zLen = 0; he = 0; ye = 0;
                            zLen = roadDetailLIst[z_index].length;
                            hs = roadDetailLIst[hs_index].length;
                            ys = roadDetailLIst[ys_index].length;

                            if (hs >= jk_jb) { }
                            else//(he<jk_jb)
                            {
                                if (hs + zLen >= jk_jb)
                                {
                                    double hzStack = roadDetailLIst[z_index].stackStart;
                                    Wid_HDmSingle hzWID_HDM = wid_hdm_;
                                    double hzAddWidth = GetAddWidth_Round(addWidth / jk_jb * (jk_jb - hs));
                                    hzWID_HDM.xingCheDaoWidth += hzAddWidth;

                                    double jbsStack = ys_line.stackStart+ jk_jb;
                                    Wid_HDmSingle jbsWID_HDM = wid_hdm_;

                                    AddOrEdit_widSort(ys_line.isLeft, hzStack, hzWID_HDM, ref wid_hdmSortedLeft, ref wid_hdmSortedRight);
                                    AddOrEdit_widSort(ys_line.isLeft, jbsStack, jbsWID_HDM, ref wid_hdmSortedLeft, ref wid_hdmSortedRight);
                                }
                                else
                                {
                                    double hzStack = roadDetailLIst[z_index].stackStart;
                                    Wid_HDmSingle hzWID_HDM = wid_hdm_;
                                    double hzAddWidth = GetAddWidth_Round(addWidth / jk_jb * (zLen));
                                    hzWID_HDM.xingCheDaoWidth += hzAddWidth;

                                    double yhStack = roadDetailLIst[hs_index].stackStart;
                                    Wid_HDmSingle yjWID_HDM = wid_hdm_;
                                    double yhAddWidth = GetAddWidth_Round(addWidth / jk_jb * (zLen + hs));
                                    yjWID_HDM.xingCheDaoWidth += yhAddWidth;

                                    double tmp_jk_JB = 0;
                                    if (hs + zLen + ye / 2 >= jk_jb) {
                                        tmp_jk_JB = jk_jb;
                                    }
                                    else
                                    {
                                        tmp_jk_JB = hs + zLen + ye / 2;
                                    }

                                    double jbeStack = roadDetailLIst[z_index].stackStart +zLen- tmp_jk_JB;
                                    Wid_HDmSingle jbeWID_HDM = wid_hdm_;
                                    double jbeAddWidth = GetAddWidth_Round(addWidth);
                                    jbeWID_HDM.xingCheDaoWidth += jbeAddWidth;

                                    AddOrEdit_widSort(ys_line.isLeft, hzStack, hzWID_HDM, ref wid_hdmSortedLeft, ref wid_hdmSortedRight);
                                    AddOrEdit_widSort(ys_line.isLeft, yhStack, yjWID_HDM, ref wid_hdmSortedLeft, ref wid_hdmSortedRight);
                                    AddOrEdit_widSort(ys_line.isLeft, jbeStack, jbeWID_HDM, ref wid_hdmSortedLeft, ref wid_hdmSortedRight);
                                }
                            }
                        }
                        else if (zhyhz_SecIndex[1] > -1 && zhyhz_SecIndex[2] == -1) //YH
                        {
                            ys = 0; hs = 0; zLen = 0; he = 0; ye = 0;

                            hs = roadDetailLIst[hs_index].length;
                            ys = roadDetailLIst[ys_index].length;

                            if (hs >= jk_jb) { }
                            else//(he<jk_jb)
                            {
                                double yhStack = roadDetailLIst[hs_index].stackStart;
                                Wid_HDmSingle yjWID_HDM = wid_hdm_;
                                double yhAddWidth = GetAddWidth_Round(addWidth / jk_jb * (zLen + hs));
                                yjWID_HDM.xingCheDaoWidth += yhAddWidth;

                                
                                if (hs + zLen + ys / 2 >= jk_jb) { 
                                }
                                else
                                {
                                    jk_jb = hs + zLen + ys / 2;
                                }

                                double jbeStack = roadDetailLIst[he_index].stackStart + jk_jb;
                                Wid_HDmSingle jbeWID_HDM = wid_hdm_;
                                double jbeAddWidth = GetAddWidth_Round(addWidth);
                                jbeWID_HDM.xingCheDaoWidth += jbeAddWidth;

                                AddOrEdit_widSort(ys_line.isLeft, yhStack, yjWID_HDM, ref wid_hdmSortedLeft, ref wid_hdmSortedRight);
                                AddOrEdit_widSort(ys_line.isLeft, jbeStack, jbeWID_HDM, ref wid_hdmSortedLeft, ref wid_hdmSortedRight);

                            }
                        }
                        else if (zhyhz_SecIndex[3] == -1 && zhyhz_SecIndex[2] > -1) //ZY
                        {
                            ys = 0; hs = 0; zLen = 0; hs = 0; ye = 0;
                            zLen = roadDetailLIst[z_index].length;

                            ys = roadDetailLIst[ys_index].length;



                            if (zLen >= jk_jb)
                            {
                            }
                            else
                            {

                                double yzStack = roadDetailLIst[z_index].stackStart;
                                Wid_HDmSingle yzWID_HDM = wid_hdm_;
                                double yzAddWidth = GetAddWidth_Round(addWidth / jk_jb * (zLen));
                                yzWID_HDM.xingCheDaoWidth += yzAddWidth;

                                if (zLen + ys / 2 >= jk_jb) { }
                                else
                                {
                                    jk_jb = zLen + ys / 2;
                                }
                                double jbeStack = roadDetailLIst[z_index].stackStart +zLen- jk_jb;
                                Wid_HDmSingle jbeWID_HDM = wid_hdm_;
                                double jbeAddWidth = GetAddWidth_Round(addWidth);
                                jbeWID_HDM.xingCheDaoWidth += jbeAddWidth;


                                AddOrEdit_widSort(ys_line.isLeft, yzStack, yzWID_HDM, ref wid_hdmSortedLeft, ref wid_hdmSortedRight);
                                AddOrEdit_widSort(ys_line.isLeft, jbeStack, jbeWID_HDM, ref wid_hdmSortedLeft, ref wid_hdmSortedRight);
                            }

                        }
                        else if (zhyhz_SecIndex[3] == -1 && zhyhz_SecIndex[2] == -1)//Y
                        {

                            if (ys/ 2 >= jk_jb) { }
                            else
                            {
                                jk_jb = ys / 2;
                            }
                            double jbeStack = roadDetailLIst[ye_index].stackStart + jk_jb;
                            Wid_HDmSingle jbeWID_HDM = wid_hdm_;
                            double jbeAddWidth = GetAddWidth_Round(addWidth );
                            jbeWID_HDM.xingCheDaoWidth += jbeAddWidth;

                            AddOrEdit_widSort(ys_line.isLeft, jbeStack, jbeWID_HDM, ref wid_hdmSortedLeft, ref wid_hdmSortedRight);
                        }
                    }
                }
            }
            else if(isJK==false&&isCG==true)  //只超高
            {

            }
            else if(isJK==true&&isCG==true) //同时加宽超高
            {

            }

           
           


            return hp_hdmSortedList;
        }

        private static void AddOrEdit_widSort(bool isleft,double stack, Wid_HDmSingle wID_HDM,ref SortedList<double, Wid_HDmSingle> wid_hdmSortedLeft ,ref SortedList<double, Wid_HDmSingle> wid_hdmSortedRight)
        {
            

            if (isleft == true)
            {
                if (wid_hdmSortedLeft.ContainsKey(stack) == false)
                {
                    wid_hdmSortedLeft.Add(stack, wID_HDM);

                }
                else
                {
                    wid_hdmSortedLeft[stack] = wID_HDM;
                }
            }
            else
            {
                if (wid_hdmSortedRight.ContainsKey(stack) == false)
                {
                    wid_hdmSortedRight.Add(stack, wID_HDM);

                }
                else
                {
                    wid_hdmSortedRight[stack] = wID_HDM;
                }

            }
        }

        #region

        //public List<Wid_HDm> GetWid_HDMInfoList(double jianGe, bool isYaoSu)
        //{
        //    List<Wid_HDm> tmpWidHDMInfoList = new List<Wid_HDm>();

        //    List<int> yIndex_list = new List<int>();
        //    for (int i = 0; i < roadDetailLIst.Count; i++)
        //    {
        //        if (roadDetailList[i].type == "3")
        //        {
        //            yIndex_list.Add(i);
        //        }
        //    }

        //    //行车道宽度
        //    Wid_HDMDetail hdmDetail = new Wid_HDMDetail(0, 3.5, 2, 0.5, 0.2, 0.25, 3);
        //    double maxh = 8 * 0.01;
        //    double typeAddW = 1.5;
        //    string tmp_baseRotate = "内边线";
        //    int design_V = 60;

        //    double hXCD = hdmDetail.xingCheDaoHP * 0.01;
        //    double hYLJ = hdmDetail.yingLuJianHP * 0.01;
        //    double hTLJ = hdmDetail.tuLuJianHP * 0.01;

        //    double wXCD = hdmDetail.xingCheDaoWidth * 0.01;
        //    double wYLJ = hdmDetail.yingLuJianWidth * 0.01;
        //    double wTLJ = hdmDetail.tuLuJianWidth * 0.01;

        //    //超高渐变段计算
        //    double lc = 0;
        //    if (hXCD != maxh)//设置超高时
        //    {
        //        if (tmp_baseRotate.Contains("内边线"))
        //        {
        //            lc = 2 * wXCD * (maxh + hXCD) * GetCG_JBL(60, "边");

        //        }
        //        else if (tmp_baseRotate.Contains("中线"))
        //        {
        //            lc = wXCD * (maxh + hXCD) * GetCG_JBL(60, "中");

        //        }

        //    }

        //    //判断起始到第一个圆
        //    int firstIndex = yIndex_list[0];
        //    LineTypeDetail firstY = roadDetailLIst[firstIndex];
        //    double yLen = firstY.length;
        //    double hLen = 0;
        //    double zLen = 0;

        //    if (firstIndex >= 2)
        //    {
        //        LineTypeDetail tmpLine = roadDetailLIst[firstIndex - 1];
        //        LineTypeDetail tmpLine_1 = roadDetailLIst[firstIndex - 2];
        //        if (tmpLine.type.Contains("2"))
        //        {
        //            hLen = tmpLine.length;
        //            if (tmpLine_1.type == "1")
        //            {
        //                zLen = tmpLine_1.length;
        //            }
        //        }
        //        else if (tmpLine.type == "1")
        //        {
        //            zLen = tmpLine.length;
        //        }
        //    }
        //    else if (firstIndex == 1)
        //    {
        //        LineTypeDetail tmpLine = roadDetailLIst[firstIndex - 1];

        //        if (tmpLine.type.Contains("2"))
        //        {
        //            hLen = tmpLine.length;

        //        }
        //        else if (tmpLine.type == "1")
        //        {
        //            zLen = tmpLine.length;
        //        }
        //    }


        //    //超高渐变段计算
        //    double lc_330 = 0;
        //    if (tmp_baseRotate.Contains("内边线"))
        //    {
        //        lc_330 = 2 * wXCD * (maxh + hXCD) * 330;

        //    }
        //    else if (tmp_baseRotate.Contains("中线"))
        //    {
        //        lc_330 = wXCD * (maxh + hXCD) * 330;

        //    }
        //    lc = Math.Min(lc_330, lc);

        //    //加宽渐变段计算
        //    double lc_jk = GetAddWidth(firstY.ro, typeAddW);
        //    if (lc_jk != 0.0)//加宽处理
        //    {
        //        if (hLen == 0.0 && (hXCD == maxh))
        //        {
        //            lc = Math.Max(lc_jk * 15, 10);
        //        }
        //    }




        //    if (hLen >= lc)
        //    {

        //    }
        //    else if (hLen + zLen >= lc)
        //    {

        //    }
        //    else if (hLen + zLen + yLen / 2 >= lc)
        //    {

        //    }
        //    else if (hLen + zLen + yLen / 2 < lc)
        //    {

        //    }


        //    //判断第一个圆到最后一个圆




        //    //判断最后一个圆到结束

        //    if (isYaoSu == true)
        //    {

        //    }
        //    return tmpWidHDMInfoList;
        //}
        #endregion
        public static bool GetIsCG(int design_v,double radis,double xcdHP)
        {
            bool isCG = true;
            Dictionary<int, int> isCG_radis_2= new Dictionary<int, int>() { { 120, 5500 }, { 100, 4000 }, { 80, 2500 }, { 60, 1500 }, { 40, 600 }, { 30, 350 }, { 20, 150 }, { 15, 90 } };
           
            Dictionary<int, int> isCG_radis_3_ = new Dictionary<int, int>() { { 120, 7500 }, { 100, 5250 }, { 80, 3350 }, { 60, 1900 }, { 40, 800 }, { 30, 450 }, { 20, 200 }, { 15, 120 } };

            //if (Math.Abs(xcdHP - 2.0) < Math.Pow(1, -15))
            if(xcdHP==2.0)
            {
                double minradis = isCG_radis_2[design_v];
                if (radis > minradis) isCG = false;
            }else if(xcdHP>2.0)
            {
                double minradis = isCG_radis_3_[design_v];
                if (radis > minradis) isCG = false;
            }

            return isCG;
        }

        public static double GetJBLen_Round(double jbLen)
        {
            return Math.Round(jbLen, 1);
        }
        public static double GetAddWidth_Round(double jbLen)
        {
            return Math.Round(jbLen, 1);
        }
        public static bool GetIsJK(double radis)
        {
            if (radis > 250) return false;

            return true;
        }
        private static double GetCG_JBL(int design_v,string location)
        {
            //double cgjbl = 100;

            Dictionary<int, int> jbl_list_bx = new Dictionary<int, int>() { { 120,250},{ 100,225},{80,200},{ 60,175},{ 40,150},{ 30,125},{ 20,100} };
            Dictionary<int, int> jbl_list_zx = new Dictionary<int, int>() { { 120, 200 }, { 100, 175 }, { 80, 150 }, { 60, 125 }, { 40, 100 }, { 30, 75 }, { 20, 50 } };
            if (location.Contains("中"))
            {
                return jbl_list_bx[design_v];
            }else if(location.Contains("边"))
            {
                return jbl_list_zx[design_v];
            }
            else
            {
                return 50;
            }
             
        }
        private static double GetAddWidth(double radius, string typeAddWidth)
        {
            double addWidth = 0;
            Dictionary<double, double> type_1 = new Dictionary<double, double>() {
            { 15,2.2},{ 20,1.8}, { 25,1.5},{ 30,1.3},{ 50,0.9},{70,0.7 },{ 100,0.6},{ 150,0.5},{ 200,0.4} };
            Dictionary<double, double> type_2 = new Dictionary<double, double>() {
                                           { 30,2.0},{50,1.5}, { 70,1.2},{ 100,0.9},{150,0.7},{200 ,0.6} };
            Dictionary<double, double> type_3 = new Dictionary<double, double>() {
                                                     { 50,2.7},{70,2.0 },{ 100,1.5},{ 150,1.0},{ 200,0.8} };
            Dictionary<double, double> type_41 = new Dictionary<double, double>() {
               { 15,3.2},{ 20,2.6}, { 25,2.0},{ 30,1.8},{ 50,1.2},{70,0.9 },{ 100,0.7},{ 150,0.5},{ 200,0.4} };
            Dictionary<double, double> type_42 = new Dictionary<double, double>() {
      {10,2.3},{ 15,1.6},{ 20,1.3}, { 25,1.0},{ 30,0.9},{ 50,0.6},{70,0.45 },{ 100,0.35},{ 150,0.25},{ 200,0.2} };
            switch (typeAddWidth)
            {
                case "一类":
                    addWidth = _GetAddWidthFuZhu(radius, type_1);
                    break;
                case "一类半":
                    addWidth = _GetAddWidthFuZhu(radius, type_1) * 0.5;
                    break;
                case "二类":
                    addWidth = _GetAddWidthFuZhu(radius, type_2);
                    break;
                case "二类半":
                    addWidth = _GetAddWidthFuZhu(radius, type_2) * 0.5;
                    break;
                case "三类":
                    addWidth = _GetAddWidthFuZhu(radius, type_3);
                    break;
                case "三类半":
                    addWidth = _GetAddWidthFuZhu(radius, type_3) * 0.5;
                    break;
                case "四一类":
                    addWidth = _GetAddWidthFuZhu(radius, type_41);
                    break;
                case "四二类":
                    addWidth = _GetAddWidthFuZhu(radius, type_42);
                    break;
                default:
                    addWidth = 0;
                    break;
            }
            return addWidth;
        }
        private static double GetAddWidth(double radius, double typeAddWidth)
        {
            double addWidth = 0;
            Dictionary<double, double> type_1 = new Dictionary<double, double>() {
            { 15,2.2},{ 20,1.8}, { 25,1.5},{ 30,1.3},{ 50,0.9},{70,0.7 },{ 100,0.6},{ 150,0.5},{ 200,0.4} };
            Dictionary<double, double> type_2 = new Dictionary<double, double>() {
                                           { 30,2.0},{50,1.5}, { 70,1.2},{ 100,0.9},{150,0.7},{200 ,0.6} };
            Dictionary<double, double> type_3 = new Dictionary<double, double>() {
                                                     { 50,2.7},{70,2.0 },{ 100,1.5},{ 150,1.0},{ 200,0.8} };
            Dictionary<double, double> type_41 = new Dictionary<double, double>() {
               { 15,3.2},{ 20,2.6}, { 25,2.0},{ 30,1.8},{ 50,1.2},{70,0.9 },{ 100,0.7},{ 150,0.5},{ 200,0.4} };
            Dictionary<double, double> type_42 = new Dictionary<double, double>() {
      {10,2.3},{ 15,1.6},{ 20,1.3}, { 25,1.0},{ 30,0.9},{ 50,0.6},{70,0.45 },{ 100,0.35},{ 150,0.25},{ 200,0.2} };
            switch (typeAddWidth)
            {
                case 1.0:
                    addWidth = _GetAddWidthFuZhu(radius, type_1);
                    break;
                case 1.5:
                    addWidth = _GetAddWidthFuZhu(radius, type_1) * 0.5;
                    break;
                case 2.0:
                    addWidth = _GetAddWidthFuZhu(radius, type_2);
                    break;
                case 2.5:
                    addWidth = _GetAddWidthFuZhu(radius, type_2) * 0.5;
                    break;
                case 3.0:
                    addWidth = _GetAddWidthFuZhu(radius, type_3);
                    break;
                case 3.5:
                    addWidth = _GetAddWidthFuZhu(radius, type_3) * 0.5;
                    break;
                case 4.1:
                    addWidth = _GetAddWidthFuZhu(radius, type_41);
                    break;
                case 4.2:
                    addWidth = _GetAddWidthFuZhu(radius, type_42);
                    break;
                default:
                    addWidth = -1.0;
                    break;
            }
            return addWidth;
        }
        private static double _GetAddWidthFuZhu(double radius, Dictionary<double, double> type_1)
        {
            double addWidth = 0;
            if (radius < type_1.Keys.First())
            {
                addWidth = type_1.Values.First();
                return addWidth;
            }
            if (radius >= 250)
            {
                addWidth = type_1.Values.Last();
                return addWidth;
            }
            //type_1.Reverse();
            //type_1.Reverse;
            foreach (var tmp in type_1.Reverse())
            {
                if (radius >= tmp.Key)
                {
                    addWidth = tmp.Value;
                    break;
                }
            }

            return addWidth;
        }
    }
    public class Matrix2D
    {
        private double[,] matrix = new double[3,3];
        private double _angel;
       public Matrix2D() { }
        public Matrix2D(double angel) {
            double cosA = Math.Cos(angel);
            double sinA = Math.Sin(angel);
            matrix = new double[3, 3] {
                            {cosA,sinA,0 },
                            {-sinA,cosA,0 },
                            {0,0,1 } };
            _angel = angel;
        }
        public Matrix2D(Vector2D childBasePoint,Vector2D childXrInGloble)
        {
            double angel = childXrInGloble.GetAngle();
            double cosA = Math.Cos(angel);
            double sinA = Math.Sin(angel);
            matrix = new double[3, 3] {
                            {cosA,sinA,0 },
                            {-sinA,cosA,0 },
                            {childBasePoint.X,childBasePoint.Y,1 } };
            _angel = angel;

        }
        public Matrix2D(Vector2D childBasePoint, double angleToXGloble)
        {
            double angel = angleToXGloble;
            double cosA = Math.Cos(angel);
            double sinA = Math.Sin(angel);
            matrix = new double[3, 3] {
                            {cosA,sinA,0 },
                            {-sinA,cosA,0 },
                            {childBasePoint.X,childBasePoint.Y,1 } };
            _angel = angel;

        }
        private Matrix2D(double [,] arr)
        {

            matrix = arr;
        }
        public double[,] GetArray()
        {
            return matrix;
        }
        public Matrix2D ParentToChild()
        {
            Matrix2D move = new Matrix2D(new double[3, 3] { { 1, 0, 0 }, {0,1, 0 }, { -matrix[2, 0], -matrix[2, 1], 1 } });
            Matrix2D rotate = new Matrix2D(new double[3, 3] { { matrix[0, 0], -matrix[0, 1], 0 }, { -matrix[1, 0], matrix[1, 1], 0 }, { 0, 0, 1 } });
            Matrix2D rend = move * rotate;
            return move*rotate;
        }
        
        public static Matrix2D operator*(Matrix2D a,Matrix2D b)
        {
            double[,] tmp = new double[3, 3];
            double[,] aArr = a.GetArray();
            double[,] bArr =b.GetArray();
            for (int i=0;i<3;i++)
            {
                for(int j=0;j<3;j++)
                {
                    for(int k=0;k<3;k++)
                    {
                        tmp[i, j] += aArr[i, k] * bArr[k, j];
                    }
                }
            }
            return new Matrix2D(tmp);
        }
    }
    public class Vector2D
    {
        public double X { set; get; }
        public double Y { set; get; }
        //public double Z { set; get; }
        public Vector2D() { }
        public Vector2D(double _X,double _Y)
        {
            X = _X;
            Y = _Y;
           
        }
        public Vector2D(double angle)
        {
            X = Math.Cos(angle);
            Y = Math.Sin(angle);
        }
        public double GetAngle()
        {
            double angel = Math.Atan2(Y,X);
            return angel;
        }
        public  Vector2D Normal()
        {
            double lenth = Math.Sqrt(X * X + Y * Y);
            if (lenth == 0) return this;
            return  this*(1.0/lenth);
        }
        public static Vector2D operator+(Vector2D a,Vector2D b)
        {
            return new Vector2D(a.X+b.X,a.Y+b.Y);
        }
        public static Vector2D operator -(Vector2D a, Vector2D b)
        {
            return new Vector2D(a.X - b.X, a.Y - b.Y);
        }
        public static Vector2D operator *(Vector2D a, double b)
        {
            return new Vector2D(a.X *b, a.Y *b);
        }
        public static Vector2D operator *( double b,Vector2D a)
        {
            return new Vector2D(a.X * b, a.Y * b);
        }

        public static Vector2D operator *(Vector2D a,Matrix2D b )
        {
            double[] arr = new double[3] { a.X,a.Y,1};
            double[,] bArr = b.GetArray();
            double[] tmp = new double[3];
            for(int i=0;i<3;i++)
            {
                for(int j=0;j<3;j++)
                {
                    tmp[i] += arr[j] * bArr[j, i];
                }
                
            }
            return new Vector2D(tmp[0],tmp[1]);
        }

        public double Dot(Vector2D a)
        {
            return X * a.X + Y * a.Y;

        }
        //public Vector2D Cross(Vector2D a)
        //{
        //    return new Vector2D();

        //}
    }
    public class LineType
    {
        public bool isLeft { set; get; }
        public string type{ set; get; }

        public double length { set; get; }
        public double ro { set; get; }
        public double rd { set; get; }

        public LineType() { }
        public LineType(bool _isRight, string _type,double _length,double _ro,double _rd)
        {
            isLeft = _isRight;
            type = _type;
            length = _length;
            ro = _ro;
            rd = _rd;
        }

        public double GetDeltaA()
        {
            double deltaA = 0;
            switch (type) {
                case "1":
                    break;
                case "3":
                    deltaA = length / ro;
                    break;
                case "21":
                    deltaA = 0.5 * length / rd;
                    break;
                case "22":
                    deltaA = 0.5 * length / ro;
                    break;
                case "23":
                    double lo = rd * length / (ro-rd);
                    deltaA = (length + lo) * 0.5 / rd - lo * 0.5 / ro;
                    break;
                case "24":
                    double lo_1 = ro * length / (rd - ro);
                    deltaA = (length + lo_1) * 0.5 / ro - lo_1 * 0.5 / rd;
                    break;
            }
            if (isLeft == true)
            {
                deltaA = -1 * deltaA;
            }
            return deltaA;
        }
    }
    public class LineTypeDetail:LineType
    {
        public double startDir { set; get; }//起始方向为以测量正北为基准  //通过road中的信息计算，实际就是冗余参数
        public Vector2D startXY { set; get; }//xy以cad的xy为基准         //通过road中的信息计算，实际就是冗余参数
        public double stackStart { set; get; }                          //通过road中的信息计算，实际就是冗余参数
        public LineTypeDetail() { }

        public double GetStartDirXY()
        {
            double rad = startDir;
            double tmp = Math.PI / 2 - rad;
            if (tmp < 0) tmp = Math.PI * 2 + tmp;
            return tmp;
        }
        public LineTypeDetail(LineType _linetype)
        {
            isLeft = _linetype.isLeft;
            type = _linetype.type;
            length = _linetype.length;
            ro = _linetype.ro;
            rd = _linetype.rd;
        }
       
        public Vector2D GetXY(double _stack)
        {
            //Vector2D tmpXY = new Vector2D();

            if (_stack > stackStart + length) _stack = stackStart;
            if (_stack< stackStart ) _stack = stackStart;

            Vector2D pGlobal = new Vector2D();
            switch (type)
            {
                case "1":
                    {
                        double deltaLen = _stack - stackStart;
                        pGlobal.X = deltaLen * Math.Cos(GetStartDirXY());
                        pGlobal.Y = deltaLen * Math.Sin(GetStartDirXY());
                        pGlobal += startXY;
                    }
                    break;
                case "3":
                    {
                        double deltaLen = _stack - stackStart;
                        Vector2D pLocal = new Vector2D();
                        double angel = deltaLen / ro;
                        pLocal.X =ro * Math.Sin(angel);
                        pLocal.Y =ro * (1 - Math.Cos(angel));
                        if (isLeft == false)
                        {
                            pLocal.Y *= -1;
                        }
                        Matrix2D childToParent = new Matrix2D(startXY,GetStartDirXY());
                        pGlobal = pLocal * childToParent;
                    }
                    break;
                case "21":
                    {
                        Vector2D pLocal = new Vector2D();
                        double deltaLen = _stack - stackStart;
                        double _A = rd * length;
                        double angel = 0.5 * deltaLen * deltaLen / (_A * _A);

                        pLocal.X = deltaLen * (1 - angel * angel / 10 + Math.Pow(angel, 4) / 216);
                        pLocal.Y = deltaLen * angel / 3.0 * (1 - angel * angel / 14 + Math.Pow(angel, 4) / 440);
                        if (isLeft == false)
                        {
                            pLocal.Y *= -1;
                        }
                        Matrix2D childToParent = new Matrix2D(startXY, GetStartDirXY());
                        pGlobal = pLocal * childToParent;
                    }
                    break;
                case "22":
                    {
                        Vector2D pLocal = new Vector2D();
                        double angel = 0.5 * length / ro;
                        pLocal.X =length * (1 - angel * angel / 10 + Math.Pow(angel, 4) / 216);
                        pLocal.Y = length * angel / 3.0 * (1 - angel * angel / 14 + Math.Pow(angel, 4) / 440);

                        Matrix2D parentToChild = new Matrix2D(pLocal, angel);
                        parentToChild = parentToChild.ParentToChild();

                        Vector2D pLocal_end = new Vector2D();
                        double _A = ro * length;
                        double deltaLen = length - (_stack - stackStart);
                        angel = 0.5 * deltaLen * deltaLen / (_A * _A);
                        pLocal_end.X = deltaLen * (1 - angel * angel / 10 + Math.Pow(angel, 4) / 216);
                        pLocal_end.Y = deltaLen * angel / 3.0 * (1 - angel * angel / 14 + Math.Pow(angel, 4) / 440);


                        pLocal_end = pLocal_end * parentToChild;
                        //镜像
                        pLocal_end.X *= -1.0;

                        if (isLeft == false)
                        {
                            pLocal_end.Y *= -1;
                        }
                        Matrix2D childToParent = new Matrix2D(startXY,GetStartDirXY());
                        pGlobal = pLocal_end * childToParent;

                      
                    }
                    break;
                case "23":
                    {
                        double lo =rd * length / (ro - rd);

                        Vector2D pLocal_start = new Vector2D();
                        double angel = 0.5 * lo / ro;
                        pLocal_start.X = lo * (1 - angel * angel / 10 + Math.Pow(angel, 4) / 216);
                        pLocal_start.Y = lo * angel / 3.0 * (1 - angel * angel / 14 + Math.Pow(angel, 4) / 440);

                        Matrix2D parentToChild = new Matrix2D(pLocal_start, angel);
                        parentToChild = parentToChild.ParentToChild();

                        Vector2D pLocal_end = new Vector2D();
                        double _A = ro * lo;
                        double deltaLen = _stack - stackStart + lo;
                        
                       angel = 0.5 * deltaLen * deltaLen / (_A * _A);
                        pLocal_end.X = deltaLen * (1 - angel * angel / 10 + Math.Pow(angel, 4) / 216);
                        pLocal_end.Y = deltaLen * angel / 3.0 * (1 - angel * angel / 14 + Math.Pow(angel, 4) / 440);

                        pLocal_end = pLocal_end * parentToChild;

                        if (isLeft == false)
                        {
                            pLocal_end.Y *= -1;
                        }

                        Matrix2D childToParent = new Matrix2D(startXY, GetStartDirXY());
                        pGlobal = pLocal_end * childToParent;
                        
                    }
                    break;
                case "24":
                    {
                        double lo = ro * length / (rd - ro);

                        Vector2D pLocal_start = new Vector2D();
                        double len =length + lo;
                        double angel = 0.5 * len / ro;
                        pLocal_start.X = len * (1 - angel * angel / 10 + Math.Pow(angel, 4) / 216);
                        pLocal_start.Y = len * angel / 3.0 * (1 - angel * angel / 14 + Math.Pow(angel, 4) / 440);



                        Matrix2D parentToChild = new Matrix2D(pLocal_start, angel);
                        parentToChild = parentToChild.ParentToChild();

                        Vector2D pLocal_end = new Vector2D();
                        double _A = rd * lo;
                        double deltaLen = length+lo - (_stack - stackStart);
                        angel = 0.5 * deltaLen * deltaLen / (_A * _A);
                        pLocal_end.X = deltaLen * (1 - angel * angel / 10 + Math.Pow(angel, 4) / 216);
                        pLocal_end.Y = deltaLen * angel / 3.0 * (1 - angel * angel / 14 + Math.Pow(angel, 4) / 440);


                        pLocal_end = pLocal_end * parentToChild;

                        //镜像
                        pLocal_end.X *= -1.0;

                        if (isLeft == false)
                        {
                            pLocal_end.Y *= -1;
                        }

                        Matrix2D childToParent = new Matrix2D(startXY, GetStartDirXY());
                        pGlobal = pLocal_end * childToParent;
                       
                    }
                    break;
            }


            return pGlobal;
        }
    }  
   //public class Wid_HDM
   // {
   //     Wid_HDmSingle Wid_HDmLeft { set; get; }
   //     Wid_HDmSingle Wid_HDmRight { set; get; }

   // }

    public class Wid_HDmSingle
    {
        //没有考虑附加车道\同桩号\不同断面问题
        public double stackMark { 
            set { stackMark = Math.Round(value, 3); }
            get { return Math.Round(stackMark, 3); }
        }
        public double zhongFenDaiWidth { set; get; }
        public double xingCheDaoWidth { set; get; }
        public double yingLuJianWidth { set; get; }
        public double tuLuJianWidth { set; get; }

        public Wid_HDmSingle() { }
        //public WID_HDMInfo(string[] widHDMStr)
        //{
        //    stackMark = Convert.ToDouble(widHDMStr[0]);
        //    zhongFenDaiWidth = Convert.ToDouble(widHDMStr[1]);
        //    xingCheDaoWidth = Convert.ToDouble(widHDMStr[2]);
        //    yingLuJianWidth = Convert.ToDouble(widHDMStr[4]);
        //    tuLuJianWidth = Convert.ToDouble(widHDMStr[5]);
        //}
        public Wid_HDmSingle(double stackMark_, double zhongFenDaiWidth_, double xingCheDaoWidth_, double yingLuJianWidth_, double tuLuJianWidth_)
        {
            stackMark =Math.Round( stackMark_,3);
            zhongFenDaiWidth = zhongFenDaiWidth_;
            xingCheDaoWidth = xingCheDaoWidth_;
            yingLuJianWidth = yingLuJianWidth_;
            tuLuJianWidth = tuLuJianWidth_;
        }

        public Wid_HDmSingle(double stackMark_, double xingCheDaoWidth_, double yingLuJianWidth_, double tuLuJianWidth_)
        {
            stackMark = Math.Round(stackMark_, 3);
            zhongFenDaiWidth = 0;
            xingCheDaoWidth = xingCheDaoWidth_;
            yingLuJianWidth = yingLuJianWidth_;
            tuLuJianWidth = tuLuJianWidth_;
        }

        //stackMark  插入数据桩号桩号
        public Wid_HDmSingle(double stackMark_, Wid_HDmSingle WidHDMStart, Wid_HDmSingle WidHDMEnd)
        {
            stackMark = Math.Round(stackMark_, 3);
            zhongFenDaiWidth = ChaZhi(WidHDMEnd.zhongFenDaiWidth, WidHDMStart.zhongFenDaiWidth, WidHDMEnd.stackMark, WidHDMStart.stackMark, stackMark_);
            xingCheDaoWidth = ChaZhi(WidHDMEnd.xingCheDaoWidth, WidHDMStart.xingCheDaoWidth, WidHDMEnd.stackMark, WidHDMStart.stackMark, stackMark_);
            yingLuJianWidth = ChaZhi(WidHDMEnd.yingLuJianWidth, WidHDMStart.yingLuJianWidth, WidHDMEnd.stackMark, WidHDMStart.stackMark, stackMark_);
            tuLuJianWidth = ChaZhi(WidHDMEnd.tuLuJianWidth, WidHDMStart.tuLuJianWidth, WidHDMEnd.stackMark, WidHDMStart.stackMark, stackMark_);

        }
        public string WID_HDMInfoToString()
        {
            int padleft = 8;
            //return Math.Round(stackMark, 3).ToString("0.000").PadLeft(padleft) + "\t" + Math.Round(zhongFenDaiWidth, 3).ToString("0.000").PadLeft(padleft) + "\t" + Math.Round(xingCheDaoWidth, 3).ToString("0.000").PadLeft(padleft) + "\t" + "0.000".PadLeft(padleft) +"\t" + Math.Round(yingLuJianWidth, 3).ToString("0.000").PadLeft(padleft) + "\t" + Math.Round(tuLuJianWidth, 3).ToString("0.000").PadLeft(padleft) + "\t" + "0".PadLeft(padleft);
            return Math.Round(stackMark, 3).ToString("0.000").PadLeft(10) + Math.Round(zhongFenDaiWidth, 3).ToString("0.000").PadLeft(padleft) + Math.Round(xingCheDaoWidth, 3).ToString("0.000").PadLeft(padleft) + "0.000".PadLeft(padleft) + Math.Round(yingLuJianWidth, 3).ToString("0.000").PadLeft(padleft) + Math.Round(tuLuJianWidth, 3).ToString("0.000").PadLeft(padleft) + "0".PadLeft(padleft);
        }
        private double ChaZhi(double y2, double y1, double x2, double x1, double x)

        {

            return y1 + (y2 - y1) * 1.0 / (x2 - x1) * (x - x1);
        }
    }

    #region
    //public class Wid_HDMDetail:Wid_HDm
    //{
    //    public double xingCheDaoHP{ set; get; }
    //    public double yingLuJianHP { set; get; }
    //    public double tuLuJianHP { set; get; }

    //    public Wid_HDMDetail() { }

    //    public Wid_HDMDetail(double stackMark_, double xingCheDaoWidth_, double xingCheDaoHP_, double yingLuJianWidth_, double yingLuJianHP_, double tuLuJianWidth_, double tuLuJianHP_)
    //    {
    //        stackMark = stackMark_;
    //        zhongFenDaiWidth =0;
    //        xingCheDaoWidth = xingCheDaoWidth_;
    //        yingLuJianWidth = yingLuJianWidth_;
    //        tuLuJianWidth = tuLuJianWidth_;
    //        xingCheDaoHP = xingCheDaoHP_;
    //        yingLuJianHP = yingLuJianHP_;
    //        tuLuJianHP = tuLuJianHP_;
    //    }
    //    public Wid_HDMDetail( Wid_HDm wID_HDM ,double xingCheDaoHP_, double yingLuJianHP_, double tuLuJianHP_)
    //    {
    //        stackMark = wID_HDM.stackMark;
    //        zhongFenDaiWidth = 0;
    //        xingCheDaoWidth = wID_HDM.xingCheDaoWidth;
    //        yingLuJianWidth = wID_HDM.yingLuJianWidth;
    //        tuLuJianWidth = wID_HDM.tuLuJianWidth;
    //        xingCheDaoHP = xingCheDaoHP_;
    //        yingLuJianHP =yingLuJianHP_;
    //        tuLuJianHP =tuLuJianHP_;
    //    }

    //}
    #endregion
    #region
    //public class Hp_HDM
    //{
    //    public double stack { get; }
    //    public double h_TLJ { get; }
    //    public double h_YLJ { get; } 
    //    public double h_XCD { get; }


    //    public Hp_HDM() { }
    //    public Hp_HDM(double[] arr)
    //    {
    //        if (arr.Count() < 4) return;
    //        stack = arr[0];
    //        h_TLJ = arr[1];
    //        h_YLJ = arr[2];
    //        h_XCD = arr[3];
    //    }
    //    public Hp_HDM(double stack_, double h_TLJ_, double h_YLJ_, double h_XCD_)
    //    {
    //        stack = stack_;
    //        h_TLJ = h_TLJ_;
    //        h_YLJ = h_YLJ_;
    //        h_XCD = h_XCD_;
    //    }
    //}
    #endregion

    public class Hp_HDM
    {
        public double stack { 
            get { return Math.Round(stack, 3); }
            set { } }

        public double h_left_TLJ { get; set; }
        public double h_left_YLJ { get; set; }
        public double h_left_XCD { get; set; }

        public double h_right_XCD { get; set; }
        public double h_right_YLJ { get; set; }
        public double h_right_TLJ { get; set; }

        public Hp_HDM() { }
        public Hp_HDM(double stack_, double h_left_TLJ_, double h_left_YLJ_, double h_left_XCD_, double h_right_XCD_, double h_right_YLJ_, double h_right_TLJ_)
        {
            stack =Math.Round( stack_,3);
            h_left_TLJ = h_left_TLJ_;
            h_left_YLJ = h_left_YLJ_;
            h_left_XCD = h_left_XCD_;
            h_right_XCD = h_right_XCD_;
            h_right_YLJ = h_right_YLJ_;
            h_right_TLJ = h_right_TLJ_;
        }
        public Hp_HDM(double[] arr)
        {
            if (arr.Count() < 7) return;
            stack =Math.Round( arr[0],3);
            h_left_TLJ = arr[1];
            h_left_YLJ = arr[2];
            h_left_XCD = arr[3];
            h_right_XCD = arr[4];
            h_right_YLJ = arr[5];
            h_right_TLJ = arr[6];
        }
        public Hp_HDM(double stack_, double h_TLJ_, double h_YLJ_, double h_XCD_)
        {
            stack = Math.Round(stack_, 3);
            h_left_TLJ = h_TLJ_;
            h_left_YLJ = h_YLJ_;
            h_left_XCD = h_XCD_;
            h_right_XCD = h_XCD_;
            h_right_YLJ = h_YLJ_;
            h_right_TLJ = h_YLJ_;
        }
        public Hp_HDM(double h_TLJ_, double h_YLJ_, double h_XCD_)
        {
            stack = -1;
            h_left_TLJ = h_TLJ_;
            h_left_YLJ = h_YLJ_;
            h_left_XCD = h_XCD_;
            h_right_XCD = h_XCD_;
            h_right_YLJ = h_YLJ_;
            h_right_TLJ = h_YLJ_;
        }

    }
   
    public class CG_W_Fuzhu
    {
       public double maxh { get; }
        public string rotateBase { get; }
        //public string jianbianType { get; }

        public string addWidthType { get; }
        public bool isCG { get; }
        public bool isJK { get; }

        public int vDesign { get; }

        public CG_W_Fuzhu(bool isCG_,double maxh_, string rotateType_,bool isJK_,string addWidthType_,int vDesign_)
        {
            isCG = isCG_;
            maxh = maxh_;
            rotateBase = rotateType_;
           
            isJK = isJK_;
            addWidthType = addWidthType_;
            vDesign = vDesign_;
        }
    }
}

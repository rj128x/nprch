using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NPRCHApp
{


    public class RecordHour
    {

        public static Dictionary<string, List<Record>> DataSDay;
        public Random rand = new Random();
        public static int FileIndex = 0;
        public static double funcRegr(List<double> X, List<double> Y, double o1, double o2, double p)
        {

            int cnt = X.Count;
            double sum = 0;
            double regr = 0;
            double xi = 0;
            double yi = 0;
            double sgnX;
            double absX;
            double d = 0;



            for (int i = 0; i < cnt; i++)
            {
                xi = X[i];
                yi = Y[i];
                sgnX = Math.Sign(xi);
                absX = Math.Abs(xi);
                if (absX > (o1 + p))
                {
                    regr = -o2 * (xi - sgnX * o1);
                }
                else if (absX < (o1 - p))
                {
                    regr = 0;
                }
                else
                {
                    regr = -o2 * sgnX / p / 4 * Math.Pow(xi - sgnX * (o1 - p), 2);
                }

                d = yi - regr;
                sum += d * d;

            }

            return sum;
        }


        public static double CheckPerv(List<double> X, List<double> Y, double s, double mp)
        {
            int cnt = X.Count;
            double sum = 0;
            double regr = 0;
            double xi = 0;
            double yi = 0;
            double sgnX;
            double absX;

            double d = 0;
            for (int i = 0; i < cnt; i++)
            {
                xi = X[i];
                yi = Y[i];
                sgnX = Math.Sign(xi);
                absX = Math.Abs(xi);
                regr = 0;
                if (absX > mp)
                {
                    d = sgnX * (absX - mp);
                    regr = -200 / s * d;
                }

                d = yi - regr;
                sum += d * d;
            }


            return sum;
        }

        public static double[] getProizv(List<double> X, List<double> Y, double o1, double o2, double p)
        {
            double[] res = new double[] { 0, 0, 0 };
            int cnt = X.Count;
            double dO1 = 0;
            double dO2 = 0;
            double dp = 0;
            double xi = 0;
            double yi = 0;
            double sgnX;
            double absX;

            double d = 0;
            double a = 0;
            double s = 0;
            double z = 0;
            double m = 0;
            for (int i = 0; i < cnt; i++)
            {
                xi = X[i];
                yi = Y[i];
                sgnX = Math.Sign(xi);
                absX = Math.Abs(xi);
                s = o2;
                z = sgnX;
                m = o1;

                if (absX > o1 + p)
                {
                    //(y-(-s*(x-z*m)))^2
                    dO1 += -2 * s * z * (s * (-m * z + xi) + yi);
                    dO2 += (-2 * m * z + 2 * xi) * (s * (-m * z + xi) + yi);
                }
                else if ((absX > o1 - p) && (absX < o1 + p))
                {
                    //(y-(-z*s/(4*p)*(x-z*(m-p))^2))^2
                    a = yi + s * z * Math.Pow(xi - z * (m - p), 2) / 4.0 / p;
                    dO1 += -s * z * z * (xi - z * (m - p)) * a / p;
                    dO2 += z * Math.Pow(xi - z * (m - p), 2) * a / 2.0 / p;
                    dp += a * (s * z * z * (xi - z * (m - p)) / p - s * z * Math.Pow(xi - z * (m - p), 2) / 2.0 / p / p);
                }


            }
            res[0] = dO1;
            res[1] = dO2;
            res[2] = dp;
            return res;
        }

        public static double calcRHO(List<Record> dataS)
        {
            List<double> X = new List<double>();
            List<double> Y = new List<double>();
            foreach (Record rec in dataS)
            {
                X.Add(rec.F_gc - 50);
                Y.Add((rec.P_fakt - rec.P_plan - rec.P_zvn));

            }

            double n = X.Count;
            double Mx = 1.0 / (n) * X.Sum();
            double My = 1.0 / (n) * Y.Sum();
            double sumTop = 0;
            double sum1Down = 0;
            double sum2Down = 0;
            for (int i = 0; i < n; i++)
            {
                sumTop += (X[i] - Mx) * (Y[i] - My);
                sum1Down += (X[i] - Mx) * (X[i] - Mx);
                sum2Down += (Y[i] - My) * (Y[i] - My);

            }
            double RHO = sumTop / Math.Sqrt(sum1Down * sum2Down);
            return RHO;

        }



        public static void calcSTATIZM(List<Record> dataS, ref double STATIZM, ref double MP, ref double PSgl, ref int step)
        {
            List<double> X = new List<double>();
            List<double> Y = new List<double>();
            SortedList<double, double> Data = new SortedList<double, double>();
            SortedList<double, double> DataAbs = new SortedList<double, double>();

            foreach (Record rec in dataS)
            {
                double d = rec.F_gc - 50;
                double y = rec.P_fakt - rec.P_plan - rec.P_zvn;
                X.Add(d);
                Y.Add(y);

            }



            int n = X.Count;


            double minDV = double.MaxValue;

            double vs0 = 0;
            double vmp0 = 0;
            double v0 = 0;
            double v1 = 0;
            double o2 = 40;
            double o1 = 0.02;
            double p = 0.001;
            double sO1 = 0.001;
            double sO2 = 1;
            double sP = 0.001;
            bool ok = false;
            OutputData.InitOutput("test");

            double prevGrad = 1;
            double prevVal = funcRegr(X, Y, o1, o2, p);
            step = 5000;


            while (!ok)
            {
                step--;
                double[] arr = getProizv(X, Y, o1, o2, p);
                double dO1 = arr[0] / X.Count;
                double dO2 = arr[1];
                double dP = arr[2] / X.Count;
                double grad = Math.Sqrt(dO1 * dO1 + dO2 * dO2 + dP * dP);
                double cO1 = Math.Abs(dO1 / grad);
                double cO2 = Math.Abs(dO2 / grad);
                double cP = Math.Abs(dP / grad);
                OutputData.writeToOutput("test", String.Join(";", new double[] { o1, o2, p, dO1, dO2, dP, grad }));

                /*o1 = dO1 < 0 ? o1 + sO1 : o1 - sO1 / 2;
                o2 = dO2 < 0 ? o2 + sO2 : o2 - sO2 / 2;
                p = dP < 0 ? p + sP  : p - sP / 2;*/

                /*cO1 = cO1 < 0.01 ? 0.01 : cO1 > 1 ? 1 : cO1;
                cO2 = cO2 < 0.01 ? 0.01 : cO2 > 1 ? 1 : cO2;
                cP = cP < 0.01 ? 0.01 : cP > 1 ? 1 : cP;*/

                o1 = o1 - sO1 * cO1 * Math.Sign(dO1);
                o2 = o2 - sO2 * cO2 * Math.Sign(dO2);
                p = p - sP * cP * Math.Sign(dP);

                double val = funcRegr(X, Y, o1, o2, p);
                prevGrad = grad;

                ok = Math.Abs(val - prevVal) < 0.0001 || step < 1;
                prevVal = val;
            }
            STATIZM = 200 / o2;
            MP = o1;
            PSgl = p;
        }

        public static void calcSTATIZMFast(List<Record> dataS, ref double STATIZM, ref double MP, ref double PSgl,BlockData bd)
        {
            SortedList<double, List<double[]>> FullData = new SortedList<double, List<double[]>>();

            List<double> Data = new List<double>();
            List<double> DataAbs = new List<double>();
            List<double> DataSign = new List<double>();

            foreach (Record rec in dataS)
            {
                double d = rec.F_gc - 50;
                double y = (rec.P_fakt - rec.P_plan - rec.P_zvn)/bd.PNom*100;

                double absD = Math.Abs(d);

                if (!FullData.ContainsKey(absD))
                {
                    FullData.Add(absD, new List<double[]>());
                }

                FullData[absD].Add(new double[] { y, Math.Sign(d) });

            }
            List<double> absXKeys = new List<double>();
            List<double> keys = FullData.Keys.ToList();
            int i = 0;
            foreach (double x in keys)
            {
                foreach (double[] subArr in FullData[x])
                {
                    double y = subArr[0];
                    double sign = subArr[1];
                    Data.Add(y);
                    DataAbs.Add(y * y);
                    DataSign.Add(sign);

                    if (i > 0)
                        DataAbs[i] += DataAbs[i - 1];
                    absXKeys.Add(x);
                    i++;
                }
            }




            double minDV = double.MaxValue;
            double minDVLine = double.MaxValue;


            double v0 = 0;

            double o2;
            double o1;

            double absX = 0;
            double sgnX = 0;
            double regr = 0;
            double xi;
            double yi = 0;
            bool okSquare = false;
            List<double> DataStat = new List<double>();
            List<double> DataStatLine = new List<double>();
            List<double> KeysStat = new List<double>();
            for (double s = 3; s < 8; s += 0.05)
            {
                DataStat.Add(0);
                DataStatLine.Add(0);
                KeysStat.Add(s);
            }

            //OutputData.InitOutput("test");
            double v0mp = 0;
            for (o1 = 0.005; o1 <= 0.03; o1 += 0.0005)
            {


                if (okSquare)
                    continue;
                v0 = 0;
                List<double> vals = new List<double>();
                for (double p = 0.0005; p < o1 - 0.0005; p += 0.0005)
                {
                    int pI = 0;
                    while (pI < absXKeys.Count)
                    {
                        if (absXKeys[pI] > (o1 - p))
                            break;
                        pI++;
                    }
                    double sum1 = DataAbs[pI - 1];
                    for (int si = 0; si < KeysStat.Count; si++)
                    {
                        DataStat[si] = sum1;
                    }


                    //pI++;
                    while (pI < absXKeys.Count)
                    {
                        absX = absXKeys[pI];
                        sgnX = DataSign[pI];
                        xi = absX * sgnX;
                        yi = Data[pI];

                        if (absX > (o1 + p))
                        {
                            regr = -(xi - sgnX * o1);
                        }
                        else
                        {
                            regr = -sgnX / p / 4 * Math.Pow(xi - sgnX * (o1 - p), 2);
                        }

                        int si = 0;
                        foreach (double s in KeysStat)
                        {
                            o2 = 200 / s;
                            DataStat[si] += (yi - o2 * regr) * (yi - o2 * regr);
                            si++;
                        }

                        pI++;
                    }

                    double minVal = DataStat.Min();
                    vals.Add(minVal);
                    if (minVal > v0 && v0 > 0)
                        break;
                    if (minVal < minDV)
                    {
                        minDV = minVal;
                        int si = DataStat.IndexOf(minVal);
                        STATIZM = KeysStat[si];
                        MP = o1;
                        PSgl = p;
                    }
                    v0 = minVal;
                }
                //OutputData.writeToOutput("test", String.Join(";", vals));
                if (v0 > v0mp && v0mp > 0)
                    okSquare = true;
                v0mp = v0;
            }

        }

        public static void calcSTATIZMRegr(List<Record> dataS, ref double STATIZM, ref double MP, ref double PSgl,BlockData bd)
        {
            SortedList<double, List<double[]>> FullData = new SortedList<double, List<double[]>>();

            List<double> Data = new List<double>();
            List<double> DataAbs = new List<double>();
            List<double> DataSign = new List<double>();
            List<double> X = new List<double>();
            List<double> Y = new List<double>();

            foreach (Record rec in dataS)
            {
                double d = rec.F_gc - 50;
                double y = (rec.P_fakt - rec.P_plan - rec.P_zvn)/bd.PNom*100;
                X.Add(d);
                Y.Add(y);
            }
            /*X = calcAVGKoleb(X,20);
            Y = calcAVGKoleb(Y,20);*/

            for (int ii = 0; ii < X.Count; ii++)
            {
                double d = X[ii];
                double y = Y[ii];
                if (Math.Abs(d) > 1)
                    continue;

                double absD = Math.Abs(d);

                if (!FullData.ContainsKey(absD))
                {
                    FullData.Add(absD, new List<double[]>());
                }

                FullData[absD].Add(new double[] { y, Math.Sign(d) });


            }
            List<double> absXKeys = new List<double>();

            List<double> keys = FullData.Keys.ToList();
            int i = 0;
            foreach (double x in keys)
            {
                foreach (double[] subArr in FullData[x])
                {
                    double y = subArr[0];
                    double sign = subArr[1];
                    Data.Add(y);
                    DataAbs.Add(y * y);
                    DataSign.Add(sign);


                    if (i > 0)
                        DataAbs[i] += DataAbs[i - 1];
                    absXKeys.Add(x);
                    i++;
                }

            }

            double minDV = double.MaxValue;
            double minDV2 = double.MaxValue;


            double v0 = 0;

            double o2;
            double o1;

            double absX = 0;
            double sgnX = 0;
            double regr = 0;
            double xi;
            double yi = 0;

            List<double> DataStat = new List<double>();
            List<double> KeysStat = new List<double>();
            for (double s = 3; s < 12; s += 0.01)
            {
                DataStat.Add(0);
                KeysStat.Add(s);
            }

            OutputData.InitOutput("test");
            for (o1 = 0.005; o1 <= 0.03; o1 += 0.0001)
            {
                int o1I = 0;
                int cnt0 = 0;
                int si = 0;
                while (o1I < absXKeys.Count)
                {
                    if (absXKeys[o1I] > (o1))
                        break;
                    o1I++;
                }
                double sum1 = DataAbs[o1I - 1];
                for (si = 0; si < KeysStat.Count; si++)
                {
                    DataStat[si] = sum1;
                }
                cnt0 = o1I;
                while (o1I < absXKeys.Count)
                {
                    absX = absXKeys[o1I];
                    sgnX = DataSign[o1I];
                    xi = absX * sgnX;
                    yi = Data[o1I];

                    regr = -(xi - sgnX * o1);


                    for (si = 0; si < KeysStat.Count; si++)
                    {
                        o2 = 200 / KeysStat[si];
                        DataStat[si] += (yi - o2 * regr) * (yi - o2 * regr);

                    }
                    o1I++;

                }

                /*for (si = 0; si < KeysStat.Count; si++)
                {

                    double mv = sum1 / cnt0 + (DataStat[si] / (absXKeys.Count - cnt0));
                    DataStat[si] = mv;

                }*/


                for (si = 1; si < KeysStat.Count - 1; si++)
                {

                    if (DataStat[si - 1] > DataStat[si] && DataStat[si + 1] > DataStat[si])
                    {
                        double val = DataStat[si];
                        if (val < minDV)
                        {
                            minDV = val;
                            minDV2 = sum1;
                            STATIZM = KeysStat[si];
                            MP = o1;
                        }
                    }


                }
                OutputData.writeToOutput("test", string.Join(";", DataStat));


            }

        }






        public void calcReact(List<Record> dataS, bool fillData)
        {
            if (dataS.Count < 100)
                return;

            List<double> X = new List<double>();
            List<double> Y = new List<double>();
            List<double> Xavg = new List<double>();
            List<double> Yavg = new List<double>();
            List<double> Xdx = new List<double>();
            List<double> Ydy = new List<double>();
            List<double> XdxAvg = new List<double>();
            List<double> YdyAvg = new List<double>();
            List<double> NoReactArr = new List<double>();

            foreach (Record rec in dataS)
            {
                X.Add(rec.P_perv/blockData.PNom*100);
                Y.Add((rec.P_fakt - rec.P_plan - rec.P_zvn)/blockData.PNom*100);
                Xdx.Add(0);
                Ydy.Add(0);

                NoReactArr.Add(0);
            }
            int n = X.Count;
            int w1 = 30;

            Xavg = calcAVG(X, w1);
            Yavg = calcAVG(Y, w1);

            Xdx[0] = 0;
            Ydy[0] = 0;
            for (int i = 1; i < n; i++)
            {
                Xdx[i] = Xavg[i] - Xavg[i - 1];
                Ydy[i] = Yavg[i] - Yavg[i - 1];
            }

            int w2 = 45;

            XdxAvg = calcAVG(Xdx, w2);
            YdyAvg = calcAVG(Ydy, w2);

            int dt = 30;

            List<double> Marr = new List<double>();
            List<double> Merr = new List<double>();
            Dictionary<int, double> res = new Dictionary<int, double>();
            List<double> Msub = new List<double>();
            for (int i = 0; i < n; i++)
            {
                int i1 = i;
                int i2 = i1 + dt ;
                if (i1 < 0)
                    i1 = 0;
                if (i2 >= n)
                    i2 = n - 1;

                Msub.Clear();
                if (i2 < n)
                {
                    for (int ii = i1; ii <= i2; ii++)
                    {
                        double m = Math.Abs(XdxAvg[i] - YdyAvg[ii]);
                        Msub.Add(m);
                    }
                    if (Msub.Count == 0)
                        continue;
                    double mMin = Msub.Min();

                    if ((mMin >= 0.0155) && (Math.Abs(XdxAvg[i]) >= 0.007))
                    {
                        NoReactArr[i] = 1;
                        Marr.Add(mMin);
                        Merr.Add(i);
                        res.Add(i, mMin);
                        //NoReactArr[i] = mMin;
                    }
                    NoReactArr[i] = mMin;
                }

            }

            if (res.Count > 0)
            {
                Msub.Clear();
                int prevI = -1;
                int start = res.Keys.First();
                foreach (int i in res.Keys)
                {
                    if (i == res.Keys.First())
                    {
                        prevI = i;
                        Msub.Add(res[i]);
                    }
                    else
                    {
                        if ((!res.ContainsKey(i - 1)) || (i == res.Keys.Last()))
                        {
                            NoReactComment += String.Format("({0}:{1}-{2}:{3} [{4:0.000}])", start / 60, start % 60, prevI / 60, prevI % 60, Msub.Max());
                            Msub.Clear();
                            Msub.Add(res[i]);
                            start = i;

                        }
                        else
                        {
                            Msub.Add(res[i]);
                        }
                        prevI = i;
                    }
                }
            }



            if (fillData)
            {
                DateTime ds = Data.First().Key;
                for (int i = 0; i < X.Count; i++)
                {

                    Data[ds.AddSeconds(i)].X_avg = XdxAvg[i];
                    Data[ds.AddSeconds(i)].Y_avg = YdyAvg[i];
                    Data[ds.AddSeconds(i)].NoReact = NoReactArr[i];

                }
            }

        }

        public static List<double> calcAVG(List<double> X, int w)
        {
            List<double> res = new List<double>();
            int n = X.Count;
            double sum = 0;
            int cnt = 0;
            for (int i = 0; i < n; i++)
            {
                res.Add(0);
                int i1 = i - w / 2;
                int i2 = i1 + w;

                if (i1 < 0)
                    i1 = 0;

                if (i2 > n)
                    i2 = n;

                if (i2 < n)
                {
                    sum = 0;
                    cnt = 0;
                    for (int ii = i1; ii < i2; ii++)
                    {
                        cnt++;
                        sum += X[ii];
                    }

                    res[i] = sum / (cnt);
                }

            }
            return res;
        }


        public static List<double> calcAVGKoleb(List<double> X, int w)
        {
            List<double> res = new List<double>();
            int n = X.Count;
            double sum = 0;
            int cnt = 0;
            for (int i = 0; i < n; i++)
            {
                res.Add(0);
                int i1 = i - w / 2;
                int i2 = i1 + w;

                if (i1 < 0)
                    i1 = 0;


                if (i2 >= n)
                {
                    i2 = n - 1;
                    //i1 = i2 - w;
                }

                sum = 0;
                cnt = 0;
                for (int ii = i1; ii < i2; ii++)
                {
                    cnt++;
                    sum += X[ii];

                }

                res[i] = sum / (double)(cnt);

            }
            return res;
        }

        public string calcAKF(List<double> Otrez, ref int T, ref double R)
        {
            List<double> RP = new List<double>();
            int N = Otrez.Count;


            for (int lag = 0; lag < N - 1; lag++)
            {
                double rxy = calcAKFLag(Otrez, lag);

                RP.Add(rxy);

            }
            for (int i = 1; i < RP.Count - 1; i++)
            {
                if (RP[i - 1] <= RP[i] && RP[i] >= RP[i + 1] )
                //if (Math.Abs(RP[i]-RP[i-1])<0.001)
                {
                    T = i;
                    R = RP[i];

                    break;

                }
            }

            return String.Format("t={0} r={1};{2}\r\notres;{3}", T, R, String.Join(";", RP), String.Join(";", Otrez));
        }


        public double calcAKFLag(List<double> Otrez, int lag)
        {
            int N = Otrez.Count;

            double sum_xy = 0;

            double sum_O2 = 0;

            for (int k = 0; k < N; k++)
            {
                sum_O2 += (Otrez[k]) * (Otrez[k]);
            }

            for (int k = 0; k < N - lag; k++)
            {
                sum_xy += (Otrez[k + lag]) * (Otrez[k]);
                //sum_O2 += (Otrez[k] ) * (Otrez[k] );

            }
            double rxy = (sum_xy) / (sum_O2);

            return rxy;
        }

        public double calcAKFLag1(List<double> Otrez, int lag)
        {
            int n = 0;
            int N = Otrez.Count;
            double x;
            double y;
            double sumX = 0;
            double sumY = 0;
            double sumXY = 0;
            double sumX2 = 0;
            double sumY2 = 0;
            for (int k = 0; k < N - lag; k++)
            {
                n++;
                x = Otrez[k];
                y = Otrez[k + lag];
                sumX += x;
                sumY += y;
            }
            //n = 1;
            double avgX = sumX / (n);
            double avgY = sumY / (n);

            //avgX = 0;
            //avgY = 0;
            for (int k = 0; k < N - lag; k++)
            {
                x = Otrez[k];
                y = Otrez[k + lag];
                sumXY += (x - avgX) * (y - avgY);
                sumX2 += (x - avgX) * (x - avgX);
                sumY2 += (y - avgY) * (y - avgY);
            }


            double avgXY = sumXY / n;

            double rxy = (sumXY) / (Math.Sqrt(sumX2 * sumY2));
            return rxy;
        }




        public void calcKoleb(List<Record> dataS, bool fillData)
        {
            string name = "out_" + this.Date.Replace(":", "_");
            //OutputData.InitOutput(name);
            List<double> P = new List<double>();
            List<double> P1 = new List<double>();
            List<double> P70 = new List<double>();

            List<double> O = new List<double>();
            List<double> F = new List<double>();
            List<double> RR = new List<double>();
            List<double> TR = new List<double>();

            double prevF = 50;
            foreach (Record rec in dataS)
            {

                P.Add(rec.P_fakt);

                O.Add(0);
                RR.Add(0);
                TR.Add(0);

                double d = (rec.F_gc - 50);
                //prevF = rec.F_gc;

                F.Add(d);

            }
            int n = P.Count;

            P1 = calcAVGKoleb(P, 9);
            P70 = calcAVGKoleb(P1, 70);
            F = calcAVGKoleb(F, 9);




            for (int i = 0; i < n; i++)
            {
                O[i] = P1[i] - P70[i];
            }

            int winStart = 0;
            int winWid = 121;
            int winStep = 10;
            List<double> Otrez = new List<double>();
            List<double> FOtrez = new List<double>();
            List<double> POtrez = new List<double>();
            List<int> intStarts = new List<int>();
            List<int> Ts = new List<int>();

            Dictionary<int, List<double>> DictO = new Dictionary<int, List<double>>();
            Dictionary<int, List<double>> DictF = new Dictionary<int, List<double>>();
            Dictionary<int, List<double>> DictP = new Dictionary<int, List<double>>();
            List<int> KeysDict = new List<int>();

            int index = 0;
            while (winStart + winWid <= n)
            {
                Otrez = new List<double>();
                FOtrez = new List<double>();
                POtrez = new List<double>();
                for (int i = winStart; i < winStart + winWid; i++)
                {
                    Otrez.Add(O[i]);
                    FOtrez.Add(F[i]);
                    POtrez.Add(P[i]);
                }
                DictF.Add(index, FOtrez);
                DictO.Add(index, Otrez);
                DictP.Add(index, POtrez);
                KeysDict.Add(index);
                index++;

                winStart += winStep;
            }

            KolebComment = "";

            foreach (int otrIndex in KeysDict)
            {
                Otrez = DictO[otrIndex];
                FOtrez = DictF[otrIndex];

                int T = 0;
                double R = 0;
                List<double> funcP = new List<double>();
                string s1 = calcAKF(Otrez, ref T, ref R);

                for (int i = 0; i < winWid; i++)
                {
                    TR[otrIndex * winStep + i] = T;
                    RR[otrIndex * winStep + i] = R;
                }
                /*if (otrIndex > 0 && otrIndex < 330)
                {
                    OutputData.writeToOutput(name, String.Format("o;") + s1);
                    OutputData.writeToOutput(name, String.Join(";", DictP[otrIndex]));
                    OutputData.writeToOutput(name, String.Join(";", DictF[otrIndex]));
                    int t1 = 0;
                    double r1 = 0;

                    string s2 = calcAKF(FOtrez, ref t1, ref r1);

                    double RF = calcAKFLag(FOtrez, T);
                    OutputData.writeToOutput(name, String.Format("f;") + s2);
                }*/

                if (R >= 0.6 && T >= 5 && T < 100)
                {
                    int t1 = 0;
                    double r1 = 0;

                    string s2 = calcAKF(FOtrez, ref t1, ref r1);

                    double RF = calcAKFLag(FOtrez, T);


                    if (RF >100)
                    {
                        KolebComment += String.Format(" {0}:{1} (T={2}) [{3:0.00}]", (otrIndex * winStep) / 60, (otrIndex * winStep) % 60, T, R);
                    }
                    else
                    {
                        int start = -1;
                        int startJ = -1;
                        int end = -1;
                        double nper = 0;
                        List<double> NPERS = new List<double>();
                        List<double> RRR = new List<double>();

                        for (int j = 0; j < KeysDict.Count; j++)
                        {
                            double r0 = calcAKFLag(DictO[j], T);
                            RRR.Add(r0); 

                            if (r0 >= 0.6 && start < 0)
                            {
                                start = j * winStep;
                                startJ = j;
                            }
                            else if ((r0 < 0.6 && start >= 0) || (j == KeysDict.Count - 1 && start >= 0))
                            {
                                end = (j ) * (winStep) + winWid-winStep;
                                nper = (double)(end - start) / (double)(T);
                               // nper = (j-startJ) / (double)T;
                                NPERS.Add(nper);

                                start = -1;

                            }
                        }
                        
                        if (NPERS.Count() > 0 && NPERS.Max() > 5)
                        {
                            KolebComment += String.Format(" {0}:{1} (T={2}) [{3:0.0}]", (otrIndex * winStep) / 60, (otrIndex * winStep) % 60, T, NPERS.Max());
                            OutputData.writeToOutput(name, String.Format("o;") + s1);
                            OutputData.writeToOutput(name, "RRR;" + String.Join(";", RRR));
                            //OutputData.writeToOutput(name, String.Join(";", DictP[otrIndex]));
                            //OutputData.writeToOutput(name, String.Join(";", DictF[otrIndex]));
                        }
                    }




                }
            }



            if (fillData)
            {
                DateTime ds = Data.First().Key;
                for (int i = 0; i < P.Count; i++)
                {
                    Data[ds.AddSeconds(i)].X_avg = TR[i];
                    Data[ds.AddSeconds(i)].Y_avg = RR[i];
                    Data[ds.AddSeconds(i)].O = O[i];
                }
            }

        }


        public static double[] getLinear(List<double> Otrez)
        {
            double sumXY = 0;
            double sumX2 = 0;
            double sumX = 0;
            double sumY = 0;

            for (int i = 0; i < Otrez.Count; i++)
            {
                double x = i;
                double y = Otrez[i];
                sumXY += x * y;
                sumX2 += x * x;
                sumX += x;
                sumY += y;
            }

            double n = Otrez.Count;
            double a = (n * sumXY - sumX * sumY) / (n * sumX2 - sumX * sumX);
            double b = (sumY - a * sumX) / n;

            double sumSign = 0;
            for (int i = 0; i < Otrez.Count; i++)
            {
                double x = i;
                double y = Otrez[i];
                double y1 = a * x + b;
                sumSign += (y1 - y) * (y1 - y);

            }
            return new double[] { a, b, sumSign };
        }


        public static string calcNoAuto(List<Record> dataS)
        {
            int width = 1800;
            List<double> Data = new List<double>();
            //Dictionary<DateTime, int> res = new Dictionary<DateTime, int>();
            string res = "";
            DateTime dt = dataS.First().Date;
            for (int i = 0; i < dataS.Count; i++)
            {
                if ((i % width == 0 && i > 0) || (i == dataS.Count - 1))
                {

                    string k = calcNoAutoOtrez(dt, Data);

                    res += k.ToString() + " ";
                    Data.Clear();
                    dt = dataS.First().Date.AddSeconds(i);
                }
                Data.Add(dataS[i].P_plan);
            }
            return res;
        }

        public static string calcNoAutoOtrez(DateTime dt, List<Double> Data)
        {


            int left = 0;
            int right = left + 1;
            List<double> IzlArr = new List<double>();
            List<double> IzlKArr = new List<double>();
            List<int> TArr = new List<int>();

            while (right < Data.Count)
            {
                List<double> Otrez = new List<double>();
                for (int i = left; i <= right; i++)
                {
                    Otrez.Add(Data[i]);
                }
                double[] appr = getLinear(Otrez);
                double k1 = appr[0];
                double b1 = appr[1];
                double sg = appr[2];
                double sg1 = sg / (Math.Sqrt(k1 * k1));

                if (sg1 > 0.00005)
                {
                    IzlArr.Add(Data[right - 1]);
                    TArr.Add(right - 1);
                    Otrez.RemoveAt(Otrez.Count - 1);
                    appr = getLinear(Otrez);
                    double k = appr[0];
                    double kAvg = 100000 * k / 100000.0;
                    IzlKArr.Add(kAvg);
                    left = right - 1;
                }
                right = right + 1;
                if (right - left >= 5)
                {
                    left = left + 1;
                }
            }
            List<double> EkstArr = new List<double>();
            List<int> EkstTarr = new List<int>();
            List<string> dates = new List<string>();
            for (int i = 1; i < TArr.Count; i++)
            {
                if (IzlKArr[i] * IzlKArr[i - 1] < 0)
                {
                    EkstTarr.Add(TArr[i]);
                    dates.Add(dt.AddSeconds(TArr[i]).ToString("mm:ss"));
                }
            }
            return String.Join(";", dates);
        }

        protected Dictionary<DateTime, Record> Data { get; set; }

        public string Date { get; set; }
        protected string TextFile { get; set; }
        public string FileName { get; set; }
        public int cntPower { get; set; }
        public int cntFreq { get; set; }
        public int cntBad { get; set; }
        public int cntBadPower { get; set; }
        public int TRepeatFreq { get; set; }
        public int TRepeatPower { get; set; }
        public int TNoRezerv { get; set; }
        public string NoReactComment { get; set; }
        public string NoAutoComment { get; set; }
        public string KolebComment { get; set; }
        public double RHO { get; set; }
        public double STATIZM { get; set; }

        public double MP { get; set; }
        public double PSGL { get; set; }
        public static bool created = false;
        public BlockData blockData;

        public RecordHour(BlockData blockData)            
        {
            this.blockData = blockData;
            if (!DataSDay.ContainsKey(blockData.BlockNumber))
            {
                DataSDay.Add(blockData.BlockNumber, new List<Record>());
            }
        }



        public void processFile(DateTime date, int DiffHour, bool fillData)
        {

            Date = date.ToString("dd.MM.yyyy HH:00");
            string fn = "";
            List<string> data = FTPClass.ReadFile(date.AddHours(-DiffHour), blockData.BlockNumber, out fn);
            FileName = fn;

            if (fillData)
            {
                TextFile = string.Join("\r\n", data);
            }


            Data = new Dictionary<DateTime, Record>();
            List<Record> dataS = new List<Record>();

            double prevP = 10e6;
            double prevF = 10e6;
            cntPower = 0;
            cntBadPower = 0;
            cntFreq = 0;
            cntBad = 0;
            List<double> prevPperv = new List<double>();
            List<double> prevPplan = new List<double>();
            List<double> prevPzvn = new List<double>();

            int cntRptFreq = 0;
            int cntRptPower = 0;
            foreach (string str in data)
            {
                Record rec = new Record(date, str,blockData.PNom,blockData.FNom, ref prevPperv,ref prevPplan,ref prevPzvn);
                dataS.Add(rec);
                if (fillData)
                    Data.Add(rec.Date, rec);
                else
                {
                    if (rec.F_gc > 49.8)
                        DataSDay[blockData.BlockNumber].Add(rec);
                }


                double diffP = Math.Abs(rec.P_fakt - prevP);
                double diffF = Math.Abs(rec.F_gc - prevF);

                if (diffP > 0 && diffP <= blockData.PNom*0.01)
                    cntPower++;

                if (diffF > 0 && diffF <= 0.001)
                    cntFreq++;


                if (diffP < 10e-6)
                {
                    cntRptPower++;
                }
                else
                {
                    if (cntRptPower >= 10)
                        TRepeatPower += cntRptPower;
                    cntRptPower = 0;
                }

                if (diffF < 10e-6)
                {
                    cntRptFreq++;
                }
                else
                {
                    if (cntRptFreq >= 10)
                        TRepeatFreq += cntRptFreq;
                    cntRptFreq = 0;
                }

                double df = Math.Abs(rec.F_gc - 50);
                double minP = blockData.PMin + 0.07 * blockData.PNom - 0.01 * blockData.PNom;
                double maxP = blockData.PMax - 0.07 * blockData.PNom + 0.01 * blockData.PNom;

                if (df < 0.02)
                {
                    if (rec.P_fakt < minP)
                        TNoRezerv++;
                    if (rec.P_fakt > maxP)
                        TNoRezerv++;
                }

                if (rec.BadQ > 0)
                    cntBad++;

                if (rec.P_fakt > rec.P_max || rec.P_fakt < rec.P_min)
                    cntBadPower++;

                prevP = rec.P_fakt;
                prevF = rec.F_gc;


            }

            //RHO = calcRHO(dataS);
            calcReact(dataS, fillData);
            NoAutoComment = calcNoAuto(dataS);
            //calcKoleb(dataS, fillData);

        }
        public Dictionary<DateTime, Record> getData()
        {
            return Data;
        }
        public string getText()
        {
            return TextFile;
        }
    }

    public class Record
    {
        public DateTime Date { get; set; }
        public double F_ob { get; set; }
        public double F_gc { get; set; }
        public double P_fakt { get; set; }
        public double P_plan { get; set; }
        public double P_planFull { get; set; }
        public double P_zvn { get; set; }
        public double P_perv { get; set; }
        public double P_min { get; set; }
        public double P_max { get; set; }
        public double X_avg { get; set; }
        public double Y_avg { get; set; }
        public double O { get; set; }
        public double NoReact { get; set; }

        public int BadQ { get; set; }
        public static Random rand = new Random();

        public Record(DateTime dt, string str,double pNom,double fNom, ref List<double> prevPperv, ref List<double> prevPplan, ref List<double> prevPzvn)
        {
            Random r = new Random();
            str = str.Replace(":", ";");
            str = str.Replace(".", ",");
            string[] vals = str.Split(new char[] { ';' });
            int sec = Int32.Parse(vals[0]);
            F_ob = Double.Parse(vals[1]);
            P_fakt = Double.Parse(vals[2]);
            P_plan = Double.Parse(vals[3]);
            //P_plan = Math.Round(P_plan * 10) / 10.0;
            P_zvn = Double.Parse(vals[5]);
            int q = Int32.Parse(vals[4]);
            if (q == 0)
                BadQ = 1;
            F_gc = F_ob / fNom * 50;
            Date = dt.AddSeconds(sec);

            if (F_gc > 49.8)
            {
                double diffF = F_gc - 50;
                if (Math.Abs(diffF) > 0.02)
                {
                    try
                    {
                        double d = F_gc > 50.02 ? F_gc - 50.02 : F_gc - 49.98;
                        P_perv = -2.0 / 5.0 * d*pNom;
                    }
                    catch
                    {
                    }
                }
            }
            //P_fakt = P_plan + P_zvn + P_perv;
            //P_zvn = 0;
            prevPperv.Add(P_perv);
            P_planFull = P_plan + P_zvn + P_perv;

            //P_fakt = P_planFull - P_perv /*+ (r.NextDouble() ) / 10*/;
            P_min = P_plan + P_zvn + prevPperv.Min() - pNom * 0.01;
            P_max = P_plan + P_zvn + prevPperv.Max() + pNom * 0.01;

            prevPplan.Add(P_plan);
            //if (prevPplan.Count > 7)
            //    P_plan = prevPplan[prevPplan.Count - 7];

            prevPzvn.Add(P_zvn);
            //if (prevPzvn.Count > 7)
            //    P_zvn = prevPzvn[prevPzvn.Count - 7];

            if (prevPperv.Count > 30)
                prevPperv.RemoveAt(0);

        }
    }
}

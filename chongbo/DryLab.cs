using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace chongbo
{
    public partial class DryLab : Form
    {

        public DryLab()
        {
            InitializeComponent();
        }
        private void DryLab_Load(object sender, EventArgs e)
        {
            this.Show();
            button1_Click(null,null);            
        }
        private void DryLab_Paint(object sender, PaintEventArgs e)
        {
            drawEcoli(true);
        }
        
        Graphics g;
        //基础参数
        double V1 = 1;
        double u = 0.02;
        double Cperiod = 40;
        double Dperiod = 20;
        double eVolume = 1;//初始体积
        double t = 0;
        int nC = 1;//复制叉数每条染色体
        int nD = 0;//复制叉已导致的分裂进程数
        double[,] eProcess = new double[8, 1];//C期复制进程
        double[,] eDivision = new double[8, 1];//D期分裂进程
        //高级参数
        double delayBlocked = 0;
        double delayRemoved = 0;
        double delayBlockedMax = 2;
        double intervelInitiation = 0;//10最小开叉时间间隔
        double restIntervel = 0;//还需等待最小开叉时间间隔
        //软件参数
        double dt = 1;//以1min为步长
        double freq = 0.1;//以0.1s-1为刷新频率
        //光控策略面板
        bool timingRemoveOrder = false;//倒计时去解除抑制
        double timingRemoveTime = 0;
        bool timingBlockOrder = false;//倒计时去抑制
        double timingBlockTime = 0;
        //蓝牙远程面板

        //预测播报
        //正常期
        double nCmax = 1;//复制叉数量上限Nmax;1.73
        double inferTimeDivision = 60;//推断多久后会发生分裂;
        double minReplication = 0;//最小复制叉的精确进度
        double rangeVolumeRight = 3.32;//体积周期性变化的范围
        double intervalDivision = 34.7;//分裂的时间间隔
        double tr1 = 0;
        double inferTimeSingle = 60;//如果此时阻遏住了所有OriC，多久后会成为无开叉的单倍染色体
        //抑制期
        int nCneed = 1;
        bool recoveryOrder = false;
        double recoveryTime = 0;
        double restDivision = 0;

        bool tr1Order = false;
        double tr1Process = 0;
        //恢复期
        bool oricBlocked = false;//是否已被抑制
        bool oldStateBlocked = false;
        double recoveryProcess = 60;
        double restDivisionNow = 0;

        int orderDivision = 0;
        int stopFlag = 0;
        string reportC;
        string reportD;
        int seriesName = 0;
        int seriesNameMax = 0;
        //string reportMask="[细菌尚未恢复正常]";

        bool oricBlockOrder = false;//是否已开灯
        double oricBlockProcess = 0;//已响应的进度

        //保存读取
        bool saveOrder = false;

        // 示意图
        int pX = 340;//340
        int pY = 180;
        int EcoliWidth = 75;
        void drawEcoli(bool permisson)
        {
            try
            {
                g = this.CreateGraphics();
                g.Clear(this.BackColor);
                Pen p = new Pen(Color.FromArgb(205, 133, 63));
                int shenchang = (int)((eVolume * 100) / 2);
                if (2 * shenchang < 1073742080)
                    g.DrawRectangle(p, pX - shenchang, pY, 2 * shenchang, EcoliWidth);//1073742080
                int eLeft = pX - shenchang;
                int eRight = pX + shenchang;
                //染色体行为
                if (nD == 0)
                {
                    g.DrawEllipse(p, pX - 10, pY + EcoliWidth / 2 - 10, 20, 20);
                }
                else
                {
                    for (int i = 1; i <= nD; i++)
                    {
                        int r = 0;
                        for (int j = 1; j <= Math.Pow(2, i - 1); j++)
                        {
                            g.DrawEllipse(p, (int)(pX - (shenchang / 2 * eDivision[0, 0]) - 10) - r, pY + EcoliWidth / 2 - 10, 20, 20);
                            g.DrawEllipse(p, (int)(pX + (shenchang / 2 * eDivision[0, 0]) - 10) + r, pY + EcoliWidth / 2 - 10, 20, 20);
                            r = r + 5;
                        }
                    }
                }
                //赤道板行为
                if (eDivision[0, 0] > 0.25)
                {
                    Pen p2 = new Pen(Color.FromArgb(255 - (int)((255 - 205) * eDivision[0, 0]), 255 - (int)((255 - 133) * eDivision[0, 0]), 255 - (int)((255 - 63) * eDivision[0, 0])));
                    g.DrawLine(p2, pX, pY, pX, pY + EcoliWidth);
                }
                //释放
                g.Dispose();
            }
            catch (System.ObjectDisposedException)
            {
                //MessageBox.Show("asswecan");
                System.Environment.Exit(0);//黑科技，全都清除
                //return;//如果主界面已经退出了，那线程也退出好了。
            }
        }

        //开始按钮
        private void button1_Click(object sender, EventArgs e)
        {
            button4.Focus();//焦点置于暂定按钮
            stopFlag = 0;
            while (stopFlag != 1)
            {
                
                reportC = String.Empty;
                reportD = String.Empty;

                //定时光控解除
                if (oricBlockOrder == true)//蓝光已开
                {
                    if (timingRemoveOrder == true)//定时关灯已被启动
                    {
                        timingRemoveTime -= dt;
                        if (timingRemoveTime <= 0)//倒计时
                        {
                            timingRemoveTime = Convert.ToDouble(this.textBox10.Text);//填满下一次倒计时
                            button6_Click(null, null);//关闭蓝光
                        }
                    }
                }
                else//蓝光已灭
                {
                    if (timingBlockOrder == true)//定时开灯已被启动
                    {
                        timingBlockTime -= dt;
                        if (timingBlockTime <= 0)//倒计时
                        {
                            timingBlockTime = Convert.ToDouble(this.textBox11.Text);//填满下一次倒计时
                            button6_Click(null, null);//开启蓝光
                        }
                    }
                }

                //延迟阻抑进度条
                oricBlocked = false;//不触发完全阻抑事件
                if (oricBlockOrder == true)
                {
                    if (delayBlocked == 0)//如果延迟为0，则瞬间完成
                    {
                        oricBlockProcess = 1 * delayBlockedMax;
                    }
                    else//如果延迟不为零，则增加进度
                    {
                        oricBlockProcess += dt / delayBlocked;
                    }
                }
                else
                {
                    if (delayRemoved == 0)
                    {
                        oricBlockProcess = 0;
                    }
                    else
                    {
                        oricBlockProcess -= dt / delayRemoved;
                    }
                    if (oricBlockProcess <= 0)
                    {
                        oricBlockProcess = 0;
                    }
                }
                if (oricBlockProcess >= 1)
                {
                    oricBlocked = true;//触发完全阻抑事件
                    if (oricBlockProcess >= 1 * delayBlockedMax)
                    {
                        oricBlockProcess = 1 * delayBlockedMax;
                    }
                }

                //判断抑制期to恢复期//异常状态下，要先清算nC的债务，再开叉还债
                if (nC < nCneed)//nC没有得到满足，光控对OriC已造成实质影响
                {
                    if (oldStateBlocked == true && oricBlocked == false)//刚刚解除OriC阻抑
                    {
                        recoveryOrder = true;
                        recoveryProcess = recoveryTime;//每次发生解除事件都重新装填倒计时
                        restDivisionNow = restDivision;//重新装填所需分裂数,仅用于播报
                    }
                }
                //判断恢复期to正常期
                if (recoveryOrder == true)
                {
                    recoveryProcess -= dt;
                    if (recoveryProcess <= 0)
                    {
                        recoveryOrder = false;
                    }
                }

                //判断正常期to抑制期//从非恢复期的正常期to抑制期
                if (recoveryOrder == false)
                {
                    if (oldStateBlocked == false && oricBlocked == true)
                    {
                        tr1Order = true;
                        tr1Process = tr1;//每次发生阻遏事件都重新装填倒计时
                    }
                }
                //判断抑制期Ato抑制期B
                if (tr1Order == true)
                {
                    tr1Process -= dt;
                    if (tr1Process <= 0)
                    {
                        tr1Order = false;
                    }
                }


                //体积增加
                eVolume = eVolume * Math.Exp(u * dt);
                Console.Write(eVolume);
                Console.Write("\n");
                //溢出提示
                if (eVolume > 1e+27)
                {
                    MessageBox.Show("The volume of your bacteria has reached One billion cubic meters. The value is too extreme, system resets the parameters automatically for you");//您的细菌体积已达10亿立方米，数值过于极端，已为您自动重置参数
                    button5_Click(null, null);
                }

                restIntervel -= dt;//最小开叉间隔倒计时
                //判定是否开新叉
                if (eVolume / (Math.Pow(2, nC)) >= V1)
                {
                    if (oricBlocked == false)
                    {
                        if (restIntervel <= 0)
                        {
                            nC = nC + 1;
                            restIntervel = intervelInitiation;
                        }
                    }
                }

                //动态设置进度条数组行数
                if (nC > eProcess.GetLength(0))
                {
                    /*
                    if (nC > 20)
                    {
                        MessageBox.Show("您的细菌已开启了20次复制叉，共1048576个OriC，数值过于极端，已为您自动重置参数");
                        button5_Click(null,null);
                    }
                    */
                    double[,] eProcessShadow = eProcess;
                    double[,] eDivisionShadow = eDivision;
                    int eProcessShadowLen = eProcessShadow.Length;
                    eProcess = new double[nC, 1];//C期复制进程
                    eDivision = new double[nC, 1];//D期分裂进程
                    if (eProcessShadow.Length > eProcess.Length)
                    {
                        eProcessShadowLen = eProcess.Length;
                    }
                    Array.Copy(eProcessShadow, 0, eProcess, 0, eProcessShadowLen);
                    Array.Copy(eDivisionShadow, 0, eDivision, 0, eProcessShadowLen);
                }


                //进度条增加
                nD = 0;
                for (int j = 0; j < nC; j++)
                {
                    eProcess[j, 0] = eProcess[j, 0] + dt / Cperiod;
                    if (eProcess[j, 0] >= 1)
                    {
                        eProcess[j, 0] = 1;
                        nD = nD + 1;
                    }
                    reportC = reportC + String.Format("{0} round:{1:N1}%\n", j + 1, 100 * eProcess[j, 0]);
                    //Console.Write("第{0}个复制叉的进度为{1:N1}%", j + 1, 100 * eProcess[j, 0]);
                    //Console.Write("\n");
                }

                //判定分裂倒计时
                orderDivision = 0;
                for (int k = 0; k < nD; k++)
                {
                    if (eProcess[k, 0] >= 1)//判断C期已完成才进展D期
                    {
                        eDivision[k, 0] = eDivision[k, 0] + dt / Dperiod;
                        if (eDivision[k, 0] >= 1)
                        {
                            orderDivision = orderDivision + 1;
                        }
                        reportD = reportD + String.Format("dividing: {0:N1}%\n", 100 * eDivision[k, 0]);
                        //Console.Write("第{0}个复制叉所致分裂的进度为{1:N1}%", k + 1, 100 * eDivision[k, 0]);
                        //Console.Write("\n");
                    }
                }

                //分裂
                if (orderDivision > 0)
                {
                    //体积减半
                    eVolume = eVolume / 2;
                    //进程减一
                    nC = nC - 1;
                    nD = nD - 1;
                    //进程继承
                    //Console.Write("hhh{0}\n", eProcess.GetLength(0));
                    for (int i = 0; i < eProcess.GetLength(0) - 1; i++)
                    {
                        eProcess[i, 0] = eProcess[i + 1, 0];
                        eDivision[i, 0] = eDivision[i + 1, 0];
                    }
                    eProcess[eProcess.GetLength(0) - 1, 0] = 0;
                    eDivision[eDivision.GetLength(0) - 1, 0] = 0;

                    orderDivision = orderDivision - 1;

                    //预测解除抑制所需的分裂数量减一
                    if (recoveryOrder == true)
                    {
                        restDivisionNow -= 1;
                    }                        
                }

                //运算全部完成，开始输出结果
                //Console.Write("=========================\n");                                          
                
                //记录本次抑制状态
                oldStateBlocked = oricBlocked;

                //对本次结果进行预测播报
                nCmax = Math.Ceiling(u * (Cperiod + Dperiod) / Math.Log(2));//复制叉数量上限Nmax;1.73   
                nCneed = (int)Math.Ceiling(Math.Log(eVolume / V1) / Math.Log(2));//当前体积所需要的复制叉数量
                inferTimeDivision = Cperiod + Dperiod - Math.Log(eVolume / V1) / u;//推断多久后会发生分裂;
                if (eVolume < V1)
                {
                    minReplication = 0;//体积小则不适用
                }
                else
                {
                    minReplication = Math.Log(eVolume / V1) / u / Cperiod - Math.Floor(Math.Log(eVolume / V1) / Math.Log(2)) * Math.Log(2) / u / Cperiod;
                }
                inferTimeSingle = Cperiod + Dperiod - Cperiod * minReplication;//如果此时阻遏住了所有OriC，多久后会成为无开叉的单倍染色体
                rangeVolumeRight = V1 * Math.Exp(u * (Cperiod + Dperiod));//体积周期性变化的范围
                intervalDivision = Math.Log(2) / u;//分裂的时间间隔
                tr1 = intervalDivision - minReplication * Cperiod;
                if (tr1Order == true)
                {
                    restDivision = 0;
                    recoveryTime = 0;
                }
                else
                {
                    restDivision = nC + Math.Ceiling((Math.Log(eVolume / V1) / u - nC * intervalDivision) / (intervalDivision - intervelInitiation));//若此时解除，为了还债要分裂几次（Z+n）
                    recoveryTime = Cperiod + Dperiod + intervelInitiation * (restDivision - nC -1);//若此时解除，要多久才正常
                }

                //输出给状态统计版
                if (oricBlocked == true)
                {
                    label40.Text = "YES";
                }
                else
                {
                    label40.Text = "NO";
                }
                //单细菌体积
                this.label1.Text = eVolume.ToString("#0.00");
                //C进度
                this.label12.Text = reportC;
                //D进度
                this.label14.Text = reportD;
                //时间步长
                this.label16.Text = dt.ToString() + " min (model)";

                //生长系数
                this.label7.Text = u.ToString();
                //C期
                this.label8.Text = Cperiod.ToString() + " min";
                //D期
                this.label9.Text = Dperiod.ToString() + " min";
                //V1
                this.label10.Text = V1.ToString();

                //输出给实时体积图表
                this.chart1.Series[seriesName].Points.AddXY(t, eVolume);
                Application.DoEvents();

                //输出给预测面板              
                if (oricBlocked == false && recoveryOrder == false)//细菌正常-既不是抑制期也不是恢复期
                {
                    this.label31.Text = "Maximum replication round " + nCmax.ToString();//复制叉数量上限
                    this.label32.Text = "Next division remains " + inferTimeDivision.ToString("#0.00" + " min");//推断多久后会发生分裂
                    this.label33.Text = "Lastest replicaiton process " + (minReplication * 100).ToString("#0.00") + "%";
                    this.label35.Text = "Volume range " + "[" + (rangeVolumeRight / 2).ToString("#0.00") + "," + rangeVolumeRight.ToString("#0.00") + "]";
                    this.label36.Text = "Division interval time " + intervalDivision.ToString("#0.00") + " min";

                    this.groupBox2.Text = "If block OriC now";
                    this.label44.Text = "Affect initiation after " + tr1.ToString("#0.00") + " min"; //多久后细菌受实质影响
                    this.label34.Text = "Become monoploid after " + inferTimeSingle.ToString("#0.00") + " min";//多久后变成单倍体
                }
                else//细菌异常-抑制期或恢复期
                {
                    //31
                    //32
                    this.label33.Text = "Required replication round " + nCneed.ToString();//当前体积需要具有的复制叉数
                    //35
                    this.label36.Text = "";//process of 单倍体
                    //groupBox2
                    //44
                    //34
                    if (oricBlocked == true)//抑制期
                    {
                        this.label31.Text = "[Blocked stage]";//细菌正处于[抑制期]
                        if (tr1Order == false)
                        {
                            this.label32.Text = "[Already affect initiation ]";//已产生实质影响
                            this.label35.Text = "";
                        }
                        else
                        {
                            this.label32.Text = "[Not yet affect initiation]";//尚未导致实质影响
                            this.label35.Text = "Affect initiation need " + tr1Process.ToString("#0.00") + " min";//还有多久细菌受实质影响
                        }
                        this.groupBox2.Text = "If release OriC now";//若此时解除OriC
                        this.label44.Text = "Recover to normal after " + recoveryTime.ToString() + " min";//恢复正常需要多久
                        this.label34.Text = "Recover to normal needs division number " + restDivision.ToString();//内存次数+还债次数
                    }
                    else//恢复期
                    {
                        this.label31.Text = "[Recovering]"; //细菌正处于[恢复期]
                        this.label32.Text = "";
                        this.label35.Text = "Recover to normal after " + recoveryProcess.ToString() + " min";//还有多久恢复正常
                        this.label36.Text = "Recover to normal needs division number " + restDivisionNow.ToString();//还有几次分裂恢复正常
                        this.groupBox2.Text = "";//有待思考
                        this.label34.Text = "";//有待思考
                        this.label44.Text = "";//有待思考
                    }
                }
                //绘制示意图
                drawEcoli(true);

                //输出完成，等待进入下一秒
                //Console.Write("=========================\n");
                System.Threading.Thread.Sleep((int)(1000 * freq));//1000:1秒
                t = t + dt;
            }
        }
        //暂停按钮
        private void button4_Click(object sender, EventArgs e)
        {
            stopFlag = 1;
            button1.Focus();//焦点置于开始按钮
        }
        //跟踪条-软件速度
        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            switch (trackBar1.Value)
            {
                case 0://最左
                    freq = 1;
                    break;
                case 1:
                    freq = 0.1;
                    break;
                case 2:
                    freq = 0.01;
                    break;
                case 3://最右
                    freq = 0.001;
                    break;
            }
        }

        //重置按钮
        private void button5_Click(object sender, EventArgs e)
        {   

            //重置基础参数
            V1 = 1;
            u = 0.02;
            Cperiod = 40;
            Dperiod = 20;
            eVolume = 1;//初始体积
            t = 0;
            nC = 1;//复制叉数每条染色体
            nD = 0;//复制叉已导致的分裂进程数
            for (int i = 0; i < eProcess.GetLength(0); i++)
            {
                for (int j = 0; j < eProcess.GetLength(1); j++)
                {
                    eProcess[i, j] = 0;//C期复制进程
                    eDivision[i, j] = 0;//D期分裂进程
                }
            }
            //重置高级参数
            delayBlocked = 0;
            delayRemoved = 0;
            delayBlockedMax = 2;
            intervelInitiation = 0;//10
            restIntervel = 0;//还需等待最小开叉时间间隔
            //重置软件参数
            dt = 1;//以1min为步长
            freq = 0.1;//以0.1秒为间隔刷新
            //光控策略面板
            timingRemoveOrder = false;//倒计时去解除抑制
            timingRemoveTime = 0;
            timingBlockOrder = false;//倒计时去抑制
            timingBlockTime = 0;
            //蓝牙远程面板

            //预测播报
            //正常期
            nCmax = 1;//复制叉数量上限Nmax;1.73            
            inferTimeDivision = 60;//推断多久后会发生分裂;
            minReplication = 0;//最小复制叉的精确进度            
            rangeVolumeRight = 3.32;//体积周期性变化的范围
            intervalDivision = 34.7;//分裂的时间间隔
            tr1 = 0;
            inferTimeSingle = 60;//如果此时阻遏住了所有OriC，多久后会成为无开叉的单倍染色体                     
            //抑制期                     
            nCneed = 1;
            recoveryOrder = false;
            recoveryTime = 0;
            restDivision = 0;

            tr1Order = false;
            tr1Process = 0;
            //恢复期
            oricBlocked = false;//是否已被抑制
            oldStateBlocked = false;
            recoveryProcess = 60;
            restDivisionNow = 0;

            orderDivision = 0;
            //stopFlag = 0;

            oricBlockOrder = false;//是否已开灯
            button6.Text = "Block OriC";
            oricBlockProcess = 0;//已响应的进度

                        
            //重置跟踪条
            trackBar1.Value = 1;
            //重置勾选
            checkBox1.Checked = false;
            checkBox2.Checked = false;
            //重置画板
            button3_Click(null, null);
            drawEcoli(true);
        }

        //清屏按钮
        private void button3_Click(object sender, EventArgs e)
        {
            for (int i = 0; i <= seriesName;i++)
            {
                this.chart1.Series[i].Points.Clear();
            }
            seriesName = 0;
        }
        
        //阻抑或解除阻抑按钮
        private void button6_Click(object sender, EventArgs e)
        {
            if (oricBlockOrder == false)
            {
                oricBlockOrder = true;
                button6.Text = "Release OriC";

                //发送蓝牙命令
                if (checkBox3.Checked == true)
                {
                    int xNum = 255;
                    string x = ((char)xNum).ToString();
                    blueconnect("ABC" + x + x + x + x + x + x + x + x + x + x + x + x + x + x + x + x);
                }

                //以切换为手动调控，重置自动调控参数
                timingRemoveTime = Convert.ToDouble(this.textBox10.Text);//重新装填倒计时
            }
            else
            {
                oricBlockOrder = false;
                button6.Text = "Block OriC";

                //发送蓝牙命令
                if (checkBox3.Checked == true)
                {
                    int xNum = 0;
                    string x = ((char)xNum).ToString();
                    blueconnect("ABC" + x + x + x + x + x + x + x + x + x + x + x + x + x + x + x + x);
                }

                //以切换为手动调控，重置自动调控参数
                timingBlockTime = Convert.ToDouble(this.textBox11.Text);//重新装填倒计时
            }
        }

        //基础参数设置按钮
        private void button2_Click(object sender, EventArgs e)
        {
            u = Convert.ToDouble(this.textBox1.Text);
            V1 = Convert.ToDouble(this.textBox2.Text);
            Cperiod = Convert.ToDouble(this.textBox3.Text);
            Dperiod = Convert.ToDouble(this.textBox4.Text);
            if (u > Math.Log(2))
            {
                MessageBox.Show("We don't recommend you to set this value greater than ln2. It will make data too big for computer to process.");//我们不建议你设置该参数大于ln2，这会导致生长过快而程序来不及处理
            }
            if (intervelInitiation > Math.Log(2) / u)
            {
                MessageBox.Show("Min. initiation interval \"d\" greater than ln2/u! Bacteria will not recover! ");//最小开叉间隔d大于ln2/u！细菌将无法恢复正常！
            }
            Application.DoEvents();
        }
        //高级参数设置按钮
        private void button9_Click(object sender, EventArgs e)
        {
            delayBlocked = Convert.ToDouble(this.textBox6.Text);
            delayRemoved = Convert.ToDouble(this.textBox7.Text);
            delayBlockedMax = Convert.ToDouble(this.textBox8.Text);
            intervelInitiation = Convert.ToDouble(this.textBox9.Text);
            if (intervelInitiation > Math.Log(2) / u)
            {
                MessageBox.Show("Min. initiation interval \"d\" greater than ln2/u! Bacteria will not recover!");//最小开叉间隔d大于ln2/u！细菌将无法恢复正常！
            }
        }
        //软件参数设置按钮
        private void button8_Click(object sender, EventArgs e)
        {
            freq = Convert.ToDouble(this.textBox5.Text);
        }

        //光控面板-每次定时解除
        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox1.Checked == true)
            {
                timingRemoveTime = Convert.ToDouble(this.textBox10.Text);//重新装填倒计时
                if (timingRemoveTime > 0)
                {
                    timingRemoveOrder = true;
                }
                else//设置负数就当无事发生过
                {
                    timingRemoveOrder = false;
                    checkBox1.Checked = false;
                    MessageBox.Show("Please make sure strategy parameter is positive first!");//请先设置时长为正数
                }
            }
            else
            {
                timingRemoveOrder = false;
            }
        }
        //光控面板-每次定时抑制
        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox2.Checked == true)
            {
                timingBlockTime = Convert.ToDouble(this.textBox11.Text);//重新装填倒计时
                if (timingBlockTime > 0)
                {
                    timingBlockOrder = true;
                }
                else//设置负数就当无事发生过
                {
                    timingBlockOrder = false;
                    checkBox2.Checked = false;
                    MessageBox.Show("Please make sure strategy parameter is positive first!");//请先设置时长为正数
                }
            }
            else
            {
                timingBlockOrder = false;
            }
        }
        //光控面板-更新参数
        private void button7_Click(object sender, EventArgs e)
        {
            timingRemoveTime = Convert.ToDouble(this.textBox10.Text);
            timingBlockTime = Convert.ToDouble(this.textBox11.Text);
            if (timingRemoveTime < 0)
            {
                timingRemoveTime = 0;
                this.textBox10.Text = timingRemoveTime.ToString();
            }
            if (timingBlockTime < 0)
            {
                timingBlockTime = 0;
                this.textBox11.Text = timingBlockTime.ToString();
            }
        }
        //蓝牙面板-蓝牙端口名称设置
        private void comboBox1_DropDown(object sender, EventArgs e)
        {
            string[] ports = System.IO.Ports.SerialPort.GetPortNames();
            Array.Sort(ports);
            comboBox1.Items.Clear();
            comboBox1.Items.AddRange(ports);
        }
        //串口方法
        public void blueconnect(string x)//, string str2
        {
            try
            {
                serialPort1.PortName = comboBox1.SelectedItem.ToString();
                serialPort1.Open();
                //serialPort1.Encoding = System.Text.Encoding.GetEncoding("GB2312");
                //byte[] data = Encoding.Unicode.GetBytes(x);
                //string str = Convert.ToBase64String(data);
                //serialPort1.WriteLine(str);
                //string str = str1;// + str2
                serialPort1.WriteLine(x);
                serialPort1.Close();
                //MessageBox.Show("数据发送成功！", "系统提示");
                label41.Text = x.ToString();// str;//str2
            }
            catch (System.NullReferenceException)
            {
                MessageBox.Show("There are no more serial port available to Bluetooth", "Warning");//蓝牙未配置可用串口
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message.ToString(), "Warning");
            }
        }

        //保存读取
        //基础参数
        double saveV1 = 0;
        double saveu = 0;
        double saveCperiod = 0;
        double saveDperiod = 0;
        double saveeVolume = 0;//初始体积
        double savet = 0;
        int savenC = 0;//复制叉数每条染色体
        int savenD = 0;//复制叉已导致的分裂进程数
        double[,] saveeProcess = new double[8, 1];//C期复制进程
        double[,] saveeDivision = new double[8, 1];//D期分裂进程
        //高级参数
        double savedelayBlocked = 0;
        double savedelayRemoved = 0;
        double savedelayBlockedMax = 0;
        double saveintervelInitiation = 0;//10最小开叉时间间隔
        double saverestIntervel = 0;//还需等待最小开叉时间间隔
                                               //光控策略面板
        bool savetimingRemoveOrder = false;//倒计时去解除抑制
        double savetimingRemoveTime = 0;
        bool savetimingBlockOrder = false;//倒计时去抑制
        double savetimingBlockTime = 0;

        //预测播报
        //正常期
        double savenCmax = 0;//复制叉数量上限Nmax;1.73
        double saveinferTimeDivision = 0;//推断多久后会发生分裂;
        double saveminReplication = 0;//最小复制叉的精确进度
        double saverangeVolumeRight = 0;//体积周期性变化的范围
        double saveintervalDivision = 0;//分裂的时间间隔
        double savetr1 = 0;
        double saveinferTimeSingle = 0;//如果此时阻遏住了所有OriC，多久后会成为无开叉的单倍染色体
                                                     //抑制期
        int savenCneed = 0;
        bool saverecoveryOrder = true;
        double saverecoveryTime = 0;
        double saverestDivision = 0;
        //恢复期
        bool saveoricBlocked = false;//是否已被抑制
        bool saveoldStateBlocked = false;
        double saverecoveryProcess = 0;
        double saverestDivisionNow = 0;

        bool savetr1Order = false;
        double savetr1Process = 0;

        int saveorderDivision = 0;

        bool saveoricBlockOrder = false;//是否已开灯
        double saveoricBlockProcess = 0;//已响应的进度
        //其他
        string savebutton6 = "NaN";//待赋值
        bool savecheckBox1Checked = false;
        bool savecheckBox2Checked = false;
        private void button11_Click(object sender, EventArgs e)
        {
            if (saveOrder == false)
            {
                saveOrder = true;
                button11.Size = new System.Drawing.Size(96, 27);
                button11.Text = "Load " + t.ToString() + " min";//读取
                //基础参数
                saveV1 = V1;
                saveu = u;
                saveCperiod = Cperiod;
                saveDperiod = Dperiod;
                saveeVolume = eVolume;//初始体积
                savet = t;
                savenC = nC;//复制叉数每条染色体
                savenD = nD;//复制叉已导致的分裂进程数
                if (eProcess.Length > 8)
                {
                    saveeProcess = new double[eProcess.Length, 1];//C期复制进程
                    saveeDivision = new double[eProcess.Length, 1];//D期分裂进程
                }
                Array.Copy(eProcess, 0, saveeProcess, 0, eProcess.Length);
                Array.Copy(eDivision, 0, saveeDivision, 0, eProcess.Length);
                //高级参数
                savedelayBlocked = delayBlocked;
                savedelayRemoved = delayRemoved;
                savedelayBlockedMax = delayBlockedMax;
                saveintervelInitiation = intervelInitiation;//10最小开叉时间间隔
                saverestIntervel = restIntervel;//还需等待最小开叉时间间隔
                //光控策略面板
                savetimingRemoveOrder = timingRemoveOrder;//倒计时去解除抑制
                savetimingRemoveTime = timingRemoveTime;
                savetimingBlockOrder = timingBlockOrder;//倒计时去抑制
                savetimingBlockTime = timingBlockTime;

                //预测播报
                //正常期
                savenCmax = nCmax;//复制叉数量上限Nmax;1.73
                saveinferTimeDivision = inferTimeDivision;//推断多久后会发生分裂;
                saveminReplication = minReplication;//最小复制叉的精确进度
                saverangeVolumeRight = rangeVolumeRight;//体积周期性变化的范围
                saveintervalDivision = intervalDivision;//分裂的时间间隔
                savetr1 = tr1;
                saveinferTimeSingle = inferTimeSingle;//如果此时阻遏住了所有OriC，多久后会成为无开叉的单倍染色体
                //抑制期
                savenCneed = nCneed;
                saverecoveryOrder = recoveryOrder;
                saverecoveryTime = recoveryTime;
                saverestDivision = restDivision;

                savetr1Order = tr1Order;
                savetr1Process = tr1Process;
                //恢复期
                saveoricBlocked = oricBlocked;//是否已被抑制
                saveoldStateBlocked = oldStateBlocked;
                saverecoveryProcess = recoveryProcess;
                saverestDivisionNow = restDivisionNow;

                saveorderDivision = orderDivision;

                saveoricBlockOrder = oricBlockOrder;//是否已开灯
                saveoricBlockProcess = oricBlockProcess;//已响应的进度
                //其他
                savebutton6 = button6.Text;
                savecheckBox1Checked = checkBox1.Checked;
                savecheckBox2Checked = checkBox2.Checked;
            }
            else
            {
                saveOrder = false;
                button11.Size = new System.Drawing.Size(56, 27);
                button11.Text = "Save";//保存
                seriesName += 1;
                if (seriesName > seriesNameMax)
                {
                    chart1.Series.Add(seriesName.ToString());//添加
                    chart1.Series[seriesName].ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Point;
                    seriesNameMax = seriesName;
                }                
                //chart1.Series[seriesName].Name= "细菌当前体积";
                //chart1.Series[seriesName].MarkerColor = Color.Green;
                //chart1.Series[0].Color = Color.Red;
                //基础参数
                V1 = saveV1;
                u = saveu;
                Cperiod = saveCperiod;
                Dperiod = saveDperiod;
                eVolume = saveeVolume;//初始体积
                t = savet;
                nC = savenC;//复制叉数每条染色体
                nD = savenD;//复制叉已导致的分裂进程数
                eProcess = new double[saveeProcess.Length, 1];//C期复制进程
                eDivision = new double[saveeProcess.Length, 1];//D期分裂进程
                Array.Copy(saveeProcess, 0, eProcess, 0, saveeProcess.Length);
                Array.Copy(saveeDivision, 0, eDivision, 0, saveeProcess.Length);
                //高级参数
                delayBlocked = savedelayBlocked;
                delayRemoved = savedelayRemoved;
                delayBlockedMax = savedelayBlockedMax;
                intervelInitiation = saveintervelInitiation;//10最小开叉时间间隔
                restIntervel = saverestIntervel;//还需等待最小开叉时间间隔
                //光控策略面板
                timingRemoveOrder = savetimingRemoveOrder;//倒计时去解除抑制
                timingRemoveTime = savetimingRemoveTime;
                timingBlockOrder = savetimingBlockOrder;//倒计时去抑制
                timingBlockTime = savetimingBlockTime;

                //预测播报
                //正常期
                nCmax = savenCmax;//复制叉数量上限Nmax;1.73
                inferTimeDivision = saveinferTimeDivision;//推断多久后会发生分裂;
                minReplication = saveminReplication;//最小复制叉的精确进度
                rangeVolumeRight = saverangeVolumeRight;//体积周期性变化的范围
                intervalDivision = saveintervalDivision;//分裂的时间间隔
                tr1 = savetr1;
                inferTimeSingle = saveinferTimeSingle;//如果此时阻遏住了所有OriC，多久后会成为无开叉的单倍染色体
                //抑制期
                nCneed = savenCneed;
                recoveryOrder = saverecoveryOrder;
                recoveryTime = saverecoveryTime;
                restDivision = saverestDivision;

                tr1Order = savetr1Order;
                tr1Process = savetr1Process;
                //恢复期
                oricBlocked = saveoricBlocked;//是否已被抑制
                oldStateBlocked = saveoldStateBlocked;
                recoveryProcess = saverecoveryProcess;
                restDivisionNow = saverestDivisionNow;

                orderDivision = saveorderDivision;

                oricBlockOrder = saveoricBlockOrder;//是否已开灯
                oricBlockProcess = saveoricBlockProcess;//已响应的进度
                //其他
                button6.Text = savebutton6;
                checkBox1.Checked = savecheckBox1Checked;
                checkBox2.Checked = savecheckBox2Checked;
            }
        }

        //快捷键
        private void DryLab_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == 'S'|| e.KeyChar == 's')
            {
                if (stopFlag == 0)
                {
                    button4_Click(null, null);// 执行暂停按钮
                }
                else
                {
                    button1_Click(null, null);// 执行开始按钮
                }
                e.Handled = true;
            }
        }

        //show example
        private void button10_Click(object sender, EventArgs e)
        {
            bool showFlag = true;
            try
            {
                serialPort1.PortName = comboBox1.SelectedItem.ToString();
                serialPort1.Open();
                //serialPort1.Encoding = System.Text.Encoding.GetEncoding("GB2312");
                //byte[] data = Encoding.Unicode.GetBytes(x);
                //string str = Convert.ToBase64String(data);
                //serialPort1.WriteLine(str);
                //string str = str1;// + str2
                serialPort1.WriteLine("hhh");
                serialPort1.Close();
            }
            catch (System.NullReferenceException)
            {
                showFlag = false;
                //MessageBox.Show("There are no more serial port available to Bluetooth", "Warning");//蓝牙未配置可用串口
            }
            if (showFlag == true)
            {
                int k = 0;
                while (k < 2)
                {
                    for (int i = 0; i < 10; i++)
                    {
                        string x = ((char)(10 * i)).ToString();
                        blueconnect("ABC" + x + x + x + x + x + x + x + x + x + x + x + x + x + x + x + x);//t.ToString() + " min ", "暴风城的将士们，开启所有灯泡吧"
                        System.Threading.Thread.Sleep((int)(10));
                    }
                    for (int j = 10; j >= 0; j--)
                    {
                        string y = ((char)(10 * j)).ToString();
                        blueconnect("ABC" + y + y + y + y + y + y + y + y + y + y + y + y + y + y + y + y);//t.ToString() + " min ", "暴风城的将士们，开启所有灯泡吧"
                        System.Threading.Thread.Sleep((int)(10));
                    }
                    k = k + 1;
                }
                string d = ((char)0).ToString();
                string l = ((char)255).ToString();
                //blueconnect("ABC" + d + d + d + d + l + l + l + l + d + d + d + d + l + l + l + l);//t.ToString() + " min ", "暴风城的将士们，开启所有灯泡吧"                                                                    //blueconnect("ABC" + y + y + y + y + y + y + y + y + y + y + y + y + y + y + y + y);//t.ToString() + " min ", "暴风城的将士们，开启所有灯泡吧"
                //System.Threading.Thread.Sleep((int)(1000));
                k = 0;
                while (k < 2)
                {
                    //blueconnect("ABC" + l + "#" + l + "#" + l + "#" + l + "#" + "#" + l + "#" + l + "#" + l + "#" + l);
                    blueconnect("ABC" + d + "#" + l + "#" + d + "#" + l + "#" + "#" + d + "#" + l + "#" + d + "#" + l);
                    System.Threading.Thread.Sleep((int)(1000));
                    blueconnect("ABC" + l + "#" + d + "#" + l + "#" + d + "#" + "#" + l + "#" + d + "#" + l + "#" + d);
                    System.Threading.Thread.Sleep((int)(1000));
                    k = k + 1;
                    blueconnect("ABC" + l + "#" + d + "#" + d + "#" + d + "#" + "#" + l + "#" + d + "#" + d + "#" + d);
                    System.Threading.Thread.Sleep((int)(100));
                    blueconnect("ABC" + d + "#" + l + "#" + d + "#" + d + "#" + "#" + d + "#" + l + "#" + d + "#" + d);
                    System.Threading.Thread.Sleep((int)(100));
                    blueconnect("ABC" + d + "#" + d + "#" + l + "#" + d + "#" + "#" + d + "#" + d + "#" + l + "#" + d);
                    System.Threading.Thread.Sleep((int)(100));
                    blueconnect("ABC" + d + "#" + d + "#" + d + "#" + l + "#" + "#" + d + "#" + d + "#" + d + "#" + l);
                    System.Threading.Thread.Sleep((int)(100));

                    blueconnect("ABC" + l + "#" + d + "#" + d + "#" + d + "#" + "#" + d + "#" + d + "#" + d + "#" + l);
                    System.Threading.Thread.Sleep((int)(100));
                    blueconnect("ABC" + d + "#" + l + "#" + d + "#" + d + "#" + "#" + d + "#" + d + "#" + l + "#" + d);
                    System.Threading.Thread.Sleep((int)(100));
                    blueconnect("ABC" + d + "#" + d + "#" + l + "#" + d + "#" + "#" + d + "#" + l + "#" + d + "#" + d);
                    System.Threading.Thread.Sleep((int)(100));
                    blueconnect("ABC" + d + "#" + d + "#" + d + "#" + l + "#" + "#" + l + "#" + d + "#" + d + "#" + d);
                    System.Threading.Thread.Sleep((int)(100));

                    blueconnect("ABC" + d + "#" + d + "#" + d + "#" + l + "#" + "#" + l + "#" + d + "#" + d + "#" + d);
                    System.Threading.Thread.Sleep((int)(100));
                    blueconnect("ABC" + d + "#" + d + "#" + l + "#" + d + "#" + "#" + d + "#" + l + "#" + d + "#" + d);
                    System.Threading.Thread.Sleep((int)(100));
                    blueconnect("ABC" + d + "#" + l + "#" + d + "#" + d + "#" + "#" + d + "#" + d + "#" + l + "#" + d);
                    System.Threading.Thread.Sleep((int)(100));
                    blueconnect("ABC" + l + "#" + d + "#" + d + "#" + d + "#" + "#" + d + "#" + d + "#" + d + "#" + l);
                    System.Threading.Thread.Sleep((int)(100));

                    blueconnect("ABC" + d + "#" + d + "#" + d + "#" + d + "#" + "#" + d + "#" + d + "#" + d + "#" + d);
                }
            }
        }
    }
}
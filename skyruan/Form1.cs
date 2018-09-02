using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Text;
using System.Windows.Forms;
using System.Management;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Threading;
using System.ServiceProcess;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using System.Net.Sockets;

namespace skyruan
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        public enum WMIPath
        {
            Win32_OperatingSystem
        }

        public class SystemInfo
        {
            private int m_ProcessorCount = 0;  
            private PerformanceCounter pcCpuLoad;  
            private long m_PhysicalMemory = 0;  
            private const int GW_HWNDFIRST = 0;
            private const int GW_HWNDNEXT = 2;
            private const int GWL_STYLE = (-16);
            private const int WS_VISIBLE = 268435456;
            private const int WS_BORDER = 8388608;
            [DllImport("IpHlpApi.dll")]
            extern static public uint GetIfTable(byte[] pIfTable, ref uint pdwSize, bool bOrder);

            [DllImport("User32")]
            private extern static int GetWindow(int hWnd, int wCmd);

            [DllImport("User32")]
            private extern static int GetWindowLongA(int hWnd, int wIndx);

            [DllImport("user32.dll")]
            private static extern bool GetWindowText(int hWnd, StringBuilder title, int maxBufSize);

            [DllImport("user32", CharSet = CharSet.Auto)]
            private extern static int GetWindowTextLength(IntPtr hWnd);

            public SystemInfo()
            {
                pcCpuLoad = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                pcCpuLoad.MachineName = ".";
                pcCpuLoad.NextValue();

                m_ProcessorCount = Environment.ProcessorCount;

                ManagementClass mc = new ManagementClass("Win32_ComputerSystem");
                ManagementObjectCollection moc = mc.GetInstances();
                foreach (ManagementObject mo in moc)
                {
                    if (mo["TotalPhysicalMemory"] != null)
                    {
                        m_PhysicalMemory = long.Parse(mo["TotalPhysicalMemory"].ToString());
                    }
                }
            }
            public long PhysicalMemory
            {
                get
                {
                    return m_PhysicalMemory;
                }
            }
        }

        private static bool ServiceStart(string serviceName)
        {
            try
            {
                ServiceController service = new ServiceController(serviceName);
                if (service.Status == ServiceControllerStatus.Running)
                {
                    return true;
                }
                else
                {
                    TimeSpan timeout = TimeSpan.FromMilliseconds(1000 * 10);
                    service.Start();
                    service.WaitForStatus(ServiceControllerStatus.Running, timeout);
                }
            }
            catch
            {
                return false;
            }
            return true;
        }

        private bool StopService(string serviseName)
        {
            try
            {
                ServiceController service = new ServiceController(serviseName);
                if (service.Status == ServiceControllerStatus.Stopped)
                {
                    return true;
                }
                else
                {
                    TimeSpan timeout = TimeSpan.FromMilliseconds(1000 * 10);
                    service.Stop();
                    service.WaitForStatus(ServiceControllerStatus.Stopped, timeout);
                }
            }
            catch
            {

                return false;
            }
            return true;
        }

        private string GetName(string IpAddress)
        {
            string name = "";
            try
            {
                IPHostEntry ipHstEntry = Dns.GetHostByAddress(IpAddress);
                //IPHostEntry ipHstEntry = Dns.GetHostEntry(IpAddress);
                name = ipHstEntry.HostName.ToString();
            }
            catch (Exception e)
            {
                name = e.Message;
            }
            return name;
        }


        [DllImport("Iphlpapi.dll")]
        private static extern int SendARP(Int32 dest, Int32 host, ref IntPtr mac, ref IntPtr length);
        [DllImport("Ws2_32.dll")]
        private static extern Int32 inet_addr(string ip);
        private string GetMacAddress(string IpAddress)
        {
            string macAddress = "";
            Int32 ldest = 0;
            try
            {
                ldest = inet_addr(IpAddress);
            }
            catch (Exception iperr)
            {
                MessageBox.Show(iperr.Message);
            }
            IntPtr macinfo = new IntPtr();
            IntPtr len = new IntPtr(6);
            try
            {
                int res = SendARP(ldest, 0, ref macinfo, ref len);
            }
            catch (Exception err)
            {
                MessageBox.Show(err.Message);
            }
            string originalMACAddress = macinfo.ToString("X4");
            if (originalMACAddress != "0000")
            { //合法MAC地址 
                macAddress = originalMACAddress.ToString().PadLeft(12, '0');
                macAddress = macAddress.Replace(":", "");
                macAddress = macAddress.Insert(4, "-");
                macAddress = macAddress.Insert(9, "-");
            }
            else
            {
                macAddress = "无法探测到MAC地址";
            }
            return macAddress;
        }

        private string GetServerVersion(string serverUrl)
        {
            XmlDocument xmlDocument = new XmlDocument();
            xmlDocument.Load(serverUrl + "update.xml");
            XmlNode documentElement = xmlDocument.DocumentElement;
            string result;
            for (int i = 0; i < documentElement.ChildNodes.Count; i++)
            {
                bool flag = documentElement.ChildNodes[i].Name == "Application";
                if (flag)
                {
                    result = documentElement.ChildNodes[i].ChildNodes[1].InnerText;
                    return result;
                }
            }
            result = "";
            return result;
        }
        private string GetUrl()
        {
            XmlDocument xmlDocument = new XmlDocument();
            xmlDocument.Load(AppDomain.CurrentDomain.BaseDirectory + "update.xml");
            XmlNode documentElement = xmlDocument.DocumentElement;
            return documentElement.ChildNodes[0].InnerText;
        }
        private string GetClientVersion(string directoryPath)
        {
            XmlDocument xmlDocument = new XmlDocument();
            xmlDocument.Load(directoryPath + "\\update.xml");
            XmlNode documentElement = xmlDocument.DocumentElement;
            string result;
            for (int i = 0; i < documentElement.ChildNodes.Count; i++)
            {
                bool flag = documentElement.ChildNodes[i].Name == "Application";
                if (flag)
                {
                    result = documentElement.ChildNodes[i].ChildNodes[1].InnerText;
                    return result;
                }
            }
            result = "";
            return result;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            CheckForIllegalCrossThreadCalls = false;

            listView1.GridLines = true;
            listView1.FullRowSelect = true;
            listView1.View = View.Details;
            listView1.Scrollable = true;
            listView1.MultiSelect = true;
            listView1.Columns.Add("IP", 100);    
            listView1.Columns.Add("计算机名", 225, HorizontalAlignment.Center);
            listView1.Columns.Add("mac", 125, HorizontalAlignment.Center);
            try
            {
                string url = this.GetUrl();
                string serverVersion = this.GetServerVersion(url);
                string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                string clientVersion = this.GetClientVersion(baseDirectory);
                if (File.Exists(Application.StartupPath + "//update.xml"))
                {
                    bool flag3 = serverVersion == clientVersion;
                    if (flag3)
                    {
                        label4.Text = "最新版本";
                    }
                    else
                    {
                        DialogResult dr = MessageBox.Show("有新版本，确认升级吗？", "提示", MessageBoxButtons.OKCancel);
                        if (dr == DialogResult.OK)
                        {
                            Process.Start(Application.StartupPath + "//Update.exe");
                            Application.Exit();
                        }
                        else if (dr == DialogResult.Cancel)
                        {
                            label4.Text = "需要升级";
                        }
                    }
                }
            }
            catch (Exception)
            {
                label4.Text="升级失败";
            }
        }

        private void toolStripMenuItem1_Click(object sender, EventArgs e)
        {
            this.richTextBox1.SelectAll();//全选
        }

        private void toolStripMenuItem2_Click(object sender, EventArgs e)
        {
            this.richTextBox1.Copy();//复制
        }

        private void toolStripMenuItem3_Click(object sender, EventArgs e)
        {
            this.richTextBox1.Paste();//粘贴
        }

        private void toolStripMenuItem4_Click(object sender, EventArgs e)
        {
            this.richTextBox1.Clear();//清除
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Thread thread = new Thread(() =>
            {
                //获取计算机名
                richTextBox1.AppendText("计算机名为：" + Environment.MachineName + "\n");
                //获取域名
                richTextBox1.AppendText("域名为：" + Environment.UserDomainName + "\n");
                //登录账号名
                richTextBox1.AppendText("电脑登陆账号为：" + Environment.UserName + "\n");
                //电脑的IP地址
                IPAddress[] ipaddress = Dns.GetHostAddresses(Dns.GetHostName());
                foreach (IPAddress ip in ipaddress)
                {
                    //电脑的IPv4地址
                    if (System.Net.Sockets.AddressFamily.InterNetwork.Equals(ip.AddressFamily))
                    {
                        richTextBox1.AppendText("电脑的局域网IP为：" + ip.ToString() + "\n");
                    }
                }
                //获取公网ip
                try
                {
                    WebClient MyWebClient = new WebClient();
                    MyWebClient.Credentials = CredentialCache.DefaultCredentials;
                    Byte[] pageData = MyWebClient.DownloadData("http://2018.ip138.com/ic.asp");
                    string pageHtml = Encoding.Default.GetString(pageData);  //如果获取网站页面采用的是GB2312，则使用这句 
                                                                             //string pageHtml = Encoding.UTF8.GetString(pageData);   //如果获取网站页面采用的是UTF-8，则使用这句
                    string[] pubulicIp = pageHtml.Split(new char[2] { '[', ']' });
                    richTextBox1.AppendText("公网IP为：" + pubulicIp[1] + "\n");
                }
                catch
                {
                    richTextBox1.AppendText("无法获取公网IP" + "\n");
                }
                //获取网卡硬件地址
                try
                {
                    string mac = "";
                    ManagementClass mc = new ManagementClass("Win32_NetworkAdapterConfiguration");
                    ManagementObjectCollection moc = mc.GetInstances();
                    foreach (ManagementObject mo in moc)
                    {
                        if ((bool)mo["IPEnabled"] == true)
                        {
                            mac = mo["MacAddress"].ToString();
                            break;
                        }
                    }
                    moc = null;
                    mc = null;
                    richTextBox1.AppendText("物理地址：" + mac + "\n");
                }
                catch
                {
                    richTextBox1.AppendText("物理地址未知" + "\n");
                }
                //系统版本
                try
                {
                    using (ManagementObjectCollection.ManagementObjectEnumerator enumerator = new ManagementClass(WMIPath.Win32_OperatingSystem.ToString()).GetInstances().GetEnumerator())
                    {
                        while (enumerator.MoveNext())
                        {
                            ManagementObject managementObject = (ManagementObject)enumerator.Current;
                            string[] array = managementObject.Properties["Name"].Value.ToString().Split(new char[]
                            {
                            '|'
                            });
                            richTextBox1.AppendText("系统版本为：" + array[0] + "\n");
                        }
                    }
                }
                catch
                {
                    richTextBox1.AppendText("系统获取错误" + "\n");
                }
                //获取电脑位数
                if (Environment.Is64BitOperatingSystem == true)
                {
                    richTextBox1.AppendText("此电脑为64位系统\n");
                }
                else
                {
                    richTextBox1.AppendText("此电脑为32位系统\n");
                }
                //获取CPU核心数
                richTextBox1.AppendText("CPU核心数为：" + Environment.ProcessorCount + "核\n");
                //获取内存大小
                SystemInfo Ram = new SystemInfo();
                int ram = Convert.ToInt32(Ram.PhysicalMemory / 1000000000);
                richTextBox1.AppendText("你的内存大小为：" + ram + "G\n");
                //获取硬盘大小
                long disk0 = 0;
                List<Dictionary<string, string>> diskInfoDic = new List<Dictionary<string, string>>();
                ManagementClass diskClass = new ManagementClass("Win32_LogicalDisk");
                ManagementObjectCollection disks = diskClass.GetInstances();
                foreach (ManagementObject disk in disks)
                {
                    Dictionary<string, string> diskInfo = new Dictionary<string, string>();
                    try
                    {
                        // 磁盘总容量
                        if (System.Convert.ToInt64(disk["Size"]) > 0)
                        {
                            long totalSpace = System.Convert.ToInt64(disk["Size"]);
                            disk0 = disk0 + totalSpace;
                        }
                    }
                    catch
                    {
                    }
                }
                richTextBox1.AppendText("你的硬盘大小为：" + disk0 / 1000000000 + "G\n");
            });
            thread.IsBackground = true;
            thread.Start();
        }

        //删除打印机任务
        private void button2_Click(object sender, EventArgs e)
        {
            Thread thread = new Thread(() =>
            {
                try
                {
                    StopService("Spooler");
                    DirectoryInfo dir = new DirectoryInfo(@"C:\Windows\System32\spool\PRINTERS");
                    FileSystemInfo[] fileinfo = dir.GetFileSystemInfos();  //返回目录中所有文件和子目录
                    foreach (FileSystemInfo i in fileinfo)
                    {
                        if (i is DirectoryInfo)            //判断是否文件夹
                        {
                            DirectoryInfo subdir = new DirectoryInfo(i.FullName);
                            subdir.Delete(true);          //删除子目录和文件
                        }
                        else
                        {
                            File.Delete(i.FullName);      //删除指定文件
                        }
                    }
                    ServiceStart("Spooler");
                    MessageBox.Show("删除完成！");
                }
                catch (Exception)
                {
                    MessageBox.Show("删除失败，请手动操作！");
                }
            });
            thread.IsBackground = true;
            thread.Start();
        }
        //系统升级失败
        private void button3_Click(object sender, EventArgs e)
        {
            Thread thread = new Thread(() =>
            {
                try
                {
                    StopService("wuauserv");
                    DirectoryInfo dir = new DirectoryInfo(@"C:\Windows\SoftwareDistribution\Download");
                    FileSystemInfo[] fileinfo = dir.GetFileSystemInfos();  //返回目录中所有文件和子目录
                    foreach (FileSystemInfo i in fileinfo)
                    {
                        if (i is DirectoryInfo)            //判断是否文件夹
                        {
                            DirectoryInfo subdir = new DirectoryInfo(i.FullName);
                            subdir.Delete(true);          //删除子目录和文件
                        }
                        else
                        {
                            File.Delete(i.FullName);      //删除指定文件
                        }
                    }
                    ServiceStart("Spooler");
                    MessageBox.Show("修复完成！");
                }
                catch (Exception)
                {
                    MessageBox.Show("删除失败，请手动操作！");
                }
            });
            thread.IsBackground = true;
            thread.Start();
        }
        //开始扫描
        private void button4_Click(object sender, EventArgs e)
        {
            bool blnTest = false;
            //Regex regex = new Regex("^[0-9]{1,3}.[0-9]{1,3}.[0-9]{1,3}.[0-9]{1,3}$");
            Regex regex = new Regex("^[0-9]{1,3}.[0-9]{1,3}.[0-9]{1,3}$");
            blnTest = regex.IsMatch(textBox1.Text);
            if (blnTest == true)
            {
                string[] strTemp = this.textBox1.Text.Split(new char[] { '.' }); // textBox1.Text.Split(new char[] { '.' });
                for (int i = 0; i < strTemp.Length; i++)
                {
                    if (Convert.ToInt32(strTemp[i]) > 255)
                    { //大于255则提示，不符合IP格式 
                        MessageBox.Show("不符合IP格式,请重新输入");
                        return;
                    }
                }
            }
            else
            {
                //输入非数字则提示，不符合IP格式 
                MessageBox.Show("不符合IP格式,请重新输入");
                return;
            }
            Thread start = new Thread(() =>
            {
                Parallel.For(1, 255, i =>
                {
                    ListViewItem i_item = new ListViewItem();
                    string Ip = textBox1.Text + "." + i;
                    pinghost LanIp = new pinghost();
                    bool resultip = LanIp.Ping(Ip);
                    string name = GetName(Ip);
                    string mac = GetMacAddress(Ip);
                    if (resultip)
                    {
                        //添加行的第一列内容
                        i_item.SubItems[0].Text = Ip;  //IP
                        //添加行的其他列内容
                        i_item.SubItems.Add(name);      //计算机名
                        i_item.SubItems.Add(mac);      //mac
                        listView1.Items.Add(i_item);
                    }
                });
                MessageBox.Show("完成");
            });
            start.IsBackground = true;
            start.Start();
        }

        private void button5_Click(object sender, EventArgs e)
        {
            bool blnTest = false;
            Regex regex = new Regex("^[0-9]{1,3}.[0-9]{1,3}.[0-9]{1,3}.[0-9]{1,3}$");
            //Regex regex = new Regex("^[0-9]{1,3}.[0-9]{1,3}.[0-9]{1,3}$");
            blnTest = regex.IsMatch(textBox2.Text);
            if (blnTest == true)
            {
                string[] strTemp = this.textBox2.Text.Split(new char[] { '.' }); // textBox1.Text.Split(new char[] { '.' });
                for (int i = 0; i < strTemp.Length; i++)
                {
                    if (Convert.ToInt32(strTemp[i]) > 255)
                    { //大于255则提示，不符合IP格式 
                        MessageBox.Show("不符合IP格式,请重新输入");
                        return;
                    }
                }
            }
            else
            {
                //输入非数字则提示，不符合IP格式 
                MessageBox.Show("不符合IP格式,请重新输入");
                return;
            }
            IPAddress ip = IPAddress.Parse(textBox2.Text);
            Thread start = new Thread(() =>
            {
                Parallel.For(1, 10000, i =>
                {
                    try
                    {
                        IPEndPoint point = new IPEndPoint(ip, i);
                        Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                        sock.Connect(point);
                        listBox1.Items.Add("连接端口" + i + "成功!" + point);
                    }
                    catch (Exception)
                    {
                    }
                });
                MessageBox.Show("完成");
            });
            start.IsBackground = true;
            start.Start();
        }

        private void button6_Click(object sender, EventArgs e)
        {
            listBox1.Items.Clear();
        }

        private void 退出ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Environment.Exit(0);
        }

        private void 检查更新ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //判断是否有更新
            try
            {
                string url = this.GetUrl();
                string serverVersion = this.GetServerVersion(url);
                string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                string clientVersion = this.GetClientVersion(baseDirectory);
                if (File.Exists(Application.StartupPath + "//update.xml"))
                {
                    bool flag3 = serverVersion == clientVersion;
                    if (flag3)
                    {
                        MessageBox.Show("最新版本");
                    }
                    else
                    {
                        DialogResult dr = MessageBox.Show("有新版本，确认升级吗？", "提示", MessageBoxButtons.OKCancel);
                        if (dr == DialogResult.OK)
                        {
                            Process.Start(Application.StartupPath + "//Update.exe");
                            Application.Exit();
                        }
                    }
                }
            }
            catch (Exception)
            {
                MessageBox.Show("升级失败");
            }
        }

        private void 认识skyruanToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Form2 f2 = new Form2();
            f2.Show();
        }
    }
}

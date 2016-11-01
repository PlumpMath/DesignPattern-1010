using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

namespace FileSystem
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            newFileName = "";
            newPathName = "";
            openFileName = "";
            emptyblock = 0;

            //初始化fat表
            string fatbin;
            StreamReader srfat = new StreamReader("fat.bin");
            for (int i = 0; i < 32; i++)
            {
                fatbin = srfat.ReadLine();
                if(fatbin==null)
                {
                    for(int k=0;k<32;k++)
                    {
                        Fat[i].Name = "";
                        Fat[i].busy = false;
                        Fat[i].next = -1;
                        Fat[i].isStart = false;
                    }
                    emptyblock = 32;
                    break;
                }
                if(fatbin=="0 0 -1 -1")
                {
                    Fat[i].Name = "";
                    Fat[i].busy = false;
                    Fat[i].next = -1;
                    Fat[i].isStart = false;
                    emptyblock++;
                }
                else
                {
                    var a = fatbin.Split(' ');
                    Fat[i].next = Convert.ToInt32(a[2]);
                    Fat[i].busy = true;
                    Fat[i].Name = a[1];
                    if (a[3] == "0")
                        Fat[i].isStart = false;
                    else
                        Fat[i].isStart = true;
                }
            }
            srfat.Close();
            srfat.Dispose();
            StreamWriter swfat = new StreamWriter("fat.bin");
            swfat.Close();
            swfat.Dispose();

            //根据保存结果恢复之前创建的目录
            DirectoryInfo dir = new DirectoryInfo(Application.StartupPath).Parent.Parent;
            path = dir.FullName;
            StreamReader sr = new StreamReader("root.bin");
            string parent;
            string name;
            string txt;
            while (true)
            {
                if ((txt = sr.ReadLine()) == null)
                    break;
                fileNode tmp = new fileNode();
                var b = txt.Split(' ');
                name = b[0];
                if (b[0] == "")
                    break;
                if (b[1] == "0")
                { 
                    tmp.size = 0;
                    tmp.BackColor = Color.Yellow;
                }
                else if (b[1] == "1")
                { 
                    FileInfo fi = new FileInfo(name);
                    tmp.size = (Int32)fi.Length;
                    
                }
                else
                    break;
                parent = b[2];
                
                //TreeNode tmp;
                //tmp = new TreeNode();
                
                tmp.Text = name;
                tmp.ToolTipText = b[1];
                FindTreeViewNode(treeView1.Nodes, parent).Nodes.Add(tmp);
            }
            treeView1.ExpandAll();
            sr.Close();
            sr.Dispose();
            StreamWriter sw = new StreamWriter("root.bin");
            sw.Close();
            sw.Dispose();
        }

        //fat表项
        public struct Block
        {
            public bool busy;
            public bool isStart;
            public string Name;
            public int next;
        }

        static string path;
        static string newPathName;
        static string newFileName;
        static string openFileName;
        static Block[] Fat = new Block[32];
        static int emptyblock;//磁盘空闲块

        //目录节点
        public class fileNode:TreeNode
        {
            public int address;
            public int size;
        }
        
        //寻找名为s的目录
        public TreeNode FindTreeViewNode(TreeNodeCollection node,string s)
        {
            if (node == null)
                return null;
            foreach (TreeNode n in node)
            {
                if (n.Text == s)
                    return n;
                if (FindTreeViewNode(n.Nodes, s) != null)
                    return FindTreeViewNode(n.Nodes, s);
            }
            return null;
        }

        //搜索文件
        public TreeNode FindFile(TreeNodeCollection node, string s)
        {
             if (node == null)
                return null;
            foreach (TreeNode n in node)
            {
                if (n.Text == s)
                    return n;
                if (FindFile(n.Nodes, s) != null)
                    return FindFile(n.Nodes, s);
            }
            return null;
        }

        //判断同级目录中是否有重复命名
        public bool repeatName(TreeNodeCollection node ,string s)
        {
            if (node == null)
                return false;
            foreach (TreeNode n in node)
            {
                if (n.Text == s)
                    return true;
            }
            return false;
        }

        //删除子目录
        public void DeletePath(TreeNode node)
        {
            node.Nodes.Clear();
        }

        //删除文件
        public void DeleteFile(TreeNodeCollection node)
        {
            if (node == null)
                return;
            foreach(TreeNode n in node)
            {
                if(n.ToolTipText=="1")
                {
                    int now = FindinFat(n.Text);
                    int pre = now;
                    while(Fat[now].next != -1)
                    {
                        pre = now;
                        now=Fat[now].next;
                        Fat[pre].isStart = false;
                        Fat[pre].Name = "";
                        Fat[pre].next = -1;
                        Fat[pre].busy = false;
                        emptyblock++;
                    }
                    Fat[now].isStart = false;
                    Fat[now].Name = "";
                    Fat[now].next = -1;
                    Fat[now].busy = false;
                    emptyblock++;
                    File.Delete(n.Text);
                }
                else
                {
                    DeleteFile(n.Nodes);
                }
            }
        }

        //格式化
        public void DeleteRootFile()
        {
            //初始化fat表
            for (int i = 0; i < 32; i++)
            {
                if(Fat[i].isStart)
                    File.Delete(Fat[i].Name);
                Fat[i].busy = false;
                Fat[i].isStart = false;
                Fat[i].Name = "";
                Fat[i].next = -1;
                Fat[i].isStart = false;
                
            }
            emptyblock = 32;
            treeView1.Nodes.Clear();
            TreeNode root = new TreeNode();
            root.Text = "root";
            root.ToolTipText = "0";
            root.BackColor = Color.Yellow;
            treeView1.Nodes.Add(root);
        }

        //程序关闭前保留信息
        public void Save(TreeNodeCollection node)
        {   
            if (node == null)
                return;
            foreach (TreeNode n in node)
            {
                FileStream fs = new FileStream("root.bin", FileMode.Append);
                StreamWriter sw = new StreamWriter(fs);
                if (n.Parent!=null)
                {
                    string line = n.Text + " " + n.ToolTipText + " " + n.Parent.Text;
                    sw.WriteLine(line);
                }
                sw.Close();
                sw.Dispose();
                fs.Close();
                fs.Dispose();
                Save(n.Nodes);
            }   
        }
    
        //存储磁盘fat表
        public void SaveFat()
        {
            for (int i = 0; i < 32; i++)
            {
                FileStream fs = new FileStream("fat.bin", FileMode.Append);
                StreamWriter sw = new StreamWriter(fs);
                string line = "";
                if (Fat[i].busy)
                { 
                    line = "1 " + Fat[i].Name + " " + Fat[i].next.ToString() + " ";
                    if (Fat[i].isStart)
                        line += "1";
                    else
                        line += "0";
                }
                else
                    line = "0 0 -1 -1";
                sw.WriteLine(line);
                sw.Close();
                sw.Dispose();
                fs.Close();
                fs.Dispose();
            }
        }

        //在fat表中找到文件起始块。
        public int FindinFat(string n)
        {
            for(int i=0;i<32;i++)
            {
                if (Fat[i].isStart && Fat[i].Name == n)
                    return i;
            }
            return -1;
        }

        //给文件分配块数
        public int CreateFile(int size,string name)
        {
            if (emptyblock == 0)
                return -1;

            int num_block;//所需块数
            if(size%32!=0)
            {
                num_block = size / 32 + 1;
            }
            else
            {
                num_block = size / 32;
            }
            int prev = -1;
            int start = -1;
            if (num_block <= emptyblock)
            {
                for (int i = 0, j = 0; j < num_block; i++)
                {
                    if (Fat[i].busy==false)
                    {
                        Fat[i].busy = true;
                        Fat[i].Name = name;
                        Fat[i].next = -1;
                        if (prev != -1)
                        { 
                            Fat[prev].next = i;
                            Fat[i].isStart = false;
                        }
                        else
                        { 
                            start = i;
                            Fat[i].isStart = true;
                        }
                        prev = i;
                        j++;
                    }
                }
                emptyblock -= num_block;
            }
            return start;
        }

        //添加目录
        private void button1_Click(object sender, EventArgs e)
        {
            if (treeView1.SelectedNode == null)
            {
                MessageBox.Show("请选择一个节点", "提示信息",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                //创建一个节点对象，并初始化
                fileNode tmp;
                tmp = new fileNode();
                if (newPathName == "")
                    MessageBox.Show("请输入名称", "提示信息",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
                else
                {
                    tmp.Text = newPathName;
                    tmp.BackColor = Color.Yellow;
                    tmp.ToolTipText = "0";
                    //在TreeView组件中加入子节点
                    tmp.size = 0;
                    tmp.address = -1;
                    if(repeatName(treeView1.SelectedNode.Nodes,tmp.Text))
                    {
                        MessageBox.Show("有重复命名，创建失败", "提示信息",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
                       return;
                    }
                    treeView1.SelectedNode.Nodes.Add(tmp);
                    treeView1.SelectedNode = tmp;
                    textBox1.Text = "";
                }
            } 
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            newPathName = textBox1.Text;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            DeleteRootFile();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            Save(treeView1.Nodes);
            SaveFat();
        }

        //删除文件
        private void button3_Click(object sender, EventArgs e)
        {
            if (treeView1.SelectedNode == null)
            {
                MessageBox.Show("请选择一个节点删除", "提示信息",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (treeView1.SelectedNode.Parent == null)
            {
                MessageBox.Show("根节点不可以删除", "提示信息",
        MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (treeView1.SelectedNode.ToolTipText == "1")
            {
                int now = FindinFat(treeView1.SelectedNode.Text);
                int pre = now;
                while (Fat[now].next != -1)
                {
                    pre = now;
                    now = Fat[now].next;
                    Fat[pre].isStart = false;
                    Fat[pre].Name = "";
                    Fat[pre].next = -1;
                    Fat[pre].busy = false;
                    emptyblock++;
                }
                Fat[now].isStart = false;
                Fat[now].Name = "";
                Fat[now].next = -1;
                Fat[now].busy = false;
                emptyblock++;
                File.Delete(treeView1.SelectedNode.Text);
            }
            else
                DeleteFile(treeView1.SelectedNode.Nodes);
            DeletePath(treeView1.SelectedNode);
            treeView1.Nodes.Remove(treeView1.SelectedNode);
        }

        //创建文件
        private void button4_Click(object sender, EventArgs e)
        {
            if (treeView1.SelectedNode == null)
            {
                MessageBox.Show("请选择一个节点", "提示信息",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                //创建一个节点对象，并初始化
                fileNode tmp;
                tmp = new fileNode();
                if (newFileName == "")
                {
                    MessageBox.Show("请输入名称", "提示信息",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                
                else
                {
                    tmp.Text = newFileName+".txt";
                    tmp.ToolTipText = "1";
                    //在TreeView组件中加入子节点
                    tmp.size = 0;
                    for (int i = 0; i < 32;i++ )
                    {
                        if(Fat[i].Name==tmp.Text)
                        {
                            MessageBox.Show("有重复命名的文件", "提示信息",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                            return;
                        }
                    }
                    tmp.address = CreateFile(1, tmp.Text);
                    if (tmp.address >= 0)
                    {
                        treeView1.SelectedNode.Nodes.Add(tmp);
                        treeView1.SelectedNode = tmp;
                        FileStream NewText = File.Create(tmp.Text);
                        NewText.Close();
                    }
                    else
                    {
                        MessageBox.Show("磁盘空间不足，创建失败", "提示信息",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    textBox2.Text = "";
                }
            } 
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            newFileName = textBox2.Text;
        }

        private void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if(treeView1.SelectedNode.ToolTipText=="1")
            {
                button1.Enabled = false;
                button4.Enabled = false;
            }
            else
            {
                button1.Enabled = true;
                button4.Enabled = true;
            }
        }

        //打开读写文件
        private void button5_Click(object sender, EventArgs e)
        {
            if(treeView1.SelectedNode==null||treeView1.SelectedNode.ToolTipText=="0")
            {
                MessageBox.Show("请选择一个文件!", "提示信息",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            openFileName = treeView1.SelectedNode.Text;
            StreamReader sr = new StreamReader(treeView1.SelectedNode.Text);
            textBox3.Text = sr.ReadToEnd();
            sr.Close();
            sr.Dispose();
            button6.Enabled = true;
            button5.Enabled = false;
            button3.Enabled = false;
            button2.Enabled = false;

        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            label1.Text = "磁盘总块数32（每块32B)\n磁盘剩余块数："+emptyblock.ToString();
        }

        private void button6_Click(object sender, EventArgs e)
        {
            StreamWriter sw = new StreamWriter(openFileName);
            sw.WriteLine(textBox3.Text);
            sw.Close();
            sw.Dispose();
            for(int i=0;i<32;i++)
            {
                if(Fat[i].Name==openFileName)
                {
                    Fat[i].isStart = false;
                    Fat[i].Name = "";
                    Fat[i].next = -1;
                    Fat[i].busy = false;
                    emptyblock++;
                }
            }
            FileInfo fi = new FileInfo(openFileName);
            int size = (Int32)fi.Length;
            CreateFile(size,openFileName);
            textBox3.Clear();
            button5.Enabled = true;
            button6.Enabled = false;
            button3.Enabled = true;
            button2.Enabled = true;
        }

        private void labelFile_Click(object sender, EventArgs e)
        {

        }

        private void buttonsearch_Click(object sender, EventArgs e)
        {
            treeView1.SelectedNode=FindFile(treeView1.SelectedNode.Nodes,textBoxSearch.Text);
            if (treeView1.SelectedNode != null)
            {
                MessageBox.Show(treeView1.SelectedNode.FullPath.ToString(), "result",
               MessageBoxButtons.OK, MessageBoxIcon.Information);
                textBoxSearch.Text = "";
            }
            else
            {
                MessageBox.Show("未在该目录下搜索到文件", "result",
               MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }
}

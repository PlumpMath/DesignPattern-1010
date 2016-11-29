using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace FileSystem
{
    //工厂模式，用于内存块划分
    class MemoryManager
    {
        public struct Block
        {
            public bool busy;
            public bool isStart;
            public string Name;
            public int next;
        }


        private int blockNum;//磁盘分块数
        private int blockSize;//磁盘块大小
        private int emptyblock;//磁盘空闲块
        private Block[] Fat;//内存使用表

        public MemoryManager(int num, int size)
        {
            blockNum = num;
            blockSize = size;
            Fat = new Block[num];
        }

        public int returnEmpty()
        {
            return emptyblock;
        }

        //初始化fat
        public void InitFat()
        {
            //初始化fat表
            for (int i = 0; i < 32; i++)
            {
                if (Fat[i].isStart)
                    File.Delete(Fat[i].Name);
                Fat[i].busy = false;
                Fat[i].isStart = false;
                Fat[i].Name = "";
                Fat[i].next = -1;
                Fat[i].isStart = false;

            }
            emptyblock = 32;
        }

        //恢复内存使用表
        public void ResumeFat()
        {
            string fatbin;
            StreamReader srfat = new StreamReader("fat.bin");
            for (int i = 0; i < blockNum; i++)
            {
                fatbin = srfat.ReadLine();
                if (fatbin == null)
                {
                    for (int k = 0; k < blockNum; k++)
                    {
                        Fat[i].Name = "";
                        Fat[i].busy = false;
                        Fat[i].next = -1;
                        Fat[i].isStart = false;
                    }
                    emptyblock = 32;
                    break;
                }
                if (fatbin == "0 0 -1 -1")
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

        }

        //保存当前内存使用表
        public void SaveFat()
        {
            for (int i = 0; i < blockSize; i++)
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

        //找寻存储文件的起始块
        public int FindinFat(string n)
        {
            for (int i = 0; i < blockNum; i++)
            {
                if (Fat[i].isStart && Fat[i].Name == n)
                    return i;
            }
            return -1;
        }

        //给文件分配块数
        public int AllocateFile(int size, string name)
        {
            if (emptyblock == 0)
                return -1;

            int num_block;//所需块数
            if (size % blockSize != 0)
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
                    if (Fat[i].busy == false)
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

        //释放文件块
        public void ReleaseFile(string name)
        {
            int now = FindinFat(name);
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

        }

        //对改写的文件重分配块
        public void AppendFile(string name, int size)
        {
            ReleaseFile(name);
            AllocateFile(size, name);
        }

        public bool CanCreate(string name)
        {
            for (int i = 0; i < 32; i++)
            {
                if (Fat[i].Name == name)
                {
                    return false;
                }
            }
            return true;
        }
    }
}

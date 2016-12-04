using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
namespace FileSystem
{
    class FileIterater
    {
        private bool isfile;//是否为文件
        private bool isdeleted;//标示是否被删除
        public string name;
        public string type;
        public int index;

        public FileIterater last;
        public FileIterater next;
        public FileIterater father;
        


        FileIterater(bool file,int id)
        {
            isdeleted = false;
            isfile = file;
            index = id;
        }

        public void addNode()
        {

        }

        public bool isFile()
        {
            return this.isfile;
        }

        public bool isDeleted()
        {
            return this.isdeleted;
        }

        public void ResumeFile()
        {
            this.isdeleted = false;
        }

        public void DeleteFile()
        {
            this.isdeleted = true;
        }

        public FileIterater FindNodeByName(string target)
        {
            string myname;
            if (this.isFile())
                myname = this.name + '.' + this.type;
            else
                myname = this.name;
            if (myname == target)
                return this;
            else
            {
                if (this.next == null)
                    return null;
                else
                    return next.FindNodeByName(target);
            }
        }

        public FileIterater FindNodeById(int id)
        {
            if (index == id)
                return this;
            else
            {
                if (this.next == null)
                    return null;
                else
                    return next.FindNodeById(id);
            }
        }

        public void AddNode(FileIterater node)
        {
            node.next = this.next;
            node.last = this;

            this.next.last = node;
            this.next = node;
        }

        public void RemoveNode()
        {
            this.last.next = this.next;
            this.next.last = this.last;
        }
    }
}

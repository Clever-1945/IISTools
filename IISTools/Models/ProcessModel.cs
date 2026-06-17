using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IISTools.Models
{
    public class ProcessModel
    {
        public int Id { set; get; }
        public string Name { set; get; }
        public string CommandLine { set; get; }
        public string Owner { set; get; }
        public Exception[] Exceptions { set; get; }
    }
}

using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CT_SCAN_Detection_tool.Models
{
    public class HomeViewModel
    {
        public IFormFile AttachedFile { get; set; }
        public string Result { get; set; }
        public string Src { get; set; }
        public decimal Procent { get; set; }
        public string Class { get; set; }
    }
}

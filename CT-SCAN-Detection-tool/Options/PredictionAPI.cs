using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CT_SCAN_Detection_tool.Options
{
    public class PredictionAPI
    {
        public string Url { get; set; }
        public string Key { get; set; }
        public float Probability { get; set; }
        public string Url2 { get; set; }
        public float Probability2 { get; set; }
    }
}

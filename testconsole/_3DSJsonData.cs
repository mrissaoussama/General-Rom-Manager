using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace testconsole
{
    public class _3DSJsonData
    {
        public string Name { get; set; }
        public string UID { get; set; }
        public string TitleID { get; set; }
        public string Version { get; set; }
        public string Size { get; set; }

        [JsonPropertyName("Product Code")]
        public string ProductCode { get; set; }
        public string Publisher { get; set; }
    }


}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SqlAgentScheduleView.Data
{
    public class SQLServerAgentJobRequest
    {
        public string JobName { get; set; }
        public string JobServer { get; set; }
        public string ConnectionString { get; set; }
    }
}

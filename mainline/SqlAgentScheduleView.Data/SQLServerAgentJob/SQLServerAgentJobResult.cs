using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SqlAgentScheduleView.Data
{
    public class SQLServerAgentJobResult
    {
        public Guid JobID { get; set; }
        public string Name { get; set; }
        public string Server { get; set; }
        public string JobType { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public double Duration { get; set; }
        public string ResultStatus { get; set; }
        public string ResultMessage { get; set; }
    }
}

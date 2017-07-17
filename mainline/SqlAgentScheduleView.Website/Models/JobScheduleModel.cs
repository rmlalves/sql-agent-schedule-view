using SqlAgentScheduleView.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SqlAgentScheduleView.Website
{
    public class JobScheduleModel
    {
        public List<JobServerModel> JobServerList { get; set; }
        public List<SQLServerAgentJobResult> JobResultList { get; set; }
        public List<SQLServerAgentJobResult> JobNextRunDateList { get; set; }
    }
}
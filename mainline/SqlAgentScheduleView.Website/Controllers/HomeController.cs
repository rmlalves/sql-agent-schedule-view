using SqlAgentScheduleView.Data;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace SqlAgentScheduleView.Website
{
    public class HomeController : Controller
    {
        public ActionResult Index(DateTime? StartDate, DateTime? EndDate)
        {
            var sqlAgentJobWorker = new SQLServerAgentJobWorker();

            var model = new JobScheduleModel();
            model.JobServerList = new List<JobServerModel>();
            model.JobResultList = new List<SQLServerAgentJobResult>();
            model.JobNextRunDateList = new List<SQLServerAgentJobResult>();

            for (int index = 0; index < ConfigurationManager.ConnectionStrings.Count; index++)
            {
                model.JobServerList.Add(new JobServerModel()
                {
                    Name = ConfigurationManager.ConnectionStrings[index].Name,
                    ConnectionString = ConfigurationManager.ConnectionStrings[index].ConnectionString,
                    HtmlColor = ColorHelper.ColorChart[index]
                });

                model.JobResultList.AddRange(sqlAgentJobWorker.History(new SQLServerAgentJobRequest()
                {
                    JobServer = ConfigurationManager.ConnectionStrings[index].Name,
                    ConnectionString = ConfigurationManager.ConnectionStrings[index].ConnectionString,
                }));

                model.JobNextRunDateList.AddRange(sqlAgentJobWorker.NextRunDate(new SQLServerAgentJobRequest()
                {
                    JobServer = ConfigurationManager.ConnectionStrings[index].Name,
                    ConnectionString = ConfigurationManager.ConnectionStrings[index].ConnectionString,
                }));
            }

            model.JobResultList = model.JobResultList
                .Where(item => item != null)
                .OrderBy(item => item.StartDate)
                .ToList();

            return View(model);
        }
    }
}
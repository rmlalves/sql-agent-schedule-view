using Dapper;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;

namespace SqlAgentScheduleView.Data
{
    public class SQLServerAgentJobWorker
    {
        public SQLServerAgentJobResult Get(SQLServerAgentJobRequest request)
        {
            using (var connection = new SqlConnection(request.ConnectionString))
            {
                connection.Open();

                var result = connection.Query<SQLServerAgentJobResult>(
                    @"
SELECT 
     sysjobs.job_id AS JobID
    ,sysjobs.name AS Name
    ,CAST(
        CAST(sysjobservers.last_run_date AS VARCHAR) 
        + ' ' 
        + STUFF(STUFF(RIGHT('000000' + CAST(sysjobservers.last_run_time AS VARCHAR), 6), 3, 0, ':'), 6, 0, ':')
    AS DATETIME) AS StartDate
    ,DATEADD(HOUR, CAST(SUBSTRING(RIGHT('000000' + CAST(sysjobservers.last_run_duration AS VARCHAR), 6), 1, 2) AS INT)
        ,DATEADD(MINUTE, CAST(SUBSTRING(RIGHT('000000' + CAST(sysjobservers.last_run_duration AS VARCHAR), 6), 3, 2) AS INT)
            ,DATEADD(SECOND, CAST(SUBSTRING(RIGHT('000000' + CAST(sysjobservers.last_run_duration AS VARCHAR), 6), 5, 2) AS INT)
                ,CAST(
                    CAST(sysjobservers.last_run_date AS VARCHAR) 
                    + ' ' 
                    + STUFF(STUFF(RIGHT('000000' + CAST(sysjobservers.last_run_time AS VARCHAR), 6), 3, 0, ':'), 6, 0, ':')
                AS DATETIME)
            )
        )
    ) AS EndDate
    ,(
        CAST(SUBSTRING(RIGHT('000000' + CAST(sysjobservers.last_run_duration AS VARCHAR), 6), 1, 2) AS INT) * 3600 -- HOURS
      + CAST(SUBSTRING(RIGHT('000000' + CAST(sysjobservers.last_run_duration AS VARCHAR), 6), 3, 2) AS INT) * 60   -- MINUTES
      + CAST(SUBSTRING(RIGHT('000000' + CAST(sysjobservers.last_run_duration AS VARCHAR), 6), 5, 2) AS INT)        -- SECONDS
    ) AS Duration
    ,CASE 
        WHEN sysjobservers.last_run_outcome = 0 THEN 'Failed'
        WHEN sysjobservers.last_run_outcome = 1 THEN 'Success'
        WHEN sysjobservers.last_run_outcome = 4 THEN 'Running'
        ELSE 'Others (' + CAST(sysjobservers.last_run_outcome AS VARCHAR) + ')'
    END AS ResultStatus
    ,sysjobservers.last_outcome_message AS ErrorMessage
FROM msdb.dbo.sysjobs
INNER JOIN msdb.dbo.sysjobservers ON (sysjobservers.job_id = sysjobs.job_id)
WHERE sysjobs.name = @jobName
                    ",
                    new
                    {
                        jobName = request.JobName
                    }
                ).FirstOrDefault();
                result.Name = request.JobName;
                result.Server = request.JobServer;
                result.JobType = "SQLServer Agent Job";

                connection.Close();

                return result;
            }
        }

        public List<SQLServerAgentJobResult> History(SQLServerAgentJobRequest request)
        {
            using (var connection = new SqlConnection(request.ConnectionString))
            {
                connection.Open();

                var result = connection.Query<SQLServerAgentJobResult>(
                    @"
-- JOBS FROM HISTORY
SELECT 
     sysjobs.job_id AS JobID
    ,sysjobs.name AS Name
    ,CAST(
        CAST(sysjobhistory.run_date AS VARCHAR) 
        + ' ' 
        + STUFF(STUFF(RIGHT('000000' + CAST(sysjobhistory.run_time AS VARCHAR), 6), 3, 0, ':'), 6, 0, ':')
    AS DATETIME) AS StartDate
    ,DATEADD(HOUR, CAST(SUBSTRING(RIGHT('000000' + CAST(sysjobhistory.run_duration AS VARCHAR), 6), 1, 2) AS INT)
        ,DATEADD(MINUTE, CAST(SUBSTRING(RIGHT('000000' + CAST(sysjobhistory.run_duration AS VARCHAR), 6), 3, 2) AS INT)
            ,DATEADD(SECOND, CAST(SUBSTRING(RIGHT('000000' + CAST(sysjobhistory.run_duration AS VARCHAR), 6), 5, 2) AS INT)
                ,CAST(
                    CAST(sysjobhistory.run_date AS VARCHAR) 
                    + ' ' 
                    + STUFF(STUFF(RIGHT('000000' + CAST(sysjobhistory.run_time AS VARCHAR), 6), 3, 0, ':'), 6, 0, ':')
                AS DATETIME)
            )
        )
    ) AS EndDate
    ,(
        CAST(SUBSTRING(RIGHT('000000' + CAST(sysjobhistory.run_duration AS VARCHAR), 6), 1, 2) AS INT) * 3600 -- HOURS
      + CAST(SUBSTRING(RIGHT('000000' + CAST(sysjobhistory.run_duration AS VARCHAR), 6), 3, 2) AS INT) * 60   -- MINUTES
      + CAST(SUBSTRING(RIGHT('000000' + CAST(sysjobhistory.run_duration AS VARCHAR), 6), 5, 2) AS INT)        -- SECONDS
    ) AS Duration
    ,CASE 
        WHEN sysjobhistory.run_status = 0 THEN 'Failed'
        WHEN sysjobhistory.run_status = 1 THEN 'Success'
        WHEN sysjobhistory.run_status = 4 THEN 'Running'
        ELSE 'Others (' + CAST(sysjobhistory.run_status AS VARCHAR) + ')'
    END AS ResultStatus
    ,sysjobhistory.[message] AS ErrorMessage
FROM msdb.dbo.sysjobs
INNER JOIN msdb.dbo.sysjobhistory ON (sysjobhistory.job_id = sysjobs.job_id)
WHERE sysjobhistory.step_id = 0 -- GET JOB RESULT

UNION 

-- JOBS RUNNING 
SELECT 
     sysjobs.job_id AS JobID
    ,sysjobs.name AS Name
    ,sysjobactivity.start_execution_date AS StartDate
    ,GETDATE() AS EndDate
    ,DATEDIFF(SECOND, sysjobactivity.start_execution_date, GETDATE()) AS Duration
    ,'Running' AS ResultStatus
    ,'The job is running...' AS ErrorMessage
FROM msdb.dbo.sysjobs
INNER JOIN msdb.dbo.sysjobactivity ON (sysjobactivity.job_id = sysjobs.job_id)
WHERE sysjobactivity.job_history_id IS NULL
    AND sysjobactivity.start_execution_date IS NOT NULL
                ").ToList();

                result.ForEach(item =>
                {
                    item.JobType = "SQLServer Agent Job";
                    item.Server = request.JobServer;
                });

                connection.Close();

                return result;
            }
        }

        public List<SQLServerAgentJobResult> NextRunDate(SQLServerAgentJobRequest request)
        {
            using (var connection = new SqlConnection(request.ConnectionString))
            {
                connection.Open();

                var result = connection.Query<SQLServerAgentJobResult>(
                    @"
SELECT 
     sysjobs.job_id AS JobId
    ,sysjobs.name AS Name
    ,MAX(sysjobactivity.next_scheduled_run_date) AS StartDate
FROM msdb.dbo.sysjobs
INNER JOIN msdb.dbo.sysjobactivity ON (sysjobactivity.job_id = sysjobs.job_id)
WHERE sysjobactivity.next_scheduled_run_date > GETDATE()
GROUP BY 
     sysjobs.job_id
    ,sysjobs.name 
                ").ToList();

                result.ForEach(item =>
                {
                    item.JobType = "SQLServer Agent Job";
                    item.Server = request.JobServer;
                });

                connection.Close();

                return result;
            }
        }
    }
}


using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Net;
using ups_Common;
using ups_DAO;
using ups_Entities;

namespace ups_Business
{
    public class JobsServiceBO
    {
        #region <<<< MÉTODOS PRIVADOS >>>>
        
        private readonly JobsDao _jobsDao = new JobsDao();
        private readonly JobStepsDao _stepsDao = new JobStepsDao();
        private readonly JobRunHistoryDao _historyDao = new JobRunHistoryDao();
        #endregion

        #region <<<< MÉTODOS PÚBLICOS >>>>

        public IEnumerable<JobVO> GetAll() => _jobsDao.GetAll();

        public JobVO GetById(int id) => _jobsDao.GetById(id);

        /// <summary>
        /// Método que valida regras de validação dos Jobs de serviço, para creação
        /// </summary>
        /// <param name="job"></param>
        /// <returns>
        /// </returns>
        /// <remarks>
        /// Created by: Silva, André
        /// Created Date: 26 01 2026
        /// </remarks>
        public OperationResult<JobVO> Create(JobVO job)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(job.Name))
                    return OperationResult<JobVO>.Fail("Nome é obrigatório.");

                job.JobId = _jobsDao.Insert(job);
                return OperationResult<JobVO>.Ok(job);
            }
            catch (Exception ex)
            {
                return OperationResult<JobVO>.Fail(ex.Message);
            }
        }

        /// <summary>
        /// Método que valida regras de validação dos Jobs de serviço, para alteração
        /// </summary>
        /// <param name="job"></param>
        /// <returns>
        /// </returns>
        /// <remarks>
        /// Created by: Silva, André
        /// Created Date: 26 01 2026
        /// </remarks>
        public OperationResult<bool> Update(JobVO job)
        {
            try
            {
                _jobsDao.Update(job);
                return OperationResult<bool>.Ok(true);
            }
            catch (Exception ex)
            {
                return OperationResult<bool>.Fail(ex.Message);
            }
        }


        /// <summary>
        /// Método que valida regras de validação dos Jobs de serviço, para exclusão 
        /// </summary>
        /// <param name="jobId"></param>
        /// <returns>
        /// </returns>
        /// <remarks>
        /// Created by: Silva, André
        /// Created Date: 26 01 2026
        /// </remarks>
        public OperationResult<bool> Delete(int jobId)
        {
            try
            {
                _jobsDao.Delete(jobId);
                return OperationResult<bool>.Ok(true);
            }
            catch (Exception ex)
            {
                return OperationResult<bool>.Fail(ex.Message);
            }
        }

        /// <summary>
        /// Executa as operações para os Jobs de serviço
        /// </summary>
        /// <param name="jobId"></param>
        /// <returns>
        /// </returns>
        /// <remarks>
        /// Created by: Silva, André
        /// Created Date: 26 01 2026
        /// </remarks>
        public OperationResult<bool> RunJob(int jobId)
        {
            var job = _jobsDao.GetById(jobId);
            if (job == null)
                return OperationResult<bool>.Fail("Job não encontrado.");

            var host = Dns.GetHostName();
            var run = new JobRunHistoryVO
            {
                JobId = jobId,
                StartedUtc = DateTime.UtcNow,
                Status = "Running",
                HostName = host
            };

            long runId = _historyDao.Insert(run);

            using (var conn = Db.CreateConnection())
            {
                conn.Open();
                using (var tx = conn.BeginTransaction(IsolationLevel.ReadCommitted))
                {
                    try
                    {
                        foreach (var step in _stepsDao.GetByJobId(jobId))
                        {
                            using (var cmd = new SqlCommand(step.Script, conn, tx))
                            {
                                cmd.CommandType = CommandType.Text;
                                if (step.TimeoutSec.HasValue)
                                    cmd.CommandTimeout = step.TimeoutSec.Value;

                                cmd.ExecuteNonQuery();
                            }
                        }

                        tx.Commit();
                        _historyDao.Mark(runId, "Success", DateTime.UtcNow, "Execução concluída.");
                        _jobsDao.UpdateLastRun(jobId, "Success", DateTime.UtcNow);
                        return OperationResult<bool>.Ok(true);
                    }
                    catch (Exception ex)
                    {
                        tx.Rollback();
                        _historyDao.Mark(runId, "Failed", DateTime.UtcNow, ex.Message);
                        _jobsDao.UpdateLastRun(jobId, "Failed", DateTime.UtcNow);
                        return OperationResult<bool>.Fail("Falha na execução: " + ex.Message);
                    }
                }
            }
        }
        #endregion
    }
}

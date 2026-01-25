
using System.Configuration;
using System.Data.SqlClient;
using System;

namespace ups_Common
{

    public static class Db
    {

        #region <<<< MÉTODOS PRIVADOS >>>>

        private const string DefaultConnectionName = "SqlServer";

        /// <summary>
        /// Obtém o valor da connectionString do arquivo de configuração.
        /// Lança exceção com mensagem clara se não existir.
        /// </summary>
        /// <param name="name"></param>
        /// <remarks>
        /// Created By: Silva, Andre
        /// Created Date: 23 01 2024
        /// </remarks>
        private static string GetConnectionString(string name)
        {
            var settings = ConfigurationManager.ConnectionStrings[name];
            if (settings == null || string.IsNullOrWhiteSpace(settings.ConnectionString))
            {
                throw new InvalidOperationException(
                    $"ConnectionString '{name}' não encontrada no arquivo de configuração. " +
                    $"Verifique <connectionStrings> no app.config/web.config do processo que está executando.");
            }

            return settings.ConnectionString;
        }
        #endregion

        #region <<<< MÉTODOS PÚBLICOS >>>>

        /// <summary>
        /// Fábrica de conexões SQL centralizada.
        /// Uso: using (var conn = Db.CreateConnection()) { ... }
        /// </summary>
        /// <returns>ValueObject</returns>
        /// <remarks>
        /// Created By: Silva, Andre
        /// Created Date: 23 01 2024
        /// </remarks>
        public static SqlConnection CreateConnection()
            => CreateConnection(DefaultConnectionName);

        /// <summary>
        /// Cria uma SqlConnection usando o nome da connectionString informado.
        /// </summary>
        /// <param name="connectionName">Nome da connectionString no config.</param>
        /// <remarks>
        /// Created By: Silva, Andre
        /// Created Date: 23 01 2024
        /// </remarks>
        public static SqlConnection CreateConnection(string connectionName)
        {
            if (string.IsNullOrWhiteSpace(connectionName))
                throw new ArgumentException("Nome de conexão inválido.", nameof(connectionName));

            var cs = GetConnectionString(connectionName);
            return new SqlConnection(cs);
        }
        #endregion

    }


}

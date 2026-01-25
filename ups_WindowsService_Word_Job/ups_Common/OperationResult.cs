
namespace ups_Common
{
    public class OperationResult<T>
    {

        #region <<<< MÉTODOS PÚBLICOS >>>>

        public bool Success { get; set; }

        public T Data { get; set; }

        public string Error { get; set; }

        /// <summary>
        /// Preenchimento de gets e sets das classes com sucesso 
        /// </summary>
        /// <param name="data"></param>
        /// <returns>OperationResult</returns>
        /// <remarks>
        /// Created By: Silva, André
        /// Created Date: 26 01 2026
        /// </remarks>
        public static OperationResult<T> Ok(T data) =>
            new OperationResult<T>
            {
                Success = true,
                Data = data
            };


        /// <summary>
        /// Preenchimento de gets e sets das classes com falha
        /// </summary>
        /// <param name="error"></param>
        /// <returns>OperationResult</returns>
        /// <remarks>
        /// Created By: Silva, André
        /// Created Date: 26 01 2026
        /// </remarks>
        public static OperationResult<T> Fail(string error) =>
            new OperationResult<T>
            {
                Success = false,
                Error = error
            };
        #endregion
    }

}

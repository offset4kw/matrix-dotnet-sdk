using System;
namespace MatrixSDK.Exceptions
{
	public enum MatrixErrorCodes{
		M_FORBIDDEN,
		M_UNKNOWN_TOKEN,
		M_BAD_JSON,
		M_NOT_JSON,
		M_NOT_FOUND,
		M_LIMIT_EXCEEDED,
		M_USER_IN_USE,
		M_ROOM_IN_USE,
		M_BAD_PAGINATION,
		CL_UNKNOWN_ERROR_CODE
	}

	public class MatrixException : Exception {
		public MatrixException(string message) : base(message){

		}
		public MatrixException(string message,Exception innerException) : base(message,innerException){

		}
	}

	public class MatrixUnsuccessfulConnection : MatrixException{
		public MatrixUnsuccessfulConnection(string message) : base(message){

		}
		public MatrixUnsuccessfulConnection(string message,Exception innerException) : base(message,innerException){
			
		}
	}

	public class MatrixServerError : MatrixException{
		public readonly MatrixErrorCodes ErrorCode;
		public readonly string ErrorCodeStr;

		public MatrixServerError (string errorcode, string message) : base(message){
			if (!Enum.TryParse<MatrixErrorCodes> (errorcode, out ErrorCode)) {
				ErrorCode = MatrixErrorCodes.CL_UNKNOWN_ERROR_CODE;
			}
			ErrorCodeStr = errorcode;
		}

	}
}
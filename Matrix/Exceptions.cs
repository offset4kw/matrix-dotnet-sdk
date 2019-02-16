using System;
using Matrix.Backends;
using Newtonsoft.Json.Linq;

namespace Matrix
{
	public class MatrixException : Exception {
		public MatrixException(string message) : base(message){

		}
		public MatrixException(string message,Exception innerException) : base(message,innerException){

		}
	}

	public class MatrixServerError : MatrixException {
		public readonly MatrixErrorCode ErrorCode;
		public readonly string ErrorCodeStr;
		public readonly JObject ErrorObject;

		public MatrixServerError (string errorcode, string message, JObject errorObject) : base(message){
			if (!Enum.TryParse (errorcode, out ErrorCode)) {
				ErrorCode = MatrixErrorCode.CL_UNKNOWN_ERROR_CODE;
				ErrorObject = errorObject;
			}
			ErrorCodeStr = errorcode;
		}

	}

	public class MatrixBadFormatException : MatrixException {
		public MatrixBadFormatException(string value,string type,string reason) : base(String.Format("Value \"{0}\" is not valid for type {1}, Reason: {2}",value,type,reason)){

		}
	}

}
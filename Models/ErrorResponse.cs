namespace Coolftc.Sourcelinks.Models
{
	public class ErrorResponse : Response
	{
		public int Code;		// Application specific numeric error code.
		public string Message;	// Message describing the error.
		public string Source;	// The source of the error.  Something like a call stack.

		public ErrorResponse(int code, string message, string source)
		{
			Code = code;
			Message = message;
			Source = source;
		}
	}

}

namespace Coolftc.Sourcelinks.Models
{
	public class ApiLinkResponse : Response
	{
		public string Host;     // Host name (no trailing delimiter)
		public string Path;     // Path name (leading delimiter, no trailing delimiter)
		public string Parameter;// Parameters (include leading delimiter)
		public bool Auth;       // True = authorization token required.  False = no authorization required.

		public ApiLinkResponse(string host, string path, string parameter, bool auth)
		{
			Host = host;
			Path = path;
			Parameter = parameter;
			Auth = auth;
		}
	}
}


using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Text;

namespace Coolftc.Sourcelinks.Models
{
	public class Response
	{
		public string JSON()
		{
			return JsonConvert.SerializeObject(this);
		}

		/// <summary>
		/// This turns the class into a JSON object and sets status of 200.  This should be used most of the time.
		/// </summary>
		/// <returns>An HTTP response message ready for the Internet.</returns>
		public HttpResponseMessage HTTP()
		{
			return new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent(JSON(), Encoding.UTF8, JsonMediaTypeFormatter.DefaultMediaType.MediaType),
			};
		}

		/// <summary>
		/// There are 4 ways to set response responseHeaders in Azure Functions.  The first it to have them apply to all
		/// outgoing traffic by placing them in the host.json file, which acts like a filter.  Use:
		///   "extensions":{"http":{"customHeaders":{"headername":"header-value",...}}}
		/// The second is in the same file using the "hsts" node at the same level as customHeaders, 
		/// creating the Strict-Transport-Security header loved by security folks.  Visual Studio will
		/// ignore this setting when running locally, since you are probably not using https://.
		/// NOTE: maxAge is in days.
		///   "hsts":{"isEnabled": true,"includeSubDomains": true,"maxAge": "365","preload": true}
		/// The third and forth ways are using this method, where content headers must go in the 
		/// contentHeaders and response headers in the responseHeaders parameter.
		/// In all cases above (except the hsts), you are free to make up custom headers and pass them along.
		/// </summary>
		/// <param name="status">The http status code, e.g. 404 = file not found.</param>
		/// <param name="headers">The regular response message responseHeaders.</param>
		/// <param name="contentHeaders">The more specialized response content responseHeaders.</param>
		/// <returns>An HTTP response message ready for the Internet.</returns>
		public HttpResponseMessage HTTP(HttpStatusCode status, HeaderDictionary responseHeaders = default, HeaderDictionary contentHeaders = default)
		{
			HttpResponseMessage msg = new HttpResponseMessage(status)
			{
				Content = new StringContent(JSON(), Encoding.UTF8, JsonMediaTypeFormatter.DefaultMediaType.MediaType),
			};
			if (responseHeaders != null)
			{
				foreach (var item in responseHeaders)
				{
					msg.Headers.Add(item.Key, item.Value.ToArray());
				}
			}
			if (contentHeaders != null)
			{
				foreach (var item in contentHeaders)
				{
					msg.Content.Headers.Add(item.Key, item.Value.ToArray());
				}
			}
			return msg;
		}
	}
}

using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Coolftc.Sourcelinks.Models;
using System.Net.Http;
using System.Net;
using System.Reflection;
using Coolftc.Sourcelinks.Utilities;

namespace Coolftc.Sourcelinks
{
	public static class ApiAnchorLink
    {
        /// <summary>
        /// This is a sample of how one would build out an API endpoint in Azure Functions.  This includes the signature, route
        /// change, error handling, logging, adding headers and accessing Application Settings.  Also, check out the host.json file.
        /// </summary>
        [FunctionName("ApiAnchorLink")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "v1/ApiAnchorLink")] HttpRequest req)
        {
            try
            {
                const string SOME_API_TARGET = "myhost";
                string host = "";
                string path = "";
                string parm = "";
                bool auth = false;

                string link = req.Query["target"];

				switch (link)
				{
                    case SOME_API_TARGET:
                        host = Environment.GetEnvironmentVariable("myhostHOST");
                        path = Environment.GetEnvironmentVariable("myhostPATH");
                        parm = Environment.GetEnvironmentVariable("myhostPARM");
                        auth = Convert.ToBoolean(Environment.GetEnvironmentVariable("myhostAUTH"));
                        break;
                    default:
                        throw new ClassExp(ClassExp.EXP_CODES.EXP_NOMATCH, MethodBase.GetCurrentMethod().DeclaringType.FullName, "Unknown Target = " + link, HttpStatusCode.NotFound);
                }

                ApiLinkResponse apiLinkResponse = new ApiLinkResponse(host, path, parm, auth);

                // Play around with adding some return headers.
                HeaderDictionary headerC = new HeaderDictionary();
                headerC.Add("Expires", DateTime.UtcNow.AddYears(-1).ToString("r"));
                HeaderDictionary headerM = new HeaderDictionary();
                headerM.Add("Cache-Control", "no-cache, no-store");

                // This .HTTP call is for demonstration, usually just call .HTTP() without all the extra stuff.
                // For example, if not setting the hearders -> return apiLinkResponse.HTTP(); is all that is required.
                return apiLinkResponse.HTTP(HttpStatusCode.OK, contentHeaders: headerC, responseHeaders: headerM);
            }
            #region Web Service Exception Catch
            /* To support the Error Handling response requires the Function to return a HttpResponseMessage. */
            catch (ClassExp kx)
            {
                string holdSrc = Environment.GetEnvironmentVariable("ApplicationName") + "." + MethodBase.GetCurrentMethod().DeclaringType.FullName + "--" + kx.codeSource;
                ServiceLedger.Entry(kx.code, ServiceLedger.SEV_CODES.SEV_EXCEPTION, kx.codeDesc(), holdSrc, ClassExp.GetIP(req));
                return kx.codeResponse().HTTP(kx.codeHttp);
            }
            catch (Exception ex)
            {
                string holdSrc = Environment.GetEnvironmentVariable("ApplicationName") + "." + MethodBase.GetCurrentMethod().DeclaringType.FullName + "--" + ex.Source;
                ClassExp kx = new ClassExp(ClassExp.EXP_CODES.EXP_UNKNOWN, holdSrc, ex.Message, HttpStatusCode.InternalServerError);
                ServiceLedger.Entry(kx.code, ServiceLedger.SEV_CODES.SEV_EXCEPTION, kx.codeDesc(), holdSrc, ClassExp.GetIP(req));
                return kx.codeResponse().HTTP(kx.codeHttp);
            }
            #endregion
        }
    }
}

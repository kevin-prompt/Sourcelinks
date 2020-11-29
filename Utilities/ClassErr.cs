using Coolftc.Sourcelinks.Models;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Net;

namespace Coolftc.Sourcelinks.Utilities
{
    public class ClassExp : Exception
    {
        // To use this class: throw new ClassExp(ClassExp.EXP_CODES.EXP_???, this.ToString());
        // Exception Codes
        public enum EXP_CODES
        {
            EXP_OK = 0,
            EXP_UNKNOWN = 17000,
            EXP_CONFIG = 17001,
            EXP_NOMATCH = 17002,
            EXP_REQFIELD = 17003,
            EXP_NODATA = 17004,
            EXP_DUPDATA = 17005,
            EXP_OUTRANGE = 17006,
            EXP_NOT_ALLOWED = 17007,
            EXP_TRANS = 17008,
            EXP_PREG = 17009,
            EXP_EXPIRED = 17010,
            EXP_PARSE_FAIL = 17011,
            EXP_AUTH_FAIL = 17012,
            EXP_NOREF = 17017,
            EXP_MAX_CALLS = 17101,
            EXP_TS_FAIL = 17201,
            EXP_TS_SIZE = 17202,
            EXP_WEB_GEN = 17301,
            EXP_WEB_NOMATCH = 17302,
            EXP_WEB_ALTKEY = 17303,
            EXP_WEB_NODATA = 17304,
            EXP_API_LIMIT = 17400,
            EXP_SYS_DBDOWN = 17500,
            HLP_INITALIZED = 20100,
            HLP_WORKED = 20110

        };
        public enum LGN_CODES
        {
            LNG_AMERICAN    // American English
        };

        // Internal State
        private EXP_CODES expCode = EXP_CODES.EXP_OK;
        private string expSource = "";
        private string expDetail = "No Detail Available";
        private HttpStatusCode httpStat = HttpStatusCode.OK;
        private const string GENERIC_ERROR_MSG = "Unknown or System generated error.";

        // Constructors
        public ClassExp(EXP_CODES code, string source)
            : base(code.ToString())
        {
            expCode = code;
            expSource = source;
        }

        public ClassExp(EXP_CODES code, string source, string detail)
            : base(code.ToString())
        {
            expCode = code;
            expSource = source;
            expDetail = detail;
        }

        public ClassExp(EXP_CODES code, string source, HttpStatusCode http)
            : base(code.ToString())
        {
            expCode = code;
            expSource = source;
            httpStat = http;
        }

        public ClassExp(EXP_CODES code, string source, string detail, HttpStatusCode http)
            : base(code.ToString())
        {
            expCode = code;
            expSource = source;
            expDetail = detail;
            httpStat = http;
        }

        // Properties
        public int codeNbr
        {
            get { return (int)expCode; }
        }
        public EXP_CODES code
        {
            get { return expCode; }
        }
        public HttpStatusCode codeHttp
        {
            get { return httpStat; }
        }
        public string codeSource
        {
            get { return expSource; }
        }
        public string codeDesc()
        {
            return codeDesc(LGN_CODES.LNG_AMERICAN);  // Default Language English
        }
        public string codeDesc(LGN_CODES language)
        {
            return desc(language);
        }

        public Dictionary<string, string> codeMap(HttpRequest req, string extra)
        {
            Dictionary<string, string> holdCodes = new System.Collections.Generic.Dictionary<string, string>();
            holdCodes.Add("Code", code.ToString());
            holdCodes.Add("HTTP", httpStat.ToString() + "(" + (int)httpStat + ")");
            holdCodes.Add("Source", codeSource);
            holdCodes.Add("Detail", expDetail);
            holdCodes.Add("Extra", extra);
            holdCodes.Add("Network", GetIP(req));
            return holdCodes;
        }

        // The safe supports hiding the detailed message from the client.
        public ErrorResponse codeResponse(bool safe = false)
		{
            return new ErrorResponse(codeNbr, safe ? GENERIC_ERROR_MSG : codeDesc(), codeSource);
		}

        // Nice to have the IP Address sometimes.
        public static String GetIP(HttpRequest req)
        {
            try
            {
                string ip = req.HttpContext.Request.GetTypedHeaders().Get<String>("HTTP_X_FORWARDED_FOR");

                if (string.IsNullOrEmpty(ip))
                {
                    ip = req.HttpContext.Request.GetTypedHeaders().Get<String>("REMOTE_ADDR");
                }

                // Sometimes get this on localhost.
                if (ip != null && ip == "::1") { ip = "127.0.0.1"; }

                if (string.IsNullOrEmpty(ip))
				{
                    ip = req.HttpContext.Connection.RemoteIpAddress.ToString();
				}

                return ip ?? "";
            } catch { return "No IP Available.";  }
        }

        private string desc(LGN_CODES lng)
        {
            string ldesc = "";
            switch (expCode)
            {
                case EXP_CODES.EXP_UNKNOWN:
                    if (lng.Equals(LGN_CODES.LNG_AMERICAN)) ldesc = GENERIC_ERROR_MSG;
                    break;
                case EXP_CODES.EXP_CONFIG:
                    if (lng.Equals(LGN_CODES.LNG_AMERICAN)) ldesc = "There was a problem reading some parameters from the configuration file.";
                    break;
                case EXP_CODES.EXP_NOMATCH:
                    if (lng.Equals(LGN_CODES.LNG_AMERICAN)) ldesc = "An expected match was not found in the data.";
                    break;
                case EXP_CODES.EXP_REQFIELD:
                    if (lng.Equals(LGN_CODES.LNG_AMERICAN)) ldesc = "A required field is missing data.";
                    break;
                case EXP_CODES.EXP_NODATA:
                    if (lng.Equals(LGN_CODES.LNG_AMERICAN)) ldesc = "The data requested does not seem to be available, please recheck the input values.";
                    break;
                case EXP_CODES.EXP_DUPDATA:
                    if (lng.Equals(LGN_CODES.LNG_AMERICAN)) ldesc = "The data to be created or changed already exists in the system and does not allow duplicate entries.";
                    break;
                case EXP_CODES.EXP_OUTRANGE:
                    if (lng.Equals(LGN_CODES.LNG_AMERICAN)) ldesc = "The input data is outside the range of allowable values or is too large to be processed by the system.";
                    break;
                case EXP_CODES.EXP_NOT_ALLOWED:
                    if (lng.Equals(LGN_CODES.LNG_AMERICAN)) ldesc = "The action is not allowed for this customer.";
                    break;
                case EXP_CODES.EXP_TRANS:
                    if (lng.Equals(LGN_CODES.LNG_AMERICAN)) ldesc = "Unable to perform all the actions needed to complete the transaction.";
                    break;
                case EXP_CODES.EXP_PREG:
                    if (lng.Equals(LGN_CODES.LNG_AMERICAN)) ldesc = "Please properly register the application before using the services.";
                    break;
                case EXP_CODES.EXP_EXPIRED:
                    if (lng.Equals(LGN_CODES.LNG_AMERICAN)) ldesc = "The data has expired and cannot be used.";
                    break;
                case EXP_CODES.EXP_PARSE_FAIL:
                    if (lng.Equals(LGN_CODES.LNG_AMERICAN)) ldesc = "The input data failed to parse into something usable.";
                    break;
                case EXP_CODES.EXP_AUTH_FAIL:
                    if (lng.Equals(LGN_CODES.LNG_AMERICAN)) ldesc = "The credentials supplied do not match any on record.";
                    break;
                case EXP_CODES.EXP_NOREF:
                    if (lng.Equals(LGN_CODES.LNG_AMERICAN)) ldesc = "The Reference Table requested does not exist.  Check that the application was installed correctly.";
                    break;
                case EXP_CODES.EXP_MAX_CALLS:
                    if (lng.Equals(LGN_CODES.LNG_AMERICAN)) ldesc = "Maximum allowed calls to the Web Service exceeded.";
                    break;
                case EXP_CODES.EXP_TS_FAIL:
                    if (lng.Equals(LGN_CODES.LNG_AMERICAN)) ldesc = "Table Storage unable to process request.";
                    break;
                case EXP_CODES.EXP_TS_SIZE:
                    if (lng.Equals(LGN_CODES.LNG_AMERICAN)) ldesc = "Table Storage unable to store data, it is too large.";
                    break;
                case EXP_CODES.EXP_WEB_GEN:
                    if (lng.Equals(LGN_CODES.LNG_AMERICAN)) ldesc = "The Web Request to the external web service failed.";
                    break;
                case EXP_CODES.EXP_WEB_NOMATCH:
                    if (lng.Equals(LGN_CODES.LNG_AMERICAN)) ldesc = "The Web Request query found no match.";
                    break;
                case EXP_CODES.EXP_WEB_ALTKEY:
                    if (lng.Equals(LGN_CODES.LNG_AMERICAN)) ldesc = "The Web Request used an alternate key.";
                    break;
                case EXP_CODES.EXP_WEB_NODATA:
                    if (lng.Equals(LGN_CODES.LNG_AMERICAN)) ldesc = "The Web Request returned less data than expected.";
                    break;
                case EXP_CODES.EXP_API_LIMIT:
                    if (lng.Equals(LGN_CODES.LNG_AMERICAN)) ldesc = "The API limit for this invocation exceeded.";
                    break;
                case EXP_CODES.EXP_SYS_DBDOWN:
                    if (lng.Equals(LGN_CODES.LNG_AMERICAN)) ldesc = "The Database is not currently responding.";
                    break;
                default:
                    if (lng.Equals(LGN_CODES.LNG_AMERICAN)) ldesc = "No matching description for error.";
                    break;
            }
            if (expDetail == "No Detail Available" && httpStat != HttpStatusCode.OK)
            {
                expDetail = "HTTP Status " + httpStat.ToString() + "(" + (int)httpStat + ")";
            }

            return ldesc + " - " + expDetail;
        }
    }
}

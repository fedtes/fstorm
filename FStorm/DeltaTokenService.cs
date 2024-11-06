using System;
using System.Text;
using System.Text.RegularExpressions;

namespace FStorm;

public class DeltaTokenService
{

    public string ComputeSkipToken(ICompilerContext context)
    {
        var pag =  context.GetPaginationClause();
        var u1 =  new Regex("\\$top=\\d+&?").Replace(context.UriRequest, String.Empty);
        u1 =  new Regex("\\$skip=\\d+&?").Replace(u1, String.Empty);
        u1 = u1.TrimEnd('&');
        u1 = u1 + $"#{pag.Skip ?? 0};{pag.Top ?? 0}";
        return "$skiptoken=" + Convert.ToBase64String(Encoding.Unicode.GetBytes(u1));
    }

    public string DecodeSkipToken(ICompilerContext context)
    {
        var skiptoken = context.GetSkipToken();
        var decodedToken = Encoding.Unicode.GetString(Convert.FromBase64String(skiptoken));
        var x = decodedToken.Split("#");
        var _url = x[0];
        var y = x[1].Split(";");
        var _skip = int.Parse(y[0]);
        var _top = int.Parse(y[1]);
        _skip = _skip + _top;
        if (_url.Contains("?"))
        {
            if (_url.EndsWith("?"))
            {
                return _url + "$skip=" + _skip .ToString() + "&$top=" + _top.ToString();
            }
            else
            {
                return _url + "&" + "$skip=" + _skip .ToString() + "&$top=" + _top.ToString();
            }
        }
        else
        {
            return _url + "?" + "$skip=" + _skip .ToString() + "&$top=" + _top.ToString();
        }

    }

}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using PX.Data;

namespace LuisAutoMailer
{
    public class LuisAPI : PXGraph<LuisAPI>
    {
        public async Task<string> GetPrediction(string querystring)
        {
            string add = "https://westus.api.cognitive.microsoft.com/luis/v2.0/apps/717af479-1d8c-40f9-afb3-fd800eda8bca?verbose=true&timezoneOffset=0&subscription-key=2b9324da598d4b52b6b78eccb45a7a05";
           string querystring2 = System.Web.HttpUtility.UrlEncode(querystring);
            add = add + "q=" + querystring2;
            Uri address = new Uri(add);
            using (var client = new HttpClient())
            {
                try
                {
                    var result = await client.GetAsync(address);
                    if (result.IsSuccessStatusCode)
                    {
                        return await result.Content.ReadAsStringAsync();
                    }
                    else
                    {
                        PXTrace.WriteInformation(address.ToString());
                        PXTrace.WriteInformation(result.ToString());
                        return result.ToString();
                    }
                }
                catch (Exception ex)
                {
                    throw new PX.Data.PXException(ex.Message);
                }
            }

        }
        public async Task<string> PostPrediction(string querystring)
        {
            string add = "https://westus.api.cognitive.microsoft.com/luis/v2.0/apps/717af479-1d8c-40f9-afb3-fd800eda8bca";
    
            Uri address = new Uri(add);
            using (var client = new HttpClient(new HttpClientHandler()))
            {
                client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", "2b9324da598d4b52b6b78eccb45a7a05");
                try
                {
                    var result = await client.PostAsJsonAsync(address,querystring);
                    if (result.IsSuccessStatusCode)
                    {
                        return await result.Content.ReadAsStringAsync();
                    }
                    else
                    {
                        PXTrace.WriteInformation(address.ToString());
                        PXTrace.WriteInformation(result.ToString());
                        return result.ToString();
                    }
                }
                catch (Exception ex)
                {
                    throw new PX.Data.PXException(ex.Message);
                }
            }

        }
    }
}

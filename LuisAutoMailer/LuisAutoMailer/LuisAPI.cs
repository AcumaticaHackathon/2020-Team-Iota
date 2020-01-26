using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;

namespace LuisAutoMailer
{
    class LuisAPI
    {
        public async Task<string> GetPrediction(string querystring)
        {
            string add = "https://westus.api.cognitive.microsoft.com/luis/v2.0/apps/717af479-1d8c-40f9-afb3-fd800eda8bca?verbose=true&timezoneOffset=0&subscription-key=2b9324da598d4b52b6b78eccb45a7a05";
            add = add + "q=" + querystring;
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
                        return result.ToString();
                    }
                }
                catch (Exception ex)
                {
                    throw new PX.Data.PXException( ex.Message);
                }
            }

        }

    }
}

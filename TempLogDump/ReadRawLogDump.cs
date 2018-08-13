using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Text;

namespace TempLogDump
{
    public static class ReadRawLogDump
    {
        [FunctionName("ReadRawLogDump")]
        public static async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            //[Blob("rawlogs/{rand-guid}.txt", FileAccess.Write)] Stream blob,
            [Blob("rawlogs/{datetime:yyyy-MM-dd-HH-mm-ss}.log", FileAccess.Write)] Stream blob,
            ILogger log)
        {
            log.LogInformation("ReadRawLogDump processed a request.");

            var requestBody = new StreamReader(req.Body).ReadToEnd();

            if (string.IsNullOrWhiteSpace(requestBody))
                return new BadRequestResult();

            using (StreamWriter streamWriter = new StreamWriter(blob))
            {
                streamWriter.Write(requestBody);
                streamWriter.Flush();
                streamWriter.Close();
            }

            // Read values from request body:
            string[] lines = requestBody.Split(
                new[] { "\r\n", "\r", "\n" },
                StringSplitOptions.None
            );

            var temps = new Temps();

            foreach (var line in lines)
            {
                if (line.StartsWith("dev.cpu"))
                {
                    var element = new Element();
                    element.Name = $"CPU{line[8]}";
                    element.Temp = int.Parse(line.Substring(23, 2));
                    temps.CPUs.Add(element);
                }
                if (line.StartsWith("ada"))
                {
                    var element = new Element();
                    element.Name = $"ADA{line[3]}";
                    element.Temp = int.Parse(line.Substring(5, 2));
                    temps.HDDs.Add(element);
                }
            }

            
            var json = JsonConvert.SerializeObject(temps);

            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri("https://prod-22.westeurope.logic.azure.com:443/workflows/c524750c6dd9459bb261bf307a6759f4/triggers/manual/paths/invoke?api-version=2016-10-01&sp=%2Ftriggers%2Fmanual%2Frun&sv=1.0&sig=MChJ0ExCmdzIHUk8fH53H3Xu24_S0MszJSHXyk_9z_k");
                try
                {
                    var result = await client.PostAsJsonAsync(string.Empty, json);
                    //var r = await client.PostAsJsonAsync(string.Empty, temps);
                    //var content = new StringContent(json.ToString(), Encoding.UTF8, "application/json");
                    //var rr = await client.PostAsync(string.Empty, content);
                }
                catch (Exception ex)
                {

                }
                //var resultContent = await result.Content.ReadAsStringAsync();
            }

            return new OkResult();
        }

        private class Temps
        {            
            public List<Element> CPUs { get; set; } = new List<Element>();
            public List<Element> HDDs { get; set; } = new List<Element>();
        }

        private class Element
        {
            public string Name { get; set; }
            public int Temp { get; set; }
            public DateTime Time { get; set; } = DateTime.Now;
        }
    }
}
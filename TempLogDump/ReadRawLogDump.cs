using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace TempLogDump
{
    public static class ReadRawLogDump
    {
        [FunctionName("ReadRawLogDump")]
        public static IActionResult Run([HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
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

            return new OkResult();
        }
    }
}
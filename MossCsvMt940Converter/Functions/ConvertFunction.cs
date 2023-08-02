using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using MossCsvMt940Converter.Models;
using Newtonsoft.Json;

namespace MossCsvMt940Converter.Functions;

public static class ConvertFunction
{
    [FunctionName("ConvertFunction")]
    public static async Task<IActionResult> RunAsync([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req, ILogger log)
    {
        var file = req.Form.Files.SingleOrDefault();
        
        if (file == null)
            return new BadRequestObjectResult("No file was uploaded.");

        var csvStream = file.OpenReadStream();
        var transactions = new List<TransactionData>();

        var dutchCulture = new CultureInfo("nl-NL");
        var csvParser = new Regex(",(?=(?:[^\"]*\"[^\"]*\")*(?![^\"]*\"))");

        using (var reader = new StreamReader(csvStream))
        {
            // Skip the first line.
            await reader.ReadLineAsync();
            
            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync();
                
                if (line == null)
                    continue;
                
                var values = csvParser.Split(line);
                
                // Trim all the quotes of each value.
                for (var i = 0; i < values.Length; i++)
                    values[i] = values[i].Trim('"');

                var transaction = new TransactionData
                {
                    PaymentDate = DateTime.Parse(values[1]),
                    SettlementDate = values[2] != string.Empty ? DateTime.Parse(values[2]) : DateTime.Parse(values[1]),
                    Amount = decimal.Parse(values[4], new NumberFormatInfo
                    {
                        CurrencyGroupSeparator = ",",
                        CurrencyDecimalSeparator = "."
                    }),
                    MerchantName = values[9],
                    Category = values[10],
                    MerchantAndCardDescription = values[29],
                    MossTransactionUrl = values[30]
                };

                transactions.Add(transaction);
            }
        }

        var mt940 = new Mt940(transactions);

        return new OkObjectResult(mt940.ToString());
    }
}


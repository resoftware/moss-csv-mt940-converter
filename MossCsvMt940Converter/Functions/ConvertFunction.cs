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
    private static List<string> Headers { get; set; }
    
    [FunctionName("ConvertFunction")]
    public static async Task<IActionResult> RunAsync([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req, ILogger log)
    {
        var file = req.Form.Files.SingleOrDefault();
        
        if (file == null)
            return new BadRequestObjectResult("No file was uploaded.");

        var csvStream = file.OpenReadStream();
        var transactions = new List<TransactionData>();

        var csvParser = new Regex(",(?=(?:[^\"]*\"[^\"]*\")*(?![^\"]*\"))");

        using (var reader = new StreamReader(csvStream))
        {
            Headers = (await reader.ReadLineAsync())?.Split(',').Select(h => h.Trim('"')).ToList();
            
            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync();
                
                if (line == null)
                    continue;
                
                var values = csvParser.Split(line);
                
                // Trim all the quotes of each value.
                for (var i = 0; i < values.Length; i++)
                    values[i] = values[i].Trim('"');

                var settlementDateIndex = GetIndexForFieldName("Settlement Date");
                var paymentDateIndex = GetIndexForFieldName("Payment Date");
                var amountIndex = GetIndexForFieldName("Amount");
                var merchantNameIndex = GetIndexForFieldName("Merchant Name");
                var categoryIndex = GetIndexForFieldName("Category");
                var merchantAndCardDescriptionIndex = GetIndexForFieldName("Merchant and Card Description");
                var mossTransactionUrlIndex = GetIndexForFieldName("Moss Record URL");
                
                var transaction = new TransactionData
                {
                    PaymentDate = DateTime.Parse(values[paymentDateIndex]),
                    SettlementDate = values[settlementDateIndex] != string.Empty ? DateTime.Parse(values[settlementDateIndex]) : DateTime.Parse(values[paymentDateIndex]),
                    Amount = decimal.Parse(values[amountIndex], new NumberFormatInfo
                    {
                        CurrencyGroupSeparator = ",",
                        CurrencyDecimalSeparator = "."
                    }),
                    MerchantName = values[merchantNameIndex],
                    Category = values[categoryIndex],
                    MerchantAndCardDescription = values[merchantAndCardDescriptionIndex],
                    MossTransactionUrl = values[mossTransactionUrlIndex]
                };

                transactions.Add(transaction);
            }
        }

        var mt940 = new Mt940(transactions);

        return new OkObjectResult(mt940.ToString());
    }

    public static int GetIndexForFieldName(string fieldName)
    {
        return Headers.IndexOf(fieldName);
    }
}


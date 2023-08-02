//
// Mt940.cs
//
// Trevi Awater
// 02-08-2023
//
// © MossCsvMt940Converter
//

using System;
using System.Collections.Generic;

namespace MossCsvMt940Converter.Models;

public class Mt940
{
    private string Content { get; }
    
    public Mt940(List<TransactionData> transactions)
    {
        Content = "{1:F01INGBNL2ABXXX0000000000}\n";

        Content += "{2:I940INGBNL2AXXXN}\n";
        Content += "{4:\n";
        Content += ":20:P230802000000001\n";
        Content += ":25:NL90KNAB0402850149\n";
        Content += ":28C:00000\n";
        Content += ":60F:C221130EUR0\n";
            
        Content += AddTransactions(transactions);
            
        Content += ":62F:C230801EUR0\n";
        Content += "-}";
    }
    
    private static string AddTransactions(List<TransactionData> transactions)
    {
        var results = "";

        foreach (var transaction in transactions)
        {
            var creditOrDebit = transaction.Amount > 0 ? "C" : "D";
            
            results += @$":61:{transaction.PaymentDate:yyMMdd}{transaction.SettlementDate:MMdd}{creditOrDebit}{Math.Abs(transaction.Amount)}NDDTEREF//22340610537069";
            results += "\n/TRCD/01018/\n";
            results += $":86:/EREF/{transaction.MossTransactionUrl}//MARF/{transaction.Category}//REMI/US\n";
            results += $"TD//{transaction.MerchantAndCardDescription}//ULTC/{transaction.MerchantName}//\n";
        }
        
        return results;
    }

    public override string ToString()
    {
        return Content;
    }
}

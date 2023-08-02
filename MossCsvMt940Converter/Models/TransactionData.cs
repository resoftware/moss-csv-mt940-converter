//
// TransactionData.cs
//
// Trevi Awater
// 02-08-2023
//
// © MossCsvMt940Converter
//

using System;

namespace MossCsvMt940Converter.Models;

public class TransactionData
{
    public DateTime PaymentDate { get; set; }
    
    public DateTime SettlementDate { get; set; }
    
    public decimal Amount { get; set; }
    
    public string MerchantName { get; set; }
    
    public string Category { get; set; }

    public string MerchantAndCardDescription { get; set; }

    public string MossTransactionUrl { get; set; }
}

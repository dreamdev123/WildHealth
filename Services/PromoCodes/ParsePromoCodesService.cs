using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using OfficeOpenXml;
using WildHealth.Application.Commands.PromoCodes;
using WildHealth.Domain.Constants;
using WildHealth.Domain.Entities.Payments;

namespace WildHealth.Application.Services.PromoCodes;

public class ParsePromoCodesService : IParsePromoCodesService
{
    public async Task<List<CreatePromoCodeCommand>> Parse(IFormFile file, List<PaymentPlan> paymentPlans, int practiceId)
    {
        await using var stream = new MemoryStream();
        await file.CopyToAsync(stream);
        using var excelPackage = new ExcelPackage(stream);
        var sheet = excelPackage.Workbook.Worksheets.First();

        return ExtractPromoCodes(sheet, paymentPlans, practiceId).ToList();
    }

    private IEnumerable<CreatePromoCodeCommand> ExtractPromoCodes(ExcelWorksheet sheet, List<PaymentPlan> paymentPlans, int practiceId)
    {
        var row = 2;
        
        while(true)
        {
            if (sheet.Cells[row, 1].Value == null) break; //when we reach the empty row we stop the cycle

            var code = sheet.Cells[row, 1].GetValue<string>();
            var discountType = sheet.Cells[row, 3].GetValue<string>();
            var discount = sheet.Cells[row, 4].GetValue<decimal>();
            var paymentPlansSeparatedByComa = sheet.Cells[row, 5].GetValue<string>();
            var expirationDate = DefaultIfThrows<DateTime?>(() => sheet.Cells[row, 6].GetValue<DateTime>());
            var discountLabs = sheet.Cells[row, 7].GetValue<string>();
            var discountStartupFee = sheet.Cells[row, 8].GetValue<string>();
            var isInsurance = sheet.Cells[row, 10].GetValue<string>();
            var isLive = sheet.Cells[row, 11].GetValue<string>();
            var description = sheet.Cells[row, 12].GetValue<string>();

            if (isLive.Trim().ToLower() == "yes")
            {
                yield return new CreatePromoCodeCommand(
                    code,
                    discount,
                    discountType == "%" ? DiscountType.Percentage : DiscountType.Nominal,
                    description,
                    expirationDate == default(DateTime) ? null : expirationDate,
                    GetPaymentPlanIds(),
                    discountStartupFee?.ToUpper() == "Y",
                    discountLabs?.ToUpper() == "Y",
                    isInsurance?.ToUpper() == "Y",
                    practiceId
                );
            }

            int[] GetPaymentPlanIds()
            {
                if (string.IsNullOrEmpty(paymentPlansSeparatedByComa)) return Array.Empty<int>();
                
                var names = paymentPlansSeparatedByComa
                    .Split(",")
                    .Select(p => p.Trim())
                    .ToArray();
                
                return paymentPlans
                    .Where(p => names.Contains(p.Name))
                    .Select(x => x.GetId())
                    .ToArray();
            }
             
            row++;
        }
    }

    private T? DefaultIfThrows<T>(Func<T> f)
    {
        try
        {
            return f();
        }
        catch 
        {
            return default(T?);
        }
    }
}
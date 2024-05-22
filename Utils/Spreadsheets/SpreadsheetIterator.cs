using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Http;

namespace WildHealth.Application.Utils.Spreadsheets;

public class SpreadsheetIterator : ISpreadsheetIterator
{
    private readonly IFormFile _formFile;
    private readonly int _startAtRow;
    
    public SpreadsheetIterator(
        IFormFile formFile,
        int startAtRow = 1)
    {
        _formFile = formFile;
        _startAtRow = startAtRow;
    }

    public async Task Iterate(
        IDictionary<string, string> importantTitles,
        Func<IDictionary<string, string>, Task> rowResultHandler)
    {
        //Lets open the existing excel file and read through its content . Open the excel using open xml sdk
        using var doc = SpreadsheetDocument.Open(_formFile.OpenReadStream(), false);
        var workbookPart = doc.WorkbookPart;
        if (workbookPart is null) return;
        var sheetCollection = workbookPart.Workbook.GetFirstChild<Sheets>();
        if (sheetCollection is null) return;
        
        //using for each loop to get the sheet from the sheet collection  
        foreach (Sheet sheet in sheetCollection)
        {
            //statement to get the worksheet object by using the sheet id  
            var sheetId = sheet.Id?.Value;
            if(sheetId is null || workbookPart is null) continue;
            var theWorksheet = (workbookPart.GetPartById(sheetId) as WorksheetPart)?.Worksheet;
            if (theWorksheet is null) continue;
            
            var sheetData = theWorksheet.GetFirstChild<SheetData>() as SheetData;
            if(sheetData is null) continue;
            
            var rowCount = 0;
            foreach (var currentRow in sheetData)
            {
                rowCount++;
                var isTitleRow = rowCount == 1;

                if (rowCount < _startAtRow && !isTitleRow)
                    continue;

                var rowResults = importantTitles.ToDictionary(o => o.Key, o => string.Empty);

                foreach (Cell currentCell in currentRow)
                {
                    //statement to take the integer value  
                    var currentCellValue = string.Empty;
                    if (currentCell.DataType != null)
                    {
                        if (currentCell.DataType == CellValues.SharedString)
                        {
                            if (int.TryParse(currentCell.InnerText, out var id))
                            {
                                var item = workbookPart
                                    .SharedStringTablePart
                                    ?.SharedStringTable
                                    ?.Elements<SharedStringItem>()
                                    ?.ElementAt(id);

                                if (item?.Text != null)
                                {
                                    //code to take the string value  
                                    currentCellValue = item.Text.Text;
                                }
                                else if (item?.InnerText != null)
                                {
                                    currentCellValue = item.InnerText;
                                }
                            }
                            else
                            {
                                currentCellValue = currentCell.InnerText;
                            }
                        }

                        var refValue = currentCell.CellReference?.Value;

                        if(refValue is null) continue;
                        
                        var rgx = new Regex("[^a-zA-Z]");
                        var column = rgx.Replace(refValue, "");

                        if (isTitleRow)
                        {
                            if (importantTitles.Keys.Contains(currentCellValue))
                            {
                                importantTitles[currentCellValue] = column;
                            }
                        }
                        else
                        {
                            if (!importantTitles.Values.Contains(column))
                            {
                                continue;
                            }

                            var title = importantTitles.First(o => o.Value.Equals(column)).Key;
                            
                            rowResults[title] = currentCellValue;
                        }
                    }
                }

                // Pass the row results to the handler
                if (!isTitleRow)
                {
                    await rowResultHandler(rowResults);
                }
            }
        }
    }
}
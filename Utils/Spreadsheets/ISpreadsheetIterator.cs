using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace WildHealth.Application.Utils.Spreadsheets;

public interface ISpreadsheetIterator
{
    Task Iterate(IDictionary<string, string> importantTitles,
        Func<IDictionary<string, string>, Task> rowResultHandler);
}
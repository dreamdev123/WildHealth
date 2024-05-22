using System;
using System.Globalization;

namespace WildHealth.Application.Utils.DnaFiles;

public static class DnaFilesHelper
{
    private const int DefaultMinimumFileNameLength = 54;

    public static string GetFileBarcode(string fileName)
    {
        if (!IsFileNameValid(fileName))
        {
            return fileName;
        }

        var parts = fileName.Split('_');

        return parts.Length == 0 ? fileName : parts[2];
    }
    
    public static DateTime GetDateFromFileName(string fileName)
    {
        if (!IsFileNameValid(fileName))
        {
            return DateTime.MinValue;
        }

        var parts = fileName.Split('_');

        if (parts.Length == 0)
        {
            return DateTime.MinValue;
        }

        var datePart = parts[1];
        if (datePart.Length != 8) //8 - length of date characters
        {
            return DateTime.MinValue;
        }

        var isDate = DateTime.TryParseExact(datePart, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date);

        return isDate ? date : DateTime.MinValue;
    }
    
    private static bool IsFileNameValid(string fileName)
    {
        return fileName.Length >= DefaultMinimumFileNameLength;
    }
}
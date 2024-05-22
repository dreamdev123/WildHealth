using System;
using System.IO;
using Microsoft.AspNetCore.Http;

namespace WildHealth.Application.Utils.Converter;

public class ConverterUtil : IConverterUtil
{
    public IFormFile ConvertBase64ToFormFile(string base64String, string fileName)
    {
        var fileBytes = Convert.FromBase64String(base64String);
        
        var memoryStream = new MemoryStream(fileBytes);
        
        return new FormFile(memoryStream, 0, memoryStream.Length, "file", fileName);
    }

    public IFormFile ConvertStringToFormFile(string content, string fileName)
    {
        var fileBytes = System.Text.Encoding.UTF8.GetBytes(content);
        
        var memoryStream = new MemoryStream(fileBytes);
        
        return new FormFile(memoryStream, 0, memoryStream.Length, "file", fileName);
    }
}
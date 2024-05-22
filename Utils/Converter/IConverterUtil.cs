using Microsoft.AspNetCore.Http;

namespace WildHealth.Application.Utils.Converter;

public interface IConverterUtil
{
    IFormFile ConvertBase64ToFormFile(string base64String, string fileName);

    IFormFile ConvertStringToFormFile(string content, string fileName);
}
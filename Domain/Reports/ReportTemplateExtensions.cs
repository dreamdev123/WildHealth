using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using WildHealth.Domain.Constants;
using WildHealth.Domain.Entities.Reports;

namespace WildHealth.Application.Domain.Reports;

public static class ReportTemplateExtensions
{
    public static string[] GetGeneralChunks(this ReportTemplate source)
    {
        var chunks = new List<string>();

        foreach (var chapter in (JArray)source.Template[ReportConstants.ReportTemplateKeys.Chapters])
        {
            var pages = chapter[ReportConstants.ReportTemplateKeys.Pages];
            if (pages is null)
            {
                continue;
            }

            foreach (var page in pages)
            {
                var sections = page[ReportConstants.ReportTemplateKeys.Sections];
                if (sections is null)
                {
                    continue;
                }

                foreach (var section in sections)
                {
                    var sectionType = section[ReportConstants.ReportTemplateKeys.Type];
                    if (sectionType is null ||
                        sectionType.ToString() != ReportConstants.ReportTemplateSectionTypes.Generic)
                    {
                        continue;
                    }

                    var sectionContent = section[ReportConstants.ReportTemplateKeys.Content];
                    if (sectionContent is null)
                    {
                        continue;
                    }

                    var chunk = "";

                    foreach (JProperty contentItem in sectionContent)
                    {
                        var contentType = ((JProperty)contentItem).Name;
                        switch (contentType)
                        {
                            case ReportConstants.ReportTemplateKeys.Text:
                            case ReportConstants.ReportTemplateKeys.SubText:
                                chunk += $"{contentItem.Value}\n";
                                break;
                            case ReportConstants.ReportTemplateKeys.TextList:
                                var texts = contentItem.Value.Children();
                                foreach (var text in texts)
                                {
                                    chunk += $"{text}\n";
                                }

                                break;
                            default:
                                break;
                        }
                    }

                    if (!string.IsNullOrEmpty(chunk))
                    {
                        chunks.Add(chunk);
                    }
                }
            }
        } 
        
        return chunks.ToArray();
    }
    
    public static string GenerateFileName(this ReportTemplate source) => $"ReportTemplate-{source.ReportType}-{source.Version}.txt";
    
    public static string GenerateDocumentSourceName(this ReportTemplate source) => $"Report Template ({source.ReportType})";
}
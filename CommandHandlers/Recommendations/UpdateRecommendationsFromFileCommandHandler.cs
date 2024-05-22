using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using OfficeOpenXml;
using WildHealth.Application.Commands.Recommendations;
using WildHealth.Domain.Entities.Recommendations;
using WildHealth.Report.Services.Recommendations;
using WildHealth.Shared.Exceptions;

namespace WildHealth.Application.CommandHandlers.Recommendations;

public class UpdateRecommendationsFromFileCommandHandler : IRequestHandler<UpdateRecommendationsFromFileCommand>
{
    private readonly IRecommendationService _recommendationService;
    private readonly ILogger _logger;

    public UpdateRecommendationsFromFileCommandHandler(
        IRecommendationService recommendationService,
        ILogger<UpdateRecommendationsFromFileCommandHandler> logger)
    {
        _recommendationService = recommendationService;
        _logger = logger;
    }

    public async Task Handle(UpdateRecommendationsFromFileCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting reading recommendations from file.");
        var recommendations = await GetRecommendationsFromFile(request.RecommendationsFile);
        _logger.LogInformation("Reading recommendations from file has finished. Starting updating the recommendations in the DB");
        await _recommendationService.UpdateBulkAsync(recommendations);
        _logger.LogInformation("Updating recommendations in the DB has finished.");
    }

    private async Task<List<Recommendation>> GetRecommendationsFromFile(IFormFile file)
    {
        await using var stream = new MemoryStream();
        await file.CopyToAsync(stream);
        using var excelPackage = new ExcelPackage(stream);
        var sheet = excelPackage.Workbook.Worksheets.First();
        AssertCorrectFileFormat(sheet);
        var recommendations = new List<Recommendation>();
        var row = 1;
        while(true)
        {
            row++;
            if (sheet.Cells[row, 1].Value == null) break; //when we reach the empty row we stop the cycle
            if (sheet.Cells[row, 1].Style.Fill.BackgroundColor.Rgb == null) continue; // we skip the rows with white background - only interested in colored ones

            var id = sheet.Cells[row, 2].GetValue<int>();
            if (recommendations.Any(x => x.Id == id)) continue; //avoiding duplicates 

            var recommendation = await _recommendationService.GetByIdAsync(id);
            if (recommendation == null) continue;
            recommendation.Content = sheet.Cells[row, 1].GetValue<string>();
            recommendations.Add(recommendation);
        }
        return recommendations;
    }

    private void AssertCorrectFileFormat(ExcelWorksheet sheet)
    {
        if (string.Compare(sheet.Cells[1, 1].GetValue<string>(), "content", StringComparison.OrdinalIgnoreCase) != 0
            || string.Compare(sheet.Cells[1, 2].GetValue<string>(), "id", StringComparison.OrdinalIgnoreCase) != 0)
        {
            throw new AppException(HttpStatusCode.BadRequest, "Incorrect file format");
        }
    }
}
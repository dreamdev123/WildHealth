using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Collections;
using System.Globalization;
using System.Threading.Tasks;
using CsvHelper.Configuration;
using System.Collections.Generic;
using System.Numerics;
using Microsoft.Extensions.Options;
using WildHealth.Domain.Models.Extensions;
using WildHealth.Application.Commands.Orders;
using WildHealth.Application.Services.Orders.Dna;
using WildHealth.Application.Utils.DateTimes;
using WildHealth.Common.Models.Orders;
using WildHealth.Common.Options;
using WildHealth.Domain.Enums.Orders;
using Microsoft.Extensions.Logging;
using Renci.SshNet;
using CsvHelper;
using MediatR;
using WildHealth.Domain.Entities.Orders;

namespace WildHealth.Application.CommandHandlers.Orders;

public class SendDnaDropShipFileCommandHandler : IRequestHandler<SendDnaDropShipFileCommand>
{
    private const string LabCorpTestCode = "19001500";
        
    private readonly OrderStatus[] _interestedStatuses =
    {
        OrderStatus.Ordered,
        OrderStatus.Placed,
        OrderStatus.Shipping,
        OrderStatus.Arrived,
        OrderStatus.ReturnProcessing,
        OrderStatus.ReturnArrived
    };
    
    private readonly CsvConfiguration _configPersons = new CsvConfiguration(CultureInfo.InvariantCulture)
    {
        HasHeaderRecord = true
    };
    
    private readonly IDnaOrdersService _dnaOrdersService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly LabCorpFtpServerOptions _labCorpFtpServerOptions;
    private readonly ILogger _logger;

    public SendDnaDropShipFileCommandHandler(
        IDnaOrdersService dnaOrdersService, 
        IDateTimeProvider dateTimeProvider, 
        IOptions<LabCorpFtpServerOptions> labCorpFtpServerOptions,
        ILogger<SendDnaDropShipFileCommandHandler> logger)
    {
        _dnaOrdersService = dnaOrdersService;
        _dateTimeProvider = dateTimeProvider;
        _labCorpFtpServerOptions = labCorpFtpServerOptions.Value;
        _logger = logger;
    }

    public async Task Handle(SendDnaDropShipFileCommand request, CancellationToken cancellationToken)
    {
        var now = _dateTimeProvider.UtcNow();
        var fromDate = now.AddDays(-7);
        
        // we use the order's placedAt value for the "from and to" comparison
        var orders = await _dnaOrdersService.SelectForIntegrationAsync(
            from: fromDate,  
            to: now,    
            statuses: _interestedStatuses
        );
        
        _logger.LogInformation($"{orders.Count()} orders have a placedAt value inclusive between {fromDate.Date} and {now.Date}");

        var readyOrders = orders.Where(IsReady).ToList();

        var barcodes = String.Join(", ", readyOrders.Select(o => o.Barcode));
        
        _logger.LogInformation($"Sending orders with the following barcodes: {barcodes}");
        
        var records = readyOrders.Select(x => new DnaDropShipOrderModel
        {
            //Quoting Labcorp:
            //  The barcode ID is what we use as the specimen ID.  The test code will always be 19001500- it’s our standard code
            //  See: https://wildhealth.atlassian.net/browse/CLAR-7226
            SpecimenID = x.Barcode,
            TestCode = LabCorpTestCode,
            
            //Quoting Labcorp:
            //  The update flag and ordering physician are not necessary for file upload and can be left blank. 
            //  See: https://wildhealth.atlassian.net/browse/CLAR-7226
            UpdateFlag = String.Empty,
            OrderingPhysician = String.Empty,
            
            Sex = x.Patient.User.Gender.ToString(),
            DOB = FormatDate(x.Patient.User.Birthday),
            TestOrderDate = FormatDate(x.PlacedAt)
        });

        var count = records.Count();
        _logger.LogInformation($"{count} orders are ready to be sent.");
        
        using var memoryStream = new MemoryStream();
        await using var writer = new StreamWriter(memoryStream);
        await using var csv = new CsvWriter(writer, _configPersons);

        await csv.WriteRecordsAsync((IEnumerable)records);

        var result = await UploadAsync(memoryStream.ToArray(), GenerateFileName(now)).ToTry();

        result.DoIfError(e => _logger.LogError("Error during sending DNA dropShip file: {error}", e));
    }

    private bool IsReady(DnaOrder o)
    {
        //The barcode must be something like "560020087943267183"
        var barcodeOk = BigInteger.TryParse(o.Barcode, out _);
        
        //The  _dnaOrdersService.SelectForIntegrationAsync 
        //returns orders where the placedAt date is null, or
        //in between the to and from dates (inclusive).
        
        //If the placed at date is not set, then it will presumably be set at
        //some point in the future by staff.
        //So we will say this order is not ready.
        
        //Only send an order where the barcode is a proper numeric
        //value, and the placedAt date is set.
        return barcodeOk && o.PlacedAt.HasValue;
    }

    private string FormatDate(DateTime? date)
    {
        //Info from LabCorp:
        //The test order date and DOB columns should not have a time
        //only the date formatted in United Kingdom English type 2023-10-17.
        return date.HasValue ? date.Value.ToString("yyyy-MM-dd") : String.Empty; 
    }

    private Task<bool> UploadAsync(byte[] bytes, string fileName)
    {
        var client = ConfigureClient();
        
        client.Connect();
        _logger.LogInformation($"Connected to {client.ConnectionInfo.Host}.");
        
        _logger.LogInformation($"Uploading {fileName}...");
        using (Stream uploadStream = client.OpenWrite(fileName))
        {
            uploadStream.Write(bytes, 0, bytes.Length);
        }
        
        _logger.LogInformation($"Uploaded {bytes.Length} bytes.");

        return Task.FromResult(true);
    }
    
    private SftpClient ConfigureClient()
    {
        var methods = new List<AuthenticationMethod>
        {
            new PasswordAuthenticationMethod(_labCorpFtpServerOptions.Login, _labCorpFtpServerOptions.Password)
        };

        var connection = new ConnectionInfo(_labCorpFtpServerOptions.Host, _labCorpFtpServerOptions.Port, _labCorpFtpServerOptions.Login, methods.ToArray());
        
        return new SftpClient(connection);
    }

    private string GenerateFileName(DateTime now)
    {
        //Info from LabCorp:
        //The name of the file should follow this format: ClientName-YYYYMMDDINS.csv
        //An instruction file sent today would be: WildHealth-20231017INS.csv
        return $"{_labCorpFtpServerOptions.Directory}WildHealth-{now:yyyyMMdd}INS.csv";
    }
}
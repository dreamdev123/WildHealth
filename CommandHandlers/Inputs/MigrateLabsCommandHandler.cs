using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using WildHealth.Application.Commands.Inputs;
using WildHealth.Application.Utils.LabNameProvider;
using WildHealth.Application.Services.Inputs;
using WildHealth.ClarityCore.WebClients.Labs;
using Microsoft.Extensions.Logging;
using WildHealth.Domain.Enums.Inputs;
using WildHealth.Domain.Entities.Inputs;
using WildHealth.Domain.Enums.User;
using WildHealth.Domain.Constants;
using WildHealth.Shared.Data.Context;
using Microsoft.EntityFrameworkCore;
using MediatR;

namespace WildHealth.Application.CommandHandlers.Inputs
{
    public class MigrateLabsCommandHandler : IRequestHandler<MigrateLabsCommand>
    {
        private static readonly string _labCorpVendorName = "LabCorp";
        private readonly IEnumerable<string> _vendorsToAdd = new List<string>() {
                _labCorpVendorName
            };

        private readonly IEnumerable<Gender> _genderItems = new List<Gender>() { Gender.Male, Gender.Female };



        private readonly ILabNameProvider _labNameProvider;
        private readonly IInputsService _inputsService;
        private readonly ILabsWebClient _labsWebClient;
        private readonly ILabNamesService _labNamesService;
        private readonly ILabVendorsService _labVendorsService;
        private readonly ILabNameAliasesService _labNameAliasesService;
        private readonly ILabNameRangesService _labNameRangesService;
        private readonly IMediator _mediator;
        private readonly ILogger<MigrateLabsCommandHandler> _logger;
        private readonly IApplicationDbContext _dbContext;

        public MigrateLabsCommandHandler(
            ILabNameProvider labNameProvider,
            IInputsService inputsService,
            ILabsWebClient labsWebClient,
            ILabNamesService labNamesService,
            ILabVendorsService labVendorsService,
            ILabNameAliasesService labNameAliasesService,
            ILabNameRangesService labNameRangesService,
            IMediator mediator,
            ILogger<MigrateLabsCommandHandler> logger,
            IApplicationDbContext dbContext)
        {
            _labNameProvider = labNameProvider;
            _inputsService = inputsService;
            _labsWebClient = labsWebClient;
            _labNamesService = labNamesService;
            _labVendorsService = labVendorsService;
            _labNameAliasesService = labNameAliasesService;
            _labNameRangesService = labNameRangesService;
            _mediator = mediator;
            _logger = logger;
            _dbContext = dbContext;
        }

        public async Task Handle(MigrateLabsCommand command, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Migrating lab information has been started.");

            try
            {
                await AddLabVendors();

                await AddLabNamesAndAliasesAndRanges();
            }
            catch (Exception e)
            {
                _logger.LogError($"Migrating lab information has failed - {e}");

                throw;
            }
        }

        /// <summary>
        /// Populate the LabVendor table
        /// </summary>
        private async Task AddLabVendors()
        {
            foreach (var vendorToAdd in _vendorsToAdd)
            {
                var labVendor = await _labVendorsService.GetByName(vendorToAdd);

                if (labVendor == null)
                {
                    await _labVendorsService.Create(new LabVendor()
                    {
                        Name = _labCorpVendorName
                    });
                }
            }
        }

        /// <summary>
        /// Populate the LabName, LabNameAlias, and LabNameRange tables
        /// </summary>
        private async Task AddLabNamesAndAliasesAndRanges()
        {
            var yearsList = Enumerable.Range(1, 100);

            var ageItems = new Dictionary<string, IEnumerable<int>>() {
                { "Month", new List<int>() {
                    1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12
                }},
                { "Year", yearsList}
            };

            var labCorpVendor = await _labVendorsService.GetByName(_labCorpVendorName);

            foreach (var kvp in Groups)
            {
                var groupName = kvp.Key;
                var labNames = kvp.Value;

                foreach (var labName in labNames)
                {
                    var resultCodes = LabCorpResultCodesMap.Where(o => o.Value == labName);

                    if (resultCodes.Any())
                    {
                        var resultCode = resultCodes.First();
                        var wildHealthName = LabNameNames[labName];

                        // See if the LabName entry alraedy exists
                        var labNameObject = await _labNamesService.Get(labName);

                        if (labNameObject == null)
                        {
                            labNameObject = await _labNamesService.Create(new LabName()
                            {
                                WildHealthName = labName,
                                WildHealthDisplayName = wildHealthName,
                                GroupName = groupName.ToString()
                            });
                        }
                        else
                        {
                            labNameObject.GroupName = groupName.ToString();
                            labNameObject.WildHealthDisplayName = wildHealthName;

                            labNameObject = await _labNamesService.Update(labNameObject);
                        }

                        // Get or create the alias
                        var labNameAliasObject = await _labNameAliasesService.GetOrCreate(labCorpVendor.GetId(), labNameObject.GetId(), resultCode.Key);

                        // If we have already added the max number of ranges, then just keep going
                        if (labNameObject.LabNameRanges.Count() == 224)
                        {
                            continue;
                        }

                        // Get proper range entry for this labName
                        if (RangeMap.ContainsKey(labName))
                        {
                            var rangeDelegate = RangeMap[labName];

                            var labNameRangesToInsert = new List<LabNameRange>();
                                    

                            var labNameRangesForLabNameAndVendor = await _labNameRangesService.Get(labNameObject, labCorpVendor);

                            var shouldIgnoreCheck = !labNameRangesForLabNameAndVendor.Any();

                            foreach (var gender in _genderItems)
                            {
                                foreach (var ageDict in ageItems)
                                {
                                    var ageUnit = ageDict.Key;
                                    foreach (var age in ageDict.Value)
                                    {
                                        var birthday = ageUnit.Equals("Month") ? DateTime.Now.AddMonths(-age) : DateTime.Now.AddYears(-age);
                                        var rangeValue = rangeDelegate(gender, birthday);

                                        LabNameRange? labNameRange = null;

                                        if(!shouldIgnoreCheck)
                                        {
                                            labNameRange = await _labNameRangesService.Get(labNameObject!, labCorpVendor!, gender, birthday);
                                        }

                                        if (labNameRange == null)
                                        {
                                            labNameRangesToInsert.Add(new LabNameRange()
                                            {
                                                LabVendorId = labCorpVendor.GetId(),
                                                Gender = gender,
                                                AgeUnit = ageUnit,
                                                Age = age,
                                                RangeFrom = rangeValue.From,
                                                RangeTo = rangeValue.To,
                                                RangeType = rangeValue.Type,
                                                RangeDimension = rangeValue.Dimension,
                                                LabNameId = labNameObject.GetId()
                                            });
                                        }
                                    }
                                }
                            }


                            var insertQuery = "INSERT INTO LabNameRanges (LabVendorId, Gender, AgeUnit, Age, RangeFrom, RangeTo, RangeType, RangeDimension, LabNameId, CreatedAt, CreatedBy) VALUES ";

                            var values = labNameRangesToInsert.Select(o => $"({o.LabVendorId}, {(int)o.Gender}, '{o.AgeUnit}', {o.Age}, {RangeResult(o.RangeFrom)}, {RangeResult(o.RangeTo)}, {(int)o.RangeType}, '{o.RangeDimension}', {o.LabNameId}, '{DateTime.Now}', 0)");

                            var query = insertQuery + String.Join(',', values);

                            await using(var command = _dbContext.Instance.Database.GetDbConnection().CreateCommand()) {
                                command.CommandText = query;
                                _dbContext.Instance.Database.OpenConnection();
                                command.ExecuteNonQuery();
                            }

                        }
                    }
                }
            }
        }

        private string RangeResult(decimal? val)
        {
            return val.HasValue ? val.Value.ToString() : "null";
        }



        private static readonly LabInputRange BasoAbsolute = new LabInputRange(LabRangeType.FromTo, "x10E3/uL", new decimal(0), new decimal(0.2));
        private static readonly LabInputRange Basos = new LabInputRange(LabRangeType.None, "%", null, null);
        private static readonly LabInputRange Eos = new LabInputRange(LabRangeType.None, "%", null, null);
        private static readonly LabInputRange EosAbsolute = new LabInputRange(LabRangeType.FromTo, "x10E3/ul", new decimal(0.0), new decimal(0.4));
        private static readonly LabInputRange Hematocrit = new LabInputRange(LabRangeType.FromTo, "%", new decimal(37.5), new decimal(51.0));
        private static readonly LabInputRange Hemoglobin = new LabInputRange(LabRangeType.FromTo, "g/dL", new decimal(13.0), new decimal(17.7));
        private static readonly LabInputRange ImmatureGransAbs = new LabInputRange(LabRangeType.FromTo, "x10E3/ul", new decimal(0.0), new decimal(0.1));
        private static readonly LabInputRange ImmatureGranulocytes = new LabInputRange(LabRangeType.None, "%", null, null);
        private static readonly LabInputRange Lymphs = new LabInputRange(LabRangeType.None, "%", null, null);
        private static readonly LabInputRange LymphsAbsolute = new LabInputRange(LabRangeType.FromTo, "x10E3/ul", new decimal(0.7), new decimal(3.1));
        private static readonly LabInputRange MCH = new LabInputRange(LabRangeType.FromTo, "pg", new decimal(26.6), new decimal(33.0));
        private static readonly LabInputRange MCHC = new LabInputRange(LabRangeType.FromTo, "g/dL", new decimal(31.5), new decimal(35.7));
        private static readonly LabInputRange MCV = new LabInputRange(LabRangeType.FromTo, "fL", new decimal(79.0), new decimal(97.0));
        private static readonly LabInputRange Monocytes = new LabInputRange(LabRangeType.None, "%", null, null);
        private static readonly LabInputRange MonocytesAbsolute = new LabInputRange(LabRangeType.FromTo, "x10E3/ul", new decimal(0.1), new decimal(0.9));
        private static readonly LabInputRange Neutrophils = new LabInputRange(LabRangeType.None, "%", null, null);
        private static readonly LabInputRange NeutrophilsAbsolute = new LabInputRange(LabRangeType.FromTo, "x10E3/ul", new decimal(1.4), new decimal(7.0));
        private static readonly LabInputRange Platelets = new LabInputRange(LabRangeType.FromTo, "x10E3/ul", new decimal(140.0), new decimal(450.0));
        private static readonly LabInputRange RBC = new LabInputRange(LabRangeType.FromTo, "x10E3/ul", new decimal(4.14), new decimal(5.8));
        private static readonly LabInputRange RDW = new LabInputRange(LabRangeType.FromTo, "%", new decimal(11.6), new decimal(15.4));
        private static readonly LabInputRange WBC = new LabInputRange(LabRangeType.FromTo, "x10E3/ul", new decimal(3.4), new decimal(10.8));
        private static readonly LabInputRange ApolipoproteinA1 = new LabInputRange(LabRangeType.FromTo, "mg/dL", new decimal(101), new decimal(178));
        private static readonly LabInputRange Crp = new LabInputRange(LabRangeType.LessThen, "mg/l", null, new decimal(1));
        private static readonly LabInputRange AgRatio = new LabInputRange(LabRangeType.MoreThen, "", new decimal(1), null);
        private static readonly LabInputRange LpPla2 = new LabInputRange(LabRangeType.LessThen, "nmol/min/ml", null, 200);
        private static readonly LabInputRange OxLdl = new LabInputRange(LabRangeType.LessThen, "U/l", null, new decimal(60));
        private static readonly LabInputRange Omega3 = new LabInputRange(LabRangeType.MoreThen, "% by wt", new decimal(5.4), null);
        private static readonly LabInputRange VitaminD = new LabInputRange(LabRangeType.FromTo, "ng/m", new decimal(50), new decimal(100));
        private static readonly LabInputRange CoQ10 = new LabInputRange(LabRangeType.MoreThen, "ug/ml", new decimal(0.75), null);
        private static readonly LabInputRange LinoleicAcid = new LabInputRange(LabRangeType.FromTo, "%", new decimal(18), new decimal(29));
        private static readonly LabInputRange VitaminB6 = new LabInputRange(LabRangeType.FromTo, "ug/L", new decimal(5.3), new decimal(46.7));
        private static readonly LabInputRange FastingGlucose = new LabInputRange(LabRangeType.LessThen, "mg/dl", null, new decimal(100));
        private static readonly LabInputRange FastingInsulin = new LabInputRange(LabRangeType.LessThen, "uIU/ml", null, new decimal(5));
        private static readonly LabInputRange LPIRScore = new LabInputRange(LabRangeType.LessThen, "units", null, new decimal(45));
        private static readonly LabInputRange HgbA1C = new LabInputRange(LabRangeType.LessThen, "%", null, new decimal(5.5));
        private static readonly LabInputRange Homocysteine = new LabInputRange(LabRangeType.LessThen, "umol/l", null, new decimal(7));
        private static readonly LabInputRange Ast = new LabInputRange(LabRangeType.LessThen, "U/l", null, new decimal(20));
        private static readonly LabInputRange Alt = new LabInputRange(LabRangeType.LessThen, "U/l", null, new decimal(20));
        private static readonly LabInputRange B12 = new LabInputRange(LabRangeType.FromTo, "pg/ml", new decimal(500), new decimal(1500));
        private static readonly LabInputRange Folate = new LabInputRange(LabRangeType.FromTo, "ng/ml", new decimal(12), new decimal(25));
        private static readonly LabInputRange FolateRbc = new LabInputRange(LabRangeType.MoreThen, "ng/ml", new decimal(280), null);
        private static readonly LabInputRange Tmao = new LabInputRange(LabRangeType.LessThen, "uM", null, new decimal(5));
        private static readonly LabInputRange UricAcid = new LabInputRange(LabRangeType.LessThen, "mg/dL", null, new decimal(5));
        private static readonly LabInputRange TotalCholesterol = new LabInputRange(LabRangeType.LessThen, "mg/dL", null, new decimal(200));
        private static readonly LabInputRange HdlCholesterol = new LabInputRange(LabRangeType.MoreThen, "mg/dl", 50, null);
        private static readonly LabInputRange HdlP = new LabInputRange(LabRangeType.MoreThen, "umol/L", new decimal(30.5), null);
        private static readonly LabInputRange LdlCholesterol = new LabInputRange(LabRangeType.LessThen, "mg/dl", null, new decimal(100));
        private static readonly LabInputRange LdlSize = new LabInputRange(LabRangeType.MoreThen, "nm", new decimal(20.5), null);
        private static readonly LabInputRange LdlHdlRatio = new LabInputRange(LabRangeType.FromTo, "ratio", new decimal(0), new decimal(3.2));
        private static readonly LabInputRange VldlCholesterol = new LabInputRange(LabRangeType.FromTo, "mg/dL", new decimal(5), new decimal(40));
        private static readonly LabInputRange Triglycerides = new LabInputRange(LabRangeType.LessThen, "mg/dl", null, new decimal(150));
        private static readonly LabInputRange LdlP = new LabInputRange(LabRangeType.LessThen, "nmol/l", null, new decimal(1000));
        private static readonly LabInputRange SmallLdlP = new LabInputRange(LabRangeType.LessThen, "nmol/l", null, new decimal(527));
        private static readonly LabInputRange LpA = new LabInputRange(LabRangeType.LessThen, "nmol/l", null, new decimal(75));
        private static readonly LabInputRange ApolipoproteinB = new LabInputRange(LabRangeType.LessThen, "mg/dL", null, new decimal(90));
        private static readonly LabInputRange Desmosterol = new LabInputRange(LabRangeType.FromTo, "mg/L", new decimal(0.5), new decimal(2));
        private static readonly LabInputRange Lathosterol = new LabInputRange(LabRangeType.FromTo, "mg/L", new decimal(0.5), new decimal(3));
        private static readonly LabInputRange Campsterol = new LabInputRange(LabRangeType.LessThen, "mg/L", null, new decimal(7));
        private static readonly LabInputRange Sitosterol = new LabInputRange(LabRangeType.LessThen, "mg/L", null, new decimal(5));
        private static readonly LabInputRange Cholestanol = new LabInputRange(LabRangeType.LessThen, "mg/L", null, new decimal(7));
        private static readonly LabInputRange Dhea = new LabInputRange(LabRangeType.FromTo, "mcg/dl", new decimal(200), new decimal(500));
        private static readonly LabInputRange Igf = new LabInputRange(LabRangeType.FromTo, "ng/ml", new decimal(100), new decimal(250));
        private static readonly LabInputRange TestosteroneTotal = new LabInputRange(LabRangeType.FromTo, "ng/dl", new decimal(250), new decimal(1100));
        private static readonly LabInputRange Shbg = new LabInputRange(LabRangeType.FromTo, "nmol/L", new decimal(10), new decimal(50));
        private static readonly LabInputRange Lh = new LabInputRange(LabRangeType.FromTo, "mIU/ml", new decimal(1.6), new decimal(8));
        private static readonly LabInputRange Tsh = new LabInputRange(LabRangeType.FromTo, "uIU/ml", new decimal(0.4), new decimal(4));
        private static readonly LabInputRange FreeT4 = new LabInputRange(LabRangeType.FromTo, "ng/dL", new decimal(0.82), new decimal(1.77));
        private static readonly LabInputRange FreeT3 = new LabInputRange(LabRangeType.FromTo, "pg/ml", new decimal(2.0), new decimal(4.4));
        private static readonly LabInputRange Cortisol = new LabInputRange(LabRangeType.FromTo, "mcg/dl", new decimal(10), new decimal(18));
        private static readonly LabInputRange Albumin = new LabInputRange(LabRangeType.FromTo, "g/dL", new decimal(3.8), new decimal(4.8));
        private static readonly LabInputRange AlkalinePhosphatase = new LabInputRange(LabRangeType.FromTo, "IU/L", new decimal(44), new decimal(121));
        private static readonly LabInputRange TotalBilirubin = new LabInputRange(LabRangeType.FromTo, "mg/dL", new decimal(0), new decimal(1.2));
        private static readonly LabInputRange BUN = new LabInputRange(LabRangeType.FromTo, "mg/dL", new decimal(6), new decimal(24));
        private static readonly LabInputRange BUNCreatinineRatio = new LabInputRange(LabRangeType.FromTo, "Ratio", new decimal(9), new decimal(23));
        private static readonly LabInputRange Calcium = new LabInputRange(LabRangeType.FromTo, "mg/dL", new decimal(8.7), new decimal(10.2));
        private static readonly LabInputRange TotalCarbonDioxide = new LabInputRange(LabRangeType.FromTo, "mmol/L", new decimal(20), new decimal(29));
        private static readonly LabInputRange Chloride = new LabInputRange(LabRangeType.FromTo, "mmol/L", new decimal(96), new decimal(106));
        private static readonly LabInputRange Creatinine = new LabInputRange(LabRangeType.FromTo, "mg/dL", new decimal(0.57), new decimal(1));
        private static readonly LabInputRange eGFRifAfricanAmerican = new LabInputRange(LabRangeType.LessThen, "mL/min/1.73", new decimal(60), null);
        private static readonly LabInputRange eGFRifNonAfricanAmerican = new LabInputRange(LabRangeType.LessThen, "mL/min/1.73", new decimal(60), null);
        private static readonly LabInputRange TotalGlobulin = new LabInputRange(LabRangeType.FromTo, "g/dL", new decimal(1.5), new decimal(4.5));
        private static readonly LabInputRange Potassium = new LabInputRange(LabRangeType.FromTo, "mmol/L", new decimal(3.5), new decimal(5.2));
        private static readonly LabInputRange TotalProtein = new LabInputRange(LabRangeType.FromTo, "g/dL", new decimal(6), new decimal(8.5));
        private static readonly LabInputRange Sodium = new LabInputRange(LabRangeType.FromTo, "mmol/L", new decimal(134), new decimal(144));
        private static readonly LabInputRange ArachidonicAcid = new LabInputRange(LabRangeType.FromTo, "% by wt", new decimal(8.6), new decimal(15.6));
        private static readonly LabInputRange ArachidonicAcidEPARatio = new LabInputRange(LabRangeType.FromTo, "Ratio", new decimal(3.7), new decimal(40.7));
        private static readonly LabInputRange DHA = new LabInputRange(LabRangeType.FromTo, "% by wt", new decimal(1.4), new decimal(5.1));
        private static readonly LabInputRange DPA = new LabInputRange(LabRangeType.FromTo, "% by wt", new decimal(0.8), new decimal(1.8));
        private static readonly LabInputRange EPA = new LabInputRange(LabRangeType.FromTo, "% by wt", new decimal(0.2), new decimal(2.3));
        private static readonly LabInputRange Omega6Total = new LabInputRange(LabRangeType.None, "% by wt", null, null);
        private static readonly LabInputRange Omega6Omega3Ratio = new LabInputRange(LabRangeType.FromTo, "Ratio", new decimal(3.7), new decimal(14.4));
        private static readonly LabInputRange OmegaCheck = new LabInputRange(LabRangeType.MoreThen, "% by wt", new decimal(5.4), null);

        #region Testosterone Free

        private static readonly LabInputRange TestosteroneFreeMale19Y = new LabInputRange(LabRangeType.None, "pg/mL", null, null);
        private static readonly LabInputRange TestosteroneFreeMale29Y = new LabInputRange(LabRangeType.FromTo, "pg/mL", new decimal(9.3), new decimal(26.5));
        private static readonly LabInputRange TestosteroneFreeMale39Y = new LabInputRange(LabRangeType.FromTo, "pg/mL", new decimal(8.7), new decimal(25.1));
        private static readonly LabInputRange TestosteroneFreeMale49Y = new LabInputRange(LabRangeType.FromTo, "pg/mL", new decimal(6.8), new decimal(21.5));
        private static readonly LabInputRange TestosteroneFreeMale59Y = new LabInputRange(LabRangeType.FromTo, "pg/mL", new decimal(7.2), new decimal(24.0));
        private static readonly LabInputRange TestosteroneFreeMaleMore59Y = new LabInputRange(LabRangeType.FromTo, "pg/mL", new decimal(6.6), new decimal(18.1));
        private static readonly LabInputRange TestosteroneFreeFemaleChild = new LabInputRange(LabRangeType.None, "pg/mL", null, null);
        private static readonly LabInputRange TestosteroneFreeFemaleAdult = new LabInputRange(LabRangeType.FromTo, "pg/mL", new decimal(0), new decimal(4.2));

        #endregion

        #region Fsh

        private static readonly LabInputRange FshMale12M = new LabInputRange(LabRangeType.None, "mIU/mL", null, null);
        private static readonly LabInputRange FshMale4Y = new LabInputRange(LabRangeType.FromTo, "mIU/mL", new decimal(0.2), new decimal(2.8));
        private static readonly LabInputRange FshMale9Y = new LabInputRange(LabRangeType.FromTo, "mIU/mL", new decimal(0.4), new decimal(3.8));
        private static readonly LabInputRange FshMale12Y = new LabInputRange(LabRangeType.FromTo, "mIU/mL", new decimal(0.4), new decimal(4.6));
        private static readonly LabInputRange FshMale16Y = new LabInputRange(LabRangeType.FromTo, "mIU/mL", new decimal(1.5), new decimal(12.9));
        private static readonly LabInputRange FshMaleAdult = new LabInputRange(LabRangeType.FromTo, "mIU/mL", new decimal(1.5), new decimal(12.4));

        private static readonly LabInputRange FshFemale12M = new LabInputRange(LabRangeType.None, "mIU/mL", null, null);
        private static readonly LabInputRange FshFemale4Y = new LabInputRange(LabRangeType.FromTo, "mIU/mL", new decimal(0.2), new decimal(11.1));
        private static readonly LabInputRange FshFemale9Y = new LabInputRange(LabRangeType.FromTo, "mIU/mL", new decimal(0.3), new decimal(11.1));
        private static readonly LabInputRange FshFemale12Y = new LabInputRange(LabRangeType.FromTo, "mIU/mL", new decimal(2.1), new decimal(11.1));
        private static readonly LabInputRange FshFemale16Y = new LabInputRange(LabRangeType.FromTo, "mIU/mL", new decimal(1.6), new decimal(17.0));
        private static readonly LabInputRange FshFemaleAdult = new LabInputRange(LabRangeType.FromTo, "mIU/mL", new decimal(3.5), new decimal(12.5));

        #endregion

        #region Ferritin

        private static readonly LabInputRange FerritinMale5M = new LabInputRange(LabRangeType.FromTo, "ng/mL", new decimal(13), new decimal(273));
        private static readonly LabInputRange FerritinMale12M = new LabInputRange(LabRangeType.FromTo, "ng/mL", new decimal(12), new decimal(95));
        private static readonly LabInputRange FerritinMale5Y = new LabInputRange(LabRangeType.FromTo, "ng/mL", new decimal(12), new decimal(64));
        private static readonly LabInputRange FerritinMale11Y = new LabInputRange(LabRangeType.FromTo, "ng/mL", new decimal(16), new decimal(77));
        private static readonly LabInputRange FerritinMale19Y = new LabInputRange(LabRangeType.FromTo, "ng/mL", new decimal(16), new decimal(124));
        private static readonly LabInputRange FerritinMaleAdult = new LabInputRange(LabRangeType.FromTo, "ng/mL", new decimal(30), new decimal(400));

        private static readonly LabInputRange FerritinFemale5M = new LabInputRange(LabRangeType.FromTo, "ng/mL", new decimal(12), new decimal(219));
        private static readonly LabInputRange FerritinFemale12M = new LabInputRange(LabRangeType.FromTo, "ng/mL", new decimal(12), new decimal(110));
        private static readonly LabInputRange FerritinFemale5Y = new LabInputRange(LabRangeType.FromTo, "ng/mL", new decimal(12), new decimal(71));
        private static readonly LabInputRange FerritinFemale11Y = new LabInputRange(LabRangeType.FromTo, "ng/mL", new decimal(15), new decimal(79));
        private static readonly LabInputRange FerritinFemale19Y = new LabInputRange(LabRangeType.FromTo, "ng/mL", new decimal(15), new decimal(77));
        private static readonly LabInputRange FerritinFemaleAdult = new LabInputRange(LabRangeType.FromTo, "ng/mL", new decimal(15), new decimal(150));

        #endregion

        #region Estradio

        private static readonly LabInputRange EstradiolMaleAdult = new LabInputRange(LabRangeType.FromTo, "pg/ml", new decimal(7.6), new decimal(42.6));
        private static readonly LabInputRange EstradiolMale10Y = new LabInputRange(LabRangeType.FromTo, "pg/ml", new decimal(0.0), new decimal(20.0));

        private static readonly LabInputRange EstradiolFemaleAdult = new LabInputRange(LabRangeType.FromTo, "pg/ml", new decimal(50), new decimal(250));
        private static readonly LabInputRange EstradiolFemale10Y = new LabInputRange(LabRangeType.FromTo, "pg/ml", new decimal(6.0), new decimal(27.0));

        #endregion

        #region Progesterone

        private static readonly LabInputRange ProgesteroneMale = new LabInputRange(LabRangeType.LessThen, "ng/dl", null, new decimal(0.5));
        private static readonly LabInputRange ProgesteroneFemale = new LabInputRange(LabRangeType.FromTo, "ng/dl", new decimal(1), new decimal(20));

        #endregion

        public delegate LabInputRange GetRange(Gender gender, DateTime birthday);
        public static readonly IDictionary<string, GetRange> RangeMap = new Dictionary<string, GetRange>
        {
            {LabNames.BasoAbsolute, (gender, birthday) => BasoAbsolute},
            {LabNames.Basos, (gender, birthday) => Basos},
            {LabNames.Eos, (gender, birthday) => Eos},
            {LabNames.EosAbsolute, (gender, birthday) => EosAbsolute},
            {LabNames.Hematocrit, (gender, birthday) => Hematocrit},
            {LabNames.Hemoglobin, (gender, birthday) => Hemoglobin},
            {LabNames.ImmatureGransAbs, (gender, birthday) => ImmatureGransAbs},
            {LabNames.ImmatureGranulocytes, (gender, birthday) => ImmatureGranulocytes},
            {LabNames.Lymphs, (gender, birthday) => Lymphs},
            {LabNames.LymphsAbsolute, (gender, birthday) => LymphsAbsolute},
            {LabNames.MCH, (gender, birthday) => MCH},
            {LabNames.MCHC, (gender, birthday) => MCHC},
            {LabNames.MCV, (gender, birthday) => MCV},
            {LabNames.Monocytes, (gender, birthday) => Monocytes},
            {LabNames.MonocytesAbsolute, (gender, birthday) => MonocytesAbsolute},
            {LabNames.Neutrophils, (gender, birthday) => Neutrophils},
            {LabNames.NeutrophilsAbsolute, (gender, birthday) => NeutrophilsAbsolute},
            {LabNames.Platelets, (gender, birthday) => Platelets},
            {LabNames.RBC, (gender, birthday) => RBC},
            {LabNames.RDW, (gender, birthday) => RDW},
            {LabNames.WBC, (gender, birthday) => WBC},
            {LabNames.ApolipoproteinA1, (gender, birthday) => ApolipoproteinA1},

            {LabNames.Albumin, (gender, birthday) => Albumin},
            {LabNames.AlkalinePhosphatase, (gender, birthday) => AlkalinePhosphatase},
            {LabNames.TotalBilirubin, (gender, birthday) => TotalBilirubin},
            {LabNames.BUN, (gender, birthday) => BUN},
            {LabNames.BUNCreatinineRatio, (gender, birthday) => BUNCreatinineRatio},
            {LabNames.Calcium, (gender, birthday) => Calcium},
            {LabNames.TotalCarbonDioxide, (gender, birthday) => TotalCarbonDioxide},
            {LabNames.Chloride, (gender, birthday) => Chloride},
            {LabNames.Creatinine, (gender, birthday) => Creatinine},
            {LabNames.eGFRifAfricanAmerican, (gender, birthday) => eGFRifAfricanAmerican},
            {LabNames.eGFRifNonAfricanAmerican, (gender, birthday) => eGFRifNonAfricanAmerican},
            {LabNames.TotalGlobulin, (gender, birthday) => TotalGlobulin},
            {LabNames.Potassium, (gender, birthday) => Potassium},
            {LabNames.TotalProtein, (gender, birthday) => TotalProtein},
            {LabNames.Sodium, (gender, birthday) => Sodium},

            {LabNames.ArachidonicAcid, (gender, birthday) => ArachidonicAcid},
            {LabNames.ArachidonicAcidEPARatio, (gender, birthday) => ArachidonicAcidEPARatio},
            {LabNames.DHA, (gender, birthday) => DHA},
            {LabNames.DPA, (gender, birthday) => DPA},
            {LabNames.EPA, (gender, birthday) => EPA},
            {LabNames.Omega6Total, (gender, birthday) => Omega6Total},
            {LabNames.Omega6Omega3Ratio, (gender, birthday) => Omega6Omega3Ratio},
            {LabNames.OmegaCheck, (gender, birthday) => OmegaCheck},

            {LabNames.Crp, (gender, birthday) => Crp},
            {LabNames.AgRatio, (gender, birthday) => AgRatio},
            {LabNames.LpPla2, (gender, birthday) => LpPla2},
            {LabNames.OxLdl, (gender, birthday) => OxLdl},
            {LabNames.Omega3, (gender, birthday) => Omega3},


            {LabNames.VitaminD, (gender, birthday) => VitaminD},
            {LabNames.CoQ10, (gender, birthday) => CoQ10},
            {LabNames.LinoleicAcid, (gender, birthday) => LinoleicAcid},
            {LabNames.VitaminB6, (gender, birthday) => VitaminB6},
            {LabNames.FastingGlucose, (gender, birthday) => FastingGlucose},
            {LabNames.FastingInsulin, (gender, birthday) => FastingInsulin},
            {LabNames.LPIRScore, (gender, birthday) => LPIRScore},
            {LabNames.HgbA1C, (gender, birthday) => HgbA1C},


            {LabNames.Homocysteine, (gender, birthday) => Homocysteine},
            {LabNames.Ast, (gender, birthday) => Ast},
            {LabNames.Alt, (gender, birthday) => Alt},
            {LabNames.B12, (gender, birthday) => B12},
            {LabNames.Folate, (gender, birthday) => Folate},
            {LabNames.FolateRbc, (gender, birthday) => FolateRbc},
            {LabNames.Tmao, (gender, birthday) => Tmao},
            {LabNames.UricAcid, (gender, birthday) => UricAcid},
            {LabNames.TotalCholesterol, (gender, birthday) => TotalCholesterol},
            {LabNames.HdlCholesterol, (gender, birthday) => HdlCholesterol},
            {LabNames.HdlP, (gender, birthday) => HdlP},
            {LabNames.LdlCholesterol, (gender, birthday) => LdlCholesterol},
            {LabNames.Triglycerides, (gender, birthday) => Triglycerides},
            {LabNames.LdlP, (gender, birthday) => LdlP},
            {LabNames.LdlSize, (gender, birthday) => LdlSize},
            {LabNames.LdlHdlRatio, (gender, birthday) => LdlHdlRatio},
            {LabNames.VldlCholesterol, (gender, birthday) => VldlCholesterol},
            {LabNames.SmallLdlP, (gender, birthday) => SmallLdlP},
            {LabNames.LpA, (gender, birthday) => LpA},
            {LabNames.ApolipoproteinB, (gender, birthday) => ApolipoproteinB},
            {LabNames.Desmosterol, (gender, birthday) => Desmosterol},
            {LabNames.Lathosterol, (gender, birthday) => Lathosterol},
            {LabNames.Campsterol, (gender, birthday) => Campsterol},
            {LabNames.Sitosterol, (gender, birthday) => Sitosterol},
            {LabNames.Cholestanol, (gender, birthday) => Cholestanol},
            {LabNames.Dhea, (gender, birthday) => Dhea},
            {LabNames.Igf, (gender, birthday) => Igf},
            {LabNames.TestosteroneTotal, (gender, birthday) => TestosteroneTotal},
            {LabNames.Shbg, (gender, birthday) => Shbg},
            {LabNames.Lh, (gender, birthday) => Lh},
            {LabNames.Tsh, (gender, birthday) => Tsh},
            {LabNames.FreeT4, (gender, birthday) => FreeT4},
            {LabNames.FreeT3, (gender, birthday) => FreeT3},
            {LabNames.Cortisol, (gender, birthday) => Cortisol},
            {LabNames.Estradio, (gender, birthday) => {
                return gender switch
                {
                    Gender.Male => true switch
                    {
                        true when birthday > DateTime.UtcNow.AddYears(-11) => EstradiolMale10Y,
                        _ => EstradiolMaleAdult
                    },
                    Gender.Female => true switch
                    {
                        true when birthday > DateTime.UtcNow.AddYears(-11) => EstradiolFemale10Y,
                        _ => EstradiolFemaleAdult
                    },
                    _ => throw new ArgumentException("Gender must be specified", nameof(gender))
                };
            }},
            {LabNames.Progesterone, (gender, birthday) => {
                return gender switch
                {
                    Gender.Male => ProgesteroneMale,
                    Gender.Female => ProgesteroneFemale,
                    _ => throw new ArgumentException("Gender must be specified", nameof(gender))
                };
            }},
            {LabNames.TestosteroneFree, (gender, birthday) =>
                {
                    return gender switch
                    {
                        Gender.Male => true switch
                        {
                            true when birthday > DateTime.UtcNow.AddYears(-20) => TestosteroneFreeMale19Y,
                            true when birthday > DateTime.UtcNow.AddYears(-30) => TestosteroneFreeMale29Y,
                            true when birthday > DateTime.UtcNow.AddYears(-40) => TestosteroneFreeMale39Y,
                            true when birthday > DateTime.UtcNow.AddYears(-50) => TestosteroneFreeMale49Y,
                            true when birthday > DateTime.UtcNow.AddYears(-60) => TestosteroneFreeMale59Y,
                            _ => TestosteroneFreeMaleMore59Y
                        },
                        Gender.Female => true switch
                        {
                            true when birthday > DateTime.UtcNow.AddYears(-20) => TestosteroneFreeFemaleChild,
                            _ => TestosteroneFreeFemaleAdult
                        },
                        _ => throw new ArgumentException("Gender must be specified", nameof(gender))
                    };
                }
            },
            {LabNames.Fsh, (gender, birthday) =>
                {
                    return gender switch
                    {
                        Gender.Male => true switch
                        {
                            true when birthday > DateTime.UtcNow.AddYears(-1) => FshMale12M,
                            true when birthday > DateTime.UtcNow.AddYears(-5) => FshMale4Y,
                            true when birthday > DateTime.UtcNow.AddYears(-10) => FshMale9Y,
                            true when birthday > DateTime.UtcNow.AddYears(-13) => FshMale12Y,
                            true when birthday > DateTime.UtcNow.AddYears(-16) => FshMale16Y,
                            _ => FshMaleAdult
                        },
                        Gender.Female => true switch
                        {
                            true when birthday > DateTime.UtcNow.AddYears(-1) => FshFemale12M,
                            true when birthday > DateTime.UtcNow.AddYears(-5) => FshFemale4Y,
                            true when birthday > DateTime.UtcNow.AddYears(-10) => FshFemale9Y,
                            true when birthday > DateTime.UtcNow.AddYears(-13) => FshFemale12Y,
                            true when birthday > DateTime.UtcNow.AddYears(-17) => FshFemale16Y,
                            _ => FshFemaleAdult
                        },
                        _ => throw new ArgumentException("Gender must be specified", nameof(gender))
                    };
                }
            },
            {LabNames.Ferritin, (gender, birthday) =>
                {
                    return gender switch
                    {
                        Gender.Male => true switch
                        {
                            true when birthday > DateTime.UtcNow.AddMonths(-6) => FerritinMale5M,
                            true when birthday > DateTime.UtcNow.AddYears(-1) => FerritinMale12M,
                            true when birthday > DateTime.UtcNow.AddYears(-6) => FerritinMale5Y,
                            true when birthday > DateTime.UtcNow.AddYears(-12) => FerritinMale11Y,
                            true when birthday > DateTime.UtcNow.AddYears(-20) => FerritinMale19Y,
                            _ => FerritinMaleAdult
                        },
                        Gender.Female => true switch
                        {
                            true when birthday > DateTime.UtcNow.AddMonths(-6) => FerritinFemale5M,
                            true when birthday > DateTime.UtcNow.AddYears(-1) => FerritinFemale12M,
                            true when birthday > DateTime.UtcNow.AddYears(-6) => FerritinFemale5Y,
                            true when birthday > DateTime.UtcNow.AddYears(-12) => FerritinFemale11Y,
                            true when birthday > DateTime.UtcNow.AddYears(-20) => FerritinFemale19Y,
                            _ => FerritinFemaleAdult
                        },
                        _ => throw new ArgumentException("Gender must be specified", nameof(gender))
                    };
                }
            }
        };

        private static readonly IDictionary<string, string> LabCorpResultCodesMap = new Dictionary<string, string>
        {
            { "144981", LabNames.TestosteroneFree },
            { "004227", LabNames.TestosteroneTotal },
            { "081953", LabNames.VitaminD },
            { "004598", LabNames.Ferritin },
            { "817653", LabNames.Omega3 },
            { "001503", LabNames.B12 },
            { "120190", LabNames.LpA },
            { "004699", LabNames.Dhea },
            { "707009", LabNames.Homocysteine },
            { "123479", LabNames.LdlCholesterol },
            { "123477", LabNames.TotalCholesterol },
            { "002020", LabNames.Folate },
            { "001481", LabNames.HgbA1C },
            { "004519", LabNames.Estradio },
            { "120768", LabNames.Crp },
            { "123476", LabNames.Triglycerides },
            { "004333", LabNames.FastingInsulin },
            { "884312", LabNames.LPIRScore },
            { "167015", LabNames.ApolipoproteinB },
            { "001032", LabNames.FastingGlucose },
            { "004055", LabNames.Cortisol },
            { "010389", LabNames.FreeT3 },
            { "004317", LabNames.Progesterone },
            { "004290", LabNames.Lh },
            { "004264", LabNames.Tsh },
            { "004316", LabNames.Fsh },
            { "010369", LabNames.Igf },
            { "001123", LabNames.Ast },
            { "001545", LabNames.Alt },
            { "123414", LabNames.Tmao },
            { "123475", LabNames.HdlCholesterol },
            { "001057", LabNames.UricAcid },
            { "019745", LabNames.FreeT4 },
            { "082016", LabNames.Shbg },
            { "123284", LabNames.LpPla2 },
            { "266022", LabNames.FolateRbc },
            { "120252", LabNames.CoQ10 },
            { "884294", LabNames.LdlP },
            { "884297", LabNames.SmallLdlP },
            { "123024", LabNames.OxLdl },
            { "012047", LabNames.AgRatio },
            { "817659", LabNames.LinoleicAcid },
            { "004656", LabNames.VitaminB6 },
            { "815567", LabNames.Campsterol },
            { "815562", LabNames.Desmosterol },
            { "823807", LabNames.Cholestanol },
            { "815568", LabNames.Sitosterol },
            { "815564", LabNames.Lathosterol },
            { "012059", LabNames.LdlCholesterol },
            { "001065", LabNames.TotalCholesterol },
            { "001172", LabNames.Triglycerides },
            { "011817", LabNames.HdlCholesterol },
            { "884296", LabNames.HdlP },
            { "884309", LabNames.LdlSize },
            { "011919", LabNames.VldlCholesterol },
            { "011831", LabNames.Comment },
            { "011852", LabNames.LdlHdlRatio },
            { "015941", LabNames.BasoAbsolute },
            { "015156", LabNames.Basos },
            { "015149", LabNames.Eos },
            { "015933", LabNames.EosAbsolute },
            { "005058", LabNames.Hematocrit },
            { "005041", LabNames.Hemoglobin },
            { "015911", LabNames.ImmatureGransAbs },
            { "015108", LabNames.ImmatureGranulocytes },
            { "015123", LabNames.Lymphs },
            { "015917", LabNames.LymphsAbsolute },
            { "015073", LabNames.MCH },
            { "015081", LabNames.MCHC },
            { "015065", LabNames.MCV },
            { "015131", LabNames.Monocytes },
            { "015925", LabNames.MonocytesAbsolute },
            { "015107", LabNames.Neutrophils },
            { "015909", LabNames.NeutrophilsAbsolute },
            { "015172", LabNames.Platelets },
            { "005033", LabNames.RBC },
            { "105007", LabNames.RDW },
            { "005025", LabNames.WBC },
            { "016873", LabNames.ApolipoproteinA1 },

            { "001081", LabNames.Albumin },
            { "001107", LabNames.AlkalinePhosphatase },
            { "001099", LabNames.TotalBilirubin },
            { "001040", LabNames.BUN },
            { "011577", LabNames.BUNCreatinineRatio },
            { "001016", LabNames.Calcium },
            { "001578", LabNames.TotalCarbonDioxide },
            { "001206", LabNames.Chloride },
            { "001370", LabNames.Creatinine },
            { "100797", LabNames.eGFRifAfricanAmerican },
            { "100791", LabNames.eGFRifNonAfricanAmerican },
            { "012039", LabNames.TotalGlobulin },
            { "001180", LabNames.Potassium },
            { "001073", LabNames.TotalProtein },
            { "001198", LabNames.Sodium },

            { "817658", LabNames.ArachidonicAcid },
            { "817651", LabNames.ArachidonicAcidEPARatio },
            { "817656", LabNames.DHA },
            { "817655", LabNames.DPA },
            { "817654", LabNames.EPA },
            { "822904", LabNames.Omega6Total },
            { "817652", LabNames.Omega6Omega3Ratio },
            { "822903", LabNames.OmegaCheck }

        };


        private static readonly IDictionary<string, string> LabNameNames = new Dictionary<string, string>
        {

            {LabNames.TotalCholesterol, "Total Cholesterol"},
            {LabNames.HdlCholesterol, "HDL Cholesterol"},
            {LabNames.HdlP, "HDL-P" },
            {LabNames.LdlCholesterol, "LDL Cholesterol"},
            {LabNames.LdlSize, "LDL Size"},
            {LabNames.Triglycerides, "Triglycerides"},
            {LabNames.LdlP, "LDL-P"},
            {LabNames.SmallLdlP, "Small LDL-p"},
            {LabNames.LpA, "Lp(a)"},
            {LabNames.Dhea, "DHEA"},
            {LabNames.Igf, "iGF"},
            {LabNames.TestosteroneTotal, "Testosterone Total"},
            {LabNames.TestosteroneFree, "Testosterone, Free"},
            {LabNames.Shbg, "SHBG"},
            {LabNames.Estradio, "Estradiol"},
            {LabNames.Progesterone, "Progesterone"},
            {LabNames.Lh, "LH"},
            {LabNames.Fsh, "FSH"},
            {LabNames.Tsh, "TSH"},
            {LabNames.FreeT4, "Free T4"},
            {LabNames.FreeT3, "Free T3"},
            {LabNames.Cortisol, "Cortisol"},
            {LabNames.Homocysteine, "Homocysteine"},
            {LabNames.Ast, "AST"},
            {LabNames.Alt, "ALT"},
            {LabNames.B12, "B12"},
            {LabNames.Folate, "Folate"},
            {LabNames.FolateRbc, "Folate RBC"},
            {LabNames.Tmao, "TMAO"},
            {LabNames.UricAcid, "Uric Acid"},
            {LabNames.Crp, "CRP"},
            {LabNames.AgRatio, "A:G ratio"},
            {LabNames.LpPla2, "LpPLA2"},
            {LabNames.OxLdl, "OxLDL"},
            {LabNames.Ferritin, "Ferritin (Iron)"},
            {LabNames.Omega3, "Omega 3"},
            {LabNames.VitaminD, "Vitamin D"},
            {LabNames.CoQ10, "CoQ10"},
            {LabNames.FastingGlucose, "Fasting Glucose"},
            {LabNames.FastingInsulin, "Fasting Insulin"},
            {LabNames.LPIRScore, "LP-IR Score"},
            {LabNames.HgbA1C, "Hgb-A1c"},
            {LabNames.ApolipoproteinB, "Apolipoprotein B"},
            {LabNames.Desmosterol, "Desmosterol"},
            {LabNames.Lathosterol, "Lathosterol"},
            {LabNames.Campsterol, "Campsterol"},
            {LabNames.Sitosterol, "Sitosterol"},
            {LabNames.Cholestanol, "Cholestanol"},
            {LabNames.LinoleicAcid, "Linoleic Acid"},
            {LabNames.VitaminB6, "Vitamin B6"},


            {LabNames.VldlCholesterol, "VLDL Cholesterol"},
            {LabNames.LdlCholCalc, "LDL Cholesterol"},
            {LabNames.Comment, "Comment"},
            {LabNames.LdlHdlRatio, "LDL/HDL Ratio"},

            {LabNames.BasoAbsolute, "Basophils (Absolute)" },
            {LabNames.Basos, "Basophils" },
            {LabNames.Eos, "Eosinophils" },
            {LabNames.EosAbsolute, "Eosinophils (Absolute)" },
            {LabNames.Hematocrit, "Hematocrit" },
            {LabNames.Hemoglobin, "Hemoglobin" },
            {LabNames.ImmatureGransAbs, "Immature Granulocytes (Absolute)" },
            {LabNames.ImmatureGranulocytes, "Immature Granulocytes" },
            {LabNames.Lymphs, "Lymphocytes" },
            {LabNames.LymphsAbsolute, "Lymphocytes (Absolute)" },
            {LabNames.MCH, "MCH" },
            {LabNames.MCHC, "MCHC" },
            {LabNames.MCV, "MCV" },
            {LabNames.Monocytes, "Monocytes" },
            {LabNames.MonocytesAbsolute, "Monocytes(Absolute)" },
            {LabNames.Neutrophils, "Neutrophils" },
            {LabNames.NeutrophilsAbsolute, "Neutrophils (Absolute)" },
            {LabNames.Platelets, "Platelets" },
            {LabNames.RBC, "RBC" },
            {LabNames.RDW, "RDW" },
            {LabNames.WBC, "WBC" },
            {LabNames.ApolipoproteinA1, "Apolipoprotein A-1"},
            {LabNames.Albumin, "Albumin" },
            {LabNames.AlkalinePhosphatase, "Alkaline Phosphatase" },
            {LabNames.TotalBilirubin, "Total Bilirubin" },
            {LabNames.BUN, "BUN" },
            {LabNames.BUNCreatinineRatio, "BUN/Creatinine Ratio" },
            {LabNames.Calcium, "Calcium" },
            {LabNames.TotalCarbonDioxide, "Total Carbon Dioxide" },
            {LabNames.Chloride, "Chloride" },
            {LabNames.Creatinine, "Creatinine" },
            {LabNames.eGFRifAfricanAmerican, "eGFR if African American" },
            {LabNames.eGFRifNonAfricanAmerican, "eGFR if NonAfrican American" },
            {LabNames.TotalGlobulin, "Total Globulin"},
            {LabNames.Potassium, "Potassium"},
            {LabNames.TotalProtein, "Total Protein"},
            {LabNames.Sodium, "Sodium"},

            {LabNames.ArachidonicAcid, "Arachidonic Acid"},
            {LabNames.ArachidonicAcidEPARatio, "Arachidonic Acid/EPA Ratio"},
            {LabNames.DHA, "DHA"},
            {LabNames.DPA, "DPA"},
            {LabNames.EPA, "EPA"},
            {LabNames.Omega6Total, "Omega-6 total"},
            {LabNames.Omega6Omega3Ratio, "Omega-6/Omega-3 Ratio"},
            {LabNames.OmegaCheck, "OmegaCheck"},

        };

        private static readonly IDictionary<string, string> QuestResultCodesMap = new Dictionary<string, string>
        {
            { "5224", LabNames.ApolipoproteinB },
            { "927", LabNames.B12 },
            { "19826", LabNames.CoQ10 },
            { "367", LabNames.Cortisol },
            { "402", LabNames.Dhea },
            { "4021", LabNames.Estradio },
            { "91731", LabNames.FastingInsulin },
            { "457", LabNames.Ferritin },
            { "466", LabNames.Folate },
            { "467", LabNames.FolateRbc },
            { "34429", LabNames.FreeT3 },
            { "866", LabNames.FreeT4 },
            { "91732", LabNames.HgbA1C },
            { "91733", LabNames.Homocysteine },
            { "16293", LabNames.Igf },
            { "91729", LabNames.LpA },
            { "94218", LabNames.LpPla2 },
            { "92701", LabNames.Omega3 },
            { "745", LabNames.Progesterone },
            { "30740", LabNames.Shbg },
            { "18944", LabNames.TestosteroneFree },
            { "15983", LabNames.TestosteroneTotal },
            { "94154", LabNames.Tmao },
            { "899", LabNames.Tsh },
            { "91735", LabNames.VitaminD },
        };

        public static IDictionary<LabGroup, string[]> Groups =>
            new Dictionary<LabGroup, string[]>
            {
                {
                    LabGroup.CBC, new[]
                    {
                        LabNames.BasoAbsolute,
                        LabNames.Basos,
                        LabNames.Eos,
                        LabNames.EosAbsolute,
                        LabNames.Hematocrit,
                        LabNames.Hemoglobin,
                        LabNames.ImmatureGransAbs,
                        LabNames.ImmatureGranulocytes,
                        LabNames.Lymphs,
                        LabNames.LymphsAbsolute,
                        LabNames.MCH,
                        LabNames.MCHC,
                        LabNames.MCV,
                        LabNames.Monocytes,
                        LabNames.MonocytesAbsolute,
                        LabNames.Neutrophils,
                        LabNames.NeutrophilsAbsolute,
                        LabNames.Platelets,
                        LabNames.RBC,
                        LabNames.RDW,
                        LabNames.WBC,
                    }
                },
                {
                    LabGroup.Inflammation, new[]
                    {
                        LabNames.Crp,
                        LabNames.AgRatio,
                        LabNames.LpPla2,
                        LabNames.OxLdl
                    }
                },
                {
                    LabGroup.VitaminsAndMicronutrients, new[]
                    {
                        LabNames.Ferritin,
                        LabNames.Omega3,
                        LabNames.VitaminD,
                        LabNames.CoQ10,
                        LabNames.LinoleicAcid,
                        LabNames.VitaminB6,
                        LabNames.ArachidonicAcid,
                        LabNames.ArachidonicAcidEPARatio,
                        LabNames.DHA,
                        LabNames.DPA,
                        LabNames.EPA,
                        LabNames.Omega6Total,
                        LabNames.Omega6Omega3Ratio,
                        LabNames.OmegaCheck
                    }
                },
                {
                    LabGroup.InsulinResistanceAndMetabolism, new[]
                    {
                        LabNames.FastingGlucose,
                        LabNames.FastingInsulin,
                        LabNames.HgbA1C,
                        LabNames.LPIRScore
                    }
                },
                {
                    LabGroup.Methylation, new[]
                    {
                        LabNames.Homocysteine,
                        LabNames.Ast,
                        LabNames.Alt,
                        LabNames.B12,
                        LabNames.Folate,
                        LabNames.FolateRbc,
                        LabNames.Tmao,
                        LabNames.UricAcid
                    }
                },
                {
                    LabGroup.Lipids,
                    new[]
                    {
                        LabNames.TotalCholesterol,
                        LabNames.HdlCholesterol,
                        LabNames.VldlCholesterol,
                        LabNames.LdlCholesterol,
                        LabNames.Triglycerides,
                        LabNames.LdlP,
                        LabNames.SmallLdlP,
                        LabNames.LpA,
                        LabNames.ApolipoproteinB,
                        LabNames.Desmosterol,
                        LabNames.Lathosterol,
                        LabNames.Campsterol,
                        LabNames.Sitosterol,
                        LabNames.Cholestanol,
                        LabNames.ApolipoproteinA1
                    }
                },
                {
                    LabGroup.Hormones,
                    new[]
                    {
                        LabNames.Dhea,
                        LabNames.Igf,
                        LabNames.TestosteroneTotal,
                        LabNames.TestosteroneFree,
                        LabNames.Shbg,
                        LabNames.Estradio,
                        LabNames.Progesterone,
                        LabNames.Lh,
                        LabNames.Fsh,
                        LabNames.Tsh,
                        LabNames.FreeT4,
                        LabNames.FreeT3,
                        LabNames.Cortisol
                    }
                },
                {
                    LabGroup.Metabolic,
                    new[]
                    {
                        LabNames.Albumin,
                        LabNames.AlkalinePhosphatase,
                        LabNames.TotalBilirubin,
                        LabNames.BUN,
                        LabNames.BUNCreatinineRatio,
                        LabNames.Calcium,
                        LabNames.TotalCarbonDioxide,
                        LabNames.Chloride,
                        LabNames.Creatinine,
                        LabNames.eGFRifAfricanAmerican,
                        LabNames.eGFRifNonAfricanAmerican,
                        LabNames.TotalGlobulin,
                        LabNames.Potassium,
                        LabNames.TotalProtein,
                        LabNames.Sodium
                    }
                }
            };

        /// Dictionary of patientIds as keys and array of reportIds in clarity core
        private IDictionary<string, string[]> patientLabReportIds = new Dictionary<string, string[]>() {
            { "58", new string[] {"561"} },
            { "87", new string[] {"1102"} },
            { "2802", new string[] {"12"} },
            { "2809", new string[] {"746", "15", "747"} },
            { "2805", new string[] {"946", "21"} },
        };

    }
}
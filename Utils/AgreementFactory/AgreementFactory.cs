using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Hosting;
using WildHealth.Application.Utils.AgreementsDataProvider;
using WildHealth.Shared.Exceptions;
using WildHealth.Domain.Entities.Agreements;
using WildHealth.Domain.Entities.Patients;
using WildHealth.Domain.Enums.Payments;
using PdfSharpCore.Pdf;
using PdfSharpCore.Pdf.AcroForms;
using PdfSharpCore.Pdf.IO;

namespace WildHealth.Application.Utils.AgreementFactory
{
    /// <summary>
    /// <see cref="IAgreementFactory"/>
    /// </summary>
    public class AgreementFactory : IAgreementFactory
    {
        private static readonly IDictionary<string, IAgreementDataProvider> DataProviders =
            new Dictionary<string, IAgreementDataProvider>
            {
                { "PATIENT_AGREEMENT", new PatientAgreementDataProvider() },
                { "COACHING_AGREEMENT", new CoachingClientAgreementDataProvider() },
                { "PATIENT_FOUNDERS_AGREEMENT", new PatientFoundersAgreementDataProvider() },
                { "COACHING_FOUNDERS_AGREEMENT", new CoachingFoundersClientAgreementDataProvider() },
                { "CROSSFIT_PATIENT_AGREEMENT", new PatientAgreementDataProvider() },
                { "CROSSFIT_COACHING_AGREEMENT", new CoachingClientAgreementDataProvider() },
                { "SINGLE_PLAN_PATIENT_AGREEMENT", new PatientSingleAgreementDataProvider() },
                { "SINGLE_PLAN_COACHING_AGREEMENT", new CoachingClientSingleAgreementDataProvider() }
            };

        private readonly IWebHostEnvironment _webHostEnvironment;

        public AgreementFactory(IWebHostEnvironment webHostEnvironment)
        {
            _webHostEnvironment = webHostEnvironment;
        }

        /// <summary>
        /// Creates and returns filled out and signed agreement documents
        /// </summary>
        /// <param name="agreement"></param>
        /// <param name="patient"></param>
        /// <param name="ipAddress"></param>
        /// <param name="planName"></param>
        /// <param name="periodInMonth"></param>
        /// <param name="paymentStrategy"></param>
        /// <returns></returns>
        public async Task<byte[]> CreateAsync(
            Agreement agreement, 
            Patient patient, 
            string ipAddress,
            string planName,
            int periodInMonth,
            PaymentStrategy paymentStrategy)
        {
            var data = MapAgreementData(
                agreement,
                patient,
                ipAddress,
                planName,
                periodInMonth,
                paymentStrategy);

            var agreementPath = Path.Combine(_webHostEnvironment.WebRootPath, agreement.Path);

            var document = PdfReader.Open(agreementPath);
            var form = document.AcroForm;

            if (form.Elements.ContainsKey("/NeedAppearances"))
            {
                form.Elements["/NeedAppearances"] = new PdfBoolean(true);
            }
            else
            {
                form.Elements.Add("/NeedAppearances", new PdfBoolean(true));
            }

            foreach (var fieldName in form.Fields.Names)
            {
                if (data.TryGetValue(fieldName, out var value))
                {
                    SetField(form, fieldName, value);
                }
            }

            await using var memoryStream = new MemoryStream();

            document.Save(memoryStream);
            document.Close();

            return memoryStream.ToArray();
        }
        
        #region private

        /// <summary>
        /// Maps all data to corresponding inputs on PDF document
        /// </summary>
        /// <param name="agreement"></param>
        /// <param name="patient"></param>
        /// <param name="ipAddress"></param>
        /// <param name="planName"></param>
        /// <param name="periodInMonth"></param>
        /// <param name="paymentStrategy"></param>
        /// <returns></returns>
        /// <exception cref="AppException"></exception>
        private IDictionary<string, string> MapAgreementData(
            Agreement agreement, 
            Patient patient,
            string ipAddress,
            string planName,
            int periodInMonth,
            PaymentStrategy paymentStrategy)
        {
            if (!DataProviders.ContainsKey(agreement.Name))
            {
                var exceptionParam = new AppException.ExceptionParameter(nameof(agreement.Id), agreement.GetId());
                throw new AppException(HttpStatusCode.InternalServerError, "Data provider for agreement does not exist", exceptionParam);
            }

            var dataProvider = DataProviders[agreement.Name];
            var date = DateTime.UtcNow;
            var fullName = patient.User.GetFullname();
            var email = patient.User.Email;
            var billingAddress = patient.User.BillingAddress.ToString();

            return dataProvider.GetData(
                fullName, 
                email,
                billingAddress,
                ipAddress, 
                planName,
                periodInMonth,
                paymentStrategy,
                date);
        }

        /// <summary>
        /// Sets value to form field and makes it readonly
        /// </summary>
        /// <param name="form"></param>
        /// <param name="fieldName"></param>
        /// <param name="value"></param>
        public static void SetField(PdfAcroForm form, string fieldName, string value)
        {
            if (bool.TryParse(value, out var boolValue))
            {
                var checkboxFormField = (PdfCheckBoxField)(form.Fields[fieldName]);
                checkboxFormField.Checked = boolValue;
                checkboxFormField.ReadOnly = true;
                return;
            }

            var formField = form.Fields[fieldName];
            formField.Value = new PdfString(value);
            formField.ReadOnly = true;
        }

        #endregion
    }
}
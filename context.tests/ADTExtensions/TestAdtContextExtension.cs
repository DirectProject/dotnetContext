using System;
using System.IO;
using System.Linq;
using System.Text;
using context.tests.Extensions;
using Health.Direct.Context;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
using MimeKit;
using MimeKit.Utils;
using Xunit;

namespace Health.Direct.Context.Tests.ADTExtensions
{
    public class TestAdtContextExtension
    {
        private const string FormatCodeUrn = "dt-org";
        private const string FormatCodeImplementationGuide = "dsm";
        private const string FormatCodeMessageType = "adt-en";
        private const string FormatCodeVersion = "1.0";
        private const string ContextCode = "79429-7";
        private const string ContextCodeSystem = "2.16.840.1.113883.6.1";
        private const string AdtTypeCodeSystem = "2.16.840.1.113883.6.1";
        private const string AdtTypeCode = "lncct-notif-01";
        private const string CreationTime = "20210416102205.156-4000";
        
        [Fact]
        public void ExampleAdtContextBuildWithObjects()
        {
            //
            // Context 
            //
            var contextBuilder = new ContextBuilder();
            var formatCode = new FormatCode()
            {
                Urn = FormatCodeUrn,
                ImplementationGuide = FormatCodeImplementationGuide,
                MessageType = FormatCodeMessageType,
                Version = FormatCodeVersion
            };

            var contextContentType = new ContextContentType()
            {
                ContentTypeCode = ContextCode,
                ContentTypeSystem = ContextCodeSystem
            };

            var adtTypeCode = new AdtTypeCode()
            {
                ContentTypeSystem = AdtTypeCodeSystem,
                ContentTypeCode = AdtTypeCode
            };

            DateTime currentDateTime = DateTime.UtcNow;

            contextBuilder
                .WithContentId(MimeUtils.GenerateMessageId())
                .WithDisposition("metadata.txt")
                .WithTransferEncoding(ContentEncoding.QuotedPrintable)
                .WithVersion("1.1")
                .WithId(MimeUtils.GenerateMessageId())
                .WithPatientId(
                    new PatientInstance
                    {
                        PidContext = "2.16.840.1.113883.19.999999",
                        LocalPatientId = "123456"
                    }.ToList()
                )
                .WithType(ContextStandard.Type.CategoryGeneral, ContextStandard.Type.ActionNotification)
                .WithPurpose(ContextStandard.Purpose.PurposeResearch)
                .WithPatient(
                    new Patient
                    {
                        GivenName = "John",
                        SurName = "Doe",
                        MiddleName = "Jacob",
                        DateOfBirth = "1961-12-31",
                        Gender = "M",
                        PostalCode = "12345"
                    }
                )
                .WithCreationTime(currentDateTime)
                .WithFormatCode(formatCode)
                .WithContextContentType(contextContentType)
                .WithAdtTypeCode(adtTypeCode);

            var context = contextBuilder.Build();

            //
            // Mime message and simple body
            //
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("HoboJoe", "hobojoe@hsm.DirectInt.lab"));
            message.To.Add(new MailboxAddress("Toby", "toby@redmond.hsgincubator.com"));
            message.Subject = "Sample message with pdf and context attached";
            message.Headers.Add(MailStandard.Headers.DirectContext, context.Headers[ContextStandard.ContentIdHeader]);
            Assert.StartsWith("<", context.Headers[HeaderId.ContentId]);
            Assert.EndsWith(">", context.Headers[HeaderId.ContentId]);

            var body = new TextPart("plain")
            {
                Text = @"Simple Body"
            };

            //
            // Mime message and simple body 
            //
            var pdf = new MimePart("application/pdf")
            {
                ContentDisposition = new ContentDisposition(ContentDisposition.Attachment),
                FileName = "report.pdf",
                ContentTransferEncoding = ContentEncoding.Base64
            };

            var byteArray = Encoding.UTF8.GetBytes("Fake PDF (invalid)");
            var stream = new MemoryStream(byteArray);
            pdf.Content = new MimeContent(stream);

            //
            // Multi part construction
            //
            var multiPart = new Multipart("mixed")
            {
                body,
                contextBuilder.BuildMimePart(),
                pdf
            };

            message.Body = multiPart;

            //
            // Assert context can be serialized and parsed.
            //
            var messageParsed = MimeMessage.Load(message.ToString().ToStream());
            Assert.True(messageParsed.ContainsDirectContext());
            Assert.Equal(context.ContentId, messageParsed.DirectContextId());
            Assert.StartsWith("<", messageParsed.Headers[MailStandard.Headers.DirectContext]);
            Assert.EndsWith(">", messageParsed.Headers[MailStandard.Headers.DirectContext]);

            var contextParsed = message.DirectContext();
            Assert.NotNull(contextParsed);

            //
            // Headers
            //
            Assert.Equal("text", contextParsed.ContentType.MediaType);
            Assert.Equal("plain", contextParsed.ContentType.MediaSubtype);
            Assert.Equal("attachment", contextParsed.ContentDisposition.Disposition);
            Assert.Equal("metadata.txt", contextParsed.ContentDisposition.FileName);
            Assert.Equal(context.ContentId, contextParsed.ContentId);

            //
            // Metadata
            //
            Assert.Equal("1.1", contextParsed.Metadata.Version);
            Assert.Equal(context.Metadata.Id, contextParsed.Metadata.Id);

            //
            // Metatdata PatientId
            //
            Assert.Equal("2.16.840.1.113883.19.999999:123456", contextParsed.Metadata.PatientId);
            Assert.Single(contextParsed.Metadata.PatientIdentifier);
            var patientIdentifiers = Enumerable.ToList(contextParsed.Metadata.PatientIdentifier);
            Assert.Equal("2.16.840.1.113883.19.999999", patientIdentifiers[0].PidContext);
            Assert.Equal("123456", patientIdentifiers[0].LocalPatientId);

            //
            // Metatdata Type
            //
            Assert.Equal("general/notification", contextParsed.Metadata.Type.ToString());
            Assert.Equal("general", contextParsed.Metadata.Type.Category);
            Assert.Equal("notification", contextParsed.Metadata.Type.Action);

            //
            // Metatdata Purpose
            //
            Assert.Equal("research", contextParsed.Metadata.Purpose);

            //
            // Metadata Patient
            //
            Assert.Equal("givenName=John; surname=Doe; middleName=Jacob; dateOfBirth=1961-12-31; gender=M; postalCode=12345",
                contextParsed.Metadata.Patient.ToString());

            Assert.Equal("John", contextParsed.Metadata.Patient.GivenName);
            Assert.Equal("Doe", contextParsed.Metadata.Patient.SurName);
            Assert.Equal("1961-12-31", contextParsed.Metadata.Patient.DateOfBirth);

            ///
            /// ADT Context 1.1 Extensions
            ///
            Assert.Equal(currentDateTime.ToString("yyyyMMddHHmmsszzz"),contextParsed.Metadata.CreationTime);
            Assert.Equal(ContextCodeSystem, contextParsed.Metadata.ContextContentType.ContentTypeSystem);
            Assert.Equal(ContextCode, contextParsed.Metadata.ContextContentType.ContentTypeCode);
            Assert.Equal(FormatCodeUrn, contextParsed.Metadata.FormatCode.Urn);
            Assert.Equal(FormatCodeMessageType, contextParsed.Metadata.FormatCode.MessageType);
            Assert.Equal(FormatCodeImplementationGuide, contextParsed.Metadata.FormatCode.ImplementationGuide);
            Assert.Equal(FormatCodeVersion, contextParsed.Metadata.FormatCode.Version);
            Assert.Equal(AdtTypeCodeSystem, contextParsed.Metadata.AdtTypeCode.ContentTypeSystem);
            Assert.Equal(AdtTypeCode, contextParsed.Metadata.AdtTypeCode.ContentTypeCode);
        }

        [Fact]
        public void ExampleAdtContextBuildWithStrings()
        {
            //
            // Context 
            //
            var contextBuilder = new ContextBuilder();

            contextBuilder
                .WithContentId(MimeUtils.GenerateMessageId())
                .WithDisposition("metadata.txt")
                .WithTransferEncoding(ContentEncoding.Base64)
                .WithVersion("1.1")
                .WithId(MimeUtils.GenerateMessageId())
                .WithPatientId(
                    new PatientInstance
                    {
                        PidContext = "2.16.840.1.113883.19.999999",
                        LocalPatientId = "123456"
                    }.ToList()
                )
                .WithType(ContextStandard.Type.CategoryGeneral, ContextStandard.Type.ActionNotification)
                .WithPurpose(ContextStandard.Purpose.PurposeResearch)
                .WithPatient(
                    new Patient
                    {
                        GivenName = "John",
                        SurName = "Doe",
                        MiddleName = "Jacob",
                        DateOfBirth = "1961-12-31",
                        Gender = "M",
                        PostalCode = "12345"
                    }
                )
                .WithCreationTime(CreationTime)
                .WithFormatCode(FormatCodeUrn, FormatCodeImplementationGuide, FormatCodeMessageType, FormatCodeVersion)
                .WithContextContentType(ContextCodeSystem, ContextCode)
                .WithAdtTypeCode(AdtTypeCodeSystem, AdtTypeCode);

            var context = contextBuilder.Build();

            //
            // Mime message and simple body
            //
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("HoboJoe", "hobojoe@hsm.DirectInt.lab"));
            message.To.Add(new MailboxAddress("Toby", "toby@redmond.hsgincubator.com"));
            message.Subject = "Sample message with pdf and context attached";
            message.Headers.Add(MailStandard.Headers.DirectContext, context.Headers[ContextStandard.ContentIdHeader]);
            Assert.StartsWith("<", context.Headers[HeaderId.ContentId]);
            Assert.EndsWith(">", context.Headers[HeaderId.ContentId]);

            var body = new TextPart("plain")
            {
                Text = @"Simple Body"
            };

            //
            // Mime message and simple body 
            //
            var pdf = new MimePart("application/pdf")
            {
                ContentDisposition = new ContentDisposition(ContentDisposition.Attachment),
                FileName = "report.pdf",
                ContentTransferEncoding = ContentEncoding.Base64
            };

            var byteArray = Encoding.UTF8.GetBytes("Fake PDF (invalid)");
            var stream = new MemoryStream(byteArray);
            pdf.Content = new MimeContent(stream);

            //
            // Multi part construction
            //
            var multiPart = new Multipart("mixed")
            {
                body,
                contextBuilder.BuildMimePart(),
                pdf
            };

            message.Body = multiPart;


            //
            // Assert context can be serialized and parsed.
            //
            var messageParsed = MimeMessage.Load(message.ToString().ToStream());
            Assert.True(messageParsed.ContainsDirectContext());
            Assert.Equal(context.ContentId, messageParsed.DirectContextId());
            Assert.StartsWith("<", messageParsed.Headers[MailStandard.Headers.DirectContext]);
            Assert.EndsWith(">", messageParsed.Headers[MailStandard.Headers.DirectContext]);

            var contextParsed = message.DirectContext();
            Assert.NotNull(contextParsed);

            //
            // Headers
            //
            Assert.Equal("text", contextParsed.ContentType.MediaType);
            Assert.Equal("plain", contextParsed.ContentType.MediaSubtype);
            Assert.Equal("attachment", contextParsed.ContentDisposition.Disposition);
            Assert.Equal("metadata.txt", contextParsed.ContentDisposition.FileName);
            Assert.Equal(context.ContentId, contextParsed.ContentId);

            //
            // Metadata
            //
            Assert.Equal("1.1", contextParsed.Metadata.Version);
            Assert.Equal(context.Metadata.Id, contextParsed.Metadata.Id);

            //
            // Metatdata PatientId
            //
            Assert.Equal("2.16.840.1.113883.19.999999:123456", contextParsed.Metadata.PatientId);
            Assert.Single(contextParsed.Metadata.PatientIdentifier);
            var patientIdentifiers = Enumerable.ToList(contextParsed.Metadata.PatientIdentifier);
            Assert.Equal("2.16.840.1.113883.19.999999", patientIdentifiers[0].PidContext);
            Assert.Equal("123456", patientIdentifiers[0].LocalPatientId);

            //
            // Metatdata Type
            //
            Assert.Equal("general/notification", contextParsed.Metadata.Type.ToString());
            Assert.Equal("general", contextParsed.Metadata.Type.Category);
            Assert.Equal("notification", contextParsed.Metadata.Type.Action);

            //
            // Metatdata Purpose
            //
            Assert.Equal("research", contextParsed.Metadata.Purpose);

            //
            // Metadata Patient
            //
            Assert.Equal("givenName=John; surname=Doe; middleName=Jacob; dateOfBirth=1961-12-31; gender=M; postalCode=12345",
                contextParsed.Metadata.Patient.ToString());

            Assert.Equal("John", contextParsed.Metadata.Patient.GivenName);
            Assert.Equal("Doe", contextParsed.Metadata.Patient.SurName);
            Assert.Equal("1961-12-31", contextParsed.Metadata.Patient.DateOfBirth);

            ///
            /// ADT Context 1.1 Extensions
            ///
            Assert.Equal(CreationTime, contextParsed.Metadata.CreationTime);
            Assert.Equal(ContextCodeSystem, contextParsed.Metadata.ContextContentType.ContentTypeSystem);
            Assert.Equal(ContextCode, contextParsed.Metadata.ContextContentType.ContentTypeCode);
            Assert.Equal(FormatCodeUrn, contextParsed.Metadata.FormatCode.Urn);
            Assert.Equal(FormatCodeMessageType, contextParsed.Metadata.FormatCode.MessageType);
            Assert.Equal(FormatCodeImplementationGuide, contextParsed.Metadata.FormatCode.ImplementationGuide);
            Assert.Equal(FormatCodeVersion, contextParsed.Metadata.FormatCode.Version);
            Assert.Equal(AdtTypeCodeSystem, contextParsed.Metadata.AdtTypeCode.ContentTypeSystem);
            Assert.Equal(AdtTypeCode, contextParsed.Metadata.AdtTypeCode.ContentTypeCode);
        }

        [Theory]
        [InlineData("ContextTestFiles/ContextSimple1.AdtContext")]
        public void TestParseAdtContext(string file)
        {
            var directMessage = MimeMessage.Load(file);
            var context = directMessage.DirectContext();

            //
            // Metadata
            //
            Assert.Equal("1.1", context.Metadata.Version);
            Assert.Equal("<2142848@direct.example.com>", context.Metadata.Id);

            //
            // Metatdata PatientId
            //
            Assert.Equal("2.16.840.1.113883.19.999999:123456", context.Metadata.PatientId);
            Assert.Single(context.Metadata.PatientIdentifier);
            var patientIdentifiers = Enumerable.ToList(context.Metadata.PatientIdentifier);
            Assert.Equal("2.16.840.1.113883.19.999999", patientIdentifiers[0].PidContext);
            Assert.Equal("123456", patientIdentifiers[0].LocalPatientId);

            //
            // Metatdata Type
            //
            Assert.Equal("radiology/report", context.Metadata.Type.ToString());
            Assert.Equal("radiology", context.Metadata.Type.Category);
            Assert.Equal("report", context.Metadata.Type.Action);

            //
            // Metatdata Purpose
            //
            Assert.Equal("research", context.Metadata.Purpose);

            //
            // Metadata Patient
            //
            Assert.Equal("givenName=John; surname=Doe; middleName=Jacob; dateOfBirth=1961-12-31; gender=M; postalCode=12345",
                context.Metadata.Patient.ToString());

            Assert.Equal("John", context.Metadata.Patient.GivenName);
            Assert.Equal("Doe", context.Metadata.Patient.SurName);
            Assert.Equal("1961-12-31", context.Metadata.Patient.DateOfBirth);

            ///
            /// ADT Context 1.1 Extensions
            ///
            Assert.Equal("20210416080510.1245-4000", context.Metadata.CreationTime);
            Assert.Equal(ContextCodeSystem, context.Metadata.ContextContentType.ContentTypeSystem);
            Assert.Equal(ContextCode, context.Metadata.ContextContentType.ContentTypeCode);
            Assert.Equal(FormatCodeUrn, context.Metadata.FormatCode.Urn);
            Assert.Equal(FormatCodeMessageType, context.Metadata.FormatCode.MessageType);
            Assert.Equal(FormatCodeImplementationGuide, context.Metadata.FormatCode.ImplementationGuide);
            Assert.Equal(FormatCodeVersion, context.Metadata.FormatCode.Version);
            Assert.Equal(AdtTypeCodeSystem, context.Metadata.AdtTypeCode.ContentTypeSystem);
            Assert.Equal(AdtTypeCode, context.Metadata.AdtTypeCode.ContentTypeCode);
        }

        [Theory]
        [InlineData("ContextTestFiles/ContextSimple.PatienIdOnly.txtDefault")]
        public void TestParseContextMissingAdtElements(string file)
        {
            var message = MimeMessage.Load(file);
            Assert.Equal("2ff6eaec83894520bbb872e5671ff49e@hobo.lab", message.DirectContextId());
            Assert.True(message.ContainsDirectContext());
            var context = message.DirectContext();
            Assert.NotNull(context);
            Assert.Equal("2.16.840.1.113883.19.999999:123456", context.Metadata.PatientId);
            Assert.Null(context.Metadata.FormatCode);
            Assert.Null(context.Metadata.ContextContentType);
            Assert.Null(context.Metadata.AdtTypeCode);
            Assert.True(string.IsNullOrWhiteSpace(context.Metadata.CreationTime));
        }
    }
}

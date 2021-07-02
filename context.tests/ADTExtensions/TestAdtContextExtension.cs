using System.IO;
using System.Linq;
using System.Text;
using context.tests.Extensions;
using MimeKit;
using MimeKit.Utils;
using Xunit;

namespace Health.Direct.Context.Tests.ADTExtensions
{
    public class TestAdtContextExtension
    {
        private const string ImplementationGuide = "HL7+N0D";
        private const string MessageType = "ADT_A01";
        private const string ContextVersion = "2.5.1";
        private const string Urn = "ihc:pcc";
        private const string CodeSystemName = "LOINC";
        private const string ContextCode = "79429-7";
        private const string CodeSystem = "urn:oid:2.16.840.1.113883.6.1";
        private const string ContextLongName = "Admission notification note";
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
                ImplementationGuide = ImplementationGuide,
                MessageType = MessageType,
                Version = ContextVersion,
                Urn = Urn
            };

            var contextContentType = new ContextContentType()
            {
                CodeSystemName = CodeSystemName,
                Code = ContextCode,
                CodeSystem = CodeSystem,
                Display = ContextLongName
            };

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
                .WithCreationTime(CreationTime)
                .WithFormatCode(formatCode)
                .WithContextContentType(contextContentType);

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
            Assert.Equal(CreationTime,contextParsed.Metadata.CreationTime);
            Assert.Equal(ContextLongName, contextParsed.Metadata.ContextContentType.Display);
            Assert.Equal(ContextCode, contextParsed.Metadata.ContextContentType.Code);
            Assert.Equal(CodeSystemName, contextParsed.Metadata.ContextContentType.CodeSystemName);
            Assert.Equal(CodeSystem, contextParsed.Metadata.ContextContentType.CodeSystem);
            Assert.Equal(ImplementationGuide, contextParsed.Metadata.FormatCode.ImplementationGuide);
            Assert.Equal(MessageType, contextParsed.Metadata.FormatCode.MessageType);
            Assert.Equal(Urn, contextParsed.Metadata.FormatCode.Urn);
            Assert.Equal(ContextVersion, contextParsed.Metadata.FormatCode.Version);
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
                .WithFormatCode(Urn, ImplementationGuide, MessageType, ContextVersion)
                .WithContextContentType(ContextCode, ContextLongName, CodeSystem, CodeSystemName);

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
            Assert.Equal(ContextLongName, contextParsed.Metadata.ContextContentType.Display);
            Assert.Equal(ContextCode, contextParsed.Metadata.ContextContentType.Code);
            Assert.Equal(CodeSystemName, contextParsed.Metadata.ContextContentType.CodeSystemName);
            Assert.Equal(CodeSystem, contextParsed.Metadata.ContextContentType.CodeSystem);
            Assert.Equal(ImplementationGuide, contextParsed.Metadata.FormatCode.ImplementationGuide);
            Assert.Equal(MessageType, contextParsed.Metadata.FormatCode.MessageType);
            Assert.Equal(Urn, contextParsed.Metadata.FormatCode.Urn);
            Assert.Equal(ContextVersion, contextParsed.Metadata.FormatCode.Version);
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
            Assert.Equal(ContextLongName, context.Metadata.ContextContentType.Display);
            Assert.Equal(ContextCode, context.Metadata.ContextContentType.Code);
            Assert.Equal(CodeSystemName, context.Metadata.ContextContentType.CodeSystemName);
            Assert.Equal(CodeSystem, context.Metadata.ContextContentType.CodeSystem);
            Assert.Equal(ImplementationGuide, context.Metadata.FormatCode.ImplementationGuide);
            Assert.Equal(MessageType, context.Metadata.FormatCode.MessageType);
            Assert.Equal(Urn, context.Metadata.FormatCode.Urn);
            Assert.Equal(ContextVersion, context.Metadata.FormatCode.Version);
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
            Assert.True(string.IsNullOrWhiteSpace(context.Metadata.CreationTime));
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using context.tests.Extensions;
using MimeKit;
using MimeKit.Utils;
using Xunit;

namespace Health.Direct.Context.Tests.v1_1
{
    public class TestContext
    {
        [Theory]
        [InlineData("ContextTestFiles/v1.1/ContextSimple1.txtQuotedPrintable")]
        //UUEncode not supported.   
        //[InlineData("ContextTestFiles\\ContextSimple1.txtUUEncode")]
        public void TestParseContext(string file)
        {
            var message = MimeMessage.Load(file);
            Assert.Equal("2ff6eaec83894520bbb872e5671ff49e@hobo.lab", message.DirectContextId());
            Assert.True(message.ContainsDirectContext());
            var context = message.DirectContext();
            Assert.NotNull(context);

            //
            // Headers
            //
            Assert.Equal("text", context.ContentType.MediaType);
            Assert.Equal("plain", context.ContentType.MediaSubtype);
            Assert.Equal("attachment", context.ContentDisposition.Disposition);
            Assert.Equal("metadata.txt", context.ContentDisposition.FileName);
            Assert.Equal("2ff6eaec83894520bbb872e5671ff49e@hobo.lab", context.ContentId);

            //
            // Metadata
            //
            Assert.Equal("1.1", context.Metadata.Version);
            Assert.Equal("<2142848@direct.example.com>", context.Metadata.Id);

            //
            // Metatdata PatientId
            //
            Assert.Equal("2.16.840.1.113883.19.999999:123456;2.16.840.1.113883.19.888888:75774", context.Metadata.PatientId);
            Assert.Equal(2, context.Metadata.PatientIdentifier.Count());
            var patientIdentifiers = Enumerable.ToList(context.Metadata.PatientIdentifier);
            Assert.Equal("2.16.840.1.113883.19.999999", patientIdentifiers[0].PidContext);
            Assert.Equal("123456", patientIdentifiers[0].LocalPatientId);
            Assert.Equal("2.16.840.1.113883.19.888888", patientIdentifiers[1].PidContext);
            Assert.Equal("75774", patientIdentifiers[1].LocalPatientId);

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
            Assert.Equal("givenName=John; surname=Doe; middleName=Jacob; dateOfBirth=1961-12-31; gender=M; localityName=John County; stateOrProvinceName=NY; postalCode=12345; country=US; directAddress=john.doe@direct.john-doe.net", context.Metadata.Patient.ToString());
            Assert.Equal("John", context.Metadata.Patient.GivenName);
            Assert.Equal("Doe", context.Metadata.Patient.SurName);
            Assert.Equal("1961-12-31", context.Metadata.Patient.DateOfBirth);
            Assert.Equal("john.doe@direct.john-doe.net", context.Metadata.Patient.DirectAddress);
            Assert.Equal("John County", context.Metadata.Patient.LocalityName);
            Assert.Equal("US", context.Metadata.Patient.Country);
            Assert.Equal("NY", context.Metadata.Patient.StateOrProvinceName);
        }
        
        [Fact]
        public void ExampleContextBuild()
        {
            //
            // Context 
            //
            var contextBuilder = new ContextBuilder();

            contextBuilder
                .WithContentId(MimeUtils.GenerateMessageId())
                .WithDisposition("metadata.txt")
                .WithTransferEncoding(ContentEncoding.Base64)
                .WithVersion("1.0")
                .WithId(MimeUtils.GenerateMessageId())
                .WithPatientId(
                    new PatientInstance
                    {
                        PidContext = "2.16.840.1.113883.19.999999",
                        LocalPatientId = "123456"
                    }.ToList()
                )
                .WithType(ContextStandard.Type.CategoryRadiology, ContextStandard.Type.ActionReport)
                .WithPurpose(ContextStandard.Purpose.PurposeResearch)
                .WithPatient(
                    new Patient
                    {
                        GivenName = "John",
                        SurName = "Doe",
                        MiddleName = "Jacob",
                        DateOfBirth = "1961-12-31",
                        Gender = "M",
                        PostalCode = "12345",
                        StateOrProvinceName = "New York",
                        LocalityName = "John County",
                        Country = "US",
                        DirectAddress = "john.doe@direct.john-doe.net"
                    }
                );

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
            Assert.Equal("1.0", contextParsed.Metadata.Version);
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
            Assert.Equal("radiology/report", contextParsed.Metadata.Type.ToString());
            Assert.Equal("radiology", contextParsed.Metadata.Type.Category);
            Assert.Equal("report", contextParsed.Metadata.Type.Action);

            //
            // Metatdata Purpose
            //
            Assert.Equal("research", contextParsed.Metadata.Purpose);

            //
            // Metadata Patient
            //
            Assert.Equal("givenName=John; surname=Doe; middleName=Jacob; dateOfBirth=1961-12-31; gender=M; localityName=John County; stateOrProvinceName=New York; postalCode=12345; country=US; directAddress=john.doe@direct.john-doe.net", 
                contextParsed.Metadata.Patient.ToString());

            Assert.Equal("John", contextParsed.Metadata.Patient.GivenName);
            Assert.Equal("Doe", contextParsed.Metadata.Patient.SurName);
            Assert.Equal("1961-12-31", contextParsed.Metadata.Patient.DateOfBirth);
            Assert.Equal("john.doe@direct.john-doe.net", context.Metadata.Patient.DirectAddress);
            Assert.Equal("John County", context.Metadata.Patient.LocalityName);
            Assert.Equal("US", context.Metadata.Patient.Country);
            Assert.Equal("New York", context.Metadata.Patient.StateOrProvinceName);
        }
    }
}

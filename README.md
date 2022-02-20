# Health.Direct.Context

[![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](https://opensource.org/licenses/MIT)
[![Twitter](https://img.shields.io/twitter/url/http/shields.io.svg?style=flat&logo=twitter)](https://twitter.com/intent/tweet?hashtags=DirectSecureMessaging,ADTnotifications,dotnet,oss,csharp&text=🚀+FusionCache.Metrics:+FusionCche+metric+plugins&url=https%3A%2F%2Fgithub.com%2Fjoeshook%2FZiggyCreatures.FusionCache.Metrics&via=josephshook)

![badge](https://img.shields.io/endpoint?url=https://gist.githubusercontent.com/JoeShook/b49a64c41decace4c01fc573ae307907/raw/direct-context-code-coverage.json)

## What is Direct.Health.Context

This is a Microsoft .NET Core reference implementation library for Expressing Context in Direct Messaging.  The implementation guide is maintained at [www.directproject.org](https://wiki.directproject.org/File/view/Implementation%2BGuide%2Bfor%2BExpressing%2BContext%2Bin%2BDirect%2BMessaging%2Bv1.1.pdf).

### Design of Context

The Context library uses the excellent [MimeKit](https://github.com/jstedfast/MimeKit) library as the base for creating and parsing mime messages.  The Direct Project libraries are primarily parsers and not builders. The current implementation is targeted for [Direct Context]((https://wiki.directproject.org/Implementation_Guide_for_Expressing_Context_in_Direct_Messaging)) version 1.1 compliance and recent ADT Notification requirements.  If you find any issues, please create and issue and/or email me directly.  I created this project in 2017 when I was very intimate with the details.  Recently I revisited this project to bring it from 1.0 to 1.1 with respect to the [Direct Context Guide]((https://wiki.directproject.org/Implementation_Guide_for_Expressing_Context_in_Direct_Messaging)), in addition making it compatible with [ADT Notifications](https://directtrust.org/standards/event-notifications-via-direct).  

Originally [Direct Context](https://wiki.directproject.org/Implementation_Guide_for_Expressing_Context_in_Direct_Messaging) was intended to evolve as various versions bound to a specification version.  For example, an application hosting multiple versions of the parser would decide which parser to load based on the version header value in the Context part.  To this point I don't believe any context other than 1.1 has ever been put into production.  ADT Notifications violate the code version binding to a Direct Context specification.  I imagine the spec will catch up at some point.  My hope is the concept of DirectContext could make way for completely different contexts, that allow for other interesting workflows.  Senders and receivers would need to understand the two contexts, but they could be anything such as carrying patient consent requests and answers.  In such a workflow we would just create a new DirectContext but the version would be something completely different such as pc-1.0 which would load the appropriate context object and leading to specific workflow actions.  

[Direct Context](https://wiki.directproject.org/Implementation_Guide_for_Expressing_Context_in_Direct_Messaging) is an excellent answer to the alternative solutions of using ad-hoc mime headers to transport context.  Just say no to ad-hoc headers.

## License Information

Copyright (c) 2010-2017, Direct Project
 All rights reserved.

 Authors:
    Joseph Shook    Joseph.Shook@Surescripts.com
  
Redistribution and use in source and binary forms, with or without modification, are permitted provided that the following conditions are met:

Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.
Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution.
Neither the name of The Direct Project (directproject.org) nor the names of its contributors may be used to endorse or promote products derived from this software without specific prior written permission.
THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

## Installing via NuGet

The easiest way to install DirectProject.DotNet.Context is via [NuGet](https://www.nuget.org/packages/DirectProject.DotNet.Context/).

In Visual Studio's [Package Manager Console](http://docs.nuget.org/docs/start-here/using-the-package-manager-console),
simply enter the following command:

    Install-Package DirectProject.DotNet.Context

### Building Context

Creating a MimeMessage with Context

    ```csharp
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
            PostalCode = "12345"
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
message.Headers.Add(MailStandard.Headers.DirectContext, context.ContentId);

var body = new TextPart("plain")
{
    Text = @"Simple Body"
};

//
// Represent PDF attachment
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
    ```

### Parsing Context

Parsing context is very simple from a file or from a stream.  Using the message from the previous example the following code shows most examples of accessing context and the metadata.

    ```csharp
//
// Assert context can be serialized and parsed.
//
var messageParsed = MimeMessage.Load(message.ToString().ToStream());
Assert.True(messageParsed.ContainsDirectContext());
Assert.Equal(context.ContentId, messageParsed.DirectContextId());
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
Assert.Equal(1, contextParsed.Metadata.PatientIdentifier.Count());
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
Assert.Equal("givenName=John; surname=Doe; middleName=Jacob; dateOfBirth=1961-12-31; gender=M; postalCode=12345", 
    contextParsed.Metadata.Patient.ToString());

Assert.Equal("John", contextParsed.Metadata.Patient.GivenName);
Assert.Equal("Doe", contextParsed.Metadata.Patient.SurName);
Assert.Equal("1961-12-31", contextParsed.Metadata.Patient.DateOfBirth);
    ```

The above code can also be ran from the context.tests project.

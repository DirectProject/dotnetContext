/* 
 Copyright (c) 2010-2021, Direct Project
 All rights reserved.

 Authors:
    Joseph Shook    Joseph.Shook@Surescripts.com
  
Redistribution and use in source and binary forms, with or without modification, are permitted provided that the following conditions are met:

Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.
Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution.
Neither the name of The Direct Project (directproject.org) nor the names of its contributors may be used to endorse or promote products derived from this software without specific prior written permission.
THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 
*/

using System;
using System.Collections.Generic;
using MimeKit;
using System.IO;
using System.Text;
using ContentDisposition = MimeKit.ContentDisposition;
using MimePart = MimeKit.MimePart;

namespace Health.Direct.Context
{
    /// <summary>
    /// 
    /// </summary>
    public class ContextBuilder
    {
        private const string Disposition = "attachment";
        private readonly  Context _directContext;

        /// <summary>
        /// Construct Context.
        /// <remarks>
        /// Defaults:
        ///   ContentType = text/plain
        ///   ContentId = new guid.
        ///   Content-Dispostion = attachment; filename=metadata.txt
        /// </remarks>
        /// </summary>
        public ContextBuilder()
        {
            _directContext = new Context();
        }

        /// <summary>
        /// Overide contentId
        /// </summary>
        /// <param name="contentId"></param>
        /// <returns>ContextBuilder</returns>
        public ContextBuilder WithContentId(string contentId)
        {
            _directContext.ContentId = contentId;

            return this;
        }

        /// <summary>
        /// Overide Content-Disposition filename
        /// </summary>
        /// <param name="fileName">attachment filename</param>
        /// <returns>ContextBuilder</returns>
        public ContextBuilder WithDisposition(string fileName)
        {
            _directContext.ContentDisposition.Disposition = Disposition;
            _directContext.ContentDisposition.FileName = fileName;
            return this;
        }

        /// <summary>
        /// Set transfer encoding
        /// </summary>
        /// <param name="encoding"></param>
        /// <returns>ContextBuilder</returns>
        public ContextBuilder WithTransferEncoding(ContentEncoding encoding)
        {
            _directContext.ContentTransferEncoding = encoding;

            return this;
        }

        /// <summary>
        /// Set metadata version
        /// </summary>
        /// <param name="version"></param>
        /// <returns>ContextBuilder</returns>
        public ContextBuilder WithVersion(string version)
        {
            _directContext.Metadata.Version = version;

            return this;
        }

        /// <summary>
        /// Set metadata id
        /// </summary>
        /// <remarks>
        /// See 3.2 Transaction ID
        /// </remarks>
        /// <param name="id"></param>
        /// <returns>ContextBuilder</returns>
        public ContextBuilder WithId(string id)
        {
            _directContext.Metadata.Id = id;

            return this;
        }

        /// <summary>
        /// Set metadata patientId/s.
        /// Call multiple times if adding more than one Id.
        /// </summary>
        /// <param name="patientId"></param>
        /// <returns>ContextBuilder</returns>
        public ContextBuilder WithPatientId(string patientId)
        {
            if (string.IsNullOrEmpty(patientId))
            {
                return this;
            }

            if (_directContext.Metadata.PatientId.IsNullOrWhiteSpace())
            {
                _directContext.Metadata.PatientId = patientId;
            }
            else
            {
                _directContext.Metadata.PatientId += $"; {patientId}";
            }

            return this;
        }

        /// <summary>
        /// Set metadata patientId/s.
        /// </summary>
        /// <param name="patients"></param>
        /// <returns>ContextBuilder</returns>
        public ContextBuilder WithPatientId(IEnumerable<PatientInstance> patients)
        {
            foreach (var patientInstance in patients)
            {
                var patientId = $"{patientInstance.PidContext}:{patientInstance.LocalPatientId}";

                if (_directContext.Metadata.PatientId.IsNullOrWhiteSpace())
                {
                    _directContext.Metadata.PatientId = patientId;
                }
                else
                {
                    _directContext.Metadata.PatientId += $"; {patientId}";
                }
            }
            
            return this;
        }


        /// <summary>
        /// Set metadata type
        /// </summary>
        /// <param name="category">Valid <see cref="ContextStandard.Type.Category"/></param>
        /// <param name="action">Valid <see cref="ContextStandard.Type.Action"/></param>
        /// <returns>ContextBuilder</returns>
        public ContextBuilder WithType(string category, string action)
        {
            _directContext.Metadata.Type = new Type
            {
                Category = category,
                Action = action
            };

            return this;
        }

        /// <summary>
        /// Set metadata type
        /// </summary>
        /// <remarks>
        /// See 3.5 Purpose of Use
        /// </remarks>
        /// <param name="purpose">Valid <see cref="ContextStandard.Purpose"/></param>
        /// <returns>ContextBuilder</returns>
        public ContextBuilder WithPurpose(string purpose)
        {
            if (string.IsNullOrEmpty(purpose))
            {
                return this;
            }

            _directContext.Metadata.Purpose = purpose;

            return this;
        }

        /// <summary>
        /// Patient matching parameters.
        /// </summary>
        /// <remarks>
        /// If a patient-id-element is included for the recipient’s domain, the recipient SHOULD disregard the patient-data-element. 
        /// </remarks>
        /// <param name="patient"></param>
        /// <returns></returns>
        public ContextBuilder WithPatient(Patient patient)
        {
            if (patient == null)
            {
                return this;
            }

            _directContext.Metadata.Patient = patient;

            return this;
        }

        /// <summary>
        /// Set metadata Encapsulation attribute.
        /// </summary>
        /// <param name="encapsulation">“http” / “hl7v2”</param>
        /// <returns></returns>
        public ContextBuilder WithEncapsulation(string encapsulation)
        {
            if (encapsulation.IsNullOrWhiteSpace())
            {
                return this;
            }

            _directContext.Metadata.Encapsulation = new Encapsulation
            {
                Type = encapsulation
            };

            return this;
        }

        #region ADT Context 1.1 extensions

        /// <summary>
        /// Set metadata creation-time attribute.
        /// </summary>
        /// <param name="creationTime">DateTime creation time</param>
        /// <returns></returns>
        public ContextBuilder WithCreationTime(DateTime creationTime)
        {
            _directContext.Metadata.CreationTime = creationTime.ToString("yyyyMMddHHmmsszzz");
            return this;
        }

        /// <summary>
        /// Set metadata creation-time attribute.
        /// </summary>
        /// <param name="creationTime">creation time as string.  Expected to be in YYYY[MM[DD[HH[MM[SS[.S[S[S[S]]]]]]]]][+/-ZZZZ] format</param>
        /// <returns></returns>
        public ContextBuilder WithCreationTime(string creationTime)
        {
            if (creationTime.IsNullOrWhiteSpace())
            {
                return this;
            }

            _directContext.Metadata.CreationTime = creationTime;
            return this;
        }

        /// <summary>
        /// FormatCode parameter.
        /// </summary>
        /// <param name="formatCode"></param>
        /// <returns></returns>
        public ContextBuilder WithFormatCode(FormatCode formatCode)
        {
            if (formatCode == null)
            {
                return this;
            }
            
            _directContext.Metadata.FormatCode = formatCode;
            return this;
        }

        /// <summary>
        /// FormatCode builder from individual parameters.
        /// </summary>
        /// <remarks>All parameters must have values to build FormatCode header</remarks>
        /// <param name="urn"></param>
        /// <param name="implementationGuide"></param>
        /// <param name="messageType"></param>
        /// <param name="version"></param>
        /// <returns></returns>
        public ContextBuilder WithFormatCode(string urn, string implementationGuide, string messageType, string version)
        {
            if (urn.IsNullOrWhiteSpace() || 
                implementationGuide.IsNullOrWhiteSpace() ||
                messageType.IsNullOrWhiteSpace() || 
                version.IsNullOrWhiteSpace())
            {
                return this;
            }
            
            _directContext.Metadata.FormatCode = new FormatCode()
            {
                Urn = urn,
                ImplementationGuide = implementationGuide,
                MessageType = messageType,
                Version = version
            };

            return this;
        }

        /// <summary>
        /// ContextContentType parameter.
        /// </summary>
        /// <param name="contextContentType"></param>
        /// <returns></returns>
        public ContextBuilder WithContextContentType(ContextContentType contextContentType)
        {
            if (contextContentType == null)
            {
                return this;
            }

            _directContext.Metadata.ContextContentType = contextContentType;
            return this;
        }

        /// <summary>
        /// ContextContentType builder from individual parameters.
        /// </summary>
        /// <remarks>All parameters must have values to build ContextContentType header</remarks>
        /// <param name="code"></param>
        /// <param name="display"></param>
        /// <param name="codeSystem"></param>
        /// <param name="codeSystemName"></param>
        /// <returns></returns>
        public ContextBuilder WithContextContentType(string code, string display, string codeSystem, string codeSystemName)
        {
            if (code.IsNullOrWhiteSpace() ||
                display.IsNullOrWhiteSpace() ||
                codeSystem.IsNullOrWhiteSpace() ||
                codeSystemName.IsNullOrWhiteSpace())
            {
                return this;
            }

            _directContext.Metadata.ContextContentType = new ContextContentType()
            {
                CodeSystem = codeSystem,
                Code = code,
                Display = display,
                CodeSystemName = codeSystemName
            };

            return this;
        }

        #endregion

        /// <summary>
        /// Return a Context object
        /// </summary>
        /// <returns></returns>
        public Context Build()
        {
            return _directContext;
        }

        /// <summary>
        /// Prepare a RFC 5322 message searialized from a Contxt object 
        /// </summary>
        /// <returns></returns>
        public MimePart BuildMimePart()
        {
            var mimePart = new MimePart(_directContext.ContentType)
            {
                ContentDisposition = new ContentDisposition(ContentDisposition.Attachment),
                ContentTransferEncoding = _directContext.ContentTransferEncoding
            };

            var contentDist = _directContext.ContentDisposition;
            mimePart.ContentDisposition.FileName = contentDist.FileName;
            mimePart.ContentId = _directContext.ContentId;

            var contextBodyStream = new MemoryStream(Encoding.UTF8.GetBytes(_directContext.Metadata.Deserialize()));
            mimePart.Content = new MimeContent(contextBodyStream);
            
            return mimePart;
        }
    }
}

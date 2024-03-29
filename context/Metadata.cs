﻿/* 
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
using System.IO;
using System.Linq;
using System.Text;

namespace Health.Direct.Context
{
    /// <summary>
    /// Direct<see cref="Context"/> metadata container.
    /// </summary>
    public class Metadata 
    {
        private string patient;

        /// <summary>
        /// Construct empty Metadata
        /// </summary>
        public Metadata() 
        {
            MetadataElements = new List<MetadataElement>();
        }

        
        public static Metadata Load(Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            var metadataParser = new MetadataParser(stream);
            var metadata = new Metadata();
            metadata.MetadataElements =  metadataParser.ParseMessage();
            
            return metadata;
        }

        /// <summary>
        /// Gets the list of headers.
        /// </summary>
        /// <value>The list of headers.</value>
        public List<MetadataElement> MetadataElements
        {
            get; private set;
        }

        /// <summary>
        /// Gets and sets the value of <c>version</c> metadata.  Value is specifically called the <c>version-identifier</c> 
        /// </summary>
        public string Version
        {
            get
            {
                return GetValue(ContextStandard.Version);
            }
            set
            {
                SetValue(ContextStandard.Version, value);
            }
        }

        /// <summary>
        /// Gets and sets the value of <c>id</c> metadata.  Value is specifically called the <c>unique-identifier</c>  
        /// </summary>
        /// <remarks>
        /// See 3.2 Transaction ID
        /// </remarks>
        public string Id
        {
            get
            {
                return GetValue(ContextStandard.Id);
            }
            set
            {
                SetValue(ContextStandard.Id, value);
            }
        }

        /// <summary>
        /// Gets and sets the value of <c>patient-id</c> metadata. 
        /// </summary>
        /// <remarks>
        /// See 3.3 Patient ID
        /// </remarks>
        public string PatientId
        {
            get
            {
                return GetValue(ContextStandard.PatientId);
            }
            set
            {
                SetValue(ContextStandard.PatientId, value.ToString());
            }
        }

        /// <summary>
        /// Return IEnumerable list <see cref="PatientIdentifier"/>
        /// </summary>
        /// <remarks>
        /// See 3.4 Transaction Type
        /// </remarks>
        public IEnumerable<PatientInstance> PatientIdentifier
        {
            get
            {
                var patientIdentifier = ContextParser.ParsePatientIdentifier(PatientId);

                return patientIdentifier;
            }
        }

        /// <summary>
        /// Gets and sets the value of <c>type</c> metadata. 
        /// </summary>
        public Type Type
        {
            get
            {
                var type = GetValue(ContextStandard.Type.Label);

                if (type == null || type == "/")
                {
                    return null;
                }

                return ContextParser.ParseType(type);
            }
            set
            {
                SetValue(ContextStandard.Type.Label, value.ToString());
            }
        }


        /// <summary>
        /// Gets and sets the value of <c>purpose</c> metadata. 
        /// </summary>
        public string Purpose
        {
            get
            {
                return GetValue(ContextStandard.Purpose.Label);
            }
            set
            {
                SetValue(ContextStandard.Purpose.Label, value);
            }
        }

        /// <summary>
        /// Gets and sets the value of <c>patient</c> metadata. 
        /// </summary>
        public Patient Patient
        {
            get
            {
                patient = GetValue(ContextStandard.Patient.Label);

                if (patient == null)
                {
                    return null;
                }

                return new Patient(patient);
            }
            set
            {
                SetValue(ContextStandard.Patient.Label, value.ToString());
            }
        }

        /// <summary>
        /// Gets and sets the value of <c>encapsulation</c> metadata. 
        /// </summary>
        public Encapsulation Encapsulation
        {
            get
            {
                var headerValue = GetValue(ContextStandard.Encapsulation.Label);

                if (headerValue == null)
                {
                    return null;
                }

                return ContextParser.ParseEncapsulation(headerValue); 
            }
            set
            {
                SetValue(ContextStandard.Encapsulation.Label, value.Type);
            }
        }

        // Admission, discharge, transfer extension properties

        /// <summary>
        /// Gets and sets the value of <c>creation-time</c> metadata. 
        /// </summary>
        public string CreationTime
        {
            get
            {
                return GetValue(ContextStandard.CreationTime);
            }
            set
            {
                SetValue(ContextStandard.CreationTime, value);
            }
        }

        public FormatCode FormatCode
        {
            get
            {
                var formatCodeValue = GetValue(ContextStandard.FormatCode.Label);

                if (string.IsNullOrWhiteSpace(formatCodeValue))
                {
                    return null;
                }

                return ContextParser.ParseFormatCode(formatCodeValue);
            }
            set
            {
                SetValue(ContextStandard.FormatCode.Label, value.ToString());
            }
        }

        public AdtTypeCode AdtTypeCode
        {
            get
            {
                var adtTypeCodeValue = GetValue(ContextStandard.AdtTypeCode);

                if (string.IsNullOrEmpty(adtTypeCodeValue))
                {
                    return null;
                }

                return ContextParser.ParseAdtTypeCode(adtTypeCodeValue);
            }
            set
            {
                SetValue(ContextStandard.AdtTypeCode, value.ToString());
            }
        }
        public ContextContentType ContextContentType
        {
            get
            {
                var contentTypeCode = GetValue(ContextStandard.ContextContentType);

                if (string.IsNullOrWhiteSpace(contentTypeCode))
                {
                    return null;
                }

                return ContextParser.ParseContextContentType(contentTypeCode);
            }
            set
            {
                SetValue(ContextStandard.ContextContentType, value.ToString());
            }
        }

        private string GetValue(string parameter)
        {
            //return MetadataElements[headerName]; //Maybe create a MetadataElementList to optimize in future?
            return MetadataElements
                .FirstOrDefault(e => e.Field.Equals(parameter, StringComparison.InvariantCultureIgnoreCase))
                ?.Value;
        }

        private void SetValue(string parameter, string value)
        {
            var element = MetadataElements
                .FirstOrDefault(e => e.Field.Equals(parameter, StringComparison.InvariantCultureIgnoreCase));

            if (element != null)
            {
                // Headers[headerName] = headerValue;
                element.Value = value;
            }
            else
            {
                MetadataElements.Add(new MetadataElement(parameter, value));
            }
        }

        internal string Deserialize()
        {
            var sb = new StringBuilder();
            sb.AppendHeader(ContextStandard.Version, Version);
            sb.AppendHeader(ContextStandard.Id, Id);
            sb.AppendHeader(ContextStandard.Encapsulation.Label, Encapsulation?.Type);
            sb.AppendHeader(ContextStandard.PatientId, PatientId);
            sb.AppendHeader(ContextStandard.Type.Label, Type?.ToString());
            sb.AppendHeader(ContextStandard.Purpose.Label, Purpose);
            sb.AppendHeader(ContextStandard.Patient.Label, Patient?.ToString());

            // ADT context 1.1 extensions
            sb.AppendHeader(ContextStandard.CreationTime, CreationTime);
            sb.AppendHeader(ContextStandard.FormatCode.Label, FormatCode?.ToString());
            sb.AppendHeader(ContextStandard.ContextContentType, ContextContentType?.ToString());
            sb.AppendHeader(ContextStandard.AdtTypeCode, AdtTypeCode?.ToString());

            return sb.ToString();
        }
    }
}
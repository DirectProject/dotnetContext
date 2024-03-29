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

namespace Health.Direct.Context
{
    /// <summary>
    /// Categorize context errors
    /// </summary>
    public enum ContextError
    {
        /// <summary>
        /// Unexpected error
        /// </summary>
        Unexpected = 0,
        /// <summary>
        /// <c>version-identifier</c> is missing
        /// </summary>
        MissingVersionIdentifier,
        /// <summary>
        /// Unsupported <c>version-identifier</c>
        /// </summary>
        UnsupportedVersionIdentifier,
        /// <summary>
        /// Invalid fields in the body of the Direct<see cref="Context"/> message. 
        /// </summary>
        InvalidContextMetadataFields,
        /// <summary>
        /// Invalid <c>patient-id</c> metadata.
        /// </summary>
        InvalidPatientId,
        /// <summary>
        /// Invalid <c>type</c> metadata.
        /// </summary>
        InvalidType,
        /// <summary>
        /// Invalid <c>patient</c> metadata.
        /// </summary>
        InvalidPatient,
        /// <summary>
        /// Invalid <c>FormatCode</c> metadata.
        /// </summary>
        InvalidFormatCode,
        /// <summary>
        /// Invalid <c>TypeCode</c> metadata.
        /// </summary>
        InvalidTypeCode,
        /// <summary>
        /// Invalid <c>ContextContentType</c> metadata.
        /// </summary>
        InvalidContextContentType,
    }
}

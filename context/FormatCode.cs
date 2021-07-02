namespace Health.Direct.Context
{
    /// <summary>
    /// Represents a <c>format-code</c>. 
    /// </summary>
    /// <remarks>
    /// This is an extension property to the direct context 1.1 IG as described in the Event Notifications via hte Direct Standard IG
    /// See <a href="https://directtrust.app.box.com/s/g7dmzskfmuczle0gzn9kk9gp0cfhjwhb">Event Notifitions via the Direct Standard</a>.
    ///
    /// ihe-format-code-element = "format-code:" urn ":" implementationguide ":" messagetype ":" version
    ///
    /// urn = ihc:pcc
    /// implementationguide = HL7+NOD
    /// messagetype = codes from the valueset of HL7 Types/Triggers (MSH-9.1 and .2)
    /// version = 2.5.1
    public class FormatCode
    {
        public string Urn { get; set; }

        public string ImplementationGuide { get; set; }

        public string MessageType { get; set; }

        public string Version { get; set; }

        /// <summary>
        /// Format <c>ihe-format-code value as urn:implementationguide:messagetype:version</c>.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"{Urn}:{ImplementationGuide}:{MessageType}:{Version}";
        }
    }
}

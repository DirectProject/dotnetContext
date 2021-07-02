namespace Health.Direct.Context
{
    /// <summary>
    /// Represents a <c>format-code</c>. 
    /// </summary>
    /// <remarks>
    /// This is an extension property to the direct context 1.1 IG as described in the Event Notifications via hte Direct Standard IG version M
    /// See <a href="https://app.box.com/s/g1a4adts5wxhq2fthmfq1bbb58jt7r2s">Event Notifitions via the Direct Standard</a>.
    ///
    /// type-code-element = "type-code:" content-type-system ": "content-type-code"
    ///
    /// content-type-system = 2.16.840.1.113883.6.1
    /// content-type-code = 86530-3
    public class AdtTypeCode
    {
        public string ContentTypeSystem { get; set; }

        public string ContentTypeCode { get; set; }

        /// <summary>
        /// Format <c>type-code value as ContentTypeSystem: ContentTypeCode</c>.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"{ContentTypeSystem}:{ContentTypeCode}";
        }
    }
}

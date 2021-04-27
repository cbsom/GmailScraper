using System;

namespace GmailScraper
{
    [Serializable]
    internal class FieldToFind
    {
        public bool Required { get; set; }
        public string Name { get; set; }
        public string Regex { get; set; }
        public int GroupNumber { get; set; }
    }
}
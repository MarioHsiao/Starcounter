
using System;
using System.Collections.Generic;

namespace Starcounter.Errors
{
    public sealed class ErrorCode
    {
        public Facility Facility
        {
            get;
            set;
        }
        
        public string Name
        {
            get;
            set;
        }
        
        public ushort Code
        {
            get;
            set;
        }
        
        public Severity Severity
        {
            get;
            set;
        }
        
        public string Description
        {
            get;
            set;
        }
        
        public IList<string> RemarkParagraphs
        {
            get;
            private set;
        }

        public string ConstantName
        {
            get { return Name.ToUpper(); }
        }
        
        public uint CodeWithFacility
        {
            get
            {
                return (Facility.Code * 1000) + Code;
            }
        }

        internal ErrorCode(
            Facility facility,
            string name,
            ushort code,
            Severity severity,
            string description,
            IEnumerable<string> remarkParagraphs)
        {
            if (code > 999)
                throw new ArgumentOutOfRangeException("code", code, "Not a valid value (allowed range is 0-999): 0x" + code.ToString("X"));

            this.Facility = facility;
            this.Name = name;
            this.Code = code;
            this.Severity = severity;
            this.Description = description;
            this.RemarkParagraphs = new List<string>(remarkParagraphs).AsReadOnly();
        }
    }
}
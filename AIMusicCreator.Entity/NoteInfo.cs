using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIMusicCreator.Entity
{
    public class NoteInfo
    {
        public double Frequency { get; set; }
        public double Amplitude { get; set; }
        public int StartSample { get; set; }
        public int StopSample { get; set; }
        public bool IsActive { get; set; }
        public int Program { get; set; }
    }
}

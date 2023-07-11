using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NoiseBarriers.Models
{
    public class BeamSetup
    {
        public double OffsetX { get; set; }

        public double OffsetZ { get; set; }

        public FamilySymbolSelector FamilyAndSymbolName { get; set; }
    }
}

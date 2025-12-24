using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIMusicCreator.Entity
{
    // 辅助模型类
    public class MixTrackRequest
    {
        public string AudioData { get; set; } = string.Empty;
        public double Volume { get; set; }
        public double DelaySeconds { get; set; }
    }
    // 辅助模型类
    public class MixTracksRequest
    {
        public List<MixTrackRequest> Tracks { get; set; } = new();
    }

    public class ConvertFormatRequest
    {
        public string AudioData { get; set; } = "";
        public string SourceFormat { get; set; } = "";
        public string TargetFormat { get; set; } = "";
        public int Mp3Quality { get; set; } = 192;
    }

}

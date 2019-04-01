namespace Lastfm.Models.Requests
{
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    [DataContract]
    public class NowPlayingRequest : BaseAuthedRequest
    {
        public string Track    { get; set; }
        public string Album    { get; set; }
        public string Artist   { get; set; }
        public int    Duration { get; set; }
        public string MbId     { get; set; }

        public override Dictionary<string, string> ToDictionary()
        {
            var dict = new Dictionary<string, string>(base.ToDictionary()) 
            {   
                { "track",    Track  },
                { "artist",   Artist },
                { "duration", Duration.ToString() }
            };
            if (!string.IsNullOrWhiteSpace(Album))
                dict.Add("album", Album);
            if (!string.IsNullOrWhiteSpace(MbId))
                dict.Add("mbid", MbId);
            return dict;
        }
    }
}
